<template>
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
</template>

<script setup lang="ts">
const config = useRuntimeConfig()
const auth = useAuth()

const envData = ref<any>(null)
const envLoading = ref(true)
const envError = ref(false)

onMounted(async () => {
  envLoading.value = true
  try {
    const res = await useNuxtApp().$apiFetch('/api/system/env-checks', {
      baseURL: config.public.apiBase,
      headers: auth.authHeaders()
    })
    envData.value = res
  } catch {
    envError.value = true
  } finally {
    envLoading.value = false
  }
})
</script>

<style scoped>
.panel {
  background: var(--bg1); border: 1px solid var(--border);
  border-radius: var(--radius); overflow: hidden;
}
.panel-header {
  padding: 14px 18px; border-bottom: 1px solid var(--border); display: flex; align-items: center; gap: 10px;
}
.panel-title { font-size: 13px; font-weight: 600; }
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
</style>
