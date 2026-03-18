<template>
  <div class="tab-targets">
    <div v-if="!app.deployTargets?.length" class="empty-targets">No deploy targets configured.</div>
    <div v-for="target in app.deployTargets" :key="target.serverId" class="target-card">
      <div class="target-head">
        <div class="target-server">Server: {{ getServerName(target.serverId) }}</div>
        <div v-if="target.port" class="target-port">Port: {{ target.port }}</div>
      </div>
      <div class="target-path">Path: <code>{{ target.deployDir }}</code></div>
      <div v-if="target.serviceName" class="target-service">Service: <code>{{ target.serviceName }}</code></div>
    </div>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'

const props = defineProps({
  app: { type: Object, required: true }
})

const serversCtx = useServers()

onMounted(() => {
  if (serversCtx.servers.length === 0) {
    serversCtx.fetchServers()
  }
})

const getServerName = (serverId) => {
  return serversCtx.servers.find(s => s.id === serverId)?.name || serverId
}
</script>

<style scoped>
.tab-targets { display: flex; flex-direction: column; gap: 12px; }
.target-card { background: var(--bg2); border: 1px solid var(--border); border-radius: 8px; padding: 12px; }
.target-head { display: flex; justify-content: space-between; margin-bottom: 8px; }
.target-server { font-size: 11px; font-weight: 600; color: var(--text2); }
.target-port { font-size: 10px; font-family: var(--mono); color: var(--accent); }
.target-path { font-size: 11px; color: var(--text3); margin-bottom: 4px; }
.target-service { font-size: 11px; color: var(--text3); }
.target-path code, .target-service code { color: var(--text2); background: var(--bg3); padding: 1px 4px; border-radius: 3px; font-family: var(--mono); }
.empty-targets { color: var(--text3); text-align: center; margin-top: 50px; font-style: italic; }
</style>
