<template>
  <div class="tab-targets">
    <div v-if="!app.deployTargets?.length" class="empty-targets">
      <div class="empty-icon">📂</div>
      <p>No deploy targets configured.</p>
    </div>
    
    <div v-for="target in app.deployTargets" :key="target.serverId" class="target-card">
      <div class="target-head">
        <div class="server-info">
          <div class="server-row">
            <span class="server-icon">🖥️</span>
            <div class="server-name">{{ getServer(target.serverId)?.name || target.serverId }}</div>
          </div>
          <div v-if="getServer(target.serverId)?.ip" class="server-ip">
            {{ getServer(target.serverId).ip }}<span v-if="getServer(target.serverId)?.sshPort" class="server-ssh">:{{ getServer(target.serverId).sshPort }}</span>
          </div>
        </div>
        <div class="target-meta">
          <div v-if="target.port" class="target-port-badge">
            <span class="dot"></span>
            Port {{ target.port }}
          </div>
        </div>
      </div>
      
      <div class="target-body">
        <div class="info-grid">
          <div class="info-item">
            <span class="info-label">Deployment Directory</span>
            <div class="info-value">
              <code>{{ target.deployDir }}</code>
            </div>
          </div>
          
          <div v-if="target.serviceName" class="info-item">
            <span class="info-label">Systemd Service</span>
            <div class="info-value">
              <code>{{ target.serviceName }}</code>
            </div>
          </div>

          <div v-if="getServer(target.serverId)?.hostname" class="info-item">
            <span class="info-label">Hostname</span>
            <div class="info-value text-muted">{{ getServer(target.serverId).hostname }}</div>
          </div>

          <div v-if="getServer(target.serverId)?.role" class="info-item">
            <span class="info-label">Server Role</span>
            <div class="info-value">
              <span class="role-tag">{{ getServer(target.serverId).role }}</span>
            </div>
          </div>

          <div v-if="getServer(target.serverId)" class="info-item">
            <span class="info-label">Nginx Status</span>
            <div class="info-value">
              <span v-if="getServer(target.serverId).nginxInstalled" class="status-pill success">
                <span class="pill-dot"></span> Installed
              </span>
              <span v-else class="status-pill warning">
                <span class="pill-dot"></span> Not Detected
              </span>
            </div>
          </div>
        </div>

        <div v-if="getServer(target.serverId)?.tags" class="tags-section">
          <div v-for="tag in getServer(target.serverId).tags.split(',')" :key="tag" class="tag">
            {{ tag.trim() }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'

const props = defineProps({
  app: { type: Object, required: true }
})

const serversCtx = useServers()

onMounted(() => {
  if (serversCtx.servers.length === 0) {
    serversCtx.fetchServers()
  }
})

const getServer = (serverId) => {
  return serversCtx.servers.find(s => s.id === serverId)
}
</script>

<style scoped>
.tab-targets {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 4px;
}

.target-card {
  background: var(--bg2);
  border: 1px solid var(--border);
  border-radius: 12px;
  padding: 16px;
  transition: all 0.25s ease;
  position: relative;
  overflow: hidden;
}

.target-card:hover {
  border-color: var(--accent);
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.12);
  transform: translateY(-1px);
}

.target-head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 16px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--bg3);
}

.server-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.server-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.server-icon {
  font-size: 14px;
  opacity: 0.8;
}

.server-name {
  font-size: 15px;
  font-weight: 700;
  color: var(--text1);
}

.server-ip {
  font-size: 12px;
  font-family: var(--mono);
  color: var(--text3);
  margin-left: 22px;
}

.server-ssh {
  color: var(--accent);
  font-weight: 600;
}

.target-port-badge {
  display: flex;
  align-items: center;
  gap: 6px;
  background: rgba(0, 201, 167, 0.08);
  color: var(--accent);
  padding: 4px 10px;
  border-radius: 20px;
  font-size: 11px;
  font-weight: 700;
  border: 1px solid rgba(0, 201, 167, 0.2);
}

.dot {
  width: 6px;
  height: 6px;
  background: var(--accent);
  border-radius: 50%;
  box-shadow: 0 0 6px var(--accent);
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 16px;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.info-label {
  font-size: 10px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: var(--text3);
}

.info-value {
  font-size: 13px;
  color: var(--text2);
}

.text-muted {
  color: var(--text3);
  font-style: italic;
}

.info-value code {
  background: var(--bg3);
  color: var(--accent);
  padding: 2px 6px;
  border-radius: 4px;
  font-family: var(--mono);
  font-size: 11px;
  border: 1px solid var(--border);
}

.role-tag {
  background: var(--bg3);
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 11px;
  font-weight: 600;
  border: 1px solid var(--border);
}

.status-pill {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 11px;
  font-weight: 600;
}

.status-pill.success {
  background: rgba(0, 201, 167, 0.1);
  color: var(--success);
}

.status-pill.warning {
  background: rgba(255, 186, 0, 0.1);
  color: var(--warning);
}

.pill-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
}

.status-pill.success .pill-dot { background: var(--success); }
.status-pill.warning .pill-dot { background: var(--warning); }

.tags-section {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 16px;
  padding-top: 12px;
  border-top: 1px dashed var(--border);
}

.tag {
  background: rgba(255, 255, 255, 0.05);
  font-size: 10px;
  color: var(--text3);
  padding: 2px 8px;
  border-radius: 10px;
  border: 1px solid var(--border);
}

.empty-targets {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  color: var(--text3);
  background: var(--bg2);
  border: 1px dashed var(--border);
  border-radius: 12px;
  text-align: center;
}

.empty-icon {
  font-size: 32px;
  margin-bottom: 12px;
  opacity: 0.5;
}

.empty-targets p {
  font-size: 14px;
  font-style: italic;
}
</style>
