<template>
  <div class="panel">
    <div class="panel-header">
      <div class="panel-title">Recent Deployments</div>
    </div>
    <div class="history-list-mini">
      <div v-for="item in deployments" :key="item.id" class="history-item-mini">
        <div class="history-item-left">
          <div class="status-dot" :class="item.status.toLowerCase()"></div>
          <div class="history-item-info">
            <div class="history-item-app">{{ item.appName }}</div>
            <div class="history-item-meta">
              {{ item.triggeredBy }} • {{ formatTime(item.createdAt) }}
            </div>
          </div>
        </div>
        <div class="history-item-status" :class="item.status.toLowerCase()">
          {{ item.status }}
        </div>
      </div>
      <div v-if="!deployments.length" style="padding: 20px; text-align: center; color: var(--text3); font-size: 13px;">No recent deployments.</div>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{ deployments: Array<{ id: string; appName: string; status: string; triggeredBy: string; createdAt: string }> }>()

const formatTime = (ts: string) => {
  if (!ts) return ''
  const d = new Date(ts)
  return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) + ' ' + d.toLocaleDateString([], { day: '2-digit', month: 'short' })
}
</script>

<style scoped>
.panel {
  background: var(--bg1); border: 1px solid var(--border);
  border-radius: var(--radius); overflow: hidden;
}
.panel-header {
  padding: 14px 18px; border-bottom: 1px solid var(--border); display: flex; align-items: center; gap: 10px;
}
.panel-title { font-size: 13px; font-weight: 600; }
.history-list-mini { display: flex; flex-direction: column; }
.history-item-mini {
  display: flex; align-items: center; justify-content: space-between;
  padding: 12px 18px; border-bottom: 1px solid var(--border);
  transition: background 0.2s;
}
.history-item-mini:last-child { border-bottom: none; }
.history-item-mini:hover { background: var(--bg2); }
.history-item-left { display: flex; align-items: center; gap: 12px; }
.status-dot { width: 8px; height: 8px; border-radius: 50%; flex-shrink: 0; }
.status-dot.success { background: var(--success); box-shadow: 0 0 8px var(--success); }
.status-dot.failed { background: var(--danger); box-shadow: 0 0 8px var(--danger); }
.status-dot.running, .status-dot.queued { background: var(--warn); box-shadow: 0 0 8px var(--warn); }
.history-item-info { display: flex; flex-direction: column; gap: 2px; }
.history-item-app { font-size: 13px; font-weight: 600; color: var(--text1); }
.history-item-meta { font-size: 11px; color: var(--text3); }
.history-item-status {
  font-size: 10px; font-weight: 700; text-transform: uppercase; padding: 2px 6px; border-radius: 4px;
}
.history-item-status.success { color: var(--success); }
.history-item-status.failed { color: var(--danger); }
.history-item-status.running, .history-item-status.queued { color: var(--warn); }
</style>
