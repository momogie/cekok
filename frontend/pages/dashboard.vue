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
        <div class="panel-header">
          <div class="panel-title">Applications</div>
        </div>
        <div v-for="app in apps" :key="app.id" class="app-row">
          <div class="app-info">
            <div class="app-name">{{ app.name }}</div>
            <div class="app-meta">{{ app.repoUrl }} • {{ app.branch }}</div>
          </div>
          <button class="btn btn-primary" @click="deploy(app.id)">Deploy</button>
        </div>
        <div v-if="!apps.length" style="padding: 20px; color: var(--text3); font-size: 13px;">No applications found.</div>
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

.grid-2 { display: grid; grid-template-columns: 1fr; gap: 16px; }
.panel {
  background: var(--bg1); border: 1px solid var(--border);
  border-radius: var(--radius); overflow: hidden;
}
.panel-header {
  padding: 14px 18px; border-bottom: 1px solid var(--border); display: flex; align-items: center; gap: 10px;
}
.panel-title { font-size: 13px; font-weight: 600; }

.app-row {
  padding: 14px 18px; border-bottom: 1px solid var(--border);
  display: flex; align-items: center; gap: 12px;
}
.app-info { flex: 1; min-width: 0; }
.app-name { font-size: 13px; font-weight: 500; }
.app-meta { font-size: 11px; color: var(--text3); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
</style>
