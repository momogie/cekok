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
      <div v-if="activeTab === 'resources'" class="tab-pane">
        <div class="res-grid">
          <div class="rc">
            <div class="rc-top">
              <span class="rc-lbl">CPU</span>
              <span class="rc-val">{{ stats?.cpuUsage?.toFixed(1) || 0 }}%</span>
            </div>
            <div class="rc-bar"><div class="rc-fill" :class="usageColor(stats?.cpuUsage)" :style="{ width: (stats?.cpuUsage || 0) + '%' }"></div></div>
            <div class="rc-sub">{{ sysInfo?.cpuCores || '...' }} vCPU</div>
          </div>
          
          <div class="rc">
            <div class="rc-top">
              <span class="rc-lbl">Memory</span>
              <span class="rc-val">{{ ramPercent }}%</span>
            </div>
            <div class="rc-bar"><div class="rc-fill" :class="usageColor(ramPercent)" :style="{ width: ramPercent + '%' }"></div></div>
            <div class="rc-sub">{{ stats?.ramUsed || 0 }}MB / {{ stats?.ramTotal || 0 }}MB</div>
          </div>

          <div class="rc" v-if="stats?.swapTotal > 0">
            <div class="rc-top">
              <span class="rc-lbl">Swap</span>
              <span class="rc-val">{{ swapPercent }}%</span>
            </div>
            <div class="rc-bar"><div class="rc-fill" :class="usageColor(swapPercent)" :style="{ width: swapPercent + '%' }"></div></div>
            <div class="rc-sub">{{ stats?.swapUsed || 0 }}MB / {{ stats?.swapTotal || 0 }}MB</div>
          </div>
        </div>

        <div class="disk-section">
          <div class="section-hdr">STORAGE</div>
          <div class="disk-list">
            <div class="disk-item" v-for="d in stats?.disks" :key="d.mount">
               <div class="disk-top">
                 <div class="disk-name">
                    <svg width="10" height="10" viewBox="0 0 16 16" fill="none" stroke="currentColor"><path d="M2 4v8a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2V4a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2z" stroke-width="1.2"/><path d="M2 8h12M6 8v6M10 8v6" stroke-width="1.2"/></svg>
                    {{ d.mount }}
                 </div>
                 <span class="disk-use-val">{{ d.percent }}</span>
               </div>
               <div class="rc-bar"><div class="rc-fill" :class="usageColor(parseInt(d.percent))" :style="{ width: d.percent }"></div></div>
               <div class="disk-meta">{{ d.used }}MB / {{ d.total }}MB · {{ d.fileSystem }}</div>
            </div>
          </div>
        </div>

        <div class="io-row">
          <div class="io-item">
            <div class="io-lbl">Download</div>
            <div class="io-val" style="color:var(--accent2)">{{ downloadSpeed.split(' ')[0] }}<span style="font-size:10px;color:var(--text3)"> {{ downloadSpeed.split(' ')[1] }}</span></div>
          </div>
          <div class="io-item">
            <div class="io-lbl">Upload</div>
            <div class="io-val" style="color:var(--purple)">{{ uploadSpeed.split(' ')[0] }}<span style="font-size:10px;color:var(--text3)"> {{ uploadSpeed.split(' ')[1] }}</span></div>
          </div>
        </div>
      </div>

      <div v-if="activeTab === 'network'" class="tab-pane">
        <div class="net-stats-summary">
          <div class="stat-box">
             <div class="stat-lbl">RX SPEED</div>
             <div class="stat-val" style="color:var(--accent2)">{{ downloadSpeed }}</div>
          </div>
          <div class="stat-box">
             <div class="stat-lbl">TX SPEED</div>
             <div class="stat-val" style="color:var(--purple)">{{ uploadSpeed }}</div>
          </div>
        </div>

        <div class="net-section">
          <div class="section-hdr">INTERFACES</div>
          <div class="net-list">
            <div class="net-item" v-for="iface in stats?.interfaces" :key="iface.name">
              <div class="net-item-hdr">
                 <div class="net-name">
                   <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M2 13h12M4 13V3m8 10V3M4 6h8M4 9h8"/></svg>
                   {{ iface.name }}
                 </div>
                 <span class="net-status">Online</span>
              </div>
              <div class="net-traffic-row">
                <div class="nt-item">
                  <svg width="8" height="8" viewBox="0 0 16 16" fill="none" stroke="currentColor"><path d="M8 12V4m0 8l-3-3m3 3l3-3" stroke-width="1.5"/></svg>
                  <span class="nt-lbl">RX:</span> <span class="nt-val">{{ iface.rxSpeed || '0 B/s' }}</span>
                </div>
                <div class="nt-item">
                  <svg width="8" height="8" viewBox="0 0 16 16" fill="none" stroke="currentColor"><path d="M8 4v8m0-8l-3 3m3-3l3 3" stroke-width="1.5"/></svg>
                  <span class="nt-lbl">TX:</span> <span class="nt-val">{{ iface.txSpeed || '0 B/s' }}</span>
                </div>
              </div>
              <div class="net-addrs">
                <div class="net-addr" v-for="addr in iface.addresses" :key="addr">{{ addr }}</div>
              </div>
            </div>
          </div>
        </div>

        <div class="net-section">
          <div class="section-hdr">LISTENING PORTS</div>
          <div class="port-grid">
            <div class="port-item" v-for="p in stats?.ports" :key="p.proto + p.port">
              <span class="port-proto" :class="p.proto">{{ p.proto }}</span>
              <span class="port-val">{{ p.port }}</span>
              <span class="port-addr">{{ p.address.split(':').slice(0,-1).join(':') || '*' }}</span>
            </div>
          </div>
        </div>
      </div>

      <div v-if="activeTab === 'processes'" class="tab-pane">
        <div class="proc-table-wrap">
          <table class="proc-table">
            <thead>
              <tr>
                <th style="width:50px">PID</th>
                <th style="width:50px">CPU</th>
                <th style="width:50px">MEM</th>
                <th>COMMAND</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="p in stats?.processes" :key="p.pid">
                <td class="mono">{{ p.pid }}</td>
                <td class="mono pct" :class="usageColor(parseFloat(p.cpu))">{{ p.cpu }}%</td>
                <td class="mono">{{ p.mem }}%</td>
                <td class="cmd" :title="p.command">{{ p.command }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div v-if="activeTab === 'terminal'" class="tab-pane terminal-pane">
        <ServerTerminal :server="srv" />
      </div>

      <div v-if="activeTab === 'apps'" class="tab-pane">
        <div class="apps-list">
          <div class="section-hdr" style="padding: 12px 12px 6px">System Applications</div>
          <div v-for="app in managedApps" :key="app.id">
            <div class="app-item">
              <div class="app-icon" :style="{ background: app.color + '1a', color: app.color }">
                <span v-if="app.id === 'nginx'">NG</span>
                <span v-else-if="app.id === 'redis'">RD</span>
                <span v-else-if="app.id === 'dotnet'">.NET</span>
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
                <button class="btn-close" @click="appLastResults[app.id] = null">&times;</button>
              </div>
              <div class="res-box-body">
                <div v-if="appLastResults[app.id].message" class="res-msg" :class="{ err: !appLastResults[app.id].success }">{{ appLastResults[app.id].message }}</div>
                <pre v-if="appLastResults[app.id].output" class="res-pre">{{ appLastResults[app.id].output }}</pre>
                <pre v-if="appLastResults[app.id].error" class="res-pre err">{{ appLastResults[app.id].error }}</pre>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div v-if="activeTab === 'info'" class="tab-pane">
        <div class="props-list">
          <div class="prop-item">
            <div class="prop-label">Hostname</div>
            <div class="prop-val">{{ sysInfo?.hostname || '...' }}</div>
          </div>
          <div class="prop-item">
            <div class="prop-label">Processor</div>
            <div class="prop-val">{{ sysInfo?.cpu || '...' }}</div>
          </div>
          <div class="prop-item">
            <div class="prop-label">Latency</div>
            <div class="prop-val">{{ stats?.latency || '...' }} ms</div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
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

const ramPercent = computed(() => {
  if (!stats.value?.ramTotal) return 0
  return Math.round((stats.value.ramUsed / stats.value.ramTotal) * 100)
})

const swapPercent = computed(() => {
  if (!stats.value?.swapTotal) return 0
  return Math.round((stats.value.swapUsed / stats.value.swapTotal) * 100)
})

const downloadSpeed = ref('0 B/s')
const uploadSpeed = ref('0 B/s')
let lastNet = { rx: 0, tx: 0, time: 0 }
let lastIfaceNet = {} // { name: { rx, tx, time } }

const usageColor = (val) => {
  if (val > 80) return 'mf-hi'
  if (val > 55) return 'mf-md'
  return 'mf-lo'
}

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
  appLastResults.value[appId] = null
  try {
    const res = await nuxtApp.$apiFetch(`/api/system-apps/${props.srv.id}/install/${appId}`, {
      method: 'POST',
      baseURL: config.public.apiBase,
      headers: auth.authHeaders()
    })
    
    appLastResults.value[appId] = res
    if (res.success) {
      setTimeout(fetchSystemApps, 2000)
    }
  } catch (e) {
    console.error(e)
    appLastResults.value[appId] = { 
      success: false, 
      message: `Failed to trigger ${appId} installation`, 
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

.res-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 8px; padding: 10px 12px; }
.rc { background: var(--bg2); border: 1px solid var(--border); border-radius: 7px; padding: 8px 10px; }
.rc-top { display: flex; align-items: center; justify-content: space-between; margin-bottom: 5px; }
.rc-lbl { font-size: 9px; color: var(--text3); font-weight: 500; text-transform: uppercase; letter-spacing: .5px; }
.rc-val { font-size: 15px; font-weight: 600; font-family: var(--mono); }
.rc-bar { height: 4px; background: var(--bg3); border-radius: 99px; overflow: hidden; }
.rc-fill { height: 100%; border-radius: 99px; transition: width .6s ease; }
.rc-sub { font-size: 9px; color: var(--text3); margin-top: 4px; font-family: var(--mono); }

.mf-lo { background: var(--accent); }
.mf-md { background: var(--warn); }
.mf-hi { background: var(--danger); }

.io-row { display: flex; border-top: 1px solid var(--border); margin-top: auto; }
.io-item { flex: 1; padding: 8px 12px; border-right: 1px solid var(--border); }
.io-item:last-child { border-right: none; }
.io-lbl { font-size: 9px; color: var(--text3); text-transform: uppercase; letter-spacing: .4px; margin-bottom: 3px; }
.io-val { font-size: 14px; font-weight: 600; font-family: var(--mono); }

.disk-section { padding: 0 12px 12px 12px; }
.section-hdr { font-size: 9px; color: var(--text3); font-weight: 600; letter-spacing: 1px; margin-bottom: 8px; }
.disk-list { display: flex; flex-direction: column; gap: 8px; }
.disk-item { background: var(--bg2); border: 1px solid var(--border); border-radius: 6px; padding: 8px 10px; }
.disk-top { display: flex; justify-content: space-between; align-items: center; margin-bottom: 5px; }
.disk-name { font-size: 11px; font-weight: 600; font-family: var(--mono); display: flex; align-items: center; gap: 5px; color: var(--text2); }
.disk-use-val { font-size: 11px; font-weight: 600; font-family: var(--mono); }
.disk-meta { font-size: 9px; color: var(--text3); margin-top: 5px; font-family: var(--mono); }

.props-list { display: flex; flex-direction: column; padding: 10px 12px; }
.prop-item { display: flex; justify-content: space-between; padding: 6px 0; border-bottom: 1px solid var(--border); }
.prop-item:last-child { border-bottom: none; }
.prop-label { font-size: 11px; color: var(--text3); }
.prop-val { font-size: 11px; color: var(--text1); font-family: var(--mono); }

.net-stats-summary { display: flex; gap: 8px; padding: 12px; }
.stat-box { flex: 1; background: var(--bg2); border: 1px solid var(--border); border-radius: 7px; padding: 10px; }
.stat-lbl { font-size: 9px; color: var(--text3); font-weight: 600; letter-spacing: .5px; margin-bottom: 4px; }
.stat-val { font-size: 16px; font-weight: 700; font-family: var(--mono); }

.net-section { padding: 0 12px 16px 12px; }
.net-list { display: flex; flex-direction: column; gap: 8px; }
.net-item { background: var(--bg2); border: 1px solid var(--border); border-radius: 7px; padding: 10px; }
.net-item-hdr { display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px; }
.net-name { font-size: 12px; font-weight: 600; font-family: var(--mono); color: var(--text1); display: flex; align-items: center; gap: 6px; }
.net-status { font-size: 9px; background: rgba(0,201,167,0.1); color: var(--accent); padding: 1px 6px; border-radius: 4px; border: 1px solid rgba(0,201,167,0.2); }
.net-addrs { display: flex; flex-wrap: wrap; gap: 4px; }
.net-traffic-row { display: flex; gap: 12px; margin-bottom: 8px; background: var(--bg3); padding: 4px 8px; border-radius: 4px; border: 1px solid var(--border); }
.nt-item { display: flex; align-items: center; gap: 4px; font-size: 10px; font-family: var(--mono); }
.nt-lbl { color: var(--text3); font-weight: 500; font-size: 9px; }
.nt-val { color: var(--text1); font-weight: 600; }
.net-addr { font-size: 10px; font-family: var(--mono); color: var(--text3); background: var(--bg3); padding: 1px 6px; border-radius: 3px; }

.port-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(100px, 1fr)); gap: 6px; }
.port-item { background: var(--bg2); border: 1px solid var(--border); border-radius: 5px; padding: 6px 8px; display: flex; align-items: center; gap: 6px; }
.port-proto { font-size: 8px; font-weight: 700; text-transform: uppercase; padding: 1px 4px; border-radius: 3px; }
.port-proto.tcp { background: rgba(0,184,212,0.1); color: #00b8d4; }
.port-proto.udp { background: rgba(156,39,176,0.1); color: #9c27b0; }
.port-val { font-size: 12px; font-weight: 700; font-family: var(--mono); color: var(--text1); }
.port-addr { font-size: 9px; color: var(--text3); font-family: var(--mono); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

.proc-table-wrap { padding: 12px; overflow-x: auto; }
.proc-table { width: 100%; border-collapse: collapse; font-size: 11px; }
.proc-table th { text-align: left; padding: 6px 8px; color: var(--text3); font-weight: 600; font-size: 9px; text-transform: uppercase; border-bottom: 1px solid var(--border); }
.proc-table td { padding: 6px 8px; border-bottom: 1px solid var(--border); color: var(--text2); }
.proc-table tr:hover { background: var(--bg2); }
.proc-table .mono { font-family: var(--mono); }
.proc-table .cmd { color: var(--text1); max-width: 200px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; font-family: var(--mono); font-size: 10px; }
.proc-table .pct { background: transparent; }
.proc-table .pct.mf-hi { color: var(--danger); font-weight: 700; }
.proc-table .pct.mf-md { color: var(--warn); font-weight: 700; }
.proc-table .pct.mf-lo { color: var(--accent); font-weight: 600; }

.sdot { width: 6px; height: 6px; border-radius: 50%; flex-shrink: 0; }
.sdot-g { background: var(--success); box-shadow: 0 0 5px var(--success); }
.sdot-r { background: var(--danger); box-shadow: 0 0 5px var(--danger); }
.sdot-x { background: var(--text3); }

.rpill { font-size: 9px; font-weight: 600; letter-spacing: .5px; text-transform: uppercase; padding: 1px 6px; border-radius: 99px; background: rgba(0,201,167,0.12); color: var(--accent); }

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
