<template>
  <div class="topbar">
    <div>
      <div class="topbar-title">Dashboard</div>
      <div class="topbar-sub">Overview of your Cekok environment</div>
    </div>
  </div>

  <div class="content">
    <div class="stats-row">
      <div class="stat-card">
        <div class="stat-label">Total apps</div>
        <div class="stat-val">{{ apps.length }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">Servers</div>
        <div class="stat-val green">{{ servers.length }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">Jobs this week</div>
        <div class="stat-val warn">-</div>
      </div>
    </div>

    <div class="grid-2">
      <div class="panel">
        <div class="panel-header" style="border-bottom: none">
          <div class="panel-title">Applications</div>
        </div>
        <div class="app-grid">
          <div v-for="app in apps" :key="app.id" class="app-card-new">
            <div class="app-card-top">
              <div class="app-name">{{ app.name }}</div>
              <div class="badge-type">{{ app.type || 'App' }}</div>
            </div>
            <div class="app-card-mid">
              <div class="app-meta-line" title="Repository">
                <svg viewBox="0 0 24 24" width="13" height="13" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round" stroke-linejoin="round" style="flex-shrink: 0"><path d="M15 22v-4a4.8 4.8 0 0 0-1-3.5c3 0 6-2 6-5.5.08-1.25-.27-2.48-1-3.5.28-1.15.28-2.35 0-3.5 0 0-1 0-3 1.5-2.64-.5-5.36-.5-8 0C6 2 5 2 5 2c-.3 1.15-.3 2.35 0 3.5A5.403 5.403 0 0 0 4 9c0 3.5 3 5.5 6 5.5-.39.49-.68 1.05-.85 1.65-.17.6-.22 1.23-.15 1.85v4"/><path d="M9 18c-4.51 2-5-2-7-2"/></svg>
                <span>{{ app.repoUrl }}</span>
              </div>
              <div class="app-meta-line" title="Branch">
                <svg viewBox="0 0 24 24" width="13" height="13" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round" stroke-linejoin="round" style="flex-shrink: 0"><line x1="6" y1="3" x2="6" y2="15"/><circle cx="18" cy="6" r="3"/><circle cx="6" cy="18" r="3"/><path d="M18 9a9 9 0 0 1-9 9"/></svg>
                <span>{{ app.branch }}</span>
              </div>
            </div>
            <div class="app-card-bot">
              <button class="btn btn-primary" @click="deploy(app.id)" style="width: 100%; justify-content: center; padding: 6px 0;">Deploy</button>
            </div>
          </div>
          <div v-if="!apps.length" style="padding: 20px; color: var(--text3); font-size: 13px; grid-column: 1/-1; text-align: center;">No applications found.</div>
        </div>
      </div>
      <div style="display: flex; flex-direction: column; gap: 16px;">
        <div class="panel">
          <div class="panel-header">
            <div class="panel-title">System Resources</div>
            <div style="margin-left:auto;">
              <button class="btn btn-secondary" @click="loadSysChecks" style="padding: 4px 10px; font-size: 11px;">Refresh Checks</button>
            </div>
          </div>
          <div style="padding: 16px;">
            <div v-if="sysLoading" style="color:var(--text3); font-size: 13px;">Loading system resources...</div>
            <div v-else-if="sysError" style="color:var(--danger); font-size: 13px;">Failed to load system checks.</div>
            <div v-else class="env-grid">
              <div class="env-card">
                <div class="env-card-top">
                  <div class="env-name">CPU Usage</div>
                  <div class="env-status" :class="(sysData?.cpuUsage || 0) > 80 ? 'danger' : 'success'">
                    {{ sysData?.cpuUsage || 0 }}%
                  </div>
                </div>
                <div class="env-version">System CPU Load</div>
                <div class="progress-bar-bg mt-1">
                  <div class="progress-bar-fill" :style="{ width: `${Math.min(sysData?.cpuUsage || 0, 100)}%`, backgroundColor: (sysData?.cpuUsage || 0) > 80 ? 'var(--danger)' : 'var(--success)' }"></div>
                </div>
              </div>

              <div class="env-card">
                <div class="env-card-top">
                  <div class="env-name">RAM Usage</div>
                  <div class="env-status" :class="((sysData?.ramUsed || 0) / (sysData?.ramTotal || 1)) > 0.8 ? 'danger' : 'success'">
                    {{ sysData?.ramUsed || 0 }} GB
                  </div>
                </div>
                <div class="env-version">of {{ sysData?.ramTotal || 0 }} GB Total</div>
                <div class="progress-bar-bg mt-1">
                  <div class="progress-bar-fill" :style="{ width: `${Math.min(((sysData?.ramUsed || 0) / (sysData?.ramTotal || 1)) * 100, 100)}%`, backgroundColor: ((sysData?.ramUsed || 0) / (sysData?.ramTotal || 1)) > 0.8 ? 'var(--danger)' : 'var(--success)' }"></div>
                </div>
              </div>

              <div class="env-card">
                <div class="env-card-top">
                  <div class="env-name">Disk Usage</div>
                  <div class="env-status" :class="((sysData?.diskUsed || 0) / (sysData?.diskTotal || 1)) > 0.8 ? 'danger' : 'success'">
                    {{ sysData?.diskUsed || 0 }} GB
                  </div>
                </div>
                <div class="env-version">of {{ sysData?.diskTotal || 0 }} GB Total</div>
                <div class="progress-bar-bg mt-1">
                  <div class="progress-bar-fill" :style="{ width: `${Math.min(((sysData?.diskUsed || 0) / (sysData?.diskTotal || 1)) * 100, 100)}%`, backgroundColor: ((sysData?.diskUsed || 0) / (sysData?.diskTotal || 1)) > 0.8 ? 'var(--danger)' : 'var(--success)' }"></div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="panel">
          <div class="panel-header">
            <div class="panel-title">Local Environment</div>
          </div>
          <div style="padding: 16px;">
          <div v-if="envLoading" style="color:var(--text3); font-size: 13px;">Checking environment dependencies...</div>
          <div v-else-if="envError" style="color:var(--danger); font-size: 13px;">Failed to load environment checks.</div>
          <div v-else class="env-grid">
            <div class="env-card">
              <div class="env-card-top">
                <div class="env-name">Git</div>
                <div class="env-status" :class="envData?.git?.installed ? 'success' : 'danger'">
                  {{ envData?.git?.installed ? 'Installed' : 'Missing' }}
                </div>
              </div>
              <div class="env-version">{{ envData?.git?.version || 'Not detected' }}</div>
            </div>
            
            <div class="env-card">
              <div class="env-card-top">
                <div class="env-name">Node.js</div>
                <div class="env-status" :class="envData?.node?.installed ? 'success' : 'danger'">
                  {{ envData?.node?.installed ? 'Installed' : 'Missing' }}
                </div>
              </div>
              <div class="env-version">{{ envData?.node?.version || 'Not detected' }}</div>
            </div>

            <div class="env-card">
              <div class="env-card-top">
                <div class="env-name">.NET 8.0 SDK</div>
                <div class="env-status" :class="envData?.dotnet?.hasSdk8 ? 'success' : 'danger'">
                  {{ envData?.dotnet?.hasSdk8 ? 'Installed' : 'Missing' }}
                </div>
              </div>
              <div class="env-version" v-if="envData?.dotnet?.hasSdk8">Detected 8.0.x</div>
              <div class="env-version" v-else>Not Detected</div>
            </div>

            <div class="env-card">
              <div class="env-card-top">
                <div class="env-name">.NET 10.0 SDK</div>
                <div class="env-status" :class="envData?.dotnet?.hasSdk10 ? 'success' : 'danger'">
                  {{ envData?.dotnet?.hasSdk10 ? 'Installed' : 'Missing' }}
                </div>
              </div>
              <div class="env-version" v-if="envData?.dotnet?.hasSdk10">Detected 10.0.x</div>
              <div class="env-version" v-else>Not Detected</div>
            </div>

            <div class="env-card">
              <div class="env-card-top">
                <div class="env-name">SSH Client</div>
                <div class="env-status" :class="envData?.ssh?.installed ? 'success' : 'danger'">
                  {{ envData?.ssh?.installed ? 'Installed' : 'Missing' }}
                </div>
              </div>
              <div class="env-version">{{ envData?.ssh?.version || 'Not detected' }}</div>
            </div>
          </div>
        </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
definePageMeta({ middleware: 'auth' })
const config = useRuntimeConfig()
const auth = useAuth()

const { data: appsData, refresh: refreshApps } = await useFetch('/api/applications', {
  baseURL: config.public.apiBase,
  headers: auth.authHeaders()
})
const apps = computed(() => appsData.value || [])

const { data: serversData } = await useFetch('/api/servers', {
  baseURL: config.public.apiBase,
  headers: auth.authHeaders()
})
const servers = computed(() => serversData.value || [])

const deploy = async (id) => {
  if (!confirm('Trigger deploy for this app?')) return
  try {
    const res = await useNuxtApp().$apiFetch(`/api/deploy/${id}`, {
      method: 'POST',
      baseURL: config.public.apiBase,
      headers: auth.authHeaders()
    })
    alert('Deploy triggered! Job ID: ' + res.id)
  } catch (e) {
    if (e.response?.status !== 401) {
      alert('Failed: ' + e.message)
    }
  }
}

const envData = ref(null)
const envLoading = ref(true)
const envError = ref(false)

const sysData = ref(null)
const sysLoading = ref(true)
const sysError = ref(false)
let sysTimer = null

const loadEnvChecks = async () => {
  envLoading.value = true
  envError.value = false
  try {
    const res = await useNuxtApp().$apiFetch('/api/system/env-checks', {
      baseURL: config.public.apiBase,
      headers: auth.authHeaders()
    })
    envData.value = res
  } catch (e) {
    envError.value = true
  } finally {
    envLoading.value = false
  }
}

const loadSysChecks = async () => {
  try {
    const res = await useNuxtApp().$apiFetch('/api/system/resources', {
      baseURL: config.public.apiBase,
      headers: auth.authHeaders()
    })
    sysData.value = res
    sysError.value = false
  } catch (e) {
    sysError.value = true
  } finally {
    sysLoading.value = false
  }
}

onMounted(() => {
  loadEnvChecks()
  loadSysChecks()
  sysTimer = setInterval(loadSysChecks, 3000)
})

onUnmounted(() => {
  if (sysTimer) clearInterval(sysTimer)
})

</script>

<style scoped>
.topbar {
  height: 54px; flex-shrink: 0;
  background: var(--bg1); border-bottom: 1px solid var(--border);
  display: flex; align-items: center; padding: 0 24px; gap: 16px;
}
.topbar-title { font-size: 15px; font-weight: 600; }
.topbar-sub { font-size: 12px; color: var(--text3); }

.content { flex: 1; overflow-y: auto; padding: 24px; }
.stats-row { display: grid; grid-template-columns: repeat(3, 1fr); gap: 14px; margin-bottom: 24px; }
.stat-card {
  background: var(--bg1); border: 1px solid var(--border);
  border-radius: var(--radius); padding: 16px 18px;
}
.stat-label { font-size: 11px; font-weight: 500; text-transform: uppercase; color: var(--text3); margin-bottom: 6px; }
.stat-val { font-size: 24px; font-weight: 600; font-family: var(--mono); }
.green { color: var(--success); }
.warn { color: var(--warn); }
.red { color: var(--danger); }

.grid-2 { display: grid; grid-template-columns: repeat(auto-fit, minmax(450px, 1fr)); gap: 16px; }
.panel {
  background: var(--bg1); border: 1px solid var(--border);
  border-radius: var(--radius); overflow: hidden;
}
.panel-header {
  padding: 14px 18px; border-bottom: 1px solid var(--border); display: flex; align-items: center; gap: 10px;
}
.panel-title { font-size: 13px; font-weight: 600; }

.app-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 16px;
  padding: 0 16px 16px 16px;
}
.app-card-new {
  background: var(--bg1);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 14px;
  transition: all 0.2s;
}
.app-card-new:hover {
  border-color: var(--primary);
  box-shadow: 0 4px 12px rgba(0,0,0,0.1);
  transform: translateY(-2px);
}
.app-card-top {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 8px;
}
.app-name { 
  font-size: 14px; 
  font-weight: 600; 
  color: var(--text1);
  word-break: break-all;
}
.badge-type {
  font-size: 10px;
  font-weight: 600;
  text-transform: uppercase;
  background: var(--bg2);
  border: 1px solid var(--border);
  padding: 3px 6px;
  border-radius: 4px;
  color: var(--text3);
}
.app-card-mid {
  display: flex;
  flex-direction: column;
  gap: 8px;
  flex: 1;
}
.app-meta-line {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: var(--text3);
}
.app-meta-line span {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.app-card-bot {
  margin-top: auto;
}

.env-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 12px;
}
.env-card {
  background: var(--bg1);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  padding: 14px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  transition: border-color 0.2s;
}
.env-card:hover {
  border-color: var(--primary);
}
.env-card-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.env-name {
  font-size: 13px;
  font-weight: 600;
  color: var(--text1);
}
.env-status {
  font-size: 10px;
  font-weight: 600;
  text-transform: uppercase;
  padding: 2px 6px;
  border-radius: 4px;
}
.env-status.success {
  background: rgba(46, 160, 67, 0.1);
  color: var(--success);
}
.env-status.danger {
  background: rgba(248, 81, 73, 0.1);
  color: var(--danger);
}
.env-version {
  font-size: 11px;
  color: var(--text3);
  font-family: var(--mono);
  word-break: break-all;
}
.progress-bar-bg {
  width: 100%;
  height: 6px;
  background: var(--bg3);
  border-radius: 3px;
  overflow: hidden;
  margin-top: auto;
}
.progress-bar-fill {
  height: 100%;
  transition: width 0.5s ease-in-out, background-color 0.3s ease;
  border-radius: 3px;
}
.mt-1 { margin-top: 4px; }
</style>
