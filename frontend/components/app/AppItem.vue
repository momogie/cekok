<template>
  <div 
    class="app-row" 
    :class="{ selected: isSelected }"
    @click="$emit('select', app)"
  >
    <div class="app-icon" :class="iconClass">{{ app.name.substring(0, 2).toUpperCase() }}</div>
    <div class="app-info">
      <div class="app-name">
        {{ app.name }}
        <span v-if="app.type" class="nginx-badge">{{ app.type }}</span>
      </div>
      <div class="app-meta">{{ app.repoUrl }} • {{ app.branch }}</div>
    </div>
    
    <div v-if="status" class="status-indicator">
      <div class="status-dot" :class="'dot-' + statusColor"></div>
      <span class="status-text" :class="statusColor">{{ status }}</span>
    </div>
    
    <div v-if="app.lastDeployedAt" class="app-time">
      {{ formatTime(app.lastDeployedAt) }}
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  app: { type: Object, required: true },
  isSelected: { type: Boolean, default: false },
  status: { type: String, default: '' }
})

const iconClass = computed(() => {
  const type = props.app.type?.toLowerCase() || ''
  if (type.includes('dotnet')) return 'icon-dotnet'
  if (type.includes('nuxt')) return 'icon-nuxt'
  if (type.includes('vue')) return 'icon-vue'
  if (type.includes('react')) return 'icon-react'
  if (type.includes('node')) return 'icon-node'
  if (type.includes('php')) return 'icon-php'
  return 'icon-static'
})

const statusColor = computed(() => {
  const s = props.status?.toLowerCase() || ''
  if (s === 'success' || s === 'completed') return 'green'
  if (s === 'failed' || s === 'error') return 'red'
  if (s === 'running' || s === 'pending') return 'yellow'
  return 'gray'
})

const formatTime = (dateStr) => {
  if (!dateStr) return ''
  const date = new Date(dateStr)
  const now = new Date()
  const diff = (now.getTime() - date.getTime()) / 1000
  if (diff < 60) return 'Just now'
  if (diff < 3600) return Math.floor(diff / 60) + 'm ago'
  if (diff < 86400) return Math.floor(diff / 3600) + 'h ago'
  return Math.floor(diff / 86400) + 'd ago'
}
</script>

<style scoped>
.app-row {
  padding: 10px 13px; border-bottom: 1px solid var(--border);
  display: flex; align-items: center; gap: 10px; cursor: pointer;
  transition: background var(--transition);
}
.app-row:last-child { border-bottom: none; }
.app-row:hover { background: var(--bg2); }
.app-row.selected { background: rgba(0, 201, 167, 0.06); border-left: 2px solid var(--accent); }

.app-icon {
  width: 32px; height: 32px; border-radius: 6px; flex-shrink: 0;
  display: flex; align-items: center; justify-content: center;
  font-size: 11px; font-weight: 700; font-family: var(--mono);
}
.icon-dotnet { background: rgba(139, 127, 255, 0.15); color: var(--purple); }
.icon-nuxt { background: rgba(0, 201, 167, 0.12); color: var(--accent); }
.icon-vue { background: rgba(0, 201, 167, 0.12); color: var(--accent); }
.icon-react { background: rgba(0, 151, 255, 0.15); color: #61dafb; }
.icon-node { background: rgba(131, 205, 41, 0.15); color: #83cd29; }
.icon-php { background: rgba(119, 123, 179, 0.15); color: #777bb3; }
.icon-static { background: rgba(0, 151, 255, 0.12); color: var(--accent2); }

.app-info { flex: 1; min-width: 0; }
.app-name { font-size: 12px; font-weight: 500; display: flex; align-items: center; gap: 6px; }
.app-meta { font-size: 10px; color: var(--text3); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; margin-top: 1px; }

.nginx-badge {
  font-size: 8px; font-weight: 600; padding: 1px 4px; border-radius: 3px;
  background: rgba(0, 151, 255, 0.1); color: var(--accent2); border: 1px solid rgba(0, 151, 255, 0.2);
  text-transform: uppercase;
}

.status-indicator { display: flex; align-items: center; gap: 5px; flex-shrink: 0; margin: 0 8px; }
.status-dot { width: 6px; height: 6px; border-radius: 50%; }
.dot-green { background: var(--success); box-shadow: 0 0 4px var(--success); }
.dot-yellow { background: var(--warn); box-shadow: 0 0 4px var(--warn); }
.dot-red { background: var(--danger); box-shadow: 0 0 4px var(--danger); }
.dot-gray { background: var(--text3); }

.status-text { font-size: 10px; font-weight: 500; text-transform: capitalize; }
.status-text.green { color: var(--success); }
.status-text.red { color: var(--danger); }
.status-text.yellow { color: var(--warn); }
.status-text.gray { color: var(--text3); }

.app-time { font-size: 9px; color: var(--text3); font-family: var(--mono); flex-shrink: 0; min-width: 50px; text-align: right; }
</style>
