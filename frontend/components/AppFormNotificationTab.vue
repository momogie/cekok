<template>
  <div class="step-panel">
    <div class="msec-label">Notification Settings</div>
    <p class="mstep-desc">Configure how you want to be notified about deployment results.</p>

    <!-- Email Notification -->
    <div class="notify-card" :class="{ active: form.notifyEmail }">
      <div class="notify-header" @click="form.notifyEmail = !form.notifyEmail">
        <div class="notify-icon icon-email">
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M1 3h14v10H1V3z"/><path d="M1 3l7 5 7-5"/></svg>
        </div>
        <div class="notify-info">
          <div class="notify-title">Email Notification</div>
          <div class="notify-sub">Send deployment reports to your inbox</div>
        </div>
        <div class="toggle-track" :class="{ enabled: form.notifyEmail }">
          <div class="toggle-thumb" :class="{ enabled: form.notifyEmail }"></div>
        </div>
      </div>
      <div v-if="form.notifyEmail" class="notify-body">
        <div class="form-group">
          <label class="form-label">Recipient Email Address <span>*</span></label>
          <input 
            v-model="form.notifyEmailAddress" 
            type="email" 
            class="form-input" 
            placeholder="e.g. admin@example.com"
          />
          <div class="form-hint">Separate multiple emails with commas</div>
        </div>
      </div>
    </div>

    <!-- Telegram Notification -->
    <div class="notify-card" :class="{ active: form.notifyTelegram }">
      <div class="notify-header" @click="form.notifyTelegram = !form.notifyTelegram">
        <div class="notify-icon icon-telegram">
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M14.5 1.5l-13 5 4.5 2 2 5 2-3.5 4.5 3.5 1.5-12zM5.5 8.5l4-4"/></svg>
        </div>
        <div class="notify-info">
          <div class="notify-title">Telegram Notification</div>
          <div class="notify-sub">Get instant alerts via Telegram Bot</div>
        </div>
        <div class="toggle-track" :class="{ enabled: form.notifyTelegram }">
          <div class="toggle-thumb" :class="{ enabled: form.notifyTelegram }"></div>
        </div>
      </div>
      <div v-if="form.notifyTelegram" class="notify-body">
        <div class="form-group">
          <label class="form-label">Chat ID / Group ID <span>*</span></label>
          <input 
            v-model="form.notifyTelegramChatId" 
            type="text" 
            class="form-input" 
            placeholder="e.g. -100123456789"
          />
          <div class="form-hint">Leave empty to use global admin chat ID from settings</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  form: Object
})
</script>

<style scoped>
.mstep-desc { font-size: 11px; color: var(--text3); margin-bottom: 20px; }

.notify-card {
  border: 1.5px solid var(--border2); border-radius: 10px;
  background: var(--bg2); margin-bottom: 12px; transition: all 0.2s;
  overflow: hidden;
}
.notify-card.active { border-color: var(--accent); background: rgba(0, 201, 167, 0.03); }

.notify-header {
  padding: 14px 16px; display: flex; align-items: center; gap: 14px; cursor: pointer;
}
.notify-header:hover { background: rgba(255,255,255,0.02); }

.notify-icon {
  width: 32px; height: 32px; border-radius: 8px;
  display: flex; align-items: center; justify-content: center;
  flex-shrink: 0;
}
.icon-email { background: rgba(139, 127, 255, 0.1); color: var(--purple); }
.icon-telegram { background: rgba(0, 151, 255, 0.1); color: #0088cc; }

.notify-info { flex: 1; min-width: 0; }
.notify-title { font-size: 13px; font-weight: 600; color: var(--text1); }
.notify-sub { font-size: 11px; color: var(--text3); margin-top: 2px; }

.notify-body {
  padding: 0 16px 16px 62px;
  animation: slideDown 0.2s ease-out;
}

@keyframes slideDown {
  from { opacity: 0; transform: translateY(-10px); }
  to { opacity: 1; transform: translateY(0); }
}

.toggle-track {
  width: 34px; height: 18px; border-radius: 99px; background: var(--bg3);
  border: 1px solid var(--border2); position: relative; transition: background 0.2s;
  flex-shrink: 0;
}
.toggle-track.enabled { background: var(--accent); border-color: transparent; }
.toggle-thumb {
  position: absolute; top: 2px; left: 2px; width: 12px; height: 12px;
  border-radius: 50%; background: #fff; transition: transform 0.2s;
}
.toggle-thumb.enabled { transform: translateX(16px); }

.form-label { display: block; font-size: 10px; font-weight: 600; color: var(--text3); margin-bottom: 6px; text-transform: uppercase; letter-spacing: 0.5px; }
.form-label span { color: var(--danger); }
.form-input {
  background: var(--bg1); border: 1px solid var(--border2);
  border-radius: 6px; padding: 8px 10px; width: 100%;
  color: var(--text1); font-size: 12px; outline: none; transition: border-color 0.2s;
}
.form-input:focus { border-color: var(--accent); }
.form-hint { font-size: 10px; color: var(--text3); margin-top: 5px; font-style: italic; }
</style>
