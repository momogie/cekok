<template>
  <div class="stats-row">
    <div class="stat-card">
      <div class="stat-label">Total apps</div>
      <div class="stat-val">{{ apps }}</div>
    </div>
    <div class="stat-card">
      <div class="stat-label">Servers</div>
      <div class="stat-val green">{{ servers }}</div>
    </div>
    <div class="stat-card">
      <div class="stat-label">Jobs this week</div>
      <div class="stat-val warn">-</div>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{ apps: number; servers: number }>()
</script>

<style scoped>
.stats-row { display: grid; grid-template-columns: repeat(3, 1fr); gap: 14px; margin-bottom: 24px; }
.stat-card {
  background: var(--bg1); border: 1px solid var(--border);
  border-radius: var(--radius); padding: 16px 18px;
}
.stat-label { font-size: 11px; font-weight: 500; text-transform: uppercase; color: var(--text3); margin-bottom: 6px; }
.stat-val { font-size: 24px; font-weight: 600; font-family: var(--mono); }
.green { color: var(--success); }
.warn { color: var(--warn); }
</style>
