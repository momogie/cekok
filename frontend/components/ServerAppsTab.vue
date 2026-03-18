<template>
  <div class="tab-pane">
    <div class="apps-list">
      <div class="section-hdr" style="padding: 12px 12px 6px">System Applications</div>
      <div v-for="app in managedApps" :key="app.id">
        <div class="app-item">
          <div class="app-icon" :style="{ background: app.color + '1a', color: app.color }">
            <span v-if="app.id === 'nginx'">NG</span>
            <span v-else-if="app.id === 'redis'">RD</span>
            <span v-else-if="app.id.startsWith('dotnet')">.NET</span>
          </div>
          <div class="app-info">
            <div class="app-name">{{ app.name }}</div>
            <div class="app-desc">{{ app.description }}</div>
          </div>
          <div class="app-status">
            <span v-if="systemAppStatuses[app.id] === 'active'" class="status-pill active">Installed</span>
            <span v-else-if="installing[app.id]" class="status-pill loading">Installing...</span>
            <span v-else-if="systemAppStatuses[app.id] === 'unknown'" class="status-pill loading">Checking...</span>
            <span v-else class="status-pill inactive">Not Installed</span>
          </div>
          <button 
            class="btn btn-ghost btn-sm" 
            v-if="systemAppStatuses[app.id] === 'inactive' && !installing[app.id]"
            @click="installSystemApp(app.id)"
          >
            Install
          </button>
          <span v-else class="installed-check">
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="var(--success)" stroke-width="2.5"><polyline points="3,8 6,11 13,4"/></svg>
          </span>
        </div>
        
        <!-- Per-app Installation Result -->
        <div v-if="appLastResults[app.id]" class="install-result-box mini">
          <div class="res-box-hdr">
            <span>Result: {{ appLastResults[app.id].exitStatus === 0 ? 'Success' : 'Failed' }}</span>
            <button class="btn-close" @click="clearResult(app.id)">&times;</button>
          </div>
          <div class="res-box-body" ref="logContainer">
            <div v-if="appLastResults[app.id].message" class="res-msg" :class="{ err: !appLastResults[app.id].success }">{{ appLastResults[app.id].message }}</div>
            <pre v-if="appLastResults[app.id].output" class="res-pre">{{ appLastResults[app.id].output }}</pre>
            <pre v-if="appLastResults[app.id].error" class="res-pre err">{{ appLastResults[app.id].error }}</pre>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  serverId: String,
  managedApps: Array,
  systemAppStatuses: Object,
  installing: Object,
  appLastResults: Object
})

const emit = defineEmits(['install', 'clearResult'])

const logContainer = ref(null)

const installSystemApp = (appId) => {
  emit('install', appId)
}

const clearResult = (appId) => {
  emit('clearResult', appId)
}

watch(() => props.appLastResults, () => {
  nextTick(() => {
    if (logContainer.value) {
      logContainer.value.scrollTop = logContainer.value.scrollHeight
    }
  })
}, { deep: true })
</script>

<style scoped>
.tab-pane { display: flex; flex-direction: column; }
.section-hdr { font-size: 9px; color: var(--text3); font-weight: 600; letter-spacing: 1px; margin-bottom: 8px; }
.apps-list { display: flex; flex-direction: column; gap: 4px; padding-bottom: 20px; }
.app-item { display: flex; align-items: center; gap: 12px; padding: 10px 16px; border-bottom: 1px solid var(--border); transition: background 0.2s; }
.app-item:hover { background: var(--bg2); }
.app-icon { width: 32px; height: 32px; border-radius: 8px; display: flex; align-items: center; justify-content: center; font-size: 10px; font-weight: 700; flex-shrink: 0; }
.app-info { flex: 1; min-width: 0; }
.app-name { font-size: 13px; font-weight: 600; color: var(--text1); }
.app-desc { font-size: 11px; color: var(--text3); }
.app-status { margin: 0 12px; }
.status-pill { font-size: 10px; font-weight: 600; padding: 2px 8px; border-radius: 99px; }
.status-pill.active { background: rgba(0, 201, 167, 0.1); color: var(--accent); }
.status-pill.inactive { background: var(--bg3); color: var(--text3); }
.status-pill.loading { background: rgba(240, 165, 0, 0.1); color: var(--warn); animation: pulse 1.5s infinite; }
.installed-check { width: 20px; height: 20px; display: flex; align-items: center; justify-content: center; }
.btn-sm { padding: 4px 10px; font-size: 11px; }

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}

.install-result-box { margin: 12px; background: var(--bg3); border: 1px solid var(--border); border-radius: 8px; overflow: hidden; display: flex; flex-direction: column; max-height: 300px; }
.install-result-box.mini { margin: 0 16px 16px 16px; border-top: none; border-top-left-radius: 0; border-top-right-radius: 0; }
.res-box-hdr { padding: 6px 12px; background: var(--bg2); border-bottom: 1px solid var(--border); display: flex; justify-content: space-between; align-items: center; font-size: 11px; font-weight: 600; }
.btn-close { background: none; border: none; color: var(--text3); cursor: pointer; font-size: 16px; padding: 0 4px; }
.res-box-body { padding: 8px; overflow-y: auto; font-family: var(--mono); }
.res-msg { font-size: 11px; margin-bottom: 8px; color: var(--success); font-weight: 600; }
.res-msg.err { color: var(--danger); }
.res-pre { font-size: 10px; white-space: pre-wrap; word-break: break-all; margin-top: 4px; color: var(--text2); }
.res-pre.err { color: var(--danger); opacity: 0.9; }
</style>
