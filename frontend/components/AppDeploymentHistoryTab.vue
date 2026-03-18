<template>
  <div class="deployment-history">
    <div v-if="!selectedHistory" class="history-list">
      <div 
        v-for="item in mockHistory" 
        :key="item.id" 
        class="history-card"
        @click="selectedHistory = item"
      >
        <div class="history-header">
          <div class="history-status" :class="item.status.toLowerCase()">
            <span class="status-indicator"></span>
            {{ item.status }}
          </div>
          <div class="history-time">{{ item.time }}</div>
        </div>
        <div class="history-title">Deploy {{ item.version }}</div>
        <div class="history-meta">Commit {{ item.commitHash }} by {{ item.author }}</div>
      </div>
    </div>

    <div v-else class="pipeline-view">
      <div class="pipeline-header">
        <button class="btn btn-ghost btn-sm" @click="selectedHistory = null">
          ← Back to History
        </button>
        <div class="pipeline-title">
          <span>Deployment Pipeline</span>
          <span class="version-badge">{{ selectedHistory.version }}</span>
        </div>
      </div>

      <div class="pipeline-steps">
        <!-- 1. Pre-check -->
        <div class="step-card" :class="getStepClass('precheck')">
          <div class="step-header">
            <div class="step-icon">1</div>
            <div class="step-info">
              <div class="step-title">Pre-check</div>
              <div class="step-desc">Validating environment</div>
            </div>
            <div class="step-status">Done</div>
          </div>
          <div class="step-content">
            <div class="check-grid">
              <div class="check-item success"><span class="icon">✓</span> Disk Space (>5GB)</div>
              <div class="check-item success"><span class="icon">✓</span> Git Installed</div>
              <div class="check-item success"><span class="icon">✓</span> .NET SDK 8.0</div>
              <div class="check-item success"><span class="icon">✓</span> Writable Directory</div>
            </div>
          </div>
        </div>

        <!-- 2. Git Clone / Pull -->
        <div class="step-card" :class="getStepClass('source')">
          <div class="step-header">
            <div class="step-icon">2</div>
            <div class="step-info">
              <div class="step-title">Source Code</div>
              <div class="step-desc">Fetching repositories</div>
            </div>
            <div class="step-status">Done</div>
          </div>
          <div class="step-content repo-list">
            <div class="repo-item">
              <div class="repo-header">
                <span class="repo-badge dependency">Dependency</span>
                <span class="repo-name">HRDesk.Shared</span>
                <span class="repo-commit">#a1b2c3d</span>
              </div>
              <div class="progress-bar"><div class="progress-fill" style="width: 100%"></div></div>
            </div>
            <div class="repo-item">
              <div class="repo-header">
                <span class="repo-badge primary">Primary</span>
                <span class="repo-name">HRDesk.Api</span>
                <span class="repo-commit">#4f5e6d7</span>
              </div>
              <div class="progress-bar"><div class="progress-fill" style="width: 100%"></div></div>
            </div>
          </div>
        </div>

        <!-- 3. Build -->
        <div class="step-card" :class="getStepClass('build')">
          <div class="step-header">
            <div class="step-icon">3</div>
            <div class="step-info">
              <div class="step-title">Build & Publish</div>
              <div class="step-desc">Runtime metadata & compilation</div>
            </div>
            <div class="step-status blinking">Running...</div>
          </div>
          <div class="step-content">
            <div class="build-meta">
              <span><strong>Runtime:</strong> .NET 8.0</span>
              <span><strong>Working Dir:</strong> /var/app/HRDesk.Api</span>
            </div>
            <div class="terminal-mock">
              <div>> dotnet clean</div>
              <div>> dotnet restore HRDesk.Api.csproj</div>
              <div>Restoring packages for /var/app/HRDesk.Api/HRDesk.Api.csproj...</div>
              <div>> dotnet publish -c Release -o ./publish</div>
              <div>Microsoft (R) Build Engine version 17.8.3</div>
              <div>Compiling HRDesk.Shared...</div>
              <div>Compiling HRDesk.Api...<span class="cursor">_</span></div>
            </div>
          </div>
        </div>

        <!-- 4. Artifact Validation -->
        <div class="step-card pending">
          <div class="step-header">
            <div class="step-icon">4</div>
            <div class="step-info">
              <div class="step-title">Artifact Validation</div>
              <div class="step-desc">Verifying build outputs</div>
            </div>
            <div class="step-status">Pending</div>
          </div>
          <div class="step-content hidden-content">
            <ul class="artifact-checks">
              <li>Checking output dir: <strong>./publish</strong></li>
              <li>Validating binary: <strong>HRDesk.Api.dll</strong> exists</li>
              <li>Size validation: <strong>~45MB</strong></li>
            </ul>
          </div>
        </div>

        <!-- 5. Deploy Targets -->
        <div class="step-card pending">
          <div class="step-header">
            <div class="step-icon">5</div>
            <div class="step-info">
              <div class="step-title">Deploy Targets</div>
              <div class="step-desc">Matrix deployment to 3 servers</div>
            </div>
            <div class="step-status">Pending</div>
          </div>
          <div class="step-content hidden-content">
            <div class="server-matrix">
              <!-- Server 1 -->
              <div class="target-server">
                <div class="server-title">
                  <span>Server Alpha (10.0.0.1)</span>
                  <button class="btn btn-ghost btn-xs text-red">Rollback</button>
                </div>
                <div class="sub-steps">
                  <div class="sub-step pending">SSH</div>
                  <div class="sub-step pending">Backup</div>
                  <div class="sub-step pending">Upload</div>
                  <div class="sub-step pending">Restart</div>
                  <div class="sub-step pending">Health</div>
                </div>
                <div class="progress-bar mt-2"><div class="progress-fill" style="width: 0%"></div></div>
              </div>
              
              <!-- Server 2 -->
              <div class="target-server">
                <div class="server-title">
                  <span>Server Beta (10.0.0.2)</span>
                  <button class="btn btn-ghost btn-xs text-red">Rollback</button>
                </div>
                <div class="sub-steps">
                  <div class="sub-step pending">SSH</div>
                  <div class="sub-step pending">Backup</div>
                  <div class="sub-step pending">Upload</div>
                  <div class="sub-step pending">Restart</div>
                  <div class="sub-step pending">Health</div>
                </div>
                <div class="progress-bar mt-2"><div class="progress-fill" style="width: 0%"></div></div>
              </div>

              <!-- Server 3 -->
              <div class="target-server">
                <div class="server-title">
                  <span>Server Gamma (10.0.0.3)</span>
                  <button class="btn btn-ghost btn-xs text-red">Rollback</button>
                </div>
                <div class="sub-steps">
                  <div class="sub-step pending">SSH</div>
                  <div class="sub-step pending">Backup</div>
                  <div class="sub-step pending">Upload</div>
                  <div class="sub-step pending">Restart</div>
                  <div class="sub-step pending">Health</div>
                </div>
                <div class="progress-bar mt-2"><div class="progress-fill" style="width: 0%"></div></div>
              </div>
            </div>
          </div>
        </div>

        <!-- 6. Cleanup -->
        <div class="step-card pending">
          <div class="step-header">
            <div class="step-icon">6</div>
            <div class="step-info">
              <div class="step-title">Cleanup</div>
              <div class="step-desc">Removing temporary workspaces</div>
            </div>
            <div class="step-status">Pending</div>
          </div>
        </div>

        <!-- 7. Post-Deploy -->
        <div class="step-card pending">
          <div class="step-header">
            <div class="step-icon">7</div>
            <div class="step-info">
              <div class="step-title">Post-Deploy</div>
              <div class="step-desc">Logs & Notifications</div>
            </div>
            <div class="step-status">Pending</div>
          </div>
        </div>

      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'

const props = defineProps({
  app: { type: Object, required: true }
})

const selectedHistory = ref(null)

const mockHistory = ref([
  { id: 1, version: 'v1.4.2', status: 'Running', time: 'Just now', commitHash: '4f5e6d7', author: 'Jane Doe' },
  { id: 2, version: 'v1.4.1', status: 'Success', time: '2 hours ago', commitHash: 'b3a2c1d', author: 'John Smith' },
  { id: 3, version: 'v1.4.0', status: 'Failed', time: 'Yesterday', commitHash: 'e9f8d7c', author: 'Jane Doe' },
])

const getStepClass = (stepName) => {
  if (selectedHistory.value?.status === 'Running') {
    if (stepName === 'precheck' || stepName === 'source') return 'success'
    if (stepName === 'build') return 'active'
    return 'pending'
  }
  return 'pending'
}
</script>

<style scoped>
.deployment-history {
  height: 100%;
}

/* History List */
.history-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.history-card {
  background: var(--bg2);
  border: 1px solid var(--border);
  border-radius: 12px;
  padding: 16px;
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
  margin-bottom: 8px;
}
.history-status {
  font-size: 12px;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 6px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}
.status-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}
.history-status.running .status-indicator { background: var(--warning); box-shadow: 0 0 8px var(--warning); }
.history-status.success .status-indicator { background: var(--success); }
.history-status.failed .status-indicator { background: var(--danger); }

.history-status.running { color: var(--warning); }
.history-status.success { color: var(--success); }
.history-status.failed { color: var(--danger); }

.history-time {
  font-size: 11px;
  color: var(--text3);
}
.history-title {
  font-size: 15px;
  font-weight: 600;
  color: var(--text1);
  margin-bottom: 4px;
}
.history-meta {
  font-size: 12px;
  color: var(--text2);
  font-family: var(--mono);
}

/* Pipeline View */
.pipeline-view {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.pipeline-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 8px;
}
.pipeline-title {
  font-size: 16px;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 8px;
}
.version-badge {
  background: rgba(0, 201, 167, 0.12);
  color: var(--accent);
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 12px;
  font-family: var(--mono);
}

.pipeline-steps {
  display: flex;
  flex-direction: column;
  gap: 12px;
  position: relative;
}
.pipeline-steps::before {
  content: '';
  position: absolute;
  left: 20px;
  top: 10px;
  bottom: 10px;
  width: 2px;
  background: var(--border);
  z-index: 0;
}

.step-card {
  background: var(--bg2);
  border: 1px solid var(--border);
  border-radius: 12px;
  padding: 16px;
  position: relative;
  z-index: 1;
  transition: all 0.3s ease;
}
.step-card.active {
  border-color: var(--accent);
  box-shadow: 0 0 0 1px var(--accent);
}
.step-card.success {
  border-color: var(--success);
}
.step-card.pending {
  opacity: 0.6;
}
.hidden-content {
  display: none;
}
.step-card.active .hidden-content,
.step-card.success .hidden-content {
  display: block;
}

.step-header {
  display: flex;
  align-items: center;
  gap: 12px;
}
.step-icon {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: var(--bg3);
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: 14px;
  border: 2px solid var(--border);
}
.step-card.active .step-icon {
  border-color: var(--accent);
  color: var(--accent);
}
.step-card.success .step-icon {
  background: var(--success);
  border-color: var(--success);
  color: #fff;
}
.step-info {
  flex: 1;
}
.step-title {
  font-size: 14px;
  font-weight: 600;
}
.step-desc {
  font-size: 12px;
  color: var(--text3);
}
.step-status {
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
  color: var(--text3);
}
.step-card.active .step-status { color: var(--accent); }
.step-card.success .step-status { color: var(--success); }
.blinking {
  animation: blink 1.5s infinite;
}
@keyframes blink { 0%, 100% { opacity: 1; } 50% { opacity: 0.5; } }

.step-content {
  margin-top: 16px;
  padding-left: 52px;
}

/* Pre-check */
.check-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  gap: 8px;
}
.check-item {
  font-size: 12px;
  display: flex;
  align-items: center;
  gap: 6px;
  background: var(--bg3);
  padding: 6px 10px;
  border-radius: 6px;
}
.check-item.success { color: var(--success); }
.check-item .icon { font-weight: bold; }

/* Source */
.repo-list { display: flex; flex-direction: column; gap: 12px; }
.repo-item { background: var(--bg3); padding: 10px; border-radius: 8px; }
.repo-header { display: flex; align-items: center; gap: 8px; margin-bottom: 8px; font-size: 12px; }
.repo-badge { padding: 2px 6px; border-radius: 4px; font-weight: 600; font-size: 10px; text-transform: uppercase; }
.repo-badge.dependency { background: rgba(139, 127, 255, 0.15); color: var(--purple); }
.repo-badge.primary { background: rgba(0, 201, 167, 0.12); color: var(--accent); }
.repo-name { font-weight: 500; flex: 1; }
.repo-commit { font-family: var(--mono); color: var(--text3); }
.progress-bar { height: 4px; background: rgba(255,255,255,0.1); border-radius: 2px; overflow: hidden; }
.progress-fill { height: 100%; background: var(--accent); transition: width 0.3s ease; }

/* Build */
.build-meta {
  display: flex;
  gap: 16px;
  font-size: 12px;
  margin-bottom: 12px;
  color: var(--text2);
}
.terminal-mock {
  background: #111;
  color: #0f0;
  font-family: var(--mono);
  font-size: 11px;
  padding: 12px;
  border-radius: 8px;
  line-height: 1.5;
  height: 120px;
  overflow-y: auto;
}
.terminal-mock > div { margin-bottom: 4px; }
.cursor {
  display: inline-block;
  width: 8px;
  height: 12px;
  background: currentColor;
  animation: blink 1s infinite;
  vertical-align: middle;
  margin-left: 4px;
}

/* Validation */
.artifact-checks {
  list-style: none;
  padding: 0;
  margin: 0;
  font-size: 12px;
  color: var(--text2);
}
.artifact-checks li { margin-bottom: 6px; padding-left: 12px; position: relative; }
.artifact-checks li::before { content: "•"; position: absolute; left: 0; color: var(--accent); }

/* Deploy */
.server-matrix { display: flex; flex-direction: column; gap: 12px; }
.target-server { background: var(--bg3); padding: 10px; border-radius: 8px; }
.server-title { display: flex; justify-content: space-between; align-items: center; font-size: 13px; font-weight: 600; margin-bottom: 10px; }
.sub-steps { display: flex; justify-content: space-between; font-size: 11px; }
.sub-step { padding: 4px 8px; border-radius: 4px; background: rgba(255,255,255,0.05); color: var(--text3); }
.sub-step.active { background: rgba(0, 201, 167, 0.12); color: var(--accent); box-shadow: 0 0 0 1px var(--accent); }
.sub-step.success { background: rgba(0, 201, 167, 0.12); color: var(--success); }
.text-red { color: var(--danger); }
.mt-2 { margin-top: 8px; }

</style>
