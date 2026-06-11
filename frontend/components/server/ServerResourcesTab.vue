<template>
  <div class="tab-pane">
    <div class="res-grid">
      <div class="rc">
        <div class="rc-top">
          <span class="rc-lbl">CPU</span>
          <span class="rc-val">{{ stats?.cpuUsage?.toFixed(1) || 0 }}%</span>
        </div>
        <div class="rc-bar"><div class="rc-fill" :class="usageColor(stats?.cpuUsage)" :style="{ width: (stats?.cpuUsage || 0) + '%' }"></div></div>
        <div class="rc-sub">{{ sysInfo?.cpuCores || '...' }} vCPU</div>
      </div>
      
      <div class="rc">
        <div class="rc-top">
          <span class="rc-lbl">Memory</span>
          <span class="rc-val">{{ ramPercent }}%</span>
        </div>
        <div class="rc-bar"><div class="rc-fill" :class="usageColor(ramPercent)" :style="{ width: ramPercent + '%' }"></div></div>
        <div class="rc-sub">{{ stats?.ramUsed || 0 }}MB / {{ stats?.ramTotal || 0 }}MB</div>
      </div>

      <div class="rc" v-if="stats?.swapTotal > 0">
        <div class="rc-top">
          <span class="rc-lbl">Swap</span>
          <span class="rc-val">{{ swapPercent }}%</span>
        </div>
        <div class="rc-bar"><div class="rc-fill" :class="usageColor(swapPercent)" :style="{ width: swapPercent + '%' }"></div></div>
        <div class="rc-sub">{{ stats?.swapUsed || 0 }}MB / {{ stats?.swapTotal || 0 }}MB</div>
      </div>
    </div>

    <div class="disk-section">
      <div class="section-hdr">STORAGE</div>
      <div class="disk-list">
        <div class="disk-item" v-for="d in stats?.disks" :key="d.mount">
           <div class="disk-top">
             <div class="disk-name">
                <svg width="10" height="10" viewBox="0 0 16 16" fill="none" stroke="currentColor"><path d="M2 4v8a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2V4a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2z" stroke-width="1.2"/><path d="M2 8h12M6 8v6M10 8v6" stroke-width="1.2"/></svg>
                {{ d.mount }}
             </div>
             <span class="disk-use-val">{{ d.percent }}</span>
           </div>
           <div class="rc-bar"><div class="rc-fill" :class="usageColor(parseInt(d.percent))" :style="{ width: d.percent }"></div></div>
           <div class="disk-meta">{{ d.used }}MB / {{ d.total }}MB · {{ d.fileSystem }}</div>
        </div>
      </div>
    </div>

    <div class="io-row">
      <div class="io-item">
        <div class="io-lbl">Download</div>
        <div class="io-val" style="color:var(--accent2)">{{ downloadSpeed.split(' ')[0] }}<span style="font-size:10px;color:var(--text3)"> {{ downloadSpeed.split(' ')[1] }}</span></div>
      </div>
      <div class="io-item">
        <div class="io-lbl">Upload</div>
        <div class="io-val" style="color:var(--purple)">{{ uploadSpeed.split(' ')[0] }}<span style="font-size:10px;color:var(--text3)"> {{ uploadSpeed.split(' ')[1] }}</span></div>
      </div>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  stats: Object,
  sysInfo: Object,
  downloadSpeed: String,
  uploadSpeed: String
})

const ramPercent = computed(() => {
  if (!props.stats?.ramTotal) return 0
  return Math.round((props.stats.ramUsed / props.stats.ramTotal) * 100)
})

const swapPercent = computed(() => {
  if (!props.stats?.swapTotal) return 0
  return Math.round((props.stats.swapUsed / props.stats.swapTotal) * 100)
})

const usageColor = (val) => {
  if (val > 80) return 'mf-hi'
  if (val > 55) return 'mf-md'
  return 'mf-lo'
}
</script>

<style scoped>
.tab-pane { display: flex; flex-direction: column; }
.res-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 8px; padding: 10px 12px; }
.rc { background: var(--bg2); border: 1px solid var(--border); border-radius: 7px; padding: 8px 10px; }
.rc-top { display: flex; align-items: center; justify-content: space-between; margin-bottom: 5px; }
.rc-lbl { font-size: 9px; color: var(--text3); font-weight: 500; text-transform: uppercase; letter-spacing: .5px; }
.rc-val { font-size: 15px; font-weight: 600; font-family: var(--mono); }
.rc-bar { height: 4px; background: var(--bg3); border-radius: 99px; overflow: hidden; }
.rc-fill { height: 100%; border-radius: 99px; transition: width .6s ease; }
.rc-sub { font-size: 9px; color: var(--text3); margin-top: 4px; font-family: var(--mono); }

.mf-lo { background: var(--accent); }
.mf-md { background: var(--warn); }
.mf-hi { background: var(--danger); }

.io-row { display: flex; border-top: 1px solid var(--border); margin-top: auto; }
.io-item { flex: 1; padding: 8px 12px; border-right: 1px solid var(--border); }
.io-item:last-child { border-right: none; }
.io-lbl { font-size: 9px; color: var(--text3); text-transform: uppercase; letter-spacing: .4px; margin-bottom: 3px; }
.io-val { font-size: 14px; font-weight: 600; font-family: var(--mono); }

.disk-section { padding: 0 12px 12px 12px; }
.section-hdr { font-size: 9px; color: var(--text3); font-weight: 600; letter-spacing: 1px; margin-bottom: 8px; }
.disk-list { display: flex; flex-direction: column; gap: 8px; }
.disk-item { background: var(--bg2); border: 1px solid var(--border); border-radius: 6px; padding: 8px 10px; }
.disk-top { display: flex; justify-content: space-between; align-items: center; margin-bottom: 5px; }
.disk-name { font-size: 11px; font-weight: 600; font-family: var(--mono); display: flex; align-items: center; gap: 5px; color: var(--text2); }
.disk-use-val { font-size: 11px; font-weight: 600; font-family: var(--mono); }
.disk-meta { font-size: 9px; color: var(--text3); margin-top: 5px; font-family: var(--mono); }
</style>
