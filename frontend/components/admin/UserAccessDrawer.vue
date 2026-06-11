<template>
  <div v-if="show" class="drawer-overlay" @click.self="$emit('update:show', false)">
    <div class="drawer wide">
      <div class="drawer-header">
        <div class="drawer-title">Server Access: {{ user?.displayName }}</div>
        <button class="btn-icon" @click="$emit('update:show', false)">
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 3l10 10M3 13L13 3"/></svg>
        </button>
      </div>
      <div class="drawer-content">
        <div class="access-list">
          <table class="access-table">
            <thead>
              <tr>
                <th>Server</th>
                <th class="center">Can Deploy</th>
                <th class="center">Can Manage</th>
                <th class="center">Action</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="server in servers" :key="server.id">
                <td>
                  <div class="server-info">
                    <span class="server-name">{{ server.name }}</span>
                    <span class="server-ip">{{ server.ip }}</span>
                  </div>
                </td>
                <td class="center">
                  <input type="checkbox" v-model="getServerAccess(server.id).canDeploy" />
                </td>
                <td class="center">
                  <input type="checkbox" v-model="getServerAccess(server.id).canManage" />
                </td>
                <td class="center">
                  <button class="btn btn-sm btn-ghost" v-if="isAccessGranted(server.id)" @click="revokeAccess(server.id)">Revoke</button>
                  <span v-else class="status-dim">Not Granted</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
      <div class="drawer-footer">
        <button class="btn btn-ghost" @click="$emit('update:show', false)">Cancel</button>
        <button class="btn btn-primary" :disabled="savingAccess" @click="$emit('save-access')">
          {{ savingAccess ? 'Saving...' : 'Update Permissions' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
const props = defineProps<{
  show: boolean
  user: any
  servers: any[]
  savingAccess: boolean
}>()

const accesses = defineModel<any[]>('accesses', { required: true })

defineEmits<{
  'update:show': [value: boolean]
  'save-access': []
}>()

const getServerAccess = (serverId: string) => {
  let access = accesses.value.find(a => a.serverId === serverId)
  if (!access) {
    access = { serverId, canDeploy: false, canManage: false, isNew: true }
    accesses.value.push(access)
  }
  return access
}

const isAccessGranted = (serverId: string) => {
  const access = accesses.value.find(a => a.serverId === serverId)
  return access && !access.isNew
}

const revokeAccess = (serverId: string) => {
  const idx = accesses.value.findIndex(a => a.serverId === serverId)
  if (idx !== -1) {
    accesses.value[idx].canDeploy = false
    accesses.value[idx].canManage = false
  }
}
</script>

<style scoped>
.drawer-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,0.5); backdrop-filter: blur(2px);
  display: flex; justify-content: flex-end; z-index: 100;
}
.drawer {
  width: 400px; background: var(--bg1); height: 100%;
  display: flex; flex-direction: column; box-shadow: -4px 0 24px rgba(0,0,0,0.2);
  animation: slideIn 0.3s ease-out;
}
.drawer.wide { width: 600px; }
@keyframes slideIn { from { transform: translateX(100%); } to { transform: translateX(0); } }

.drawer-header {
  padding: 16px 20px; border-bottom: 1px solid var(--border);
  display: flex; align-items: center; justify-content: space-between;
}
.drawer-title { font-size: 14px; font-weight: 600; }

.drawer-content { flex: 1; padding: 24px; overflow-y: auto; display: flex; flex-direction: column; gap: 20px; }

.drawer-footer {
  padding: 16px 20px; border-top: 1px solid var(--border);
  display: flex; justify-content: flex-end; gap: 12px;
}

.access-table { width: 100%; border-collapse: collapse; }
.access-table th { text-align: left; padding: 12px; font-size: 11px; font-weight: 600; color: var(--text3); text-transform: uppercase; border-bottom: 1px solid var(--border); }
.access-table td { padding: 12px; font-size: 13px; border-bottom: 1px solid var(--border); }
.access-table th.center, .access-table td.center { text-align: center; }

.server-info { display: flex; flex-direction: column; }
.server-name { font-weight: 500; color: var(--text1); }
.server-ip { font-size: 11px; color: var(--text3); font-family: var(--font-mono); }

.status-dim { font-size: 11px; color: var(--text3); font-style: italic; }

.btn {
  display: flex; align-items: center; justify-content: center; gap: 8px;
  padding: 8px 16px; border-radius: 6px; font-size: 13px; font-weight: 500;
  cursor: pointer; transition: all 0.2s; border: none; white-space: nowrap;
}
.btn-sm { padding: 4px 10px; font-size: 11px; }
.btn-primary { background: var(--accent); color: #fff; }
.btn-primary:hover { opacity: 0.9; transform: translateY(-1px); }
.btn-primary:disabled { opacity: 0.5; cursor: not-allowed; transform: none; }
.btn-ghost { background: transparent; color: var(--text2); border: 1px solid var(--border); }
.btn-ghost:hover { background: var(--bg2); color: var(--text1); }

.btn-icon {
  width: 32px; height: 32px; display: flex; align-items: center; justify-content: center;
  border-radius: 8px; border: 1px solid transparent; background: transparent; cursor: pointer;
  color: var(--text3); transition: all 0.2s;
}
.btn-icon:hover { background: var(--bg2); color: var(--text1); }

@media (max-width: 600px) {
  .drawer { width: 100%; }
}
</style>
