<template>
  <div class="terminal-container">
    <div class="terminal-body" ref="terminalBody">
      <div v-for="(line, idx) in history" :key="idx" class="terminal-line" :class="line.type">
        <span v-if="line.type === 'cmd'" class="prompt">$</span>
        <span class="content">{{ line.text }}</span>
      </div>
      <div v-if="loading" class="terminal-line system">
        <span class="loader-inline"></span> Executing...
      </div>
    </div>
    <div class="terminal-input-row">
      <span class="prompt">$</span>
      <input 
        v-model="cmd" 
        @keydown.enter="execute" 
        ref="cmdInput"
        type="text" 
        placeholder="Command..."
        :disabled="loading"
      >
      <button v-if="loading" @click="stop" class="stop-btn">Stop</button>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  server: { type: Object, required: true }
})

const config = useRuntimeConfig()
const auth = useAuth()
const nuxtApp = useNuxtApp()

const cmd = ref('')
const loading = ref(false)
const history = ref([
  { type: 'system', text: `Console: ${props.server.name}` }
])
const terminalBody = ref(null)
const cmdInput = ref(null)

const abortController = ref(null)

const execute = async () => {
  if (!cmd.value.trim() || loading.value) return
  
  const currentCmd = cmd.value
  history.value.push({ type: 'cmd', text: currentCmd })
  cmd.value = ''
  loading.value = true
  
  scrollToBottom()
  
  abortController.value = new AbortController()

  try {
    const response = await fetch(`${config.public.apiBase}/api/servers/${props.server.id}/execute-stream`, {
      method: 'POST',
      headers: {
        ...auth.authHeaders(),
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ command: currentCmd }),
      signal: abortController.value.signal
    })

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}))
      throw new Error(errorData.detail || 'Failed to execute command')
    }

    const reader = response.body.getReader()
    const decoder = new TextDecoder()
    let buffer = ''
    
    while (true) {
      const { done, value } = await reader.read()
      if (done) break
      
      buffer += decoder.decode(value, { stream: true })
      const lines = buffer.split('\n')
      
      buffer = lines.pop() || ''
      
      for (const line of lines) {
        if (line.startsWith('ERROR: ')) {
          history.value.push({ type: 'error', text: line.substring(7) })
        } else if (line.trim() !== '' || lines.length > 1) {
          history.value.push({ type: 'output', text: line })
        }
        scrollToBottom()
      }
    }
    
    if (buffer) {
      history.value.push({ type: 'output', text: buffer })
    }
  } catch (e) {
    if (e.name === 'AbortError') {
      history.value.push({ type: 'system', text: 'Command stopped by user.' })
    } else {
      history.value.push({ type: 'error', text: 'Error: ' + e.message })
    }
  } finally {
    loading.value = false
    abortController.value = null
    scrollToBottom()
    nextTick(() => cmdInput.value?.focus())
  }
}

const stop = () => {
  if (abortController.value) {
    abortController.value.abort()
  }
}

const scrollToBottom = () => {
  nextTick(() => {
    if (terminalBody.value) {
      terminalBody.value.scrollTop = terminalBody.value.scrollHeight
    }
  })
}

onMounted(() => {
  cmdInput.value?.focus()
})
</script>

<style scoped>
.terminal-container {
  display: flex; flex-direction: column; height: 100%;
  background: #000; border-radius: var(--radius); overflow: hidden;
  border: 1px solid var(--border);
}

.terminal-body {
  flex: 1; overflow-y: auto; padding: 10px; font-family: var(--mono); font-size: 11px; line-height: 1.5;
  background: #000; color: #fff;
}
.terminal-line { margin-bottom: 2px; white-space: pre-wrap; word-break: break-all; }
.terminal-line.cmd { color: var(--accent); font-weight: 500; }
.terminal-line.output { color: #ccc; }
.terminal-line.system { color: #555; font-style: italic; }
.terminal-line.error { color: var(--danger); background: rgba(240,80,96,0.05); padding: 2px 6px; border-radius: 4px; }

.prompt { color: var(--warn); margin-right: 6px; user-select: none; }

.terminal-input-row {
  display: flex; align-items: center; padding: 6px 10px; background: #080808; border-top: 1px solid #111;
}
.terminal-input-row input {
  flex: 1; background: transparent; border: none; color: #fff; font-family: var(--mono); font-size: 11px; outline: none;
}
.stop-btn {
  background: var(--danger); color: #fff; border: none; padding: 2px 8px; border-radius: 3px; font-size: 10px; cursor: pointer;
  margin-left: 8px; font-weight: 600; text-transform: uppercase;
}
.stop-btn:hover { opacity: 0.9; }
.terminal-input-row input::placeholder { color: #222; }

.loader-inline {
  display: inline-block; width: 7px; height: 7px; border: 1.5px solid #222; border-top-color: var(--accent); border-radius: 50%;
  animation: spin 0.8s linear infinite; margin-right: 4px;
}

@keyframes spin { to { transform: rotate(360deg); } }

.terminal-body::-webkit-scrollbar { width: 3px; }
.terminal-body::-webkit-scrollbar-thumb { background: #1a1a1a; border-radius: 1px; }
</style>
