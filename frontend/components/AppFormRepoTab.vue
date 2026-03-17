<template>
  <div class="step-panel">
    <div class="msec-label">GitHub repository</div>
    <div class="form-group">
      <label class="form-label">Repository URL <span>*</span></label>
      <input v-model="form.repoUrl" class="form-input" placeholder="https://github.com/org/repo.git">
      <div class="form-hint">Public or private repo. Use HTTPS URL.</div>
    </div>
    <div class="form-row">
      <div class="form-group">
        <label class="form-label">Branch <span>*</span></label>
        <input v-model="form.branch" class="form-input" placeholder="main">
      </div>
      <div class="form-group">
        <label class="form-label">Deploy trigger</label>
        <select v-model="form.trigger" class="form-select">
          <option value="manual">Manual only</option>
          <option value="webhook">GitHub webhook push</option>
          <option value="schedule">Schedule (cron)</option>
          <option value="both">Webhook + Schedule</option>
        </select>
      </div>
    </div>
    <div class="msec-label" style="margin-top:16px">Authentication</div>
    <div class="form-group">
      <label class="form-label">Personal access token</label>
      <input v-model="form.token" class="form-input" type="password" placeholder="ghp_xxxxxxxxxxxxxxxxxxxx">
      <div class="form-hint">Required for private repos. Stored encrypted on server.</div>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  form: Object
})
</script>
