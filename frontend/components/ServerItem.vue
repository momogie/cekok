<template>
  <div 
    class="server-row" 
    :class="{ 'selected': isSelected }"
    @click="$emit('select', srv)"
  >
    <div class="srv-avatar" :class="'srv-' + roleClass">{{ roleAbbr }}</div>
    
    <div class="srv-info">
      <div class="srv-name">
        <div class="status-dot" :class="pingStatus"></div>
        {{ srv.name }}
        <span class="role-pill" :class="'role-' + roleClass">{{ srv.role }}</span>
      </div>
      <div class="srv-meta">{{ srv.ip }} · {{ srv.tags || 'default' }}</div>
    </div>

    <div class="srv-res">
      <div class="mini-bars">
        <div class="mini-row">
          <div class="mini-label">CPU</div>
          <div class="mini-bar">
            <div class="mini-fill" :class="usageColor(cpuLoad)" :style="{ width: cpuLoad + '%' }"></div>
          </div>
          <div class="mini-pct">{{ cpuLoad }}%</div>
        </div>
        <div class="mini-row">
          <div class="mini-label">RAM</div>
          <div class="mini-bar">
            <div class="mini-fill" :class="usageColor(ramLoad)" :style="{ width: ramLoad + '%' }"></div>
          </div>
          <div class="mini-pct">{{ ramLoad }}%</div>
        </div>
      </div>
    </div>

    <div class="row-actions" v-if="auth.isAdmin">
      <button class="btn-tool" @click.stop="$emit('edit', srv)">
        <svg width="11" height="11" viewBox="0 0 16 16" fill="none" stroke="currentColor"><path d="M11 2a1.5 1.5 0 012.1 2.1L5.5 11.7l-2.5.5.5-2.5L11 2z" stroke-width="1.2" stroke-linecap="round" stroke-linejoin="round"/></svg>
      </button>
      <button class="btn-tool text-danger" @click.stop="$emit('delete', srv.id)">
        <svg width="11" height="11" viewBox="0 0 16 16" fill="none" stroke="currentColor"><path d="M3 4h10M5 4V3a1 1 0 011-1h4a1 1 0 011 1v1m1 0v9a1 1 0 01-1 1H5a1 1 0 01-1-1V4h8z" stroke-width="1.2"/></svg>
      </button>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  srv: { type: Object, required: true },
  isSelected: { type: Boolean, default: false }
})

defineEmits(['select', 'edit', 'delete'])

const config = useRuntimeConfig()
const auth = useAuth()
const nuxtApp = useNuxtApp()

const sysInfo = ref(null)
const pingStatus = ref('dot-gray')

const roleAbbr = computed(() => {
  const r = props.srv.role?.toLowerCase() || ''
  if (r.includes('web')) return 'WB'
  if (r.includes('db') || r.includes('data')) return 'DB'
  if (r.includes('proxy') || r.includes('nginx')) return 'PX'
  if (r.includes('worker')) return 'WK'
  if (r.includes('cache') || r.includes('redis')) return 'CA'
  return props.srv.name.substring(0, 2).toUpperCase()
})

const roleClass = computed(() => {
  const r = props.srv.role?.toLowerCase() || ''
  if (r.includes('web')) return 'web'
  if (r.includes('db') || r.includes('data')) return 'db'
  if (r.includes('proxy') || r.includes('nginx')) return 'proxy'
  if (r.includes('worker')) return 'worker'
  if (r.includes('cache') || r.includes('redis')) return 'cache'
  return 'web'
})

const cpuLoad = computed(() => sysInfo.value?.stats?.cpuUsage?.toFixed(0) || 0)
const ramLoad = computed(() => {
  if (!sysInfo.value?.stats) return 0
  const { ramUsed, ramTotal } = sysInfo.value.stats
  return Math.round((ramUsed / ramTotal) * 100)
})

const usageColor = (val) => {
  if (val > 80) return 'fill-high'
  if (val > 55) return 'fill-mid'
  return 'fill-low'
}

const fetchStats = async () => {
  try {
    const data = await nuxtApp.$apiFetch(`/api/servers/${props.srv.id}/sys-info`, {
      baseURL: config.public.apiBase,
      headers: auth.authHeaders()
    })
    sysInfo.value = data
    pingStatus.value = 'dot-green'
  } catch (e) {
    pingStatus.value = 'dot-red'
  }
}

onMounted(() => {
  fetchStats()
  const timer = setInterval(fetchStats, 30000)
  onUnmounted(() => clearInterval(timer))
})
</script>

<style scoped>
.server-row {
  padding: 7px 13px; border-bottom: 1px solid var(--border);
  display: flex; align-items: center; gap: 10px; cursor: pointer;
  transition: all var(--transition); position: relative;
}
.server-row:last-child { border-bottom: none; }
.server-row:hover { background: var(--bg2); }
.server-row.selected { background: rgba(0, 201, 167, 0.05); border-left: 2px solid var(--accent); }

.srv-avatar {
  width: 28px; height: 28px; border-radius: 6px; flex-shrink: 0;
  display: flex; align-items: center; justify-content: center;
  font-size: 9px; font-weight: 700; font-family: var(--mono);
}
.srv-web    { background: rgba(0, 201, 167, 0.12); color: var(--accent); }
.srv-db     { background: rgba(240, 165, 0, 0.12); color: var(--warn); }
.srv-proxy  { background: rgba(0, 151, 255, 0.12); color: var(--accent2); }
.srv-worker { background: rgba(139, 127, 255, 0.12); color: var(--purple); }
.srv-cache  { background: rgba(240, 80, 96, 0.10); color: var(--danger); }

.srv-info { flex: 1; min-width: 0; }
.srv-name { font-size: 12px; font-weight: 500; display: flex; align-items: center; gap: 5px; }
.srv-meta { font-size: 10px; color: var(--text3); margin-top: 1px; font-family: var(--mono); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

.status-dot { width: 6px; height: 6px; border-radius: 50%; flex-shrink: 0; }
.dot-green { background: var(--success); box-shadow: 0 0 5px var(--success); }
.dot-red { background: var(--danger); box-shadow: 0 0 5px var(--danger); }
.dot-gray { background: var(--text3); }

.role-pill { 
  font-size: 9px; font-weight: 600; letter-spacing: 0.5px; text-transform: uppercase; 
  padding: 1px 6px; border-radius: 99px; flex-shrink: 0; 
}
.role-web    { background: rgba(0, 201, 167, 0.12); color: var(--accent); }
.role-db     { background: rgba(240, 165, 0, 0.12); color: var(--warn); }
.role-proxy  { background: rgba(0, 151, 255, 0.12); color: var(--accent2); }
.role-worker { background: rgba(139, 127, 255, 0.12); color: var(--purple); }
.role-cache  { background: rgba(240, 80, 96, 0.10); color: var(--danger); }

.srv-res { display: flex; align-items: center; gap: 6px; flex-shrink: 0; }
.mini-bars { display: flex; flex-direction: column; gap: 3px; }
.mini-row { display: flex; align-items: center; gap: 4px; }
.mini-label { font-size: 9px; color: var(--text3); width: 16px; }
.mini-bar { width: 48px; height: 3px; background: var(--bg3); border-radius: 99px; overflow: hidden; }
.mini-fill { height: 100%; border-radius: 99px; transition: width 0.3s ease; }
.fill-low  { background: var(--accent); }
.fill-mid  { background: var(--warn); }
.fill-high { background: var(--danger); }
.mini-pct { font-size: 9px; font-family: var(--mono); color: var(--text3); width: 26px; text-align: right; }

.row-actions { 
  display: flex; 
  align-items: center; 
  gap: 4px; 
  width: 0;
  opacity: 0;
  overflow: hidden;
  transition: all 0.2s ease-in-out;
  flex-shrink: 0;
}
.server-row:hover .row-actions, .server-row.selected .row-actions { 
  width: 46px; /* Approximate width for two small buttons */
  opacity: 1;
  margin-left: 8px;
}
.btn-tool { background: none; border: none; color: var(--text3); cursor: pointer; padding: 4px; border-radius: 4px; flex-shrink: 0; }
.btn-tool:hover { color: var(--text1); background: var(--bg2); }
</style>
