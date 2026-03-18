<template>
  <div class="app-detail">
    <div class="detail-header">
      <div class="header-icon" :class="iconClass">{{ app.name.substring(0, 2).toUpperCase() }}</div>
      <div class="header-text">
        <div class="header-name">{{ app.name }}</div>
        <div class="header-meta">
          <span class="meta-tag">{{ app.type }}</span>
          <span class="meta-sep">•</span>
          <span class="meta-repo">{{ app.branch }} @ {{ app.repoUrl }}</span>
        </div>
      </div>
      <div class="header-right">
        <button class="btn btn-ghost btn-sm" @click="$emit('edit', app)">Edit</button>
        <button 
          class="btn btn-primary btn-sm" 
          :disabled="isDeploying" 
          @click="handleDeploy"
        >
          {{ isDeploying ? 'Deploying...' : 'Deploy Now' }}
        </button>
      </div>
    </div>

    <div class="detail-tabs">
      <button 
        v-for="tab in tabs" 
        :key="tab.id"
        class="tab-btn" 
        :class="{ active: activeTab === tab.id }"
        @click="activeTab = tab.id"
      >
        {{ tab.label }}
      </button>
    </div>

    <div class="detail-content scrollable">
      <AppOverviewTab v-if="activeTab === 'overview'" :app="app" :currentJob="currentJob" />
      <AppLogsTab v-if="activeTab === 'logs'" :logs="logs" />
      <AppTargetsTab v-if="activeTab === 'targets'" :app="app" />
      <AppDeploymentHistoryTab v-if="activeTab === 'history'" :app="app" />
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  app: { type: Object, required: true },
  currentJob: { type: Object, default: null },
  logs: { type: Array, default: () => [] }
})

const emit = defineEmits(['deploy', 'edit'])
const activeTab = ref('overview')

const tabs = [
  { id: 'overview', label: 'Overview' },
  { id: 'logs', label: 'Logs' },
  { id: 'targets', label: 'Targets' },
  { id: 'history', label: 'History' }
]

const isDeploying = computed(() => {
  const s = props.currentJob?.status?.toLowerCase() || ''
  return s === 'running' || s === 'pending'
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

const handleDeploy = () => {
  emit('deploy', props.app.id)
}
</script>

<style scoped>
.app-detail { display: flex; flex-direction: column; height: 100%; }

.detail-header {
  padding: 16px 20px; border-bottom: 1px solid var(--border);
  display: flex; align-items: center; gap: 14px;
}
.header-icon {
  width: 42px; height: 42px; border-radius: 9px; flex-shrink: 0;
  display: flex; align-items: center; justify-content: center;
  font-size: 14px; font-weight: 700; font-family: var(--mono);
}
.icon-dotnet { background: rgba(139, 127, 255, 0.15); color: var(--purple); }
.icon-nuxt { background: rgba(0, 201, 167, 0.12); color: var(--accent); }
.icon-vue { background: rgba(0, 201, 167, 0.12); color: var(--accent); }
.icon-react { background: rgba(0, 151, 255, 0.15); color: #61dafb; }
.icon-node { background: rgba(131, 205, 41, 0.15); color: #83cd29; }
.icon-php { background: rgba(119, 123, 179, 0.15); color: #777bb3; }
.icon-static { background: rgba(0, 151, 255, 0.12); color: var(--accent2); }

.header-text { flex: 1; min-width: 0; }
.header-name { font-size: 16px; font-weight: 600; }
.header-meta { font-size: 11px; color: var(--text3); display: flex; align-items: center; gap: 6px; margin-top: 2px; }
.meta-tag { font-weight: 600; text-transform: uppercase; color: var(--accent2); }
.meta-repo { white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

.header-right { display: flex; gap: 8px; }
.btn-sm { padding: 4px 10px; font-size: 11px; }

.detail-tabs { display: flex; border-bottom: 1px solid var(--border); padding: 0 20px; }
.tab-btn {
  padding: 12px 14px; font-size: 12px; font-weight: 500; color: var(--text3);
  background: none; border: none; border-bottom: 2px solid transparent; cursor: pointer;
  transition: all var(--transition);
}
.tab-btn.active { color: var(--accent); border-bottom-color: var(--accent); }
.tab-btn:hover:not(.active) { color: var(--text1); }

.detail-content { flex: 1; padding: 20px; }
.scrollable { overflow-y: auto; }
</style>
