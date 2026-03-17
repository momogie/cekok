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
      <div v-if="activeTab === 'overview'" class="tab-overview">
        <div class="info-grid">
          <div class="info-item">
            <div class="info-label">Current Status</div>
            <div class="info-val">
              <span class="status-pill" :class="statusColor">
                {{ currentJob ? currentJob.status : 'Idle' }}
              </span>
            </div>
          </div>
          <div class="info-item">
            <div class="info-label">Last Deploy</div>
            <div class="info-val">{{ formatDate(app.lastDeployedAt) }}</div>
          </div>
          <div class="info-item">
            <div class="info-label">Branch</div>
            <div class="info-val">{{ app.branch }}</div>
          </div>
          <div class="info-item">
            <div class="info-label">Build Command</div>
            <div class="info-val"><code>{{ app.buildCmd || 'None' }}</code></div>
          </div>
        </div>

        <div v-if="currentJob" class="deploy-progress">
          <div class="progress-header">
            <div class="progress-title">Active Deployment: {{ currentJob.id }}</div>
            <div class="progress-pct">{{ currentJob.progress || 0 }}%</div>
          </div>
          <div class="progress-bar">
            <div class="progress-fill" :style="{ width: (currentJob.progress || 0) + '%' }"></div>
          </div>
        </div>

        <div class="section-title">Schedule</div>
        <div class="schedule-card">
          <div class="schedule-info">
            <div class="schedule-cron">{{ app.scheduleCron || 'No schedule' }}</div>
            <div class="schedule-status" :class="{ enabled: app.scheduleEnabled }">
              {{ app.scheduleEnabled ? 'Enabled' : 'Disabled' }}
            </div>
          </div>
        </div>
      </div>

      <div v-if="activeTab === 'logs'" class="tab-logs">
        <div class="log-terminal">
          <div v-for="(log, i) in logs" :key="i" class="log-line">
            <span class="log-ts">[{{ formatTimestamp(log.timestamp) }}]</span>
            <span class="log-msg" :class="'log-' + log.type">{{ log.message }}</span>
          </div>
          <div v-if="!logs.length" class="empty-logs">No logs found for this application.</div>
        </div>
      </div>

      <div v-if="activeTab === 'targets'" class="tab-targets">
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

const serversCtx = useServers()

onMounted(() => {
  if (serversCtx.servers.length === 0) {
    serversCtx.fetchServers()
  }
})

const getServerName = (serverId) => {
  return serversCtx.servers.find(s => s.id === serverId)?.name || serverId
}

const tabs = [
  { id: 'overview', label: 'Overview' },
  { id: 'logs', label: 'Logs' },
  { id: 'targets', label: 'Targets' }
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

const statusColor = computed(() => {
  const s = props.currentJob?.status?.toLowerCase() || ''
  if (s === 'success' || s === 'completed') return 'pill-success'
  if (s === 'failed' || s === 'error') return 'pill-failed'
  if (s === 'running' || s === 'pending') return 'pill-running'
  return 'pill-idle'
})

const handleDeploy = () => {
  emit('deploy', props.app.id)
}

const formatDate = (dateStr) => {
  if (!dateStr) return 'Never'
  return new Date(dateStr).toLocaleString()
}

const formatTimestamp = (ts) => {
  return new Date(ts).toLocaleTimeString()
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

.info-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 16px; margin-bottom: 24px; }
.info-label { font-size: 10px; font-weight: 600; text-transform: uppercase; color: var(--text3); letter-spacing: 0.5px; margin-bottom: 4px; }
.info-val { font-size: 13px; font-weight: 500; }
.info-val code { font-family: var(--mono); font-size: 11px; background: var(--bg2); padding: 2px 5px; border-radius: 4px; }

.status-pill {
  font-size: 10px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.4px;
  padding: 2px 8px; border-radius: 99px; display: inline-block;
}
.pill-success { background: rgba(0, 201, 167, 0.12); color: var(--accent); }
.pill-running { background: rgba(0, 151, 255, 0.12); color: var(--accent2); animation: pulse 1.5s infinite; }
.pill-failed { background: rgba(240, 80, 96, 0.12); color: var(--danger); }
.pill-idle { background: var(--bg3); color: var(--text3); }

@keyframes pulse { 0% { opacity: 1; } 50% { opacity: 0.6; } 100% { opacity: 1; } }

.deploy-progress { margin-bottom: 24px; background: var(--bg2); padding: 12px; border-radius: 8px; border: 1px solid var(--border); }
.progress-header { display: flex; justify-content: space-between; margin-bottom: 8px; }
.progress-title { font-size: 11px; font-weight: 600; color: var(--text2); }
.progress-pct { font-size: 11px; font-family: var(--mono); color: var(--accent); }
.progress-bar { height: 6px; background: var(--bg3); border-radius: 99px; overflow: hidden; }
.progress-fill { height: 100%; background: linear-gradient(90deg, var(--accent), var(--accent2)); transition: width 0.4s ease; }

.section-title { font-size: 11px; font-weight: 600; text-transform: uppercase; color: var(--text3); letter-spacing: 0.8px; margin-bottom: 10px; }
.schedule-card { background: var(--bg2); border: 1px solid var(--border); border-radius: 8px; padding: 12px; }
.schedule-info { display: flex; justify-content: space-between; align-items: center; }
.schedule-cron { font-family: var(--mono); font-size: 12px; color: var(--accent2); }
.schedule-status { font-size: 10px; font-weight: 600; text-transform: uppercase; color: var(--text3); }
.schedule-status.enabled { color: var(--success); }

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
