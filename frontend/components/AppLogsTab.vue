<template>
  <div class="tab-logs">
    <div class="terminal-header">
      <div class="terminal-dots">
        <span></span><span></span><span></span>
      </div>
      <div class="terminal-title">Deployment Console</div>
      <div v-if="isLive" class="live-indicator">
        <span class="dot"></span> LIVE
      </div>
    </div>
    <div class="log-terminal" ref="terminal">
      <div v-if="!displayLogs.length" class="empty-logs">
        <p>No logs available for this deployment.</p>
      </div>
      <div v-for="(log, i) in displayLogs" :key="i" class="log-line">
        <span class="log-ts">[{{ formatTimestamp(log.timestamp) }}]</span>
        <span class="log-msg" :class="'log-' + log.level">{{ log.message }}</span>
      </div>
      <div v-if="isLive" class="terminal-cursor"></div>
    </div>
  </div>
</template>

<script setup>
import { computed, ref, onMounted, onUnmounted, watch, nextTick } from 'vue'

const props = defineProps({
  appId: { type: String, required: true },
  jobId: { type: String, default: null },
  logs: { type: Array, default: () => [] }
})

const appsCtx = useApps()
const terminal = ref(null)
const pollTimer = ref(null)

const mockLogs = []

const displayLogs = computed(() => {
  return props.logs && props.logs.length > 0 ? props.logs : (props.jobId ? [] : mockLogs)
})

const isLive = computed(() => {
  const status = appsCtx.currentJob?.status?.toLowerCase()
  return status === 'running' || status === 'queued'
})

const scrollToBottom = () => {
  if (terminal.value) {
    terminal.value.scrollTop = terminal.value.scrollHeight
  }
}

const startPolling = () => {
  if (pollTimer.value) return
  pollTimer.value = setInterval(async () => {
    if (isLive.value) {
      await appsCtx.fetchLogs(props.appId, props.jobId)
    } else if (pollTimer.value) {
      clearInterval(pollTimer.value)
      pollTimer.value = null
    }
  }, 1000) // Fast 1s polling for "realtime" feel
}

const stopPolling = () => {
  if (pollTimer.value) {
    clearInterval(pollTimer.value)
    pollTimer.value = null
  }
}

onMounted(() => {
  scrollToBottom()
  if (isLive.value) startPolling()
})

onUnmounted(() => {
  stopPolling()
})

watch(() => displayLogs.value.length, () => {
  nextTick(() => scrollToBottom())
})

watch(isLive, (val) => {
  if (val) startPolling()
  else stopPolling()
})

const formatTimestamp = (ts) => {
  if (!ts) return ''
  return new Date(ts).toLocaleTimeString()
}
</script>

<style scoped>
.terminal-header {
  background: #1a1b1e; border-radius: 8px 8px 0 0; padding: 10px 14px;
  display: flex; align-items: center; border: 1px solid #2d2e32; border-bottom: none;
}
.terminal-dots { display: flex; gap: 6px; }
.terminal-dots span { width: 10px; height: 10px; border-radius: 50%; background: #3c3d42; }
.terminal-dots span:nth-child(1) { background: #ff5f56; }
.terminal-dots span:nth-child(2) { background: #ffbd2e; }
.terminal-dots span:nth-child(3) { background: #27c93f; }
.terminal-title { flex: 1; text-align: center; font-size: 11px; color: var(--text3); font-weight: 500; font-family: var(--mono); }

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
  background: #0d0e11; border-radius: 0 0 8px 8px; padding: 15px; font-family: var(--mono);
  font-size: 11.5px; line-height: 1.6; height: 400px; overflow-y: auto; color: #d1d5db;
  border: 1px solid #2d2e32;
}

.log-line { margin-bottom: 2px; }
.log-ts { color: #4b5563; margin-right: 12px; font-size: 10.5px; }
.log-msg { white-space: pre-wrap; word-break: break-all; }

.log-error { color: #f87171; }
.log-warn { color: #fbbf24; }
.log-info { color: #60a5fa; }
.log-success { color: #34d399; }
.log-cmd { color: #a78bfa; font-weight: 600; }

.terminal-cursor {
  display: inline-block; width: 8px; height: 16px; background: #60a5fa;
  vertical-align: middle; margin-left: 4px; animation: blink 1s step-end infinite;
}

.empty-logs { 
  color: #4b5563; text-align: center; margin-top: 140px; 
  font-style: italic; font-size: 12px;
}

@keyframes pulse {
  0% { opacity: 0.4; }
  50% { opacity: 1; }
  100% { opacity: 0.4; }
}

@keyframes blink {
  50% { opacity: 0; }
}
</style>
