<template>
  <div class="tab-overview">
    <div class="info-grid">
      <div class="info-item">
        <div class="info-label">Current Status</div>
        <div class="info-val">
          <span class="status-pill" :class="statusColor">
            {{ currentJob ? currentJob.status : 'Idle' }}
          </span>
        </div>
      </div>
      <div class="info-item">
        <div class="info-label">Last Deploy</div>
        <div class="info-val">{{ formatDate(app.lastDeployedAt) }}</div>
      </div>
      <div class="info-item">
        <div class="info-label">Branch</div>
        <div class="info-val">{{ app.branch }}</div>
      </div>
      <div class="info-item">
        <div class="info-label">Build Command</div>
        <div class="info-val"><code>{{ app.buildCmd || 'None' }}</code></div>
      </div>
    </div>

    <div v-if="currentJob" class="deploy-progress">
      <div class="progress-header">
        <div class="progress-title">Active Deployment: {{ currentJob.id }}</div>
        <div class="progress-pct">{{ currentJob.progress || 0 }}%</div>
      </div>
      <div class="progress-bar">
        <div class="progress-fill" :style="{ width: (currentJob.progress || 0) + '%' }"></div>
      </div>
    </div>

    <div class="section-title">Schedule</div>
    <div class="schedule-card">
      <div class="schedule-info">
        <div class="schedule-cron">{{ app.scheduleCron || 'No schedule' }}</div>
        <div class="schedule-status" :class="{ enabled: app.scheduleEnabled }">
          {{ app.scheduleEnabled ? 'Enabled' : 'Disabled' }}
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  app: { type: Object, required: true },
  currentJob: { type: Object, default: null }
})

const statusColor = computed(() => {
  const s = props.currentJob?.status?.toLowerCase() || ''
  if (s === 'success' || s === 'completed') return 'pill-success'
  if (s === 'failed' || s === 'error') return 'pill-failed'
  if (s === 'running' || s === 'pending') return 'pill-running'
  return 'pill-idle'
})

const formatDate = (dateStr) => {
  if (!dateStr) return 'Never'
  return new Date(dateStr).toLocaleString()
}
</script>

<style scoped>
.info-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 16px; margin-bottom: 24px; }
.info-label { font-size: 10px; font-weight: 600; text-transform: uppercase; color: var(--text3); letter-spacing: 0.5px; margin-bottom: 4px; }
.info-val { font-size: 13px; font-weight: 500; }
.info-val code { font-family: var(--mono); font-size: 11px; background: var(--bg2); padding: 2px 5px; border-radius: 4px; }

.status-pill {
  font-size: 10px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.4px;
  padding: 2px 8px; border-radius: 99px; display: inline-block;
}
.pill-success { background: rgba(0, 201, 167, 0.12); color: var(--accent); }
.pill-running { background: rgba(0, 151, 255, 0.12); color: var(--accent2); animation: pulse 1.5s infinite; }
.pill-failed { background: rgba(240, 80, 96, 0.12); color: var(--danger); }
.pill-idle { background: var(--bg3); color: var(--text3); }

@keyframes pulse { 0% { opacity: 1; } 50% { opacity: 0.6; } 100% { opacity: 1; } }

.deploy-progress { margin-bottom: 24px; background: var(--bg2); padding: 12px; border-radius: 8px; border: 1px solid var(--border); }
.progress-header { display: flex; justify-content: space-between; margin-bottom: 8px; }
.progress-title { font-size: 11px; font-weight: 600; color: var(--text2); }
.progress-pct { font-size: 11px; font-family: var(--mono); color: var(--accent); }
.progress-bar { height: 6px; background: var(--bg3); border-radius: 99px; overflow: hidden; }
.progress-fill { height: 100%; background: linear-gradient(90deg, var(--accent), var(--accent2)); transition: width 0.4s ease; }

.section-title { font-size: 11px; font-weight: 600; text-transform: uppercase; color: var(--text3); letter-spacing: 0.8px; margin-bottom: 10px; }
.schedule-card { background: var(--bg2); border: 1px solid var(--border); border-radius: 8px; padding: 12px; }
.schedule-info { display: flex; justify-content: space-between; align-items: center; }
.schedule-cron { font-family: var(--mono); font-size: 12px; color: var(--accent2); }
.schedule-status { font-size: 10px; font-weight: 600; text-transform: uppercase; color: var(--text3); }
.schedule-status.enabled { color: var(--success); }
</style>
