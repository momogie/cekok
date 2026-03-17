<template>
  <div class="step-panel">
    <div class="msec-label">Config Files</div>
    <div class="form-hint" style="margin-bottom:12px;">Create config files (e.g. appsettings.json) that will be written to the app root directory during deploy.</div>
    <div class="setting-files-list">
      <div v-for="(sf, i) in form.settingFiles" :key="i" class="setting-file-row">
        <div class="setting-file-header">
          <input v-model="sf.filePath" class="form-input form-input-sm" placeholder="e.g. appsettings.json or google/fcm.json">
          <button class="env-del" @click="removeSettingFile(i)">×</button>
        </div>
        <textarea v-model="sf.content" class="form-input form-textarea" placeholder="File content..."></textarea>
      </div>
    </div>
    <button class="add-row-btn" @click="addSettingFile">
      <svg width="11" height="11" viewBox="0 0 12 12" fill="none"><path d="M6 2v8M2 6h8" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/></svg>
      Add config file
    </button>
  </div>
</template>

<script setup>
const props = defineProps({
  form: Object
})

const emit = defineEmits(['add-setting', 'remove-setting'])

const addSettingFile = () => emit('add-setting')
const removeSettingFile = (i) => emit('remove-setting', i)
</script>
