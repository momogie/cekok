<template>
  <div class="panel">
    <div class="panel-header">
      <div class="panel-title">System Resources</div>
      <div style="margin-left:auto;">
        <button class="btn btn-ghost" @click="loadSysChecks" style="padding: 4px 10px; font-size: 11px;">Refresh Checks</button>
      </div>
    </div>
    <div style="padding: 16px;">
      <div v-if="sysLoading" style="color:var(--text3); font-size: 13px;">Loading system resources...</div>
      <div v-else-if="sysError" style="color:var(--danger); font-size: 13px;">Failed to load system checks.</div>
      <div v-else>
        <div class="server-info-list">
          <div class="info-item">
            <div class="info-label">Hostname</div>
            <div class="info-value">{{ sysData?.hostname || 'Unknown' }}</div>
          </div>
          <div class="info-item">
            <div class="info-label">Processor</div>
            <div class="info-value">{{ sysData?.cpuName || 'Unknown' }}</div>
            <div class="info-sub">{{ sysData?.vcpuCount }} vCPUs available</div>
          </div>
          <div class="info-item">
            <div class="info-label">System Uptime</div>
            <div class="info-value">{{ sysData?.uptime || 'Unknown' }}</div>
          </div>
          <div class="info-item">
            <div class="info-label">Operating System</div>
            <div class="info-value">{{ sysData?.osVersion || 'Unknown' }}</div>
          </div>
        </div>

        <div class="env-grid mt-4">
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
  </div>
</template>

<script setup lang="ts">
const config = useRuntimeConfig()
const auth = useAuth()

const sysData = ref<any>(null)
const sysLoading = ref(true)
const sysError = ref(false)
let sysTimer: ReturnType<typeof setInterval> | null = null

const loadSysChecks = async () => {
  try {
    const res = await useNuxtApp().$apiFetch('/api/system/resources', {
      baseURL: config.public.apiBase,
      headers: auth.authHeaders()
    })
    sysData.value = res
    sysError.value = false
  } catch {
    sysError.value = true
  } finally {
    sysLoading.value = false
  }
}

onMounted(() => {
  loadSysChecks()
  sysTimer = setInterval(loadSysChecks, 3000)
})

onUnmounted(() => {
  if (sysTimer) clearInterval(sysTimer)
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

.server-info-list {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 16px;
  padding: 16px;
  background: var(--bg2);
  border: 1px solid var(--border);
  border-radius: var(--radius);
}
.info-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.info-label {
  font-size: 11px;
  font-weight: 500;
  color: var(--text3);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}
.info-value {
  font-size: 13px;
  font-weight: 600;
  color: var(--text1);
}
.info-sub {
  font-size: 10px;
  color: var(--text3);
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
.mt-4 { margin-top: 16px; }
</style>
