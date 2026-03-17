<template>
  <div class="step-panel">
    <div class="msec-label">App runtime</div>
    <div class="type-grid">
      <div 
        v-for="t in appTypes" 
        :key="t.id"
        class="type-card" 
        :class="{ selected: form.type === t.id }"
        @click="selectType(t.id)"
      >
        <div class="type-card-icon" :class="t.cls">{{ t.icon }}</div>
        <div class="type-card-name">{{ t.name }}</div>
        <div class="type-card-desc">{{ t.desc }}</div>
      </div>
    </div>
    <div class="msec-label">Display name</div>
    <div class="form-group">
      <label class="form-label">App name <span>*</span></label>
      <input v-model="form.name" class="form-input" placeholder="e.g. HRDesk API">
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  form: Object,
  appTypes: Array
})

const emit = defineEmits(['select-type'])

const selectType = (id) => {
  emit('select-type', id)
}
</script>
