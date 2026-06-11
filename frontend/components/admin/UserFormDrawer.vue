<template>
  <div v-if="show" class="drawer-overlay" @click.self="$emit('update:show', false)">
    <div class="drawer">
      <div class="drawer-header">
        <div class="drawer-title">{{ mode === 'create' ? 'Add New User' : 'Edit User' }}</div>
        <button class="btn-icon" @click="$emit('update:show', false)">
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 3l10 10M3 13L13 3"/></svg>
        </button>
      </div>
      <div class="drawer-content">
        <div class="form-group" v-if="mode === 'create'">
          <label>Username</label>
          <input v-model="form.username" placeholder="john.doe" />
        </div>
        <div class="form-group">
          <label>Display Name</label>
          <input v-model="form.displayName" placeholder="John Doe" />
        </div>
        <div class="form-group" v-if="mode === 'create'">
          <label>Password</label>
          <input v-model="form.password" type="password" placeholder="••••••••" />
        </div>
        <div class="form-group" v-if="mode === 'edit'">
          <label>New Password (leave blank to keep current)</label>
          <div class="input-with-action">
            <input v-model="form.newPassword" type="password" placeholder="••••••••" />
            <button class="btn btn-sm" @click="$emit('reset-password')" :disabled="!form.newPassword">Reset</button>
          </div>
        </div>
        <div class="form-group">
          <label>Role</label>
          <select v-model="form.role">
            <option value="admin">Admin (Full Access)</option>
            <option value="operator">Operator (Manage Apps/Servers)</option>
            <option value="viewer">Viewer (Read Only)</option>
          </select>
        </div>
        <div class="form-group checkbox">
          <label class="checkbox-label">
            <input type="checkbox" v-model="form.isActive" />
            <span>Allow user to login (Active)</span>
          </label>
        </div>
      </div>
      <div class="drawer-footer">
        <button class="btn btn-ghost" @click="$emit('update:show', false)">Cancel</button>
        <button class="btn btn-primary" :disabled="saving" @click="$emit('save')">
          {{ saving ? 'Saving...' : (mode === 'create' ? 'Create User' : 'Save Changes') }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  show: boolean
  mode: 'create' | 'edit'
  saving: boolean
}>()

const form = defineModel<any>('form', { required: true })

defineEmits<{
  'update:show': [value: boolean]
  save: []
  'reset-password': []
}>()
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
