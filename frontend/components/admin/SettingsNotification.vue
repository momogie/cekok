<template>
  <div class="settings-grid">
    <div class="panel">
      <div class="panel-header">
        <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="var(--accent)" stroke-width="1.5"><path d="M1 3h14v10H1V3z"/><path d="M1 3l7 5 7-5"/></svg>
        <span class="panel-title">SMTP Settings</span>
      </div>
      <div class="panel-body">
        <div class="form-group">
          <label>SMTP Host</label>
          <input v-model="model.smtp_host" type="text" placeholder="smtp.example.com" />
        </div>
        <div class="form-row">
          <div class="form-group">
            <label>SMTP Port</label>
            <input v-model="model.smtp_port" type="number" placeholder="587" />
          </div>
          <div class="form-group">
            <label>Encryption</label>
            <select v-model="model.smtp_encryption">
              <option value="none">None</option>
              <option value="ssl">SSL</option>
              <option value="tls">TLS/STARTTLS</option>
            </select>
          </div>
        </div>
        <div class="form-group">
          <label>Username</label>
          <input v-model="model.smtp_username" type="text" />
        </div>
        <div class="form-group">
          <label>Password</label>
          <input v-model="model.smtp_password" type="password" placeholder="********" />
          <span class="help-text">Leave as ******** to keep existing password</span>
        </div>
        <div class="form-group">
          <label>From Email</label>
          <input v-model="model.smtp_from_email" type="email" placeholder="noreply@cekok.io" />
        </div>
        <div class="form-group">
          <label>From Name</label>
          <input v-model="model.smtp_from_name" type="text" placeholder="Cekok System" />
        </div>
      </div>
    </div>

    <div class="panel">
      <div class="panel-header">
        <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="var(--accent)" stroke-width="1.5"><path d="M14.5 1.5l-13 5 4.5 2 2 5 2-3.5 4.5 3.5 1.5-12zM5.5 8.5l4-4"/></svg>
        <span class="panel-title">Telegram Bot</span>
      </div>
      <div class="panel-body">
        <div class="form-group">
          <label>Bot Token</label>
          <input v-model="model.telegram_bot_token" type="password" placeholder="********" />
          <span class="help-text">The token from @BotFather</span>
        </div>
        <div class="form-group">
          <label>Admin Chat ID</label>
          <input v-model="model.telegram_admin_chat_id" type="text" placeholder="-100123456789" />
          <span class="help-text">Where to send notifications</span>
        </div>

        <div class="subscribers-section">
          <label>Active Subscribers</label>
          <div v-if="subscribers.length === 0" class="empty-subs">
            No active subscribers. Ask users to type <code>/subscribe</code> to the bot.
          </div>
          <div v-else class="subs-list">
            <div v-for="sub in subscribers" :key="sub.chatId" class="sub-item" :class="{ inactive: !sub.isActive }">
              <div class="sub-info">
                <span class="sub-name">{{ sub.displayName || sub.username || 'Anonymous' }}</span>
                <span class="sub-id">ID: {{ sub.chatId }}</span>
              </div>
              <div class="sub-actions">
                <span v-if="!sub.isActive" class="status-badge">Inactive</span>
                <button class="btn-icon delete" @click="$emit('remove-subscriber', sub.chatId)" title="Remove">
                  <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 3l10 10M3 13L13 3"/></svg>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{ subscribers: any[] }>()
const model = defineModel<any>('model', { required: true })
defineEmits<{ 'remove-subscriber': [chatId: string] }>()
</script>

<style scoped>
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
  display: flex; align-items: center; gap: 10px;
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
.subscribers-section { margin-top: 12px; border-top: 1px solid var(--border); padding-top: 20px; }
.empty-subs { font-size: 11px; color: var(--text3); padding: 12px; background: rgba(0,0,0,0.1); border-radius: 6px; text-align: center; }
.empty-subs code { background: var(--bg2); padding: 2px 4px; border-radius: 4px; color: var(--accent); }
.subs-list { display: flex; flex-direction: column; gap: 8px; }
.sub-item {
  display: flex; align-items: center; justify-content: space-between;
  padding: 10px 12px; background: var(--bg2); border: 1px solid var(--border); border-radius: 6px;
}
.sub-item.inactive { opacity: 0.6; }
.sub-info { display: flex; flex-direction: column; gap: 2px; }
.sub-name { font-size: 12px; font-weight: 500; color: var(--text1); }
.sub-id { font-size: 10px; color: var(--text3); font-family: var(--font-mono); }
.sub-actions { display: flex; align-items: center; gap: 10px; }
.status-badge { font-size: 9px; padding: 2px 6px; background: var(--bg3); color: var(--text3); border-radius: 4px; text-transform: uppercase; }
.btn-icon {
  width: 28px; height: 28px; display: flex; align-items: center; justify-content: center;
  border-radius: 6px; border: 1px solid transparent; background: transparent; cursor: pointer;
  color: var(--text3); transition: all 0.2s;
}
.btn-icon:hover { background: var(--bg3); color: var(--text1); }
.btn-icon.delete:hover { border-color: rgba(255,100,100,0.3); color: #ff6b6b; }
</style>
