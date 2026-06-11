<template>
  <div class="app-loader">
    <div class="loader-content">
      <div class="loader-spinner"></div>
      <div class="loader-text">Restoring session...</div>
    </div>
  </div>
</template>

<style scoped>
.app-loader {
  position: fixed; inset: 0; background: var(--bg0);
  display: flex; align-items: center; justify-content: center; z-index: 9999;
}
.loader-content { display: flex; flex-direction: column; align-items: center; gap: 16px; }
.loader-spinner {
  width: 28px; height: 28px; border: 2px solid var(--bg3);
  border-top-color: var(--accent); border-radius: 50%;
  animation: spin 0.8s linear infinite;
}
.loader-text { font-size: 12px; color: var(--text2); font-weight: 500; letter-spacing: 0.5px; }
@keyframes spin { to { transform: rotate(360deg); } }
</style>
