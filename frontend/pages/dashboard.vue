<template>
  <div class="topbar">
    <div>
      <div class="topbar-title">Dashboard</div>
      <div class="topbar-sub">Overview of your Cekok environment</div>
    </div>
  </div>

  <div class="content">
    <DashboardStats :apps="apps.length" :servers="servers.length" />

    <div class="grid-2">
      <DashboardApps :apps="apps" @deploy="handleDeploy" />

      <div style="display: flex; flex-direction: column; gap: 16px;">
        <SystemResourcesPanel />
        <EnvChecksPanel />
        <RecentDeployments :deployments="recentDeploys" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
definePageMeta({ middleware: 'auth' })

const config = useRuntimeConfig()
const auth = useAuth()

const { data: appsData } = await useFetch('/api/applications', {
  baseURL: config.public.apiBase,
  headers: auth.authHeaders()
})
const apps = computed(() => (appsData.value as any[]) || [])

const { data: serversData } = await useFetch('/api/servers', {
  baseURL: config.public.apiBase,
  headers: auth.authHeaders()
})
const servers = computed(() => (serversData.value as any[]) || [])

const { data: historyData, refresh: refreshHistory } = await useFetch('/api/deploy/history?size=5', {
  baseURL: config.public.apiBase,
  headers: auth.authHeaders()
})

const recentDeploys = computed(() => {
  const history = (historyData.value as any[]) || []
  return history.map((h: any) => {
    const app = apps.value.find((a: any) => a.id === h.appId)
    return { ...h, appName: app?.name || 'Unknown App' }
  })
})

const handleDeploy = async (id: string) => {
  if (!confirm('Trigger deploy for this app?')) return
  try {
    const res = await useNuxtApp().$apiFetch(`/api/deploy/${id}`, {
      method: 'POST',
      baseURL: config.public.apiBase,
      headers: auth.authHeaders()
    })
    alert('Deploy triggered! Job ID: ' + (res as any).id)
    refreshHistory()
  } catch (e: any) {
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
.grid-2 { display: grid; grid-template-columns: repeat(auto-fit, minmax(450px, 1fr)); gap: 16px; }
</style>
