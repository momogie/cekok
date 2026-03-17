<template>
  <div class="topbar">
    <div class="topbar-title">Server Manager</div>
    <span class="topbar-sub">{{ servers.length }} servers · 1 region</span>
    <div class="topbar-right">
      <button v-if="auth.isAdmin" class="btn btn-ghost" @click="openForm()">
        <svg width="11" height="11" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
          <line x1="8" y1="2" x2="8" y2="14"/><line x1="2" y1="8" x2="14" y2="8"/>
        </svg>
        Add Server
      </button>
      <button class="btn btn-ghost">
        <svg width="11" height="11" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.8">
          <path d="M1 4h14M1 8h10M1 12h6"/>
        </svg>
        Filter
      </button>
    </div>
  </div>

  <div class="content">
    <!-- Stats Row (Compact) -->
    <div class="stats-row">
      <div class="stat-card">
        <div class="stat-icon" style="background:rgba(0,201,167,0.1)">
          <svg width="13" height="13" viewBox="0 0 16 16" fill="none" stroke="var(--accent)" stroke-width="1.8">
            <rect x="1" y="3" width="14" height="5" rx="1"/><rect x="1" y="10" width="14" height="3" rx="1"/>
          </svg>
        </div>
        <div class="stat-info">
          <div class="stat-label">Online</div>
          <div class="stat-val green">{{ onlineCount }}<span style="font-size:11px;color:var(--text3)"> /{{ servers.length }}</span></div>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon" style="background:rgba(240,80,96,0.1)">
          <svg width="13" height="13" viewBox="0 0 16 16" fill="none" stroke="var(--danger)" stroke-width="1.8">
            <circle cx="8" cy="8" r="6"/><line x1="8" y1="5" x2="8" y2="9"/><circle cx="8" cy="11" r="0.8" fill="var(--danger)" stroke="none"/>
          </svg>
        </div>
        <div class="stat-info">
          <div class="stat-label">Incidents</div>
          <div class="stat-val red">0</div>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon" style="background:rgba(0,151,255,0.1)">
          <svg width="13" height="13" viewBox="0 0 16 16" fill="none" stroke="var(--accent2)" stroke-width="1.8">
            <polyline points="1,13 4,9 7,11 10,6 13,8 15,4"/>
          </svg>
        </div>
        <div class="stat-info">
          <div class="stat-label">Avg CPU</div>
          <div class="stat-val blue">--<span style="font-size:11px">%</span></div>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon" style="background:rgba(139,127,255,0.1)">
          <svg width="13" height="13" viewBox="0 0 16 16" fill="none" stroke="var(--purple)" stroke-width="1.8">
            <rect x="2" y="3" width="12" height="9" rx="1"/><line x1="5" y1="14" x2="11" y2="14"/><line x1="8" y1="12" x2="8" y2="14"/>
          </svg>
        </div>
        <div class="stat-info">
          <div class="stat-label">Avg RAM</div>
          <div class="stat-val" style="color:var(--purple)">--<span style="font-size:11px">%</span></div>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon" style="background:rgba(240,165,0,0.1)">
          <svg width="13" height="13" viewBox="0 0 16 16" fill="none" stroke="var(--warn)" stroke-width="1.8">
            <path d="M13 8A5 5 0 1 1 3.05 5.5"/><path d="M13 1v4h-4"/>
          </svg>
        </div>
        <div class="stat-info">
          <div class="stat-label">Uptime</div>
          <div class="stat-val warn">99.9<span style="font-size:11px">%</span></div>
        </div>
      </div>
    </div>

    <!-- Main Grid Layout -->
    <div class="grid-main">
      <!-- Left: Server List -->
      <div class="panel list-panel">
        <div class="panel-header">
          <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="var(--accent)" stroke-width="1.8">
            <rect x="1" y="2" width="14" height="4" rx="1"/><rect x="1" y="9" width="14" height="4" rx="1"/>
          </svg>
          <span class="panel-title">All Servers</span>
          <span class="panel-sub" v-if="servers.length">sorted by status</span>
        </div>
        
        <div class="server-rows">
          <ServerItem 
            v-for="srv in servers" 
            :key="srv.id" 
            :srv="srv" 
            :is-selected="selectedServer?.id === srv.id"
            @select="selectedServer = srv"
            @edit="openForm"
            @delete="deleteServer"
          />
          <div v-if="!servers?.length" class="empty-state">No servers configured.</div>
        </div>

        <div v-if="auth.isAdmin" class="add-server-row" @click="openForm()">
          <svg width="11" height="11" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2"><line x1="8" y1="2" x2="8" y2="14"/><line x1="2" y1="8" x2="14" y2="8"/></svg>
          Add new server
        </div>
      </div>

      <!-- Right: Server Detail Panel -->
      <div class="panel detail-panel">
        <ServerDetailPanel v-if="selectedServer" :srv="selectedServer" />
        <div v-else class="empty-detail">
          <svg width="32" height="32" viewBox="0 0 16 16" fill="none" stroke="var(--text3)" stroke-width="1" opacity="0.3">
            <rect x="2" y="2" width="12" height="5" rx="1"/><rect x="2" y="9" width="12" height="5" rx="1"/>
          </svg>
          <div style="margin-top:8px;color:var(--text3);font-size:12px;">Select server</div>
        </div>
      </div>
    </div>
  </div>

  <Transition name="fade">
    <ServerForm 
      v-if="showModal" 
      :server="editServer" 
      @close="showModal = false" 
      @saved="refresh()" 
    />
  </Transition>
</template>

<script setup>
definePageMeta({ middleware: 'auth' })
const config = useRuntimeConfig()
const auth = useAuth()

const { data: serversData, refresh } = await useFetch('/api/servers', {
  baseURL: config.public.apiBase,
  headers: auth.authHeaders()
})
const servers = computed(() => serversData.value || [])
const selectedServer = ref(null)

watchEffect(() => {
  if (servers.value.length && !selectedServer.value) {
    selectedServer.value = servers.value[0]
  }
})

const onlineCount = computed(() => servers.value.length) 
const showModal = ref(false)
const editServer = ref(null)

const openForm = (server = null) => {
  editServer.value = server
  showModal.value = true
}

const deleteServer = async (id) => {
  if (!confirm('Delete this server?')) return
  try {
    await useNuxtApp().$apiFetch(`/api/servers/${id}`, {
      method: 'DELETE',
      baseURL: config.public.apiBase,
      headers: auth.authHeaders()
    })
    await refresh()
    if (selectedServer.value?.id === id) selectedServer.value = null
  } catch (e) {
    if (e.response?.status !== 401) alert(e.data?.message || e.message)
  }
}
</script>

<style scoped>
.topbar {
  height: 52px; flex-shrink: 0;
  background: var(--bg1); border-bottom: 1px solid var(--border);
  display: flex; align-items: center; padding: 0 20px; gap: 14px;
}
.topbar-title { font-size: 13px; font-weight: 600; }
.topbar-sub { font-size: 11px; color: var(--text3); margin-left:4px; }
.topbar-right { margin-left: auto; display: flex; align-items: center; gap: 7px; }

.content { flex: 1; overflow-y: auto; padding: 18px; display: flex; flex-direction: column; }

/* Stats Row (Compact) */
.stats-row { display: grid; grid-template-columns: repeat(5, 1fr); gap: 14px; margin-bottom: 18px; flex-shrink: 0; }
.stat-card {
  background: var(--bg1); border: 1px solid var(--border);
  border-radius: var(--radius); padding: 12px 16px;
  display: flex; align-items: center; gap: 12px;
  animation: fadeUp 0.3s ease both;
}
.stat-card:nth-child(n){animation-delay: calc(0.04s * var(--i, 0));}
.stat-card:nth-child(1){--i:1} .stat-card:nth-child(2){--i:2} .stat-card:nth-child(3){--i:3} .stat-card:nth-child(4){--i:4} .stat-card:nth-child(5){--i:5}

.stat-icon { width: 30px; height: 30px; border-radius: 7px; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
.stat-info { min-width: 0; }
.stat-label { font-size: 10px; font-weight: 500; letter-spacing: 0.4px; text-transform: uppercase; color: var(--text3); }
.stat-val { font-size: 20px; font-weight: 600; font-family: var(--mono); letter-spacing: -1px; line-height: 1.2; }
.stat-val.green { color: var(--success); }
.stat-val.red { color: var(--danger); }
.stat-val.blue { color: var(--accent2); }
.stat-val.warn { color: var(--warn); }

/* Grid Layout */
.grid-main { display: grid; grid-template-columns: 1fr 420px; gap: 12px; flex: 1; min-height: 0; }

.panel {
  background: var(--bg1); border: 1px solid var(--border);
  border-radius: var(--radius); display: flex; flex-direction: column; overflow: hidden;
}

.panel-header {
  padding: 9px 13px; border-bottom: 1px solid var(--border);
  display: flex; align-items: center; gap: 8px; flex-shrink: 0;
}
.panel-title { font-size: 12px; font-weight: 600; }
.panel-sub { font-size: 10px; color: var(--text3); margin-left: auto; }

.server-rows { flex: 1; overflow-y: auto; }
.empty-state { padding: 24px; text-align: center; color: var(--text3); font-size: 12px; }

.add-server-row {
  padding: 7px 13px; border-top: 1px solid var(--border);
  display: flex; align-items: center; gap: 7px; color: var(--text3);
  cursor: pointer; font-size: 11px; transition: all var(--transition);
}
.add-server-row:hover { color: var(--accent); background: var(--bg2); }

.empty-detail { flex: 1; display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 32px; text-align: center; }

@keyframes fadeUp {
  from { opacity: 0; transform: translateY(6px); }
  to { opacity: 1; transform: translateY(0); }
}

@media (max-width: 1100px) {
  .stats-row { grid-template-columns: repeat(3, 1fr); }
}
@media (max-width: 900px) {
  .grid-main { grid-template-columns: 1fr; }
  .detail-panel { display: none; }
}
</style>
