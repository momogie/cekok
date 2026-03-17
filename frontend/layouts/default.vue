<template>
  <div class="shell">
    <aside class="sidebar">
      <div class="sidebar-logo">
        <div class="logo-icon">C</div>
        <div class="logo-text">Ce<span>kok</span></div>
      </div>
      <nav class="sidebar-nav">
        <div class="nav-section">
          <div class="nav-label">Overview</div>
          <NuxtLink to="/dashboard" class="nav-item" active-class="active">
            <svg class="nav-icon" viewBox="0 0 16 16" fill="none"><rect x="1" y="1" width="6" height="6" rx="1.5" fill="currentColor" opacity=".7"/><rect x="9" y="1" width="6" height="6" rx="1.5" fill="currentColor"/><rect x="1" y="9" width="6" height="6" rx="1.5" fill="currentColor"/><rect x="9" y="9" width="6" height="6" rx="1.5" fill="currentColor" opacity=".7"/></svg>
            Dashboard
          </NuxtLink>
          <NuxtLink to="/servers" class="nav-item" active-class="active">
            <svg class="nav-icon" viewBox="0 0 16 16" fill="none"><rect x="2" y="2" width="12" height="5" rx="1.5" stroke="currentColor" stroke-width="1.2"/><rect x="2" y="9" width="12" height="5" rx="1.5" stroke="currentColor" stroke-width="1.2"/><circle cx="13" cy="4.5" r="1" fill="currentColor"/><circle cx="13" cy="11.5" r="1" fill="currentColor"/></svg>
            Servers
          </NuxtLink>
        </div>
        <div class="nav-section" v-if="auth.user?.role === 'admin'">
          <div class="nav-label">Admin</div>
          <NuxtLink to="/admin/users" class="nav-item" active-class="active">
            <svg class="nav-icon" viewBox="0 0 16 16" fill="none"><circle cx="8" cy="6" r="3" stroke="currentColor" stroke-width="1.2"/><path d="M3 14c0-2.8 2-5 5-5s5 2.2 5 5" stroke="currentColor" stroke-width="1.2" stroke-linecap="round"/></svg>
            Users
          </NuxtLink>
        </div>
      </nav>
      <div class="sidebar-footer">
        <div class="theme-toggle" @click="toggleTheme">
          <svg width="12" height="12" viewBox="0 0 16 16" fill="none"><circle cx="8" cy="8" r="3" stroke="currentColor" stroke-width="1.3"/><path d="M8 1v2M8 13v2M1 8h2M13 8h2M3.2 3.2l1.4 1.4M11.4 11.4l1.4 1.4M3.2 12.8l1.4-1.4M11.4 4.6l1.4-1.4" stroke="currentColor" stroke-width="1.3" stroke-linecap="round"/></svg>
          <span>Theme</span>
          <div class="toggle-track"><div class="toggle-thumb" :style="!isDark ? 'transform:translateX(13px)' : ''"></div></div>
        </div>
        <div class="user-info" @click="auth.logout()">
          <div class="user-name">
            {{ (auth.user?.displayName || 'User').split(' ')[0] }}
            <span class="logout">Logout</span>
          </div>
        </div>
      </div>
    </aside>

    <main class="main">
      <slot />
    </main>
  </div>
</template>

<script setup>
const auth = useAuth()
const isDark = ref(true)

onMounted(() => {
  if (import.meta.client) {
    const saved = localStorage.getItem('theme')
    if (saved) isDark.value = saved === 'dark'
    updateTheme()
  }
})

const toggleTheme = () => {
  isDark.value = !isDark.value
  if (import.meta.client) localStorage.setItem('theme', isDark.value ? 'dark' : 'light')
  updateTheme()
}

const updateTheme = () => {
  if (import.meta.client) document.documentElement.setAttribute('data-theme', isDark.value ? 'dark' : 'light')
}
</script>

<style scoped>
.shell { display:flex; height:100vh; overflow:hidden; }
.sidebar {
  width: var(--sidebar); flex-shrink:0;
  background:var(--bg1); border-right:1px solid var(--border);
  display:flex; flex-direction:column; overflow:hidden;
  transition:background var(--transition);
}
.sidebar-logo {
  padding: 16px 16px 14px;
  display:flex; align-items:center; gap:8px;
  border-bottom:1px solid var(--border);
}
.logo-icon {
  width:26px; height:26px; border-radius:6px;
  background:linear-gradient(135deg,var(--accent),var(--accent2));
  display:flex; align-items:center; justify-content:center;
  font-size:13px; font-weight:700; color:#fff; letter-spacing:-0.5px;
  flex-shrink:0;
}
.logo-text { font-size:13px; font-weight:600; letter-spacing:-0.3px; }
.logo-text span { color:var(--accent); }

.sidebar-nav { flex:1; padding:12px 12px; overflow-y:auto; }
.nav-section { margin-bottom:20px; }
.nav-label { font-size:10px; font-weight:600; letter-spacing:1.2px; text-transform:uppercase; color:var(--text3); padding:0 10px; margin-bottom:6px; }
.nav-item {
  display:flex; align-items:center; gap:12px; text-decoration: none;
  padding:9px 12px; border-radius:var(--radius);
  color:var(--text2); cursor:pointer; transition:all var(--transition);
  font-size:13px; font-weight:400; user-select:none;
}
.nav-item:hover { background:var(--bg2); color:var(--text1); }
.nav-item.active { background:rgba(0, 201, 167, 0.1); color:var(--accent); font-weight:500; }
.nav-icon { width:14px; height:14px; opacity:0.8; flex-shrink:0; }

.sidebar-footer { padding:12px 12px; border-top:1px solid var(--border); display:flex; flex-direction:column; gap:8px; }
.theme-toggle {
  display:flex; align-items:center; gap:12px;
  padding:8px 12px; border-radius:var(--radius);
  cursor:pointer; color:var(--text2); font-size:13px;
  transition:all var(--transition);
}
.theme-toggle:hover { background:var(--bg2); color:var(--text1); }
.toggle-track {
  width:28px; height:15px; border-radius:99px; background:var(--bg3);
  border:1px solid var(--border2); position:relative; transition:background var(--transition);
  margin-left:auto; flex-shrink:0;
}
.toggle-thumb {
  position:absolute; top:2px; left:2px;
  width:9px; height:9px; border-radius:50%; background:#fff;
  transition:transform var(--transition);
}

.user-info {
  display:flex; align-items:center; gap:12px;
  padding:8px 12px; border-radius:var(--radius);
  color:var(--text2); font-size:13px; cursor:pointer;
}
.user-info:hover { background:rgba(240,80,96,0.1); color:var(--danger); }
.user-info:hover .logout { display:inline; color:var(--danger); }
.logout { display:none; margin-left:auto; font-size:9px; font-weight:600; text-transform:uppercase; }

.main { flex:1; display:flex; flex-direction:column; overflow:hidden; }
</style>
