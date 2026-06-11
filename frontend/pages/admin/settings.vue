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

    <div class="tabs">
      <div class="tab-item" :class="{ active: activeTab === 'general' }" @click="activeTab = 'general'">General</div>
      <div class="tab-item" :class="{ active: activeTab === 'notification' }" @click="activeTab = 'notification'">Notification</div>
    </div>

    <div class="content scroll-area">
      <SettingsGeneral v-if="activeTab === 'general'" />
      <SettingsNotification
        v-if="activeTab === 'notification'"
        v-model:model="form"
        :subscribers="subscribers"
        @remove-subscriber="removeSubscriber"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
definePageMeta({ middleware: 'auth' })

const auth = useAuth()
const config = useRuntimeConfig()
const saving = ref(false)
const activeTab = ref('notification')
const subscribers = ref<any[]>([])

const form = ref<any>({
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

const fetchSubscribers = async () => {
  try {
    subscribers.value = await $fetch(`${config.public.apiBase}/api/system/telegram/subscribers`, {
      headers: auth.authHeaders()
    })
  } catch { console.error('Failed to load subscribers') }
}

onMounted(async () => {
  try {
    const res: any[] = await $fetch(`${config.public.apiBase}/api/system/settings`, {
      headers: auth.authHeaders()
    })
    res.forEach((s: any) => {
      if (Object.prototype.hasOwnProperty.call(form.value, s.key)) {
        form.value[s.key] = s.value
      }
    })
    await fetchSubscribers()
  } catch { console.error('Failed to load settings') }
})

const removeSubscriber = async (chatId: string) => {
  if (!confirm('Are you sure you want to remove this subscriber?')) return
  try {
    await $fetch(`${config.public.apiBase}/api/system/telegram/subscribers/${chatId}`, {
      method: 'DELETE', headers: auth.authHeaders()
    })
    await fetchSubscribers()
  } catch { alert('Failed to remove subscriber') }
}

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
      method: 'POST', body: payload, headers: auth.authHeaders()
    })
    alert('Settings saved successfully')
  } catch { alert('Failed to save settings') }
  finally { saving.value = false }
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
.tabs {
  display: flex; gap: 12px; padding: 0 20px;
  background: var(--bg1); border-bottom: 1px solid var(--border); flex-shrink: 0;
}
.tab-item {
  padding: 12px 10px; font-size: 12px; font-weight: 500; color: var(--text3);
  cursor: pointer; border-bottom: 2px solid transparent; transition: all 0.2s;
}
.tab-item:hover { color: var(--text1); }
.tab-item.active { color: var(--accent); border-bottom-color: var(--accent); }
.content { flex: 1; padding: 24px; display: flex; flex-direction: column; min-height: 0; }
.scroll-area { overflow-y: auto; scrollbar-width: thin; }
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
</style>
