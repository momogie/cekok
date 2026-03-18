<template>
  <div class="users-page">
    <div class="topbar">
      <div class="topbar-title">Manage Users</div>
      <span class="topbar-sub">Manage user accounts, roles, and server access permissions</span>
      <div class="topbar-right">
        <button class="btn btn-primary" @click="openCreateDrawer">
          <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M8 3v10M3 8h10"/></svg>
          Add User
        </button>
      </div>
    </div>

    <div class="content scroll-area">
      <div v-if="loading" class="loading-state">
        <svg class="spin" width="24" height="24" viewBox="0 0 16 16" fill="none"><path d="M8 2a6 6 0 1 1-6 6" stroke="var(--accent)" stroke-width="2" stroke-linecap="round"/></svg>
        <span>Loading users...</span>
      </div>

      <div v-else-if="users.length === 0" class="empty-state">
        <div class="empty-icon">👥</div>
        <h3>No users found</h3>
        <p>Create your first user account to get started.</p>
        <button class="btn btn-primary" @click="openCreateDrawer">Add User</button>
      </div>

      <div v-else class="users-grid">
        <div v-for="user in users" :key="user.id" class="user-card" :class="{ inactive: !user.isActive }">
          <div class="user-card-header">
            <div class="user-avatar" :style="getAvatarStyle(user.displayName)">
              {{ user.displayName.charAt(0).toUpperCase() }}
            </div>
            <div class="user-main-info">
              <div class="user-name-row">
                <span class="user-display-name">{{ user.displayName }}</span>
                <span class="role-badge" :class="user.role">{{ user.role }}</span>
              </div>
              <div class="user-username">@{{ user.username }}</div>
            </div>
            <div class="user-actions">
              <button class="btn-icon" @click="openEditDrawer(user)" title="Edit User">
                <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M11 2l3 3-9 9-3 1 1-3 9-9z"/></svg>
              </button>
              <button class="btn-icon" @click="openAccessDrawer(user)" title="Manage Access">
                <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><rect x="3" y="3" width="10" height="10" rx="2"/><path d="M8 6v4M6 8h4"/></svg>
              </button>
              <button class="btn-icon delete" @click="confirmDelete(user)" title="Delete User">
                <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M3 3l10 10M3 13L13 3"/></svg>
              </button>
            </div>
          </div>
          <div class="user-card-body">
            <div class="info-item">
              <span class="info-label">Status</span>
              <span class="status-indicator" :class="{ active: user.isActive }">
                {{ user.isActive ? 'Active' : 'Disabled' }}
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">Created</span>
              <span class="info-value">{{ formatDate(user.createdAt) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">Last Login</span>
              <span class="info-value">{{ user.lastLoginAt ? formatDate(user.lastLoginAt) : 'Never' }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Create/Edit User Drawer -->
    <div v-if="drawer.show" class="drawer-overlay" @click.self="drawer.show = false">
      <div class="drawer">
        <div class="drawer-header">
          <div class="drawer-title">{{ drawer.mode === 'create' ? 'Add New User' : 'Edit User' }}</div>
          <button class="btn-icon" @click="drawer.show = false">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 3l10 10M3 13L13 3"/></svg>
          </button>
        </div>
        <div class="drawer-content">
          <div class="form-group" v-if="drawer.mode === 'create'">
            <label>Username</label>
            <input v-model="drawer.form.username" placeholder="john.doe" />
          </div>
          <div class="form-group">
            <label>Display Name</label>
            <input v-model="drawer.form.displayName" placeholder="John Doe" />
          </div>
          <div class="form-group" v-if="drawer.mode === 'create'">
            <label>Password</label>
            <input v-model="drawer.form.password" type="password" placeholder="••••••••" />
          </div>
          <div class="form-group" v-if="drawer.mode === 'edit'">
            <label>New Password (leave blank to keep current)</label>
            <div class="input-with-action">
              <input v-model="drawer.form.newPassword" type="password" placeholder="••••••••" />
              <button class="btn btn-sm" @click="resetPassword" :disabled="!drawer.form.newPassword">Reset</button>
            </div>
          </div>
          <div class="form-group">
            <label>Role</label>
            <select v-model="drawer.form.role">
              <option value="admin">Admin (Full Access)</option>
              <option value="operator">Operator (Manage Apps/Servers)</option>
              <option value="viewer">Viewer (Read Only)</option>
            </select>
          </div>
          <div class="form-group checkbox">
            <label class="checkbox-label">
              <input type="checkbox" v-model="drawer.form.isActive" />
              <span>Allow user to login (Active)</span>
            </label>
          </div>
        </div>
        <div class="drawer-footer">
          <button class="btn btn-ghost" @click="drawer.show = false">Cancel</button>
          <button class="btn btn-primary" :disabled="saving" @click="saveUser">
            {{ saving ? 'Saving...' : (drawer.mode === 'create' ? 'Create User' : 'Save Changes') }}
          </button>
        </div>
      </div>
    </div>

    <!-- Server Access Drawer -->
    <div v-if="accessDrawer.show" class="drawer-overlay" @click.self="accessDrawer.show = false">
      <div class="drawer wide">
        <div class="drawer-header">
          <div class="drawer-title">Server Access: {{ accessDrawer.user.displayName }}</div>
          <button class="btn-icon" @click="accessDrawer.show = false">
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
          <button class="btn btn-ghost" @click="accessDrawer.show = false">Cancel</button>
          <button class="btn btn-primary" :disabled="savingAccess" @click="saveAccess">
            {{ savingAccess ? 'Saving...' : 'Update Permissions' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
definePageMeta({ middleware: 'admin' })

const auth = useAuth()
const config = useRuntimeConfig()
const users = ref([])
const servers = ref([])
const loading = ref(true)
const saving = ref(false)
const savingAccess = ref(false)

const drawer = ref({
  show: false,
  mode: 'create',
  form: {
    id: '',
    username: '',
    displayName: '',
    password: '',
    newPassword: '',
    role: 'viewer',
    isActive: true
  }
})

const accessDrawer = ref({
  show: false,
  user: null,
  accesses: []
})

const fetchUsers = async () => {
  try {
    users.value = await $fetch(`${config.public.apiBase}/api/users`, {
      headers: auth.authHeaders()
    })
  } catch (err) {
    console.error('Failed to fetch users', err)
  }
}

const fetchServers = async () => {
  try {
    servers.value = await $fetch(`${config.public.apiBase}/api/servers`, {
      headers: auth.authHeaders()
    })
  } catch (err) {
    console.error('Failed to fetch servers', err)
  }
}

onMounted(async () => {
  await Promise.all([fetchUsers(), fetchServers()])
  loading.value = false
})

const openCreateDrawer = () => {
  drawer.value = {
    show: true,
    mode: 'create',
    form: {
      username: '',
      displayName: '',
      password: '',
      role: 'viewer',
      isActive: true
    }
  }
}

const openEditDrawer = (user) => {
  drawer.value = {
    show: true,
    mode: 'edit',
    form: {
      id: user.id,
      username: user.username,
      displayName: user.displayName,
      role: user.role,
      isActive: user.isActive,
      newPassword: ''
    }
  }
}

const saveUser = async () => {
  if (drawer.value.mode === 'create' && (!drawer.value.form.username || !drawer.value.form.password)) {
    alert('Username and password are required')
    return
  }

  saving.value = true
  try {
    if (drawer.value.mode === 'create') {
      await $fetch(`${config.public.apiBase}/api/users`, {
        method: 'POST',
        body: drawer.value.form,
        headers: auth.authHeaders()
      })
    } else {
      await $fetch(`${config.public.apiBase}/api/users/${drawer.value.form.id}`, {
        method: 'PUT',
        body: {
          displayName: drawer.value.form.displayName,
          role: drawer.value.form.role,
          isActive: drawer.value.form.isActive
        },
        headers: auth.authHeaders()
      })
    }
    await fetchUsers()
    drawer.value.show = false
  } catch (err) {
    alert('Failed to save user: ' + (err.data?.message || err.message))
  } finally {
    saving.value = false
  }
}

const resetPassword = async () => {
  if (!drawer.value.form.newPassword) return
  if (!confirm('Are you sure you want to reset the password for this user?')) return

  try {
    await $fetch(`${config.public.apiBase}/api/users/${drawer.value.form.id}/password`, {
      method: 'PUT',
      body: { password: drawer.value.form.newPassword },
      headers: auth.authHeaders()
    })
    alert('Password reset successfully')
    drawer.value.form.newPassword = ''
  } catch (err) {
    alert('Failed to reset password')
  }
}

const confirmDelete = async (user) => {
  if (user.id === auth.user.id) {
    alert('You cannot delete your own account')
    return
  }
  if (!confirm(`Are you sure you want to delete user "${user.displayName}"? This action cannot be undone.`)) return

  try {
    await $fetch(`${config.public.apiBase}/api/users/${user.id}`, {
      method: 'DELETE',
      headers: auth.authHeaders()
    })
    await fetchUsers()
  } catch (err) {
    alert('Failed to delete user')
  }
}

const openAccessDrawer = async (user) => {
  accessDrawer.value.user = user
  accessDrawer.value.show = true
  try {
    accessDrawer.value.accesses = await $fetch(`${config.public.apiBase}/api/users/${user.id}/server-access`, {
      headers: auth.authHeaders()
    })
  } catch (err) {
    console.error('Failed to fetch user access', err)
    accessDrawer.value.accesses = []
  }
}

const getServerAccess = (serverId) => {
  let access = accessDrawer.value.accesses.find(a => a.serverId === serverId)
  if (!access) {
    access = { serverId, canDeploy: false, canManage: false, isNew: true }
    accessDrawer.value.accesses.push(access)
  }
  return access
}

const isAccessGranted = (serverId) => {
  const access = accessDrawer.value.accesses.find(a => a.serverId === serverId)
  return access && !access.isNew
}

const revokeAccess = (serverId) => {
  const idx = accessDrawer.value.accesses.findIndex(a => a.serverId === serverId)
  if (idx !== -1) {
    accessDrawer.value.accesses[idx].canDeploy = false
    accessDrawer.value.accesses[idx].canManage = false
  }
}

const saveAccess = async () => {
  savingAccess.value = true
  try {
    const payload = accessDrawer.value.accesses
      .filter(a => a.canDeploy || a.canManage)
      .map(a => ({
        serverId: a.serverId,
        canDeploy: a.canDeploy,
        canManage: a.canManage
      }))
    
    await $fetch(`${config.public.apiBase}/api/users/${accessDrawer.value.user.id}/server-access`, {
      method: 'PUT',
      body: payload,
      headers: auth.authHeaders()
    })
    accessDrawer.value.show = false
    alert('Permissions updated successfully')
  } catch (err) {
    alert('Failed to update permissions')
  } finally {
    savingAccess.value = false
  }
}

const formatDate = (dateStr) => {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleString()
}

const getAvatarStyle = (name) => {
  const colors = ['#00c9a7', '#3498db', '#9b59b6', '#f1c40f', '#e67e22', '#e74c3c']
  const index = name.charCodeAt(0) % colors.length
  return {
    background: colors[index],
    color: '#fff'
  }
}
</script>

<style scoped>
.users-page { flex: 1; display: flex; flex-direction: column; overflow: hidden; background: var(--bg0); }

.topbar {
  height: 52px; flex-shrink: 0;
  background: var(--bg1); border-bottom: 1px solid var(--border);
  display: flex; align-items: center; padding: 0 20px; gap: 14px;
}
.topbar-title { font-size: 13px; font-weight: 600; }
.topbar-sub { font-size: 11px; color: var(--text3); }
.topbar-right { margin-left: auto; }

.content { flex: 1; padding: 24px; min-height: 0; }
.scroll-area { overflow-y: auto; scrollbar-width: thin; }

.loading-state, .empty-state {
  display: flex; flex-direction: column; align-items: center; justify-content: center;
  height: 300px; gap: 12px; color: var(--text3);
}
.empty-icon { font-size: 48px; margin-bottom: 12px; }
.empty-state h3 { color: var(--text1); font-size: 16px; margin: 0; }
.empty-state p { font-size: 13px; margin: 0 0 16px; }

.users-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(340px, 1fr));
  gap: 16px;
  max-width: 1400px;
}

.user-card {
  background: var(--bg1); border: 1px solid var(--border);
  border-radius: var(--radius); overflow: hidden;
  transition: all 0.2s;
}
.user-card:hover { border-color: var(--accent); transform: translateY(-2px); box-shadow: 0 4px 12px rgba(0,0,0,0.1); }
.user-card.inactive { opacity: 0.6; filter: grayscale(1); }

.user-card-header {
  padding: 16px; border-bottom: 1px solid var(--border);
  display: flex; align-items: center; gap: 12px;
}
.user-avatar {
  width: 40px; height: 40px; border-radius: 10px;
  display: flex; align-items: center; justify-content: center;
  font-weight: 700; font-size: 16px;
}
.user-main-info { flex: 1; min-width: 0; }
.user-name-row { display: flex; align-items: center; gap: 8px; margin-bottom: 2px; }
.user-display-name { font-size: 14px; font-weight: 600; color: var(--text1); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.user-username { font-size: 12px; color: var(--text3); font-family: var(--font-mono); }
.role-badge { font-size: 9px; padding: 2px 6px; border-radius: 4px; text-transform: uppercase; font-weight: 700; letter-spacing: 0.5px; }
.role-badge.admin { background: rgba(231, 76, 60, 0.1); color: #e74c3c; }
.role-badge.operator { background: rgba(52, 152, 219, 0.1); color: #3498db; }
.role-badge.viewer { background: rgba(149, 165, 166, 0.1); color: #95a5a6; }

.user-actions { display: flex; gap: 4px; }

.user-card-body { padding: 12px 16px; display: flex; flex-direction: column; gap: 8px; }
.info-item { display: flex; justify-content: space-between; align-items: center; font-size: 12px; }
.info-label { color: var(--text3); }
.info-value { color: var(--text2); }
.status-indicator { display: flex; align-items: center; gap: 6px; color: var(--danger); font-weight: 500; }
.status-indicator::before { content: ''; width: 6px; height: 6px; border-radius: 50%; background: var(--danger); }
.status-indicator.active { color: var(--accent); }
.status-indicator.active::before { background: var(--accent); }

/* Drawer Styles */
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

.form-group { display: flex; flex-direction: column; gap: 8px; }
.form-group label { font-size: 11px; font-weight: 600; color: var(--text3); text-transform: uppercase; letter-spacing: 0.5px; }
.form-group input, .form-group select {
  background: var(--bg2); border: 1px solid var(--border); border-radius: 6px;
  padding: 10px 12px; color: var(--text1); font-size: 13px; outline: none; transition: border 0.2s;
}
.form-group input:focus, .form-group select:focus { border-color: var(--accent); }

.input-with-action { display: flex; gap: 8px; }
.input-with-action input { flex: 1; }

.checkbox-label { display: flex; align-items: center; gap: 10px; cursor: pointer; user-select: none; }
.checkbox-label span { font-size: 13px; color: var(--text2); }

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
.btn-icon.delete:hover { color: var(--danger); background: rgba(240, 80, 96, 0.1); }

.spin { animation: spin 1s linear infinite; }
@keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }

@media (max-width: 600px) {
  .drawer { width: 100%; }
}
</style>
