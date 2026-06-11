<template>
  <div class="panel">
    <div class="panel-header" style="border-bottom: none">
      <div class="panel-title">Applications</div>
    </div>
    <div class="app-grid">
      <div v-for="app in apps" :key="app.id" class="app-card-new">
        <div class="app-card-top">
          <div class="app-name">{{ app.name }}</div>
          <div class="badge-type">{{ app.type || 'App' }}</div>
        </div>
        <div class="app-card-mid">
          <div class="app-meta-line" title="Repository">
            <svg viewBox="0 0 24 24" width="13" height="13" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round" stroke-linejoin="round" style="flex-shrink: 0"><path d="M15 22v-4a4.8 4.8 0 0 0-1-3.5c3 0 6-2 6-5.5.08-1.25-.27-2.48-1-3.5.28-1.15.28-2.35 0-3.5 0 0-1 0-3 1.5-2.64-.5-5.36-.5-8 0C6 2 5 2 5 2c-.3 1.15-.3 2.35 0 3.5A5.403 5.403 0 0 0 4 9c0 3.5 3 5.5 6 5.5-.39.49-.68 1.05-.85 1.65-.17.6-.22 1.23-.15 1.85v4"/><path d="M9 18c-4.51 2-5-2-7-2"/></svg>
            <span>{{ app.repoUrl }}</span>
          </div>
          <div class="app-meta-line" title="Branch">
            <svg viewBox="0 0 24 24" width="13" height="13" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round" stroke-linejoin="round" style="flex-shrink: 0"><line x1="6" y1="3" x2="6" y2="15"/><circle cx="18" cy="6" r="3"/><circle cx="6" cy="18" r="3"/><path d="M18 9a9 9 0 0 1-9 9"/></svg>
            <span>{{ app.branch }}</span>
          </div>
        </div>
        <div class="app-card-bot">
          <button class="btn btn-primary" @click="$emit('deploy', app.id)" style="width: 100%; justify-content: center; padding: 6px 0;">Deploy</button>
        </div>
      </div>
      <div v-if="!apps.length" style="padding: 20px; color: var(--text3); font-size: 13px; grid-column: 1/-1; text-align: center;">No applications found.</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { Application } from '~/types'

defineProps<{ apps: Application[] }>()
defineEmits<{ deploy: [id: string] }>()
</script>

<style scoped>
.panel {
  background: var(--bg1); border: 1px solid var(--border);
  border-radius: var(--radius); overflow: hidden;
}
.panel-header {
  padding: 14px 18px; display: flex; align-items: center; gap: 10px;
}
.panel-title { font-size: 13px; font-weight: 600; }
.app-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 16px;
  padding: 0 16px 16px 16px;
}
.app-card-new {
  background: var(--bg1);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 14px;
  transition: all 0.2s;
}
.app-card-new:hover {
  border-color: var(--primary);
  box-shadow: 0 4px 12px rgba(0,0,0,0.1);
  transform: translateY(-2px);
}
.app-card-top {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 8px;
}
.app-name { 
  font-size: 14px; 
  font-weight: 600; 
  color: var(--text1);
  word-break: break-all;
}
.badge-type {
  font-size: 10px;
  font-weight: 600;
  text-transform: uppercase;
  background: var(--bg2);
  border: 1px solid var(--border);
  padding: 3px 6px;
  border-radius: 4px;
  color: var(--text3);
}
.app-card-mid {
  display: flex;
  flex-direction: column;
  gap: 8px;
  flex: 1;
}
.app-meta-line {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: var(--text3);
}
.app-meta-line span {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.app-card-bot {
  margin-top: auto;
}
</style>
