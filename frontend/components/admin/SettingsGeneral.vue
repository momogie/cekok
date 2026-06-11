<template>
  <div class="panel">
    <div class="panel-header">
      <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="var(--accent)" stroke-width="1.5"><path d="M8 1a7 7 0 1 0 0 14A7 7 0 0 0 8 1zM8 4v4m0 4h.01" /></svg>
      <span class="panel-title">System Info</span>
    </div>
    <div class="panel-body">
      <div class="form-group">
        <label>App Version</label>
        <input value="v1.0.0-stable" disabled />
      </div>
    </div>
  </div>
</template>

<style scoped>
.panel {
  background: var(--bg1); border: 1px solid var(--border);
  border-radius: var(--radius); display: flex; flex-direction: column; overflow: hidden;
  height: fit-content;
}
.panel-header {
  padding: 12px 16px; border-bottom: 1px solid var(--border);
  display: flex; align-items: center; gap: 10px;
  background: rgba(255,255,255,0.02);
}
.panel-title { font-size: 12px; font-weight: 600; text-transform: uppercase; color: var(--text2); letter-spacing: 0.5px; }
.panel-body { padding: 20px; display: flex; flex-direction: column; gap: 16px; }
.form-group { display: flex; flex-direction: column; gap: 6px; }
label { font-size: 11px; font-weight: 500; color: var(--text3); text-transform: uppercase; }
input {
  background: var(--bg2); border: 1px solid var(--border);
  border-radius: 6px; padding: 8px 12px; color: var(--text1);
  font-size: 13px; outline: none;
}
</style>
