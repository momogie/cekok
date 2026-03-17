<template>
  <NuxtLayout>
    <NuxtPage />
  </NuxtLayout>
</template>

<style>
/* ── Reset & Typography ── */
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
  --radius: 10px;
  --radius-sm: 6px;
  --font: 'Sora', sans-serif;
  --mono: 'JetBrains Mono', monospace;
  --sidebar: 220px;
  --transition: 0.2s ease;
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
  font-size: 14px;
  line-height: 1.5;
  transition: background var(--transition), color var(--transition);
}
#__nuxt { height: 100%; }

/* Scrollbar */
::-webkit-scrollbar { width:5px; height:5px; }
::-webkit-scrollbar-track { background:transparent; }
::-webkit-scrollbar-thumb { background:var(--border2); border-radius:99px; }

/* Global Utilities */
.btn {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 6px 14px; border-radius: var(--radius-sm);
  font-size: 12px; font-weight: 500; cursor: pointer;
  border: none; font-family: var(--font); transition: all var(--transition);
}
.btn-primary { background: var(--accent); color: #0d1814; }
.btn-primary:hover { opacity: 0.88; transform: translateY(-1px); }
.btn-primary:disabled { opacity: 0.5; cursor: not-allowed; }
.btn-ghost { background: var(--bg3); color: var(--text2); border: 1px solid var(--border2); }
.btn-ghost:hover { color: var(--text1); background: var(--bg2); }
.btn-danger { background: rgba(240,80,96,0.15); color: var(--danger); border: 1px solid rgba(240,80,96,0.25); }
.btn-danger:hover { background: rgba(240,80,96,0.25); }

</style>
