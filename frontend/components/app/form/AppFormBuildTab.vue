<template>
  <div class="step-panel">
    <div class="msec-label">Build</div>
    <div class="form-group">
      <label class="form-label">Build command</label>
      <input v-model="form.buildCmd" class="form-input" :placeholder="currentType?.buildCmd">
      <div class="form-hint">Runs inside the cloned repo directory.</div>
    </div>
    <div class="form-row">
      <div class="form-group">
        <label class="form-label">Build output dir</label>
        <input v-model="form.outputDir" class="form-input" :placeholder="currentType?.outputDir">
      </div>
      <div class="form-group">
        <label class="form-label">Entry file name</label>
        <input v-model="form.entryFile" class="form-input" :placeholder="currentType?.entryFile || 'index.js'">
      </div>
    </div>

    <!-- Deploy targets: multi-server -->
    <div class="msec-label" style="margin-top:20px">
      Deploy targets
      <span class="target-badge">{{ form.deployTargets.length }} server{{ form.deployTargets.length !== 1 ? 's' : '' }}</span>
    </div>

    <div v-if="serversCtx.loading" class="servers-loading">Loading servers…</div>
    <div v-else-if="serversCtx.servers.length === 0" class="servers-empty">No servers registered yet.</div>
    <div v-else class="server-target-section">
      <!-- Search and Add Server -->
      <div class="searchable-dropdown">
        <div class="form-group" style="margin-bottom: 0;">
          <input 
            v-model="serverSearchQuery" 
            @focus="showServerDropdown = true"
            @blur="hideServerDropdown"
            class="form-input" 
            placeholder="Search server by name or IP to add..."
          >
        </div>
        <div v-if="showServerDropdown && filteredServers.length > 0" class="dropdown-menu">
          <div 
            v-for="srv in filteredServers" 
            :key="srv.id" 
            class="dropdown-item"
            @mousedown.prevent="addSpecificTargetServer(srv.id)"
          >
            <div class="di-name">{{ srv.name }}</div>
            <div class="di-ip">{{ srv.ip }}</div>
          </div>
        </div>
        <div v-if="showServerDropdown && filteredServers.length === 0" class="dropdown-menu">
          <div class="dropdown-item empty">No servers match your search</div>
        </div>
      </div>

      <div class="server-target-list">
        <div
          v-for="(t, index) in filteredTargets"
          :key="t.serverId"
          class="server-target-item selected"
        >
          <div class="sti-header">
            <div class="sti-info">
              <div class="sti-name">{{ getServerName(t.serverId) }}</div>
              <div class="sti-ip">{{ getServerIp(t.serverId) }}</div>
            </div>
            <button class="env-del" @click="removeTargetServer(t.serverId)" title="Remove target">×</button>
          </div>

          <div class="sti-config">
            <div class="form-row">
              <div class="form-group">
                <label class="form-label">Deploy dir <span>*</span></label>
                <input v-model="t.deployDir" class="form-input form-input-sm" placeholder="/var/www/my-app">
              </div>
              <div class="form-group">
                <label class="form-label">Service name</label>
                <input v-model="t.serviceName" class="form-input form-input-sm" placeholder="my-app">
              </div>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label class="form-label">Port</label>
                <input v-model="t.port" class="form-input form-input-sm" type="number" placeholder="5000">
              </div>
              <div class="form-group">
                <label class="form-label">Health check URL</label>
                <input v-model="t.healthCheckUrl" class="form-input form-input-sm" placeholder="http://localhost:5000/health">
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="msec-label" style="margin-top:20px">Environment variables</div>
    <div class="env-list">
      <div v-for="(ev, i) in form.envVars" :key="i" class="env-row">
        <input v-model="ev.key" class="form-input" placeholder="KEY">
        <input v-model="ev.val" class="form-input" placeholder="value">
        <button class="env-del" @click="removeEnvVar(i)">×</button>
      </div>
    </div>
    <button class="add-row-btn" @click="addEnvVar">
      <svg width="11" height="11" viewBox="0 0 12 12" fill="none"><path d="M6 2v8M2 6h8" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/></svg>
      Add variable
    </button>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'

const props = defineProps({
  form: Object,
  currentType: Object,
  serversCtx: Object,
})

const emit = defineEmits(['add-env', 'remove-env'])

// State for searchable dropdown
const serverSearchQuery = ref('')
const showServerDropdown = ref(false)

const hideServerDropdown = () => {
  showServerDropdown.value = false
}

const filteredTargets = computed(() => {
  if (!props.serversCtx.servers) return []
  return props.form.deployTargets.filter(t => props.serversCtx.servers.some(s => s.id === t.serverId))
})

const filteredServers = computed(() => {
  if (!props.serversCtx.servers) return []
  const available = props.serversCtx.servers.filter(s => !props.form.deployTargets.some(t => t.serverId === s.id))
  if (!serverSearchQuery.value) return available
  
  const q = serverSearchQuery.value.toLowerCase()
  return available.filter(s => s.name.toLowerCase().includes(q) || s.ip.toLowerCase().includes(q))
})

const getServerName = (id) => props.serversCtx.servers?.find(s => s.id === id)?.name || id
const getServerIp = (id) => props.serversCtx.servers?.find(s => s.id === id)?.ip || ''

const addSpecificTargetServer = (id) => {
  props.form.deployTargets.push({
    serverId: id,
    deployDir: '',
    serviceName: '',
    port: '',
    healthCheckUrl: ''
  })
  serverSearchQuery.value = ''
  showServerDropdown.value = false
}

const removeTargetServer = (serverId) => {
  const index = props.form.deployTargets.findIndex(t => t.serverId === serverId)
  if (index !== -1) {
    props.form.deployTargets.splice(index, 1)
  }
}

const addEnvVar = () => emit('add-env')
const removeEnvVar = (i) => emit('remove-env', i)
</script>

<style scoped>
.searchable-dropdown { position: relative; margin-bottom: 16px; z-index: 10; }
.dropdown-menu {
  position: absolute; top: 100%; left: 0; right: 0; margin-top: 4px;
  background: var(--bg2); border: 1px solid var(--border); border-radius: 6px;
  max-height: 200px; overflow-y: auto; box-shadow: 0 4px 12px rgba(0,0,0,0.2);
}
.dropdown-item {
  padding: 8px 12px; cursor: pointer; display: flex; justify-content: space-between; align-items: center;
  border-bottom: 1px solid var(--border2);
}
.dropdown-item:last-child { border-bottom: none; }
.dropdown-item:hover { background: var(--bg3); }
.dropdown-item.empty { cursor: default; color: var(--text3); font-style: italic; }
.di-name { font-size: 12px; font-weight: 500; color: var(--text1); }
.di-ip { font-size: 10px; color: var(--text3); font-family: var(--mono); }
</style>
