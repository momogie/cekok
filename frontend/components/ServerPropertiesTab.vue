<template>
  <div class="tab-pane">
    <div class="props-list">
      <div class="prop-item">
        <div class="prop-label">Hostname</div>
        <div class="prop-val">{{ sysInfo?.hostname || '...' }}</div>
      </div>
      <div class="prop-item">
        <div class="prop-label">Processor</div>
        <div class="prop-val">{{ sysInfo?.cpu || '...' }}</div>
      </div>
      <div class="prop-item">
        <div class="prop-label">Latency</div>
        <div class="prop-val">{{ stats?.latency || '...' }} ms</div>
      </div>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  sysInfo: Object,
  stats: Object
})
</script>

<style scoped>
.tab-pane { display: flex; flex-direction: column; }
.props-list { display: flex; flex-direction: column; padding: 10px 12px; }
.prop-item { display: flex; justify-content: space-between; padding: 6px 0; border-bottom: 1px solid var(--border); }
.prop-item:last-child { border-bottom: none; }
.prop-label { font-size: 11px; color: var(--text3); }
.prop-val { font-size: 11px; color: var(--text1); font-family: var(--mono); }
</style>
