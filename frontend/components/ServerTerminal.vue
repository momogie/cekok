<template>
  <div class="terminal-wrapper">
    <div class="terminal-header">
      <div class="terminal-dots">
        <span></span><span></span><span></span>
      </div>
      <div class="terminal-title">Terminal: {{ server.name }}</div>
      <div v-if="loading" class="live-indicator">
        <span class="dot"></span> EXECUTING
      </div>
    </div>
    
    <div class="log-terminal" ref="terminalBody">
      <div v-for="(line, idx) in history" :key="idx" class="log-line" :class="'log-' + line.type">
        <span v-if="line.type === 'cmd'" class="log-ts">$</span>
        <span class="log-msg">{{ line.text }}</span>
      </div>
      <div v-if="loading" class="terminal-cursor"></div>
    </div>

    <div class="terminal-input-row">
      <span class="prompt">$</span>
      <input 
        v-model="cmd" 
        @keydown.enter="execute" 
        ref="cmdInput"
        type="text" 
        placeholder="Type a command..."
        :disabled="loading"
        autocomplete="off"
        spellcheck="false"
      >
      <button v-if="loading" @click="stop" class="stop-btn">
        <span class="stop-icon">■</span> Stop
      </button>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, nextTick } from 'vue'

const props = defineProps({
  server: { type: Object, required: true }
})

const config = useRuntimeConfig()
const auth = useAuth()

const cmd = ref('')
const loading = ref(false)
const history = ref([
  { type: 'system', text: `Console connection established to ${props.server.name}` }
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
.terminal-wrapper {
  display: flex;
  flex-direction: column;
  height: 500px;
  background: #0d0e11;
  border-radius: 8px;
  border: 1px solid #2d2e32;
  overflow: hidden;
}

.terminal-header {
  background: #1a1b1e;
  padding: 10px 14px;
  display: flex;
  align-items: center;
  border-bottom: 1px solid #2d2e32;
}

.terminal-dots {
  display: flex;
  gap: 6px;
}

.terminal-dots span {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: #3c3d42;
}

.terminal-dots span:nth-child(1) { background: #ff5f56; }
.terminal-dots span:nth-child(2) { background: #ffbd2e; }
.terminal-dots span:nth-child(3) { background: #27c93f; }

.terminal-title {
  flex: 1;
  text-align: center;
  font-size: 11px;
  color: #9ca3af;
  font-weight: 500;
  font-family: var(--mono);
}

.live-indicator { 
  display: flex; align-items: center; gap: 6px; font-size: 10px; 
  color: var(--success); font-weight: 700; background: rgba(39, 201, 63, 0.1);
  padding: 2px 8px; border-radius: 12px;
}

.live-indicator .dot { 
  width: 6px; height: 6px; background: var(--success); border-radius: 50%;
  box-shadow: 0 0 6px var(--success); animation: pulse 1s infinite;
}

.log-terminal {
  flex: 1;
  background: #0d0e11;
  padding: 15px;
  font-family: var(--mono);
  font-size: 11.5px;
  line-height: 1.6;
  overflow-y: auto;
  color: #d1d5db;
}

.log-line { margin-bottom: 2px; }
.log-ts { color: #4b5563; margin-right: 12px; font-size: 10.5px; }
.log-msg { white-space: pre-wrap; word-break: break-all; }

.log-error { color: #f87171; }
.log-system { color: #60a5fa; font-style: italic; }
.log-output { color: #d1d5db; }
.log-cmd { color: #a78bfa; font-weight: 600; }

.terminal-cursor {
  display: inline-block; width: 8px; height: 16px; background: #60a5fa;
  vertical-align: middle; margin-left: 4px; animation: blink 1s step-end infinite;
}

.terminal-input-row {
  display: flex;
  align-items: center;
  padding: 10px 15px;
  background: #111216;
  border-top: 1px solid #2d2e32;
}

.prompt {
  color: #a78bfa;
  font-family: var(--mono);
  font-size: 11.5px;
  margin-right: 10px;
  font-weight: 600;
}

.terminal-input-row input {
  flex: 1;
  background: transparent;
  border: none;
  color: #fff;
  font-family: var(--mono);
  font-size: 11.5px;
  outline: none;
}

.terminal-input-row input::placeholder {
  color: #3f3f46;
}

.stop-btn {
  background: #ef4444;
  color: #fff;
  border: none;
  padding: 4px 10px;
  border-radius: 4px;
  font-size: 10px;
  cursor: pointer;
  margin-left: 10px;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 4px;
  transition: all 0.2s;
}

.stop-btn:hover {
  background: #dc2626;
  transform: translateY(-1px);
}

.stop-icon { font-size: 8px; }

@keyframes pulse {
  0% { opacity: 0.4; }
  50% { opacity: 1; }
  100% { opacity: 0.4; }
}

@keyframes blink {
  50% { opacity: 0; }
}

.log-terminal::-webkit-scrollbar { width: 5px; }
.log-terminal::-webkit-scrollbar-track { background: transparent; }
.log-terminal::-webkit-scrollbar-thumb { background: #2d2e32; border-radius: 10px; }
.log-terminal::-webkit-scrollbar-thumb:hover { background: #3f3f46; }
</style>
