<template>
  <div class="detail-content">
    <div class="det-hdr">
      <div class="det-name"><div class="sdot" :class="pingStatus"></div>{{ srv.name }}<span class="rpill">{{ srv.role }}</span></div>
      <div class="det-meta">
        <span v-if="srv.hostname">{{ srv.hostname }}</span>
        <span v-else>{{ srv.ip }}</span>
        · {{ sysInfo?.cpuCores || '?' }} vCPU · {{ stats?.ramTotal ? Math.round(stats.ramTotal / 1024) + ' GB' : '...' }}
      </div>
      <div class="det-row">
        <span class="upbadge">
          <svg width="9" height="9" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2"><circle cx="8" cy="8" r="6"/><polyline points="8,5 8,8 10,10"/></svg>
          {{ stats?.uptime || sysInfo?.uptime || '...' }}
        </span>
        <span class="tag">Ubuntu 22.04</span>
      </div>
      
      <div class="det-btns">
        <button class="btn btn-ghost" style="font-size:10px;padding:3px 9px" @click="activeTab = 'terminal'">
          <svg width="10" height="10" viewBox="0 0 16 16" fill="none" stroke="currentColor"><rect x="1" y="4" width="14" height="9" rx="2" stroke-width="1.2"/><path d="M4 9l2-1.5L4 6" stroke-width="1.2" stroke-linecap="round"/><line x1="8" y1="9" x2="12" y2="9" stroke-width="1.2"/></svg>
          SSH
        </button>
        <button class="btn btn-ghost" style="font-size:10px;padding:3px 9px">
          <svg width="10" height="10" viewBox="0 0 16 16" fill="none" stroke="currentColor"><path d="M13 8A5 5 0 1 1 3.05 5.5" stroke-width="1.2"/><path d="M13 1v4h-4" stroke-width="1.2" stroke-linecap="round"/></svg>
          Restart
        </button>
        <button class="btn btn-danger" style="font-size:10px;padding:3px 9px">
          <svg width="10" height="10" viewBox="0 0 16 16" fill="none" stroke="currentColor"><line x1="6" y1="2" x2="6" y2="14"/><line x1="10" y1="2" x2="10" y2="14" stroke-width="1.2"/></svg>
          Stop
        </button>
      </div>
    </div>

    <div class="dtabs">
      <div class="dtab" :class="{ active: activeTab === 'resources' }" @click="activeTab = 'resources'">Resources</div>
      <div class="dtab" :class="{ active: activeTab === 'network' }" @click="activeTab = 'network'">Network</div>
      <div class="dtab" :class="{ active: activeTab === 'processes' }" @click="activeTab = 'processes'">Processes</div>
      <div class="dtab" :class="{ active: activeTab === 'terminal' }" @click="activeTab = 'terminal'">Terminal</div>
      <div class="dtab" :class="{ active: activeTab === 'apps' }" @click="activeTab = 'apps'">Apps</div>
      <div class="dtab" :class="{ active: activeTab === 'info' }" @click="activeTab = 'info'">Properties</div>
    </div>

    <div class="det-body">
      <ServerResourcesTab 
        v-if="activeTab === 'resources'" 
        :stats="stats" 
        :sysInfo="sysInfo" 
        :downloadSpeed="downloadSpeed" 
        :uploadSpeed="uploadSpeed" 
      />

      <ServerNetworkTab 
        v-if="activeTab === 'network'" 
        :stats="stats" 
        :downloadSpeed="downloadSpeed" 
        :uploadSpeed="uploadSpeed" 
      />

      <ServerProcessesTab 
        v-if="activeTab === 'processes'" 
        :stats="stats" 
      />

      <div v-if="activeTab === 'terminal'" class="tab-pane terminal-pane">
        <ServerTerminal :server="srv" />
      </div>

      <ServerAppsTab 
        v-if="activeTab === 'apps'" 
        :serverId="srv.id"
        :managedApps="managedApps"
        :systemAppStatuses="systemAppStatuses"
        :installing="installing"
        :appLastResults="appLastResults"
        @install="installSystemApp"
        @clearResult="appId => appLastResults[appId] = null"
      />

      <ServerPropertiesTab 
        v-if="activeTab === 'info'" 
        :sysInfo="sysInfo" 
        :stats="stats" 
      />
    </div>
  </div>
</template>

<script setup>
import ServerResourcesTab from './ServerResourcesTab.vue'
import ServerNetworkTab from './ServerNetworkTab.vue'
import ServerProcessesTab from './ServerProcessesTab.vue'
import ServerAppsTab from './ServerAppsTab.vue'
import ServerPropertiesTab from './ServerPropertiesTab.vue'

const props = defineProps({
  srv: { type: Object, required: true }
})

const config = useRuntimeConfig()
const auth = useAuth()
const nuxtApp = useNuxtApp()

const activeTab = ref('resources')
const sysInfo = ref(null)
const pingStatus = ref('sdot-x')
const stats = computed(() => sysInfo.value?.stats)

const managedApps = [
  { id: 'nginx', name: 'Nginx', description: 'Web server & Reverse Proxy', color: 'var(--accent)' },
  { id: 'redis', name: 'Redis', description: 'In-memory data store', color: 'var(--danger)' },
  { id: 'dotnet', name: '.NET SDK', description: 'v8.0 Runtime & SDK', color: 'var(--purple)' }
]
const systemAppStatuses = ref({ nginx: 'unknown', redis: 'unknown', dotnet: 'unknown' })
const installing = ref({ nginx: false, redis: false, dotnet: false })
const appLastResults = ref({ nginx: null, redis: null, dotnet: null })

const downloadSpeed = ref('0 B/s')
const uploadSpeed = ref('0 B/s')
let lastNet = { rx: 0, tx: 0, time: 0 }
let lastIfaceNet = {} // { name: { rx, tx, time } }

const formatBytes = (bytes) => {
  if (bytes === 0) return '0 B/s'
  const k = 1024
  const sizes = ['B/s', 'KB/s', 'MB/s', 'GB/s']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i]
}

const fetchStaticInfo = async () => {
  try {
    const data = await nuxtApp.$apiFetch(`/api/servers/${props.srv.id}/sys-info`, {
      baseURL: config.public.apiBase,
      headers: auth.authHeaders()
    })
    sysInfo.value = { ...sysInfo.value, ...data }
  } catch (e) {
    console.error('Failed to fetch static info', e)
  }
}

const fetchSystemApps = async () => {
  try {
    const statuses = await nuxtApp.$apiFetch(`/api/system-apps/${props.srv.id}/status`, {
      baseURL: config.public.apiBase,
      headers: auth.authHeaders()
    })
    systemAppStatuses.value = statuses
  } catch (e) {
    console.error('Failed to fetch system apps status', e)
  }
}

const installSystemApp = async (appId) => {
  if (installing.value[appId]) return
  installing.value[appId] = true
  appLastResults.value[appId] = { success: true, message: `Starting ${appId} installation...`, output: '', exitStatus: 0 }
  
  try {
    const response = await fetch(`${config.public.apiBase}/api/system-apps/${props.srv.id}/install/${appId}`, {
      method: 'POST',
      headers: {
        ...auth.authHeaders()
      }
    })

    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`)
    
    const reader = response.body.getReader()
    const decoder = new TextDecoder()
    
    while (true) {
      const { done, value } = await reader.read()
      if (done) break
      
      const chunk = decoder.decode(value, { stream: true })
      appLastResults.value[appId].output += chunk
    }
    
    // Parse exit status from the end of the output
    const output = appLastResults.value[appId].output
    const marker = '\n[EXIT_STATUS]: '
    const markerIndex = output.lastIndexOf(marker)
    if (markerIndex !== -1) {
      const exitStatusStr = output.substring(markerIndex + marker.length).trim()
      const exitStatus = parseInt(exitStatusStr)
      appLastResults.value[appId].exitStatus = exitStatus
      appLastResults.value[appId].success = exitStatus === 0
      appLastResults.value[appId].output = output.substring(0, markerIndex)
    }
    
    appLastResults.value[appId].message = appLastResults.value[appId].success 
      ? `${appId} installation completed` 
      : `${appId} installation failed`
    setTimeout(fetchSystemApps, 2000)
  } catch (e) {
    console.error(e)
    appLastResults.value[appId] = { 
      success: false, 
      message: `Failed during ${appId} installation`, 
      error: e.message,
      exitStatus: -1
    }
  } finally {
    installing.value[appId] = false
  }
}

const fetchRealtime = async () => {
  try {
    let endpoint = `/api/servers/${props.srv.id}/stats`
    if (activeTab.value === 'network') endpoint = `/api/servers/${props.srv.id}/network`
    if (activeTab.value === 'processes') endpoint = `/api/servers/${props.srv.id}/processes`

    const data = await nuxtApp.$apiFetch(endpoint, {
      baseURL: config.public.apiBase,
      headers: auth.authHeaders()
    })
    
    if (activeTab.value === 'resources') {
      if (lastNet.time > 0 && data) {
        const now = Date.now()
        const diffSec = (now - lastNet.time) / 1000
        const rxSpeed = (data.netRx - lastNet.rx) / diffSec
        const txSpeed = (data.netTx - lastNet.tx) / diffSec
        downloadSpeed.value = formatBytes(Math.max(0, rxSpeed))
        uploadSpeed.value = formatBytes(Math.max(0, txSpeed))
      }
      
      if (data) {
        lastNet = { rx: data.netRx, tx: data.netTx, time: Date.now() }
        sysInfo.value = { ...sysInfo.value, stats: data }
      }
    } else if (activeTab.value === 'network') {
      if (data?.interfaces) {
        const now = Date.now()
        data.interfaces = data.interfaces.map(iface => {
          const last = lastIfaceNet[iface.name]
          let rxSpeed = '0 B/s'
          let txSpeed = '0 B/s'
          
          if (last && last.time > 0) {
            const diffSec = (now - last.time) / 1000
            rxSpeed = formatBytes(Math.max(0, (iface.rx - last.rx) / diffSec))
            txSpeed = formatBytes(Math.max(0, (iface.tx - last.tx) / diffSec))
          }
          
          lastIfaceNet[iface.name] = { rx: iface.rx, tx: iface.tx, time: now }
          return { ...iface, rxSpeed, txSpeed }
        })
      }
      sysInfo.value = { ...sysInfo.value, stats: { ...sysInfo.value?.stats, ...data } }
    } else if (activeTab.value === 'processes') {
      sysInfo.value = { ...sysInfo.value, stats: { ...sysInfo.value?.stats, processes: data } }
    }

    if (activeTab.value === 'apps') {
      await fetchSystemApps()
    }

    pingStatus.value = 'sdot-g'
  } catch (e) {
    pingStatus.value = 'sdot-r'
  }
}

const isPolling = ref(false)
let timer = null

const startPolling = async () => {
  if (isPolling.value) return
  isPolling.value = true
  
  try {
    await fetchRealtime()
  } finally {
    isPolling.value = false
    timer = setTimeout(startPolling, 3000)
  }
}

onMounted(() => {
  fetchStaticInfo()
  startPolling()
})

onUnmounted(() => {
  if (timer) clearTimeout(timer)
})

watch(() => props.srv.id, () => {
  if (timer) clearTimeout(timer)
  sysInfo.value = null
  lastNet = { rx: 0, tx: 0, time: 0 }
  lastIfaceNet = {}
  fetchStaticInfo()
  startPolling()
})

watch(activeTab, () => {
  fetchRealtime()
})
</script>

<style scoped>
.detail-content { flex: 1; display: flex; flex-direction: column; overflow: hidden; }

.det-hdr { padding: 11px 13px; border-bottom: 1px solid var(--border); }
.det-name { font-size: 13px; font-weight: 600; display: flex; align-items: center; gap: 6px; }
.det-meta { font-size: 10px; color: var(--text3); font-family: var(--mono); margin-top: 2px; }
.det-row { display: flex; gap: 5px; margin-top: 7px; flex-wrap: wrap; align-items: center; }

.upbadge { display: inline-flex; align-items: center; gap: 4px; font-size: 10px; font-family: var(--mono); color: var(--accent); background: rgba(0,201,167,.08); border: 1px solid rgba(0,201,167,.15); border-radius: 99px; padding: 2px 8px; }
.tag { font-size: 9px; padding: 1px 6px; border-radius: 4px; background: var(--bg3); color: var(--text3); border: 1px solid var(--border); }
.det-btns { display: flex; gap: 5px; margin-top: 8px; }

.dtabs { display: flex; border-bottom: 1px solid var(--border); }
.dtab { padding: 7px 11px; font-size: 11px; font-weight: 500; color: var(--text3); cursor: pointer; border-bottom: 2px solid transparent; transition: all var(--transition); }
.dtab.active { color: var(--accent); border-bottom-color: var(--accent); background: var(--bg1); }
.dtab:hover:not(.active) { color: var(--text1); }

.det-body { flex: 1; overflow-y: auto; }
.tab-pane { display: flex; flex-direction: column; }
.terminal-pane { height: 400px; }

.sdot { width: 6px; height: 6px; border-radius: 50%; flex-shrink: 0; }
.sdot-g { background: var(--success); box-shadow: 0 0 5px var(--success); }
.sdot-r { background: var(--danger); box-shadow: 0 0 5px var(--danger); }
.sdot-x { background: var(--text3); }

.rpill { font-size: 9px; font-weight: 600; letter-spacing: .5px; text-transform: uppercase; padding: 1px 6px; border-radius: 99px; background: rgba(0,201,167,0.12); color: var(--accent); }
</style>
