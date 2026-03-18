<template>
  <div class="tab-logs">
    <div class="log-terminal">
      <div v-for="(log, i) in displayLogs" :key="i" class="log-line">
        <span class="log-ts">[{{ formatTimestamp(log.timestamp) }}]</span>
        <span class="log-msg" :class="'log-' + log.type">{{ log.message }}</span>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  logs: { type: Array, default: () => [] }
})

const mockLogs = [
  { timestamp: new Date(Date.now() - 60000).toISOString(), type: 'info', message: 'Starting deployment process...' },
  { timestamp: new Date(Date.now() - 58000).toISOString(), type: 'info', message: 'Cloning repository (branch: main)...' },
  { timestamp: new Date(Date.now() - 50000).toISOString(), type: 'success', message: 'Repository cloned successfully in 8.2s.' },
  { timestamp: new Date(Date.now() - 48000).toISOString(), type: 'info', message: 'Installing dependencies...' },
  { timestamp: new Date(Date.now() - 25000).toISOString(), type: 'warn', message: 'npm WARN deprecated fsevents@2.1.2: Please update to v 2.3.2' },
  { timestamp: new Date(Date.now() - 15000).toISOString(), type: 'success', message: 'Dependencies installed successfully in 33s.' },
  { timestamp: new Date(Date.now() - 14000).toISOString(), type: 'info', message: 'Building application using "npm run build"...' },
  { timestamp: new Date(Date.now() - 4000).toISOString(), type: 'info', message: 'Build finished in 10s.' },
  { timestamp: new Date(Date.now() - 3000).toISOString(), type: 'info', message: 'Deploying artifacts to targets...' },
  { timestamp: new Date(Date.now() - 1000).toISOString(), type: 'success', message: 'Deployment completed successfully!' }
]

const displayLogs = computed(() => {
  return props.logs && props.logs.length > 0 ? props.logs : mockLogs
})

const formatTimestamp = (ts) => {
  if (!ts) return ''
  return new Date(ts).toLocaleTimeString()
}
</script>

<style scoped>
.log-terminal {
  background: #000; border-radius: 8px; padding: 12px; font-family: var(--mono);
  font-size: 11px; line-height: 1.6; height: 350px; overflow-y: auto; color: #fff;
}
.log-ts { color: var(--text3); margin-right: 10px; font-size: 10px; }
.log-msg { white-space: pre-wrap; word-break: break-all; }
.log-error { color: var(--danger); }
.log-warn { color: var(--warn); }
.log-info { color: var(--accent2); }
.log-success { color: var(--success); }
.empty-logs { color: var(--text3); text-align: center; margin-top: 100px; font-style: italic; }
</style>
