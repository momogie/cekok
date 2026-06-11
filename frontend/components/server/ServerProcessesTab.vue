<template>
  <div class="tab-pane">
    <div class="proc-table-wrap">
      <table class="proc-table">
        <thead>
          <tr>
            <th style="width:50px">PID</th>
            <th style="width:50px">CPU</th>
            <th style="width:50px">MEM</th>
            <th>COMMAND</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in stats?.processes" :key="p.pid">
            <td class="mono">{{ p.pid }}</td>
            <td class="mono pct" :class="usageColor(parseFloat(p.cpu))">{{ p.cpu }}%</td>
            <td class="mono">{{ p.mem }}%</td>
            <td class="cmd" :title="p.command">{{ p.command }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  stats: Object
})

const usageColor = (val) => {
  if (val > 80) return 'mf-hi'
  if (val > 55) return 'mf-md'
  return 'mf-lo'
}
</script>

<style scoped>
.tab-pane { display: flex; flex-direction: column; }
.proc-table-wrap { padding: 12px; overflow-x: auto; }
.proc-table { width: 100%; border-collapse: collapse; font-size: 11px; }
.proc-table th { text-align: left; padding: 6px 8px; color: var(--text3); font-weight: 600; font-size: 9px; text-transform: uppercase; border-bottom: 1px solid var(--border); }
.proc-table td { padding: 6px 8px; border-bottom: 1px solid var(--border); color: var(--text2); }
.proc-table tr:hover { background: var(--bg2); }
.proc-table .mono { font-family: var(--mono); }
.proc-table .cmd { color: var(--text1); max-width: 200px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; font-family: var(--mono); font-size: 10px; }
.proc-table .pct { background: transparent; }
.proc-table .pct.mf-hi { color: var(--danger); font-weight: 700; }
.proc-table .pct.mf-md { color: var(--warn); font-weight: 700; }
.proc-table .pct.mf-lo { color: var(--accent); font-weight: 600; }

.mf-lo { color: var(--accent); }
.mf-md { color: var(--warn); }
.mf-hi { color: var(--danger); }
</style>
