<template>
  <div class="settings-page">
    <div class="topbar">
      <div class="topbar-title">System Settings</div>
      <span class="topbar-sub">Configure global parameters, SMTP, and integrations</span>
      <div class="topbar-right">
        <button class="btn btn-primary" :disabled="saving" @click="saveSettings">
          <svg v-if="saving" class="spin" width="12" height="12" viewBox="0 0 16 16" fill="none"><path d="M8 2a6 6 0 1 1-6 6" stroke="currentColor" stroke-width="2" stroke-linecap="round"/></svg>
          <svg v-else width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2"><path d="M2 8l4 4 8-8"/></svg>
          {{ saving ? 'Saving...' : 'Save Changes' }}
        </button>
      </div>
    </div>

    <div class="content scroll-area">
      <div class="settings-grid">
        <!-- SMTP Settings -->
        <div class="panel">
          <div class="panel-header">
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="var(--accent)" stroke-width="1.5"><path d="M1 3h14v10H1V3z"/><path d="M1 3l7 5 7-5"/></svg>
            <span class="panel-title">SMTP Settings</span>
          </div>
          <div class="panel-body">
            <div class="form-group">
              <label>SMTP Host</label>
              <input v-model="form.smtp_host" type="text" placeholder="smtp.example.com" />
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>SMTP Port</label>
                <input v-model="form.smtp_port" type="number" placeholder="587" />
              </div>
              <div class="form-group">
                <label>Encryption</label>
                <select v-model="form.smtp_encryption">
                  <option value="none">None</option>
                  <option value="ssl">SSL</option>
                  <option value="tls">TLS/STARTTLS</option>
                </select>
              </div>
            </div>
            <div class="form-group">
              <label>Username</label>
              <input v-model="form.smtp_username" type="text" />
            </div>
            <div class="form-group">
              <label>Password</label>
              <input v-model="form.smtp_password" type="password" placeholder="********" />
              <span class="help-text">Leave as ******** to keep existing password</span>
            </div>
            <div class="form-group">
              <label>From Email</label>
              <input v-model="form.smtp_from_email" type="email" placeholder="noreply@cekok.io" />
            </div>
            <div class="form-group">
              <label>From Name</label>
              <input v-model="form.smtp_from_name" type="text" placeholder="Cekok System" />
            </div>
          </div>
        </div>

        <!-- Telegram Settings -->
        <div class="panel">
          <div class="panel-header">
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="var(--accent)" stroke-width="1.5"><path d="M14.5 1.5l-13 5 4.5 2 2 5 2-3.5 4.5 3.5 1.5-12zM5.5 8.5l4-4"/></svg>
            <span class="panel-title">Telegram Bot</span>
          </div>
          <div class="panel-body">
            <div class="form-group">
              <label>Bot Token</label>
              <input v-model="form.telegram_bot_token" type="password" placeholder="********" />
              <span class="help-text">The token from @BotFather</span>
            </div>
            <div class="form-group">
              <label>Admin Chat ID</label>
              <input v-model="form.telegram_admin_chat_id" type="text" placeholder="-100123456789" />
              <span class="help-text">Where to send notifications</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
definePageMeta({ middleware: 'auth' })

const auth = useAuth()
const config = useRuntimeConfig()
const saving = ref(false)

const form = ref({
  smtp_host: '',
  smtp_port: 587,
  smtp_encryption: 'tls',
  smtp_username: '',
  smtp_password: '********',
  smtp_from_email: '',
  smtp_from_name: '',
  telegram_bot_token: '********',
  telegram_admin_chat_id: ''
})

onMounted(async () => {
  try {
    const res = await $fetch(`${config.public.apiBase}/api/system/settings`, {
      headers: auth.authHeaders()
    })
    
    res.forEach(s => {
      if (Object.prototype.hasOwnProperty.call(form.value, s.key)) {
        form.value[s.key] = s.value
      }
    })
  } catch (err) {
    console.error('Failed to load settings', err)
  }
})

const saveSettings = async () => {
  saving.value = true
  try {
    const payload = Object.entries(form.value).map(([key, value]) => ({
      key,
      value: String(value),
      group: key.startsWith('smtp_') ? 'mail' : (key.startsWith('telegram_') ? 'telegram' : 'general'),
      isSecure: key.endsWith('_password') || key.endsWith('_token')
    }))
    
    await $fetch(`${config.public.apiBase}/api/system/settings`, {
      method: 'POST',
      body: payload,
      headers: auth.authHeaders()
    })
    // Alert or Toast could be added here
    alert('Settings saved successfully')
  } catch (err) {
    console.error('Failed to save settings', err)
    alert('Failed to save settings')
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.settings-page { flex: 1; display: flex; flex-direction: column; overflow: hidden; background: var(--bg0); }

.topbar {
  height: 52px; flex-shrink: 0;
  background: var(--bg1); border-bottom: 1px solid var(--border);
  display: flex; align-items: center; padding: 0 20px; gap: 14px;
}
.topbar-title { font-size: 13px; font-weight: 600; }
.topbar-sub { font-size: 11px; color: var(--text3); }
.topbar-right { margin-left: auto; display: flex; align-items: center; gap: 8px; }

.content { flex: 1; padding: 24px; display: flex; flex-direction: column; min-height: 0; }
.scroll-area { overflow-y: auto; scrollbar-width: thin; }

.settings-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
  gap: 24px;
  max-width: 1200px;
}

.panel {
  background: var(--bg1); border: 1px solid var(--border);
  border-radius: var(--radius); display: flex; flex-direction: column; overflow: hidden;
  height: fit-content;
}

.panel-header {
  padding: 12px 16px; border-bottom: 1px solid var(--border);
  display: flex; align-items: center; gap: 10px; flex-shrink: 0;
  background: rgba(255,255,255,0.02);
}
.panel-title { font-size: 12px; font-weight: 600; text-transform: uppercase; color: var(--text2); letter-spacing: 0.5px; }

.panel-body { padding: 20px; display: flex; flex-direction: column; gap: 16px; }

.form-group { display: flex; flex-direction: column; gap: 6px; }
.form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }

label { font-size: 11px; font-weight: 500; color: var(--text3); text-transform: uppercase; }

input, select {
  background: var(--bg2); border: 1px solid var(--border);
  border-radius: 6px; padding: 8px 12px; color: var(--text1);
  font-size: 13px; outline: none; transition: border var(--transition);
}
input:focus, select:focus { border-color: var(--accent); }

.help-text { font-size: 10px; color: var(--text3); font-style: italic; }

.btn {
  display: flex; align-items: center; gap: 8px;
  padding: 6px 14px; border-radius: 6px; font-size: 12px; font-weight: 500;
  cursor: pointer; transition: all var(--transition); border: none;
}
.btn-primary { background: var(--accent); color: #fff; }
.btn-primary:hover { opacity: 0.9; transform: translateY(-1px); }
.btn-primary:disabled { opacity: 0.5; cursor: not-allowed; transform: none; }

.spin { animation: spin 1s linear infinite; }
@keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }

@media (max-width: 600px) {
  .settings-grid { grid-template-columns: 1fr; }
  .form-row { grid-template-columns: 1fr; }
}
</style>
