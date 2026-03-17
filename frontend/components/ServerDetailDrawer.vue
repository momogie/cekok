<template>
  <Transition name="fade">
    <div v-if="modelValue" class="drawer-overlay" @click.self="$emit('update:modelValue', false)">
      <Transition name="slide">
        <div v-if="modelValue" class="drawer">
          <div class="drawer-header">
            <div class="drawer-title">
              <div class="status-dot" :class="pingStatus"></div>
              <span>{{ srv.name }}</span>
            </div>
            <button class="close-btn" @click="$emit('update:modelValue', false)">&times;</button>
          </div>
          
          <div class="drawer-content">
            <!-- Resource Usage Charts -->
            <div class="drawer-section">
              <div class="section-title">Resource Usage (Real-time)</div>
              <div class="charts-container">
                <!-- CPU Bar -->
                <div class="chart-item">
                  <div class="chart-label">
                    <span>CPU Usage</span>
                    <span class="chart-value">{{ stats?.cpuUsage?.toFixed(1) || 0 }}%</span>
                  </div>
                  <div class="progress-bar">
                    <div class="progress-fill" :style="{ width: (stats?.cpuUsage || 0) + '%', background: getBarColor(stats?.cpuUsage) }"></div>
                  </div>
                </div>

                <!-- RAM Bar -->
                <div class="chart-item">
                  <div class="chart-label">
                    <span>RAM Usage ({{ stats?.ramUsed || 0 }}MB / {{ stats?.ramTotal || 0 }}MB)</span>
                    <span class="chart-value">{{ ramPercent }}%</span>
                  </div>
                  <div class="progress-bar">
                    <div class="progress-fill" :style="{ width: ramPercent + '%', background: getBarColor(ramPercent) }"></div>
                  </div>
                </div>

                <!-- Disk Bar -->
                <div class="chart-item">
                  <div class="chart-label">
                    <span>Disk Usage ({{ formatMB(stats?.diskUsed) }} / {{ formatMB(stats?.diskTotal) }})</span>
                    <span class="chart-value">{{ diskPercent }}%</span>
                  </div>
                  <div class="progress-bar">
                    <div class="progress-fill" :style="{ width: diskPercent + '%', background: getBarColor(diskPercent) }"></div>
                  </div>
                </div>

                <!-- Network Grid -->
                <div class="net-info">
                  <div class="net-item">
                    <div class="net-label">
                      <svg width="12" height="12" viewBox="0 0 16 16" fill="none"><path d="M8 3v10M3 8l3 3 7-7" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" transform="rotate(180 8 8)"/></svg>
                      Download
                    </div>
                    <div class="net-val">{{ downloadSpeed }}</div>
                  </div>
                  <div class="net-item">
                    <div class="net-label">
                      <svg width="12" height="12" viewBox="0 0 16 16" fill="none"><path d="M8 3v10M3 8l3 3 7-7" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" transform="rotate(0 8 8)"/></svg>
                      Upload
                    </div>
                    <div class="net-val">{{ uploadSpeed }}</div>
                  </div>
                </div>
              </div>
            </div>

            <div class="drawer-section">
              <div class="section-title">System Information</div>
              <div class="sys-info-grid">
                <div class="sys-item">
                  <div class="sys-label">Hostname</div>
                  <div class="sys-val">{{ currentSysInfo?.hostname || '...' }}</div>
                </div>
                <div class="sys-item">
                  <div class="sys-label">CPU Info</div>
                  <div class="sys-val">{{ currentSysInfo?.cpu || '...' }}</div>
                </div>
                <div class="sys-item">
                  <div class="sys-label">Uptime</div>
                  <div class="sys-val">{{ currentSysInfo?.uptime || '...' }}</div>
                </div>
                <div class="sys-item">
                  <div class="sys-label">Latency</div>
                  <div class="sys-val">{{ pingMs !== null ? pingMs + 'ms' : '...' }}</div>
                </div>
              </div>
            </div>

            <!-- Inline Terminal -->
            <div class="drawer-section terminal-section">
              <div class="section-title">Terminal Access</div>
              <div class="terminal-wrapper">
                <ServerTerminal :server="srv" />
              </div>
            </div>
          </div>
        </div>
      </Transition>
    </div>
  </Transition>
</template>

<script setup>
const props = defineProps({
  modelValue: { type: Boolean, default: false },
  srv: { type: Object, required: true },
  sysInfo: { type: Object, default: null },
  pingMs: { type: [Number, String], default: null },
  pingStatus: { type: String, default: 'dot-gray' }
})
defineEmits(['update:modelValue'])

const config = useRuntimeConfig()
const auth = useAuth()
const nuxtApp = useNuxtApp()

const currentSysInfo = ref(props.sysInfo)
const stats = computed(() => currentSysInfo.value?.stats)

const ramPercent = computed(() => {
  if (!stats.value?.ramTotal) return 0
  return Math.round((stats.value.ramUsed / stats.value.ramTotal) * 100)
})

const diskPercent = computed(() => {
  if (!stats.value?.diskTotal) return 0
  return Math.round((stats.value.diskUsed / stats.value.diskTotal) * 100)
})

const downloadSpeed = ref('0 B/s')
const uploadSpeed = ref('0 B/s')
let lastNet = { rx: 0, tx: 0, time: 0 }

const formatMB = (mb) => {
  if (!mb) return '0GB'
  if (mb > 1024) return (mb / 1024).toFixed(1) + 'GB'
  return mb + 'MB'
}

const formatBytes = (bytes) => {
  if (bytes === 0) return '0 B/s'
  const k = 1024
  const sizes = ['B/s', 'KB/s', 'MB/s', 'GB/s']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i]
}

const getBarColor = (percent) => {
  if (percent > 90) return 'var(--danger)'
  if (percent > 70) return 'var(--warn)'
  return 'var(--accent)'
}

let pollTimer = null

const fetchRealtime = async () => {
  if (!props.modelValue) return
  try {
    const data = await nuxtApp.$apiFetch(`/api/servers/${props.srv.id}/sys-info`, {
      baseURL: config.public.apiBase,
      headers: auth.authHeaders()
    })
    
    // Calculate network speed
    if (lastNet.time > 0 && data.stats) {
      const now = Date.now()
      const diffSec = (now - lastNet.time) / 1000
      const rxSpeed = (data.stats.netRx - lastNet.rx) / diffSec
      const txSpeed = (data.stats.netTx - lastNet.tx) / diffSec
      downloadSpeed.value = formatBytes(Math.max(0, rxSpeed))
      uploadSpeed.value = formatBytes(Math.max(0, txSpeed))
    }
    
    if (data.stats) {
      lastNet = { rx: data.stats.netRx, tx: data.stats.netTx, time: Date.now() }
    }

    currentSysInfo.value = data
  } catch (e) {
    console.error('Realtime fetch error:', e)
  }
}

watch(() => props.modelValue, (val) => {
  if (val) {
    fetchRealtime()
    pollTimer = setInterval(fetchRealtime, 3000)
  } else {
    clearInterval(pollTimer)
    lastNet = { rx: 0, tx: 0, time: 0 }
  }
})

onUnmounted(() => {
  clearInterval(pollTimer)
})
</script>

<style scoped>
.drawer-overlay {
  position: fixed; inset: 0; z-index: 1000;
  background: rgba(0,0,0,0.4); backdrop-filter: blur(2px);
  display: flex; justify-content: flex-end;
}
.drawer {
  width: 100%; max-width: 700px; height: 100%;
  background: var(--bg1); border-left: 1px solid var(--border);
  display: flex; flex-direction: column; box-shadow: -10px 0 30px rgba(0,0,0,0.5);
}
.drawer-header {
  padding: 16px 20px; border-bottom: 1px solid var(--border);
  display: flex; align-items: center; justify-content: space-between;
  background: var(--bg2);
}
.drawer-title { display: flex; align-items: center; gap: 12px; font-weight: 600; font-size: 15px; }

.drawer-content { flex: 1; overflow-y: auto; padding: 24px; display: flex; flex-direction: column; gap: 24px; }
.drawer-section { display: flex; flex-direction: column; gap: 12px; }
.section-title { font-size: 11px; font-weight: 700; text-transform: uppercase; color: var(--text3); letter-spacing: 0.5px; }

/* Charts */
.charts-container { display: flex; flex-direction: column; gap: 16px; background: var(--bg2); padding: 20px; border-radius: 12px; border: 1px solid var(--border); }
.chart-item { display: flex; flex-direction: column; gap: 10px; }
.chart-label { display: flex; justify-content: space-between; font-size: 12px; color: var(--text2); font-weight: 500; }
.chart-value { font-family: var(--mono); color: var(--text1); }

.progress-bar { height: 6px; background: var(--bg3); border-radius: 99px; overflow: hidden; }
.progress-fill { height: 100%; border-radius: 99px; transition: width 0.5s ease-in-out, background 0.3s; }

/* Net Info */
.net-info { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-top: 4px; padding-top: 16px; border-top: 1px solid var(--border); }
.net-item { display: flex; flex-direction: column; gap: 4px; }
.net-label { font-size: 10px; color: var(--text3); text-transform: uppercase; font-weight: 700; display: flex; align-items: center; gap: 4px; }
.net-val { font-family: var(--mono); font-size: 13px; color: var(--accent); font-weight: 600; }

.sys-info-grid {
  display: grid; grid-template-columns: 1fr 1fr; gap: 8px;
  background: var(--bg2); border-radius: 8px; padding: 12px;
}
.sys-item { display: flex; flex-direction: column; gap: 2px; }
.sys-label { font-size: 9px; text-transform: uppercase; color: var(--text3); font-weight: 600; }
.sys-val { font-size: 11px; color: var(--text2); font-family: var(--mono); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

.terminal-section { flex: 1; display: flex; flex-direction: column; min-height: 400px; }
.terminal-wrapper { flex: 1; min-height: 0; }

.close-btn { 
  background: none; border: none; color: var(--text3); font-size: 24px; cursor: pointer;
  line-height:1; padding: 4px;
}
.close-btn:hover { color: var(--text1); }

.status-dot { width: 8px; height: 8px; border-radius: 50%; }
.dot-green { background: var(--success); box-shadow: 0 0 6px var(--success); }
.dot-red { background: var(--danger); box-shadow: 0 0 6px var(--danger); }
.dot-gray { background: var(--text3); }
</style>
