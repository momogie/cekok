<template>
  <div class="step-panel">
    <div class="review-section-title">Application</div>
    <div class="review-block">
      <div class="review-row"><span class="review-key">Name</span><span class="review-val accent">{{ form.name }}</span></div>
      <div class="review-row"><span class="review-key">Type</span><span class="review-val">{{ currentType?.name }}</span></div>
    </div>
    <div class="review-section-title">Repository</div>
    <div class="review-block">
      <div class="review-row"><span class="review-key">Repo</span><span class="review-val">{{ form.repoUrl }}</span></div>
      <div class="review-row"><span class="review-key">Branch</span><span class="review-val">{{ form.branch }}</span></div>
    </div>
    <div class="review-section-title">Build</div>
    <div class="review-block">
      <div class="review-row"><span class="review-key">Build cmd</span><span class="review-val">{{ form.buildCmd || currentType?.buildCmd }}</span></div>
      <div class="review-row"><span class="review-key">Output dir</span><span class="review-val">{{ form.outputDir || currentType?.outputDir || '—' }}</span></div>
      <div class="review-row"><span class="review-key">Entry point</span><span class="review-val">{{ form.entryFile || currentType?.entryFile || '—' }}</span></div>
    </div>
    <div class="review-section-title">Deploy targets ({{ form.deployTargets.length }})</div>
    <div v-if="form.deployTargets.length === 0" class="review-block">
      <div class="review-row" style="color:var(--text3)">No servers selected</div>
    </div>
    <div v-for="t in form.deployTargets" :key="t.serverId" class="review-block" style="margin-bottom:6px">
      <div class="review-row">
        <span class="review-key">Server</span>
        <span class="review-val accent">{{ serverName(t.serverId) }}</span>
      </div>
      <div class="review-row"><span class="review-key">Deploy dir</span><span class="review-val">{{ t.deployDir || '—' }}</span></div>
      <div class="review-row"><span class="review-key">Service</span><span class="review-val">{{ t.serviceName || '—' }}</span></div>
    </div>

    <div class="review-section-title">Notification</div>
    <div class="review-block">
      <div class="review-row">
        <span class="review-key">Email</span>
        <span class="review-val" :class="{ accent: form.notifyEmail }">{{ form.notifyEmail ? 'Enabled' : 'Disabled' }}</span>
      </div>
      <div v-if="form.notifyEmail" class="review-row">
        <span class="review-key">Recipient</span>
        <span class="review-val">{{ form.notifyEmailAddress }}</span>
      </div>
      <div class="review-row">
        <span class="review-key">Telegram</span>
        <span class="review-val" :class="{ accent: form.notifyTelegram }">{{ form.notifyTelegram ? 'Enabled' : 'Disabled' }}</span>
      </div>
      <div v-if="form.notifyTelegram && form.notifyTelegramChatId" class="review-row">
        <span class="review-key">Chat ID</span>
        <span class="review-val">{{ form.notifyTelegramChatId }}</span>
      </div>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  form: Object,
  currentType: Object,
  serverName: Function
})
</script>
