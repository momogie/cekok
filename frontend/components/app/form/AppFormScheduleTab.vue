<template>
  <div class="step-panel">
    <div class="msec-label">Auto-deploy schedule</div>
    <div class="form-group">
      <div class="toggle-row" @click="form.scheduleEnabled = !form.scheduleEnabled">
        <div class="toggle-track" :class="{ enabled: form.scheduleEnabled }">
          <div class="toggle-thumb" :class="{ enabled: form.scheduleEnabled }"></div>
        </div>
        <span class="toggle-label">{{ form.scheduleEnabled ? 'Schedule enabled' : 'Schedule disabled' }}</span>
      </div>
    </div>
    <div v-if="form.scheduleEnabled" class="schedule-form">
      <div class="msec-label">Quick presets</div>
      <div class="cron-presets">
        <button 
          v-for="p in cronPresets" 
          :key="p.val" 
          class="cron-preset" 
          @click="applyCronPreset(p.val)"
        >
          {{ p.label }}
        </button>
      </div>
      <div class="msec-label">Cron expression</div>
      <div class="cron-fields">
        <div v-for="(field, i) in ['Minute','Hour','Day','Month','Weekday']" :key="field" class="cron-field">
          <div class="cron-flabel">{{ field }}</div>
          <input v-model="cronParts[i]" class="cron-input" @input="updateCronFromParts">
        </div>
      </div>
      <div class="cron-preview-box">
        <span class="cron-expr">{{ form.scheduleCron }}</span>
        <span class="cron-human">{{ humanizeCron(form.scheduleCron) }}</span>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'

const props = defineProps({
  form: Object,
  cronPresets: Array
})

const cronParts = ref(props.form.scheduleCron.split(' '))

watch(() => props.form.scheduleCron, (newVal) => {
  const parts = newVal.split(' ')
  if (parts.join(' ') !== cronParts.value.join(' ')) {
    cronParts.value = parts
  }
})

const applyCronPreset = (val) => {
  props.form.scheduleCron = val
  cronParts.value = val.split(' ')
}

const updateCronFromParts = () => {
  props.form.scheduleCron = cronParts.value.join(' ')
}

const humanizeCron = (cron) => {
  const map = {
    '0 0 * * *': 'Every day at midnight',
    '0 2 * * *': 'Every day at 02:00',
    '0 */6 * * *': 'Every 6 hours',
    '0 2 * * 1': 'Every Monday at 02:00',
    '0 * * * *': 'Every hour',
    '30 2 * * *': 'Every day at 02:30',
  }
  return map[cron] || 'Custom schedule'
}
</script>
