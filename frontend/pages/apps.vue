<template>
  <div class="apps-page">
    <div class="topbar">
      <div class="topbar-title">Application Management</div>
      <span class="topbar-sub">{{ apps.length }} projects · {{ activeDeploys.length }} deploying</span>
      <div class="topbar-right">
        <button class="btn btn-ghost" @click="refresh()">
          <svg width="11" height="11" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M1 8A7 7 0 1 1 15 8A7 7 0 1 1 1 8"/><path d="M1 8L4 5M1 8L4 11"/></svg>
          Refresh
        </button>
        <button v-if="canAddApp" class="btn btn-primary" @click="showForm = true">
          <svg width="11" height="11" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2.5"><line x1="8" y1="2" x2="8" y2="14"/><line x1="2" y1="8" x2="14" y2="8"/></svg>
          Add App
        </button>
      </div>
    </div>

    <div class="content">
      <div class="grid-main">
        <!-- Left Sidebar: App List -->
        <div class="panel list-panel">
          <div class="panel-header">
            <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="var(--accent)" stroke-width="1.8"><rect x="1" y="3" width="14" height="10" rx="2"/><path d="M5 7h6M5 10h4" stroke-linecap="round"/></svg>
            <span class="panel-title">Your Projects</span>
            <span class="panel-sub" v-if="apps.length">A-Z</span>
          </div>
          
          <div class="scroll-area">
            <div v-if="loading" class="empty-state">Loading projects...</div>
            <div v-else-if="!apps.length" class="empty-state">No applications found.</div>
            <AppItem 
              v-for="app in apps" 
              :key="app.id" 
              :app="app" 
              :is-selected="currentApp?.id === app.id"
              :status="getAppStatus(app.id)"
              @select="selectApp(app)"
            />
          </div>

          <div v-if="canAddApp" class="add-btn-row" @click="showForm = true">
            <svg width="10" height="10" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2.2"><line x1="8" y1="2" x2="8" y2="14"/><line x1="2" y1="8" x2="14" y2="8"/></svg>
            Create new application
          </div>
        </div>

        <!-- Main Area: App Detail -->
        <div class="panel detail-panel">
          <AppDetailPanel 
            v-if="currentApp" 
            :app="currentApp" 
            :current-job="currentJob"
            :logs="logs"
            @deploy="deployApp"
            @edit="openEditForm"
          />
          <div v-else class="empty-detail">
            <div class="empty-icon">
              <svg width="48" height="48" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="0.8" opacity="0.2"><rect x="1" y="3" width="14" height="10" rx="2"/><path d="M5 7h6M5 10h4"/></svg>
            </div>
            <div class="empty-text">Select an application to view details</div>
            <div class="empty-sub">Deploy managed apps or monitor build logs here.</div>
          </div>
        </div>
      </div>
    </div>

    <!-- Modals -->
    <AppForm 
      v-if="showForm" 
      :app="editApp" 
      @close="closeForm" 
      @saved="refresh()" 
    />
  </div>
</template>

<script setup>
import { storeToRefs } from 'pinia'
definePageMeta({ middleware: 'auth' })

const auth = useAuth()
const appsCtx = useApps()
const { apps, loading, currentApp, currentJob, logs } = storeToRefs(appsCtx)

const canAddApp = computed(() => auth.isAdmin || auth.isOperator)
const showForm = ref(false)
const editApp = ref(null)
const polling = ref(null)

onMounted(async () => {
  await appsCtx.fetchApps()
  if (apps.value.length && !currentApp.value) {
    selectApp(apps.value[0])
  }
  
  // Polling for active deployments status
  polling.value = setInterval(() => {
    if (currentApp.value) {
      appsCtx.fetchStatus(currentApp.value.id)
    }
  }, 3000)
})

onUnmounted(() => {
  if (polling.value) clearInterval(polling.value)
})

const activeDeploys = computed(() => apps.value.filter(a => getAppStatus(a.id) === 'Running'))

const selectApp = (app) => {
  appsCtx.selectApp(app)
}

const deployApp = async (appId) => {
  await appsCtx.deploy(appId)
}

const openEditForm = (app) => {
  editApp.value = app
  showForm.value = true
}

const closeForm = () => {
  showForm.value = false
  editApp.value = null
}

const refresh = () => {
  appsCtx.fetchApps()
}

const getAppStatus = (appId) => {
  // Simple check for now
  if (currentApp.value?.id === appId) return currentJob.value?.status || 'Idle'
  return 'Idle'
}
</script>

<style scoped>
.apps-page { flex: 1; display: flex; flex-direction: column; overflow: hidden; background: var(--bg0); }

.topbar {
  height: 52px; flex-shrink: 0;
  background: var(--bg1); border-bottom: 1px solid var(--border);
  display: flex; align-items: center; padding: 0 20px; gap: 14px;
}
.topbar-title { font-size: 13px; font-weight: 600; }
.topbar-sub { font-size: 11px; color: var(--text3); }
.topbar-right { margin-left: auto; display: flex; align-items: center; gap: 8px; }

.content { flex: 1; padding: 18px; display: flex; flex-direction: column; min-height: 0; }

.grid-main { display: grid; grid-template-columns: 1fr 600px; gap: 12px; flex: 1; min-height: 0; }

.panel {
  background: var(--bg1); border: 1px solid var(--border);
  border-radius: var(--radius); display: flex; flex-direction: column; overflow: hidden;
}

.panel-header {
  padding: 10px 14px; border-bottom: 1px solid var(--border);
  display: flex; align-items: center; gap: 8px; flex-shrink: 0;
}
.panel-title { font-size: 11px; font-weight: 600; text-transform: uppercase; color: var(--text2); letter-spacing: 0.5px; }
.panel-sub { font-size: 10px; color: var(--text3); margin-left: auto; }

.scroll-area { flex: 1; overflow-y: auto; scrollbar-width: thin; }
.empty-state { padding: 40px 20px; text-align: center; color: var(--text3); font-size: 12px; }

.add-btn-row {
  padding: 9px 14px; border-top: 1px solid var(--border);
  display: flex; align-items: center; gap: 8px; color: var(--text3);
  cursor: pointer; font-size: 11px; transition: all var(--transition);
}
.add-btn-row:hover { color: var(--accent); background: var(--bg2); }

.detail-panel { min-width: 0; }
.empty-detail {
  flex: 1; display: flex; flex-direction: column; align-items: center; justify-content: center;
  padding: 40px; text-align: center; color: var(--text3);
}
.empty-icon { margin-bottom: 16px; color: var(--border2); }
.empty-text { font-size: 14px; font-weight: 500; color: var(--text2); }
.empty-sub { font-size: 12px; margin-top: 4px; max-width: 250px; line-height: 1.5; }

@media (max-width: 1000px) {
  .grid-main { grid-template-columns: 1fr; }
  .detail-panel { display: none; }
}

@keyframes spin { to { transform: rotate(360deg); } }
</style>
