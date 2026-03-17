<template>
  <NuxtLayout>
    <div v-if="loading" class="app-loader">
      <div class="loader-content">
        <div class="loader-spinner"></div>
        <div class="loader-text">Restoring session...</div>
      </div>
    </div>
    <NuxtPage v-else />
  </NuxtLayout>
</template>

<script setup>
const auth = useAuth()
const loading = ref(true)

onMounted(async () => {
  if (import.meta.client) {
    const hasToken = localStorage.getItem('cekok_refresh')
    if (hasToken) {
      await auth.refresh()
    }
    loading.value = false
  }
})
</script>

<style>
/* ── Compact Reset & Typography ── */
:root {
  --bg0: #0d0f14;
  --bg1: #13161e;
  --bg2: #1a1e28;
  --bg3: #222636;
  --border: rgba(255,255,255,0.07);
  --border2: rgba(255,255,255,0.13);
  --text1: #e8eaf0;
  --text2: #8b8fa8;
  --text3: #4e5268;
  --accent: #00c9a7;
  --accent2: #0097ff;
  --warn: #f0a500;
  --danger: #f05060;
  --success: #00c9a7;
  --purple: #8b7fff;
  --radius: 8px;
  --radius-sm: 5px;
  --font: 'Sora', sans-serif;
  --mono: 'JetBrains Mono', monospace;
  --sidebar: 240px;
  --transition: 0.15s ease;
}

[data-theme="light"] {
  --bg0: #f0f2f7;
  --bg1: #ffffff;
  --bg2: #f5f6fa;
  --bg3: #eaecf3;
  --border: rgba(0,0,0,0.07);
  --border2: rgba(0,0,0,0.13);
  --text1: #1a1d2e;
  --text2: #5a5e7a;
  --text3: #a0a4b8;
  --accent: #009e84;
  --accent2: #0077cc;
}

* { margin:0; padding:0; box-sizing:border-box; }
html, body {
  height: 100%;
  font-family: var(--font);
  background: var(--bg0);
  color: var(--text1);
  font-size: 13px;
  line-height: 1.4;
  transition: background var(--transition), color var(--transition);
}
#__nuxt { height: 100%; }

/* Compact Scrollbar */
::-webkit-scrollbar { width:4px; height:4px; }
::-webkit-scrollbar-track { background:transparent; }
::-webkit-scrollbar-thumb { background:var(--border2); border-radius:99px; }

/* Global Utilities */
.btn {
  display: inline-flex; align-items: center; gap: 5px;
  padding: 4px 11px; border-radius: var(--radius-sm);
  font-size: 11px; font-weight: 500; cursor: pointer;
  border: none; font-family: var(--font); transition: all var(--transition);
}
.btn-primary { background: var(--accent); color: #0d1814; }
.btn-primary:hover { opacity: 0.88; transform: translateY(-1px); }
.btn-primary:disabled { opacity: 0.5; cursor: not-allowed; }
.btn-ghost { background: var(--bg3); color: var(--text2); border: 1px solid var(--border2); }
.btn-ghost:hover { color: var(--text1); background: var(--bg2); }
.btn-danger { background: rgba(240,80,96,0.15); color: var(--danger); border: 1px solid rgba(240,80,96,0.25); }
.btn-danger:hover { background: rgba(240,80,96,0.25); }

/* App Loader */
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

/* Transitions */
.fade-enter-active, .fade-leave-active { transition: opacity 0.15s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }

.slide-enter-active, .slide-leave-active { transition: transform 0.2s cubic-bezier(0.4, 0, 0.2, 1); }
.slide-enter-from, .slide-leave-to { transform: translateX(100%); }

</style>
