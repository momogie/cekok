<template>
  <div class="topbar">
    <div>
      <div class="topbar-title">Servers</div>
      <div class="topbar-sub">Manage deploy targets</div>
    </div>
  </div>

  <div class="content">
    <div class="servers-grid">
      <div v-for="srv in servers" :key="srv.id" class="server-card">
        <div class="server-card-header">
          <div class="status-dot dot-green" style="width:8px;height:8px;flex-shrink:0"></div>
          <div class="server-card-name">{{ srv.name }}</div>
          <div class="server-card-ip">{{ srv.ip }}</div>
          <span class="role-pill">{{ srv.role }}</span>
        </div>
        <div style="font-size:11px; margin-top:8px">
          SSH: {{ srv.sshUser }}@{{ srv.ip }}:{{ srv.sshPort }}
        </div>
      </div>
      <div v-if="!servers?.length" style="padding: 20px; color: var(--text3); font-size: 13px;">No servers configured.</div>
    </div>
  </div>
</template>

<script setup>
definePageMeta({ middleware: 'auth' })
const config = useRuntimeConfig()
const auth = useAuth()

const { data: serversData } = await useFetch('/api/servers', {
  baseURL: config.public.apiBase,
  headers: auth.authHeaders()
})
const servers = computed(() => serversData.value || [])
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
.servers-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 14px; }
.server-card {
  background: var(--bg1); border: 1px solid var(--border);
  border-radius: var(--radius); padding: 16px 18px; transition: all 0.2s;
}
.server-card-header { display: flex; align-items: center; gap: 10px; margin-bottom: 12px; }
.server-card-ip { font-family: var(--mono); font-size: 11px; color: var(--text3); background: var(--bg3); padding: 2px 7px; border-radius: 4px; }
.server-card-name { font-size: 13px; font-weight: 600; flex: 1; }

.status-dot { width: 8px; height: 8px; border-radius: 50%; box-shadow: 0 0 6px currentcolor; }
.dot-green { background: var(--success); color: var(--success); }

.role-pill {
  font-size: 9px; font-weight: 600; letter-spacing: 0.6px; text-transform: uppercase;
  padding: 2px 7px; border-radius: 99px; flex-shrink: 0;
  background: rgba(0,201,167,0.12); color: var(--accent);
}
</style>
