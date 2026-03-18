<template>
  <div class="shell" :class="{ 'is-collapsed': isCollapsed }">
    <aside class="sidebar">
      <div class="sidebar-logo">
        <div class="logo-icon">C</div>
        <div class="logo-text">Ce<span>kok</span></div>
      </div>
      <button class="collapse-toggle" @click="isCollapsed = !isCollapsed" :title="isCollapsed ? 'Expand Sidebar' : 'Collapse Sidebar'">
        <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.8">
          <path d="M10 4L6 8L10 12" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </button>
      <nav class="sidebar-nav">
        <div class="nav-section">
          <div class="nav-label">Overview</div>
          <NuxtLink to="/dashboard" class="nav-item" active-class="active" title="Dashboard">
            <svg class="nav-icon" viewBox="0 0 16 16" fill="none"><rect x="1" y="1" width="6" height="6" rx="1.5" fill="currentColor" opacity=".7"/><rect x="9" y="1" width="6" height="6" rx="1.5" fill="currentColor"/><rect x="1" y="9" width="6" height="6" rx="1.5" fill="currentColor"/><rect x="9" y="9" width="6" height="6" rx="1.5" fill="currentColor" opacity=".7"/></svg>
            <span class="nav-text">Dashboard</span>
          </NuxtLink>
          <NuxtLink to="/apps" class="nav-item" active-class="active" title="Applications">
            <svg class="nav-icon" viewBox="0 0 16 16" fill="none"><rect x="1" y="3" width="14" height="10" rx="2" stroke="currentColor" stroke-width="1.2"/><path d="M5 7h6M5 10h4" stroke="currentColor" stroke-width="1.2" stroke-linecap="round"/></svg>
            <span class="nav-text">Applications</span>
          </NuxtLink>
          <NuxtLink to="/servers" class="nav-item" active-class="active" title="Servers">
            <svg class="nav-icon" viewBox="0 0 16 16" fill="none"><rect x="2" y="2" width="12" height="5" rx="1.5" stroke="currentColor" stroke-width="1.2"/><rect x="2" y="9" width="12" height="5" rx="1.5" stroke="currentColor" stroke-width="1.2"/><circle cx="13" cy="4.5" r="1" fill="currentColor"/><circle cx="13" cy="11.5" r="1" fill="currentColor"/></svg>
            <span class="nav-text">Servers</span>
          </NuxtLink>
        </div>
        <div class="nav-section" v-if="auth.user?.role === 'admin'">
          <div class="nav-label">Admin</div>
          <NuxtLink to="/admin/users" class="nav-item" active-class="active" title="Users">
            <svg class="nav-icon" viewBox="0 0 16 16" fill="none"><circle cx="8" cy="6" r="3" stroke="currentColor" stroke-width="1.2"/><path d="M3 14c0-2.8 2-5 5-5s5 2.2 5 5" stroke="currentColor" stroke-width="1.2" stroke-linecap="round"/></svg>
            <span class="nav-text">Users</span>
          </NuxtLink>
          <NuxtLink to="/admin/settings" class="nav-item" active-class="active" title="Settings">
            <svg class="nav-icon" viewBox="0 0 16 16" fill="none"><path d="M8 11a3 3 0 100-6 3 3 0 000 6z" stroke="currentColor" stroke-width="1.2"/><path d="M12.9 8.6l1.1.8c.1.1.1.3 0 .4l-1 1.7c-.1.1-.3.1-.4.1l-1.3-.5c-.3.2-.5.4-.8.5l-.2 1.4c0 .1-.2.2-.3.2h-2c-.1 0-.3-.1-.3-.2l-.2-1.4c-.3-.1-.5-.3-.8-.5l-1.3.5c-.1 0-.3 0-.4-.1l-1-1.7c-.1-.1-.1-.3 0-.4l1.1-.8c0-.2 0-.4 0-.6l-1.1-.8c-.1-.1-.1-.3 0-.4l1-1.7c.1-.1.3-.1.4-.1l1.3.5c.3-.2.5-.4.8-.5l.2-1.4c0-.1.2-.2.3-.2h2c.1 0 .3.1.3.2l.2 1.4c.3.1.5.3.8.5l1.3-.5c.1 0 .3 0 .4.1l1 1.7c.1.1.1.3 0 .4l-1.1.8c0 .2 0 .4 0 .6z" stroke="currentColor" stroke-width="1.2" stroke-linejoin="round"/></svg>
            <span class="nav-text">Settings</span>
          </NuxtLink>
        </div>
      </nav>
      <div class="sidebar-footer">
        <div class="theme-toggle" @click="toggleTheme" title="Toggle Theme">
          <svg class="nav-icon" viewBox="0 0 16 16" fill="none"><circle cx="8" cy="8" r="3" stroke="currentColor" stroke-width="1.3"/><path d="M8 1v2M8 13v2M1 8h2M13 8h2M3.2 3.2l1.4 1.4M11.4 11.4l1.4 1.4M3.2 12.8l1.4-1.4M11.4 4.6l1.4-1.4" stroke="currentColor" stroke-width="1.3" stroke-linecap="round"/></svg>
          <span class="nav-text">Theme</span>
          <div class="toggle-track" v-if="!isCollapsed"><div class="toggle-thumb" :style="!isDark ? 'transform:translateX(13px)' : ''"></div></div>
        </div>
        <div class="user-info" @click="auth.logout()" title="Logout">
          <div class="user-avatar" v-if="isCollapsed">{{ (auth.user?.displayName || 'U').charAt(0).toUpperCase() }}</div>
          <div class="user-name" v-else>
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
const isCollapsed = ref(false)

onMounted(() => {
  if (import.meta.client) {
    const saved = localStorage.getItem('theme')
    if (saved) isDark.value = saved === 'dark'
    updateTheme()
    
    const collapsed = localStorage.getItem('sidebar_collapsed')
    if (collapsed) isCollapsed.value = collapsed === 'true'
  }
})

watch(isCollapsed, (val) => {
  if (import.meta.client) localStorage.setItem('sidebar_collapsed', val)
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
.shell { display:flex; height:100vh; overflow:hidden; --sidebar: 240px; }
.shell.is-collapsed { --sidebar: 68px; }

.sidebar {
  width: var(--sidebar); flex-shrink:0;
  background:var(--bg1); border-right:1px solid var(--border);
  display:flex; flex-direction:column;
  transition: width 0.3s cubic-bezier(0.4, 0, 0.2, 1), background var(--transition);
  position: relative;
  z-index: 10;
}

.sidebar-logo {
  height: 57px;
  padding: 0 16px;
  display:flex; align-items:center; gap:12px;
  border-bottom:1px solid var(--border);
  overflow: hidden;
  transition: padding var(--transition);
}
.is-collapsed .sidebar-logo { padding: 0; justify-content: center; }

.logo-icon {
  width:32px; height:32px; border-radius:8px;
  background:linear-gradient(135deg,var(--accent),var(--accent2));
  display:flex; align-items:center; justify-content:center;
  font-size:16px; font-weight:700; color:#fff; letter-spacing:-0.5px;
  flex-shrink:0;
}

.logo-text { 
  font-size:15px; font-weight:600; letter-spacing:-0.3px; 
  white-space: nowrap;
  transition: opacity 0.2s, transform 0.2s;
}
.logo-text span { color:var(--accent); }

.collapse-toggle {
  position: absolute;
  right: -10px; top: 18px;
  width: 20px; height: 20px;
  background: var(--bg1);
  border: 1px solid var(--border);
  border-radius: 50%;
  color: var(--text3);
  display: flex; align-items: center; justify-content: center;
  cursor: pointer;
  z-index: 100;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}
.collapse-toggle:hover { color: var(--accent); border-color: var(--accent2); background: var(--bg2); transform: translateY(-1px) scale(1.1); }
.is-collapsed .collapse-toggle { transform: rotate(180deg); right: -10px; }
.is-collapsed .logo-text { display: none; }

.sidebar-nav { flex:1; padding:12px 10px; overflow-y:auto; overflow-x: hidden; }
.nav-section { margin-bottom:20px; }
.nav-label { 
  font-size:10px; font-weight:600; letter-spacing:1.2px; text-transform:uppercase; 
  color:var(--text3); padding:0 12px; margin-bottom:8px;
  white-space: nowrap;
  transition: opacity 0.2s;
}
.is-collapsed .nav-label { opacity: 0; pointer-events: none; }

.nav-item {
  display:flex; align-items:center; gap:12px; text-decoration: none;
  padding:10px 12px; border-radius:var(--radius);
  color:var(--text2); cursor:pointer; transition:all 0.2s ease;
  font-size:13px; font-weight:400; user-select:none;
  margin-bottom: 2px;
}
.is-collapsed .nav-item { justify-content: center; padding: 10px 0; gap: 0; }
.nav-item:hover { background:var(--bg2); color:var(--text1); }
.nav-item.active { background:rgba(0, 201, 167, 0.1); color:var(--accent); font-weight:500; }
.nav-icon { width:18px; height:18px; opacity:0.8; flex-shrink:0; }

.nav-text { 
  white-space: nowrap;
  transition: opacity 0.2s, transform 0.2s;
}
.is-collapsed .nav-text { display: none; }

.sidebar-footer { padding:12px 10px; border-top:1px solid var(--border); display:flex; flex-direction:column; gap:8px; }

.theme-toggle {
  display:flex; align-items:center; gap:12px;
  padding:10px 12px; border-radius:var(--radius);
  cursor:pointer; color:var(--text2); font-size:13px;
  transition:all var(--transition);
}
.is-collapsed .theme-toggle { justify-content: center; padding: 10px 0; gap: 0; }
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
  padding:10px 12px; border-radius:var(--radius);
  color:var(--text2); font-size:13px; cursor:pointer;
  transition: all 0.2s;
}
.is-collapsed .user-info { justify-content: center; padding: 10px 0; }
.user-info:hover { background:rgba(240,80,96,0.1); color:var(--danger); }
.user-info:hover .logout { display:inline; color:var(--danger); }

.user-avatar {
  width: 32px; height: 32px; border-radius: 50%;
  background: var(--bg3); border: 1px solid var(--border);
  display: flex; align-items: center; justify-content: center;
  font-size: 13px; font-weight: 600; color: var(--text1);
  flex-shrink: 0;
}

.user-name {
  display: flex; align-items: center; width: 100%;
  white-space: nowrap;
  transition: opacity 0.2s;
}
.is-collapsed .user-name { display: none; }

.main { flex:1; display:flex; flex-direction:column; overflow:hidden; }

/* Custom Scrollbar for Sidebar */
.sidebar-nav::-webkit-scrollbar { width: 3px; }
.sidebar-nav::-webkit-scrollbar-thumb { background: transparent; }
.sidebar-nav:hover::-webkit-scrollbar-thumb { background: var(--border2); }
</style>
