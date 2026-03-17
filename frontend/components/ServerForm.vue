<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-card">
      <div class="modal-header">
        <div class="modal-title">{{ isEdit ? 'Edit Server' : 'Add New Server' }}</div>
        <button class="close-btn" @click="$emit('close')">&times;</button>
      </div>
      <form @submit.prevent="submit" class="modal-body">
        <div class="form-grid">
          <div class="form-group full-width">
            <label>Server Name</label>
            <input v-model="form.name" type="text" placeholder="e.g. prod-web-01" required>
          </div>
          <div class="form-group">
            <label>IP Address / Host</label>
            <input v-model="form.ip" type="text" placeholder="1.2.3.4" required>
          </div>
          <div class="form-group">
            <label>SSH Port</label>
            <input v-model.number="form.sshPort" type="number" placeholder="22" required>
          </div>
          <div class="form-group">
            <label>SSH User</label>
            <input v-model="form.sshUser" type="text" placeholder="root" required>
          </div>
          <div class="form-group">
            <label>Role</label>
            <select v-model="form.role">
              <option value="worker">Worker</option>
              <option value="web">Web Server</option>
              <option value="db">Database</option>
              <option value="proxy">Reverse Proxy</option>
              <option value="cache">Cache / Redis</option>
            </select>
          </div>
          <div class="form-group full-width">
            <label>SSH Password {{ isEdit ? '(Leave blank to keep current)' : '' }}</label>
            <input v-model="form.sshPassword" type="password" placeholder="••••••••" :required="!isEdit">
          </div>
          <div class="form-group full-width">
            <label>Tags (Optional)</label>
            <input v-model="form.tags" type="text" placeholder="sg-ap, ubuntu-22, production">
            <div class="form-hint">Comma separated tags for organization</div>
          </div>
        </div>
        
        <div v-if="error" class="error-msg">{{ error }}</div>
        
        <div class="modal-footer">
          <button type="button" class="btn btn-ghost" @click="$emit('close')">Cancel</button>
          <button type="submit" class="btn btn-primary" :disabled="loading">
            {{ loading ? 'Saving...' : (isEdit ? 'Save Changes' : 'Add Server') }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  server: { type: Object, default: null }
})
const emit = defineEmits(['close', 'saved'])

const config = useRuntimeConfig()
const auth = useAuth()

const isEdit = computed(() => !!props.server)
const loading = ref(false)
const error = ref('')

const form = ref({
  name: props.server?.name || '',
  ip: props.server?.ip || '',
  sshPort: props.server?.sshPort || 22,
  sshUser: props.server?.sshUser || 'root',
  sshPassword: '',
  role: props.server?.role || 'worker',
  tags: props.server?.tags || ''
})

const submit = async () => {
  loading.value = true
  error.value = ''
  try {
    const url = isEdit.value ? `/api/servers/${props.server.id}` : '/api/servers'
    const method = isEdit.value ? 'PUT' : 'POST'
    
    await useNuxtApp().$apiFetch(url, {
      method,
      baseURL: config.public.apiBase,
      headers: auth.authHeaders(),
      body: form.value
    })
    
    emit('saved')
    emit('close')
  } catch (e) {
    if (e.response?.status !== 401) {
      error.value = e.data?.message || 'Failed to save server'
    }
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.modal-overlay {
  position: fixed; inset: 0; z-index: 1000;
  background: rgba(0,0,0,0.6); backdrop-filter: blur(4px);
  display: flex; align-items: center; justify-content: center; padding: 20px;
}
.modal-card {
  background: var(--bg1); border: 1px solid var(--border2);
  border-radius: var(--radius); width: 100%; max-width: 480px;
  box-shadow: 0 30px 60px rgba(0,0,0,0.5);
  animation: modalScale 0.2s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes modalScale {
  from { opacity: 0; transform: scale(0.95) translateY(10px); }
  to { opacity: 1; transform: scale(1) translateY(0); }
}

.modal-header {
  padding: 16px 20px; border-bottom: 1px solid var(--border);
  display: flex; align-items: center; justify-content: space-between;
}
.modal-title { font-size: 14px; font-weight: 600; letter-spacing: -0.2px; }
.close-btn { 
  background: none; border: none; color: var(--text3); font-size: 20px; cursor: pointer; 
  width: 28px; height: 28px; display: flex; align-items: center; justify-content: center;
  border-radius: 6px; transition: all 0.2s;
}
.close-btn:hover { background: var(--bg2); color: var(--text1); }

.modal-body { padding: 20px; }
.form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
.form-group { display: flex; flex-direction: column; gap: 6px; }
.full-width { grid-column: span 2; }
.form-group label { font-size: 10px; font-weight: 700; color: var(--text3); text-transform: uppercase; letter-spacing: 0.8px; }

.form-group input, .form-group select {
  background: var(--bg2); border: 1px solid var(--border); border-radius: var(--radius-sm);
  padding: 10px 12px; color: var(--text1); font-family: var(--mono); font-size: 12px;
  outline: none; transition: all 0.2s;
}
.form-group input:focus, .form-group select:focus { border-color: var(--accent); background: var(--bg3); box-shadow: 0 0 0 2px rgba(0, 201, 167, 0.1); }
.form-group input::placeholder { color: var(--text3); }

.form-hint { font-size: 10px; color: var(--text3); margin-top: 2px; }

.modal-footer { margin-top: 24px; display: flex; justify-content: flex-end; gap: 10px; padding-top: 16px; border-top: 1px solid var(--border); }

.error-msg { margin-top: 16px; color: var(--danger); font-size: 11px; background: rgba(240,80,96,0.1); padding: 10px 14px; border-radius: 8px; border: 1px solid rgba(240,80,96,0.2); }

.btn { font-size: 12px; font-weight: 600; }
</style>
