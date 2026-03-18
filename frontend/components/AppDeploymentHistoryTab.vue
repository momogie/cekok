<template>
  <div class="deployment-history">
    <div v-if="!selectedHistory" class="history-list">
      <div v-if="loading" class="empty-state">Loading history...</div>
      <div v-else-if="!history.length" class="empty-state">No deployment history found.</div>
      
      <div 
        v-for="item in history" 
        :key="item.id" 
        class="history-card"
        @click="selectJob(item)"
      >
        <div class="history-header">
          <div class="history-status" :class="item.status.toLowerCase()">
            <span class="status-indicator"></span>
            {{ item.status }}
          </div>
          <div class="history-time">{{ formatTime(item.startedAt || item.createdAt || item.finishedAt) }}</div>
        </div>
        <div class="history-title">Deploy {{ item.commitHash?.substring(0, 7) || 'Manual' }}</div>
        <div class="history-meta">
          <span v-if="item.commitMsg">{{ item.commitMsg }}</span>
          <span v-else>Triggered by {{ item.triggeredBy || item.triggeredByUser || 'System' }}</span>
          <span class="meta-sep">•</span>
          <span>Hash: {{ item.commitHash || 'N/A' }}</span>
        </div>
      </div>
    </div>

    <div v-else class="pipeline-view">
      <div class="pipeline-header">
        <button class="btn btn-ghost btn-sm" @click="selectedHistory = null">
          ← Back to History
        </button>
        <div class="pipeline-title">
          <span>Deployment Pipeline</span>
          <span class="version-badge">{{ selectedHistory.commitHash?.substring(0, 7) || 'Manual' }}</span>
        </div>
        <div class="pipeline-status-badge" :class="selectedHistory.status.toLowerCase()">
          {{ selectedHistory.status }}
        </div>
      </div>

      <div class="pipeline-steps">
        <!-- 1. Pre-check -->
        <div class="step-card" :class="getStepClass(1)">
          <div class="step-header">
            <div class="step-icon">
              <span v-if="getStepClass(1) === 'success'">✓</span>
              <span v-else>1</span>
            </div>
            <div class="step-info">
              <div class="step-title">Pre-check</div>
              <div class="step-desc" v-if="getStepClass(1) !== 'success'">Validating environment</div>
            </div>
            <div class="step-status">{{ getStepStatusLabel(1) }}</div>
          </div>
          <div class="step-content hidden-content">
            <div class="check-grid">
              <div class="check-item success"><span class="icon">✓</span> Environment Ready</div>
              <div class="check-item success"><span class="icon">✓</span> Database Connection</div>
            </div>
          </div>
        </div>

        <!-- 2. Source Code -->
        <div class="step-card" :class="getStepClass(2)">
          <div class="step-header">
            <div class="step-icon">
              <span v-if="getStepClass(2) === 'success'">✓</span>
              <span v-else>2</span>
            </div>
            <div class="step-info">
              <div class="step-title">Source Code</div>
              <div class="step-desc" v-if="getStepClass(2) !== 'success'">Fetching repositories</div>
            </div>
            <div class="step-status">{{ getStepStatusLabel(2) }}</div>
          </div>
          <div class="step-content hidden-content repo-list">
            <div class="repo-item">
              <div class="repo-header">
                <span class="repo-badge primary">Primary</span>
                <span class="repo-name">{{ app.name }}</span>
                <span class="repo-commit">#{{ selectedHistory.commitHash?.substring(0, 7) || '...' }}</span>
              </div>
              <div class="progress-bar"><div class="progress-fill" :style="{ width: getStepClass(2) === 'success' ? '100%' : '50%' }"></div></div>
            </div>
          </div>
        </div>

        <!-- 3. Build -->
        <div class="step-card" :class="getStepClass(3)">
          <div class="step-header">
            <div class="step-icon">
              <span v-if="getStepClass(3) === 'success'">✓</span>
              <span v-else>3</span>
            </div>
            <div class="step-info">
              <div class="step-title">Build & Publish</div>
              <div class="step-desc" v-if="getStepClass(3) !== 'success'">Compilation & bundling</div>
            </div>
            <div class="step-status" :class="{ blinking: getStepClass(3) === 'active' }">
              {{ getStepStatusLabel(3) }}
            </div>
          </div>
          <div class="step-content hidden-content">
            <div class="terminal-mock" ref="terminalRef">
              <div v-for="(log, idx) in masterLogs" :key="idx" :class="log.level">
                <span v-if="log.level === 'cmd'">$ </span>{{ log.message }}
              </div>
              <div v-if="getStepClass(3) === 'active'"><span class="cursor">_</span></div>
            </div>
          </div>
        </div>

        <!-- 4. Artifact Validation -->
        <div class="step-card" :class="getStepClass(4)">
          <div class="step-header">
            <div class="step-icon">
              <span v-if="getStepClass(4) === 'success'">✓</span>
              <span v-else>4</span>
            </div>
            <div class="step-info">
              <div class="step-title">Artifact Validation</div>
              <div class="step-desc" v-if="getStepClass(4) !== 'success'">Verifying build outputs</div>
            </div>
            <div class="step-status">{{ getStepStatusLabel(4) }}</div>
          </div>
          <div class="step-content hidden-content">
            <ul class="artifact-checks">
              <li>Verifying output integrity...</li>
              <li v-if="getStepClass(4) === 'success'">Validation passed.</li>
            </ul>
          </div>
        </div>

        <!-- 5. Deploy Targets -->
        <div class="step-card" :class="getStepClass(5)">
          <div class="step-header">
            <div class="step-icon">
              <span v-if="getStepClass(5) === 'success'">✓</span>
              <span v-else>5</span>
            </div>
            <div class="step-info">
              <div class="step-title">Deploy Targets</div>
              <div class="step-desc" v-if="getStepClass(5) !== 'success'">Deployment to servers</div>
            </div>
            <div class="step-status">{{ getStepStatusLabel(5) }}</div>
          </div>
          <div class="step-content hidden-content">
            <div class="server-matrix">
              <div v-for="server in targetServers" :key="server.id" class="target-server">
                <div class="server-title">
                  <span>{{ server.name }} ({{ server.ip }})</span>
                  <button 
                    v-if="selectedHistory.status === 'success'" 
                    class="btn btn-ghost btn-xs text-red"
                    @click.stop="handleRollback"
                  >Rollback</button>
                </div>
                <div class="sub-steps">
                  <div class="sub-step" :class="getServerSubStepStatus(server.id, 'SSH')">SSH</div>
                  <div class="sub-step" :class="getServerSubStepStatus(server.id, 'Upload')">Upload</div>
                  <div class="sub-step" :class="getServerSubStepStatus(server.id, 'Restart')">Restart</div>
                  <div class="sub-step" :class="getServerSubStepStatus(server.id, 'Health')">Health</div>
                </div>
                <!-- Mini Progress -->
                <div class="progress-bar mt-1" style="height: 3px;">
                  <div class="progress-fill" :style="{ width: getServerProgress(server.id) + '%' }"></div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 6. Cleanup -->
        <div class="step-card" :class="getStepClass(6)">
          <div class="step-header">
            <div class="step-icon">
              <span v-if="getStepClass(6) === 'success'">✓</span>
              <span v-else>6</span>
            </div>
            <div class="step-info">
              <div class="step-title">Cleanup</div>
            </div>
            <div class="step-status">{{ getStepStatusLabel(6) }}</div>
          </div>
        </div>

        <!-- 7. Post-Deploy -->
        <div class="step-card" :class="getStepClass(7)">
          <div class="step-header">
            <div class="step-icon">
              <span v-if="getStepClass(7) === 'success'">✓</span>
              <span v-else>7</span>
            </div>
            <div class="step-info">
              <div class="step-title">Post-Deploy</div>
            </div>
            <div class="step-status">{{ getStepStatusLabel(7) }}</div>
          </div>
        </div>

      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted, computed, watch, nextTick } from 'vue'

const props = defineProps({
  app: { type: Object, required: true }
})

const nuxtApp = useNuxtApp()
const auth = useAuth()
const history = ref([])
const loading = ref(false)
const selectedHistory = ref(null)
const logs = ref([])
const polling = ref(null)
const terminalRef = ref(null)

const fetchHistory = async () => {
  try {
    loading.value = true
    const data = await nuxtApp.$apiFetch(`/api/deploy/${props.app.id}/history`, {
      headers: auth.authHeaders()
    })
    history.value = data
  } catch (e) {
    console.error('Failed to fetch history', e)
  } finally {
    loading.value = false
  }
}

const fetchJobDetails = async (jobId) => {
  try {
    const jobLogs = await nuxtApp.$apiFetch(`/api/deploy/${props.app.id}/logs?jobId=${jobId}`, {
      headers: auth.authHeaders()
    })
    logs.value = jobLogs

    // Update the selectedHistory if it's currently running to refresh status
    if (selectedHistory.value?.status === 'running') {
      const statusData = await nuxtApp.$apiFetch(`/api/deploy/${props.app.id}/status`, {
        headers: auth.authHeaders()
      })
      if (statusData && statusData.id === jobId) {
        selectedHistory.value = statusData
      }
    }
  } catch (e) {
    console.error('Failed to fetch job details', e)
  }
}

const selectJob = (job) => {
  selectedHistory.value = job
  logs.value = []
  fetchJobDetails(job.id)
}

const startPolling = () => {
  if (polling.value) return
  polling.value = setInterval(() => {
    if (selectedHistory.value) {
      fetchJobDetails(selectedHistory.value.id)
    } else {
      fetchHistory()
    }
  }, 3000)
}

const stopPolling = () => {
  if (polling.value) {
    clearInterval(polling.value)
    polling.value = null
  }
}

onMounted(() => {
  fetchHistory()
  startPolling()
})

onUnmounted(() => {
  stopPolling()
})

// Logic to derive step status from logs and job state
const masterLogs = computed(() => logs.value.filter(l => !l.serverId))
const serverLogsGrouped = computed(() => {
  const groups = {}
  logs.value.filter(l => l.serverId).forEach(l => {
    if (!groups[l.serverId]) groups[l.serverId] = []
    groups[l.serverId].push(l)
  })
  return groups
})

watch(masterLogs, () => {
  nextTick(() => {
    if (terminalRef.value) {
      terminalRef.value.scrollTop = terminalRef.value.scrollHeight
    }
  })
}, { deep: true })

const targetServers = computed(() => {
  // If we had the actual servers used in the job, we'd use that.
  // For now, we derive from logs and application targets.
  const uniqueServerIdsInLogs = Object.keys(serverLogsGrouped.value)
  // This is a bit of a fallback, ideally the API returns targets per job
  return props.app.deployTargets || []
})

const getStepClass = (stepNum) => {
  const status = selectedHistory.value?.status?.toLowerCase()
  if (!selectedHistory.value) return 'pending'

  // Map status to steps
  if (status === 'success') return 'success'
  if (status === 'failed') {
    // Determine where it failed based on logs (simplified)
    if (logs.value.some(l => l.level === 'error')) {
      // If error exists in master logs before server logs, it's build phase (3)
      const errorLog = logs.value.find(l => l.level === 'error')
      if (!errorLog.serverId) return stepNum < 3 ? 'success' : (stepNum === 3 ? 'failed' : 'pending')
      return stepNum < 5 ? 'success' : (stepNum === 5 ? 'failed' : 'pending')
    }
    return 'failed'
  }

  // Running logic
  if (status === 'running' || status === 'queued') {
    if (stepNum === 1) return 'success'
    if (stepNum === 2) return 'success'
    if (stepNum === 3) {
      const hasDeployStarted = logs.value.some(l => l.serverId)
      return hasDeployStarted ? 'success' : 'active'
    }
    if (stepNum === 4) {
      const hasDeployStarted = logs.value.some(l => l.serverId)
      return hasDeployStarted ? 'success' : 'pending'
    }
    if (stepNum === 5) {
      const hasDeployStarted = logs.value.some(l => l.serverId)
      const isFinished = status === 'success' || status === 'failed'
      return isFinished ? 'success' : (hasDeployStarted ? 'active' : 'pending')
    }
    return 'pending'
  }
  return 'pending'
}

const getStepStatusLabel = (stepNum) => {
  const cls = getStepClass(stepNum)
  if (cls === 'success') return 'Done'
  if (cls === 'active') return 'Running...'
  if (cls === 'failed') return 'Failed'
  return 'Pending'
}

const getServerLogs = (serverId) => serverLogsGrouped.value[serverId] || []

const getServerSubStepStatus = (serverId, stepName) => {
  const sLogs = getServerLogs(serverId)
  const msg = sLogs.map(l => l.message.toLowerCase()).join(' ')
  
  if (stepName === 'SSH' && msg.includes('ssh')) return 'success'
  if (stepName === 'Upload' && msg.includes('upload complete')) return 'success'
  if (stepName === 'Restart' && msg.includes('restart')) return 'success'
  if (stepName === 'Health' && msg.includes('health check ok')) return 'success'
  
  // Active check
  if (stepName === 'SSH' && sLogs.length > 0) return 'active'
  if (stepName === 'Upload' && msg.includes('scp')) return 'active'
  if (stepName === 'Restart' && msg.includes('systemctl')) return 'active'
  
  return 'pending'
}

const getServerProgress = (serverId) => {
  const sLogs = getServerLogs(serverId)
  if (sLogs.some(l => l.message.toLowerCase().includes('complete'))) return 100
  if (sLogs.length === 0) return 0
  return Math.min(sLogs.length * 20, 90)
}

const formatTime = (ts) => {
  if (!ts) return ''
  const d = new Date(ts)
  return d.toLocaleString()
}

const handleRollback = () => {
  if (confirm('Trigger rollback for this job?')) {
    nuxtApp.$apiFetch(`/api/deploy/${selectedHistory.value.id}/rollback`, { 
      method: 'POST',
      headers: auth.authHeaders()
    })
      .then(() => alert('Rollback triggered'))
  }
}
</script>

<style scoped>
.deployment-history {
  height: 100%;
}

.empty-state {
  padding: 40px;
  text-align: center;
  color: var(--text3);
  background: var(--bg2);
  border-radius: 12px;
  border: 1px dashed var(--border);
}

/* History List */
.history-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.history-card {
  background: var(--bg2);
  border: 1px solid var(--border);
  border-radius: 8px;
  padding: 10px 14px;
  cursor: pointer;
  transition: all var(--transition);
}
.history-card:hover {
  border-color: var(--accent);
  background: var(--bg3);
}
.history-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 4px;
}
.history-status {
  font-size: 11px;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 5px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}
.status-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}
.history-status.running .status-indicator,
.history-status.queued .status-indicator { background: var(--warning); box-shadow: 0 0 8px var(--warning); }
.history-status.success .status-indicator { background: var(--success); }
.history-status.failed .status-indicator { background: var(--danger); }

.history-status.running, .history-status.queued { color: var(--warning); }
.history-status.success { color: var(--success); }
.history-status.failed { color: var(--danger); }

.history-time {
  font-size: 10px;
  color: var(--text3);
}
.history-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text1);
  margin-bottom: 2px;
}
.history-meta {
  font-size: 11px;
  color: var(--text2);
  display: flex;
  align-items: center;
  gap: 8px;
}
.meta-sep { color: var(--border); }

/* Pipeline View */
.pipeline-view {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.pipeline-header {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 4px;
}
.pipeline-title {
  font-size: 14px;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 8px;
}
.version-badge {
  background: rgba(0, 201, 167, 0.1);
  color: var(--accent);
  padding: 1px 6px;
  border-radius: 4px;
  font-size: 11px;
  font-family: var(--mono);
}
.pipeline-status-badge {
  margin-left: auto;
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  padding: 2px 8px;
  border-radius: 4px;
}
.pipeline-status-badge.success { background: rgba(0, 201, 167, 0.15); color: var(--success); }
.pipeline-status-badge.failed { background: rgba(255, 71, 87, 0.15); color: var(--danger); }
.pipeline-status-badge.running { background: rgba(255, 186, 0, 0.15); color: var(--warning); }

.pipeline-steps {
  display: flex;
  flex-direction: column;
  gap: 8px;
  position: relative;
}
.pipeline-steps::before {
  content: '';
  position: absolute;
  left: 14px;
  top: 10px;
  bottom: 10px;
  width: 1px;
  background: var(--border);
  z-index: 0;
}

.step-card {
  background: var(--bg2);
  border: 1px solid var(--border);
  border-radius: 8px;
  padding: 8px 12px;
  position: relative;
  z-index: 1;
  transition: all 0.2s ease;
}
.step-card.active {
  border-color: var(--accent);
  background: var(--bg3);
}
.step-card.success {
  border-color: var(--border); /* Less prominent border for success */
  background: var(--bg1);
  opacity: 0.9;
}
.step-card.failed {
  border-color: var(--danger);
  background: rgba(255, 71, 87, 0.05);
}
.step-card.pending {
  opacity: 0.5;
}
.hidden-content {
  display: none;
}
.step-card.active .hidden-content,
.step-card.failed .hidden-content {
  display: block;
}
/* For success, we keep content hidden unless explicitly styled otherwise */
.step-card.success .hidden-content {
  display: none;
}

.step-header {
  display: flex;
  align-items: center;
  gap: 10px;
}
.step-icon {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: var(--bg3);
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: 11px;
  border: 1.5px solid var(--border);
  flex-shrink: 0;
}
.step-card.active .step-icon {
  border-color: var(--accent);
  color: var(--accent);
  background: rgba(0, 201, 167, 0.1);
}
.step-card.success .step-icon {
  background: var(--success);
  border-color: var(--success);
  color: #fff;
}
.step-card.failed .step-icon {
  background: var(--danger);
  border-color: var(--danger);
  color: #fff;
}
.step-info {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 8px;
}
.step-title {
  font-size: 13px;
  font-weight: 600;
}
.step-desc {
  font-size: 11px;
  color: var(--text3);
  opacity: 0.8;
}
.step-status {
  font-size: 10px;
  font-weight: 600;
  text-transform: uppercase;
  color: var(--text3);
}
.step-card.active .step-status { color: var(--accent); }
.step-card.success .step-status { color: var(--success); }
.step-card.failed .step-status { color: var(--danger); }

.blinking {
  animation: blink 1.5s infinite;
}
@keyframes blink { 0%, 100% { opacity: 1; } 50% { opacity: 0.5; } }

.step-content {
  margin-top: 8px;
  padding-left: 38px;
}

/* Terminal & Logs */
.terminal-mock {
  background: #080808;
  color: #d0d0d0;
  font-family: var(--mono);
  font-size: 10px;
  padding: 8px 10px;
  border-radius: 6px;
  line-height: 1.5;
  max-height: 150px;
  overflow-y: auto;
  border: 1px solid #1a1a1a;
}
.terminal-mock div.cmd { color: #5ccfe6; }
.terminal-mock div.error { color: #ff3333; }
.terminal-mock div.success { color: #bae67e; }
.terminal-mock div.warn { color: #ffd580; }

.mini-log { font-size: 10px; font-family: var(--mono); margin-top: 2px; opacity: 0.8; }
.mini-log.error { color: var(--danger); }
.mini-log.success { color: var(--success); }

/* Deploy Targets */
.server-matrix { display: flex; flex-direction: column; gap: 8px; }
.target-server { background: var(--bg3); padding: 8px 10px; border-radius: 6px; border: 1px solid var(--border); }
.server-title { display: flex; justify-content: space-between; align-items: center; font-size: 12px; font-weight: 600; margin-bottom: 6px; }
.sub-steps { display: flex; gap: 6px; flex-wrap: wrap; }
.sub-step { padding: 2px 6px; border-radius: 4px; background: rgba(255,255,255,0.05); color: var(--text3); font-size: 9px; font-weight: 600; transition: all 0.2s; }
.sub-step.active { background: rgba(0, 201, 167, 0.1); color: var(--accent); box-shadow: 0 0 0 1px var(--accent); }
.sub-step.success { background: rgba(0, 201, 167, 0.15); color: var(--success); }

.text-red { color: var(--danger); }
.mt-1 { margin-top: 4px; }
.mt-2 { margin-top: 8px; }

.cursor {
  display: inline-block;
  width: 8px;
  height: 12px;
  background: currentColor;
  animation: blink 1s infinite;
  vertical-align: middle;
}
</style>
