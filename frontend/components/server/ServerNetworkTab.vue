<template>
  <div class="tab-pane">
    <div class="net-stats-summary">
      <div class="stat-box">
         <div class="stat-lbl">RX SPEED</div>
         <div class="stat-val" style="color:var(--accent2)">{{ downloadSpeed }}</div>
      </div>
      <div class="stat-box">
         <div class="stat-lbl">TX SPEED</div>
         <div class="stat-val" style="color:var(--purple)">{{ uploadSpeed }}</div>
      </div>
    </div>

    <div class="net-section">
      <div class="section-hdr">INTERFACES</div>
      <div class="net-list">
        <div class="net-item" v-for="iface in stats?.interfaces" :key="iface.name">
          <div class="net-item-hdr">
             <div class="net-name">
               <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M2 13h12M4 13V3m8 10V3M4 6h8M4 9h8"/></svg>
               {{ iface.name }}
             </div>
             <span class="net-status">Online</span>
          </div>
          <div class="net-traffic-row">
            <div class="nt-item">
              <svg width="8" height="8" viewBox="0 0 16 16" fill="none" stroke="currentColor"><path d="M8 12V4m0 8l-3-3m3 3l3-3" stroke-width="1.5"/></svg>
              <span class="nt-lbl">RX:</span> <span class="nt-val">{{ iface.rxSpeed || '0 B/s' }}</span>
            </div>
            <div class="nt-item">
              <svg width="8" height="8" viewBox="0 0 16 16" fill="none" stroke="currentColor"><path d="M8 4v8m0-8l-3 3m3-3l3 3" stroke-width="1.5"/></svg>
              <span class="nt-lbl">TX:</span> <span class="nt-val">{{ iface.txSpeed || '0 B/s' }}</span>
            </div>
          </div>
          <div class="net-addrs">
            <div class="net-addr" v-for="addr in iface.addresses" :key="addr">{{ addr }}</div>
          </div>
        </div>
      </div>
    </div>

    <!-- Firewall Section -->
    <div class="net-section" v-if="stats?.fwType && stats.fwType !== 'none'">
      <div class="section-hdr">
        {{ stats.fwType.toUpperCase() }} 
        <span class="fw-status-badge" :class="stats.fwStatus">
          {{ stats.fwStatus?.toUpperCase() || 'UNKNOWN' }}
        </span>
      </div>
      <div class="fw-grid" v-if="stats?.fwStatus === 'active' && stats?.fwPorts?.length > 0">
        <div class="fw-item" v-for="fw in stats.fwPorts" :key="fw">
          <svg width="10" height="10" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M8 2l6 3v5c0 4-6 5-6 5s-6-1-6-5V5l6-3z"/></svg>
          {{ fw }}
        </div>
      </div>
      <div v-else class="fw-empty">
        {{ stats?.fwStatus === 'active' ? 'No specific ports allowed.' : 'Firewall is currently disabled.' }}
      </div>
    </div>

    <div class="net-section">
      <div class="section-hdr">LISTENING PORTS</div>
      <div class="port-grid">
        <div class="port-item" v-for="p in stats?.ports" :key="p.proto + p.port">
          <span class="port-proto" :class="p.proto">{{ p.proto }}</span>
          <span class="port-val">{{ p.port }}</span>
          <span class="port-addr">{{ p.address.split(':').slice(0,-1).join(':') || '*' }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  stats: Object,
  downloadSpeed: String,
  uploadSpeed: String
})
</script>

<style scoped>
.tab-pane { display: flex; flex-direction: column; }
.net-stats-summary { display: flex; gap: 8px; padding: 12px; }
.stat-box { flex: 1; background: var(--bg2); border: 1px solid var(--border); border-radius: 7px; padding: 10px; }
.stat-lbl { font-size: 9px; color: var(--text3); font-weight: 600; letter-spacing: .5px; margin-bottom: 4px; }
.stat-val { font-size: 16px; font-weight: 700; font-family: var(--mono); }

.net-section { padding: 0 12px 16px 12px; }
.net-list { display: flex; flex-direction: column; gap: 8px; }
.net-item { background: var(--bg2); border: 1px solid var(--border); border-radius: 7px; padding: 10px; }
.net-item-hdr { display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px; }
.net-name { font-size: 12px; font-weight: 600; font-family: var(--mono); color: var(--text1); display: flex; align-items: center; gap: 6px; }
.net-status { font-size: 9px; background: rgba(0,201,167,0.1); color: var(--accent); padding: 1px 6px; border-radius: 4px; border: 1px solid rgba(0,201,167,0.2); }
.net-addrs { display: flex; flex-wrap: wrap; gap: 4px; }
.net-traffic-row { display: flex; gap: 12px; margin-bottom: 8px; background: var(--bg3); padding: 4px 8px; border-radius: 4px; border: 1px solid var(--border); }
.nt-item { display: flex; align-items: center; gap: 4px; font-size: 10px; font-family: var(--mono); }
.nt-lbl { color: var(--text3); font-weight: 500; font-size: 9px; }
.nt-val { color: var(--text1); font-weight: 600; }
.net-addr { font-size: 10px; font-family: var(--mono); color: var(--text3); background: var(--bg3); padding: 1px 6px; border-radius: 3px; }

.fw-grid { display: flex; flex-wrap: wrap; gap: 6px; }
.fw-item { background: rgba(0,201,167,0.06); border: 1px solid rgba(0,201,167,0.15); border-radius: 5px; padding: 4px 10px; font-size: 11px; font-weight: 600; font-family: var(--mono); color: var(--accent); display: flex; align-items: center; gap: 6px; }
.fw-item svg { color: var(--accent); opacity: 0.8; }
.fw-empty { font-size: 10px; color: var(--text3); font-style: italic; background: var(--bg2); padding: 6px 10px; border-radius: 5px; border: 1px dashed var(--border); }

.fw-status-badge { font-size: 8px; padding: 1px 5px; border-radius: 4px; margin-left: 6px; vertical-align: middle; }
.fw-status-badge.active { background: rgba(0,201,167,0.15); color: var(--accent); }
.fw-status-badge.inactive { background: rgba(239,68,68,0.1); color: var(--danger); }

.port-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(100px, 1fr)); gap: 6px; }
.port-item { background: var(--bg2); border: 1px solid var(--border); border-radius: 5px; padding: 6px 8px; display: flex; align-items: center; gap: 6px; }
.port-proto { font-size: 8px; font-weight: 700; text-transform: uppercase; padding: 1px 4px; border-radius: 3px; }
.port-proto.tcp { background: rgba(0,184,212,0.1); color: #00b8d4; }
.port-proto.udp { background: rgba(156,39,176,0.1); color: #9c27b0; }
.port-val { font-size: 12px; font-weight: 700; font-family: var(--mono); color: var(--text1); }
.port-addr { font-size: 9px; color: var(--text3); font-family: var(--mono); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
</style>
