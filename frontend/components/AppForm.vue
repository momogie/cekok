<template>
  <div class="modal-overlay" @mousedown.self="onOverlayDown = true" @mouseup.self="onOverlayUp">
    <div class="modal" @mousedown.stop>
      <div class="modal-header">
        <div class="modal-header-icon" :class="app ? 'icon-edit' : 'icon-add'">
          <svg v-if="app" width="18" height="18" viewBox="0 0 16 16" fill="none" stroke="#fff" stroke-width="2"><path d="M11 2a2 2 0 1 1 3 3L6 13l-4 1 1-4 8-8z"/></svg>
          <svg v-else width="18" height="18" viewBox="0 0 16 16" fill="none"><path d="M8 3v10M3 8h10" stroke="#fff" stroke-width="2" stroke-linecap="round"/></svg>
        </div>
        <div>
          <div class="modal-title">{{ app ? 'Update' : 'Add' }} Application</div>
          <div class="modal-subtitle">Configure your app for auto-deployment</div>
        </div>
        <div class="modal-close" @click="$emit('close')">
          <svg width="14" height="14" viewBox="0 0 16 16" fill="none"><path d="M3 3l10 10M13 3L3 13" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/></svg>
        </div>
      </div>

      <!-- Step Indicator -->
      <div class="modal-steps" v-if="!success">
        <div 
          v-for="i in totalSteps" 
          :key="i"
          class="mstep" 
          :class="{ active: currentStep === i, done: currentStep > i }"
        >
          <div class="mstep-num">
            <template v-if="currentStep > i">
              <svg width="10" height="10" viewBox="0 0 12 12" fill="none"><path d="M2 6l3 3 5-5" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/></svg>
            </template>
            <template v-else>{{ i }}</template>
          </div>
          <span class="mstep-label">{{ stepLabels[i-1] }}</span>
          <div v-if="i < totalSteps" class="mstep-sep"></div>
        </div>
      </div>

      <div class="modal-body scrollable">
        <div v-if="success" class="modal-success">
          <div class="success-icon">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none"><path d="M5 12l5 5 9-9" stroke="var(--accent)" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/></svg>
          </div>
          <div class="success-title">{{ form.name }} {{ app ? 'Updated' : 'Created' }}!</div>
          <div class="success-sub">App registered as <code style="color:var(--accent2);font-family:var(--mono)">{{ form.serviceName || 'service' }}</code>.</div>
          <div style="display:flex;gap:10px;margin-top:8px">
            <button class="btn btn-primary" @click="$emit('close')">Go to dashboard</button>
            <button v-if="!app" class="btn btn-ghost" @click="resetForm">Add another</button>
          </div>
        </div>

        <template v-else>
          <!-- Step 1: App Type -->
          <AppFormTypeTab v-if="currentStep === 1" :form="form" :app-types="APP_TYPES" @select-type="selectType" />

          <!-- Step 2: Repository -->
          <AppFormRepoTab v-if="currentStep === 2" :form="form" />

          <!-- Step 3: Build & Deploy -->
          <AppFormBuildTab v-if="currentStep === 3" :form="form" :current-type="currentType" :servers-ctx="serversCtx" @toggle-target="toggleTargetServer" @add-env="addEnvVar" @remove-env="removeEnvVar" />

          <!-- Step 4: Settings -->
          <AppFormSettingsTab v-if="currentStep === 4" :form="form" @add-setting="addSettingFile" @remove-setting="removeSettingFile" />

          <!-- Step 5: Schedule -->
          <AppFormScheduleTab v-if="currentStep === 5" :form="form" :cron-presets="CRON_PRESETS" />

          <!-- Step 6: Notification -->
          <AppFormNotificationTab v-if="currentStep === 6" :form="form" />

          <!-- Step 7: Review -->
          <AppFormReviewTab v-if="currentStep === 7" :form="form" :current-type="currentType" :server-name="serverName" />
        </template>
      </div>

      <div class="modal-footer" v-if="!success">
        <div class="modal-footer-left">{{ footerHint }}</div>
        <button v-if="currentStep > 1" class="btn btn-ghost" @click="currentStep--">← Back</button>
        <button 
          class="btn btn-primary" 
          :disabled="loading" 
          @click="nextStep"
        >
          {{ currentStep === totalSteps ? (app ? '✓ Update App' : '✓ Create App') : 'Continue →' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  app: { type: Object, default: null }
})

const emit = defineEmits(['close', 'saved'])
const loading = ref(false)
const success = ref(false)
const currentStep = ref(1)
const totalSteps = 7

// Overlay interactions to prevent closing when dragging selection out
const onOverlayDown = ref(false)
const onOverlayUp = () => {
  if (onOverlayDown.value) emit('close')
  onOverlayDown.value = false
}

const APP_TYPES = [
  { id:'dotnet', icon:'.NT', cls:'icon-dotnet', name:'.NET / C#',   desc:'ASP.NET Core API, Worker, Blazor',    buildCmd:'dotnet publish -c Release -o ./publish', outputDir:'publish/' },
  { id:'nuxt',   icon:'NX',  cls:'icon-nuxt',   name:'Nuxt 3',       desc:'SSR or static site generation',       buildCmd:'npm run build',                          outputDir:'.output/' },
  { id:'vue',    icon:'VU',  cls:'icon-vue',     name:'Vue / Vite',   desc:'SPA — served via nginx',              buildCmd:'npm run build',                          outputDir:'dist/' },
  { id:'node',   icon:'JS',  cls:'icon-node',    name:'Node.js',      desc:'Express, Fastify, NestJS',            buildCmd:'npm install && npm run build',           outputDir:'dist/', entryFile: 'index.js' },
  { id:'react',  icon:'RE',  cls:'icon-react',   name:'React',        desc:'Vite/CRA — served via nginx',         buildCmd:'npm run build',                          outputDir:'dist/' },
  { id:'php',    icon:'PH',  cls:'icon-php',     name:'PHP (Laravel)',desc:'Artisan, Composer based',              buildCmd:'composer install --no-dev',              outputDir:'./', entryFile: 'public/index.php' },
]

const CRON_PRESETS = [
  {label:'Daily 00:00', val:'0 0 * * *'},
  {label:'Daily 02:00', val:'0 2 * * *'},
  {label:'Every 6h',    val:'0 */6 * * *'},
  {label:'Mon 02:00',   val:'0 2 * * 1'},
  {label:'Hourly',      val:'0 * * * *'},
]

const stepLabels = ['App Type', 'Repository', 'Build', 'Settings', 'Schedule', 'Notification', 'Review']

// Helper to parse environment variables from JSON or Array
const parseEnvVars = (raw) => {
  if (!raw) return [{ key: '', val: '' }]
  try {
    const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw
    return (Array.isArray(parsed) && parsed.length) ? [...parsed] : [{ key: '', val: '' }]
  } catch {
    return [{ key: '', val: '' }]
  }
}

const form = ref({
  type: props.app?.type || 'dotnet',
  name: props.app?.name || '',
  repoUrl: props.app?.repoUrl || '',
  branch: props.app?.branch || 'main',
  trigger: props.app?.trigger || 'manual',
  token: '',
  buildCmd: props.app?.buildCmd || (props.app ? '' : APP_TYPES.find(t => t.id === 'dotnet')?.buildCmd || ''),
  outputDir: props.app?.outputDir || (props.app ? '' : APP_TYPES.find(t => t.id === 'dotnet')?.outputDir || ''),
  entryFile: props.app?.entryFile || (props.app ? '' : APP_TYPES.find(t => t.id === 'dotnet')?.entryFile || ''),
  envVars: parseEnvVars(props.app?.envVars),
  settingFiles: [],
  scheduleEnabled: props.app?.scheduleEnabled || false,
  scheduleCron: props.app?.scheduleCron || '0 2 * * *',
  notifyEmail: props.app?.notifyEmail || false,
  notifyEmailAddress: props.app?.notifyEmailAddress || '',
  notifyTelegram: props.app?.notifyTelegram || false,
  notifyTelegramChatId: props.app?.notifyTelegramChatId || '',
  deployTargets: props.app?.deployTargets ? JSON.parse(JSON.stringify(props.app.deployTargets)) : [],
})

// Load servers and settings when component mounts
const serversCtx = useServers()
onMounted(async () => {
  serversCtx.fetchServers()
  if (props.app) {
    try {
      const config = useRuntimeConfig()
      const auth = useAuth()
      const s = await $fetch(`${config.public.apiBase}/api/applications/${props.app.id}/settings`, { 
        headers: auth.authHeaders() 
      })
      if (s && s.length > 0) form.value.settingFiles = s
    } catch (e) { console.error('Failed to load settings', e) }
  }
})

const isTargetServer = (serverId) =>
  form.value.deployTargets.some(t => t.serverId === serverId)

const getTarget = (serverId) => {
  let t = form.value.deployTargets.find(t => t.serverId === serverId)
  if (!t) {
    t = { serverId, deployDir: '', serviceName: '', port: '', healthCheckUrl: '' }
    form.value.deployTargets.push(t)
  }
  return t
}

const toggleTargetServer = (serverId) => {
  const idx = form.value.deployTargets.findIndex(t => t.serverId === serverId)
  if (idx === -1) {
    form.value.deployTargets.push({ serverId, deployDir: '', serviceName: '', port: '', healthCheckUrl: '' })
  } else {
    form.value.deployTargets.splice(idx, 1)
  }
}

const serverName = (serverId) =>
  serversCtx.servers.find(s => s.id === serverId)?.name ?? serverId

const currentType = computed(() => APP_TYPES.find(t => t.id === form.value.type))

const footerHint = computed(() => `Step ${currentStep.value} of ${totalSteps} — ${stepLabels[currentStep.value-1].toLowerCase()}`)

const selectType = (id) => {
  const oldType = APP_TYPES.find(x => x.id === form.value.type)
  form.value.type = id
  const t = APP_TYPES.find(x => x.id === id)
  if (t) {
    if (!form.value.buildCmd || (oldType && form.value.buildCmd === oldType.buildCmd)) {
      form.value.buildCmd = t.buildCmd
    }
    if (!form.value.outputDir || (oldType && form.value.outputDir === oldType.outputDir)) {
      form.value.outputDir = t.outputDir
    }
    if (!form.value.entryFile || (oldType && form.value.entryFile === oldType.entryFile)) {
      form.value.entryFile = t.entryFile || ''
    }
  }
}

const addEnvVar = () => form.value.envVars.push({ key: '', val: '' })
const removeEnvVar = (i) => form.value.envVars.splice(i, 1)

const addSettingFile = () => form.value.settingFiles.push({ filePath: '', content: '' })
const removeSettingFile = (i) => form.value.settingFiles.splice(i, 1)

const nextStep = () => {
  if (currentStep.value < totalSteps) {
    // Basic validation
    if (currentStep.value === 1 && !form.value.name) return
    if (currentStep.value === 2 && !form.value.repoUrl) return
    currentStep.value++
  } else {
    submit()
  }
}

const submit = async () => {
  loading.value = true
  try {
    const appsCtx = useApps()
    // Clean up env vars — remove empty rows
    const cleanEnvVars = form.value.envVars
      .filter(e => e.key.trim())
      .map(e => ({ key: e.key.trim(), val: e.val }))

    const cleanSettingFiles = form.value.settingFiles
      .filter(s => s.filePath.trim())
      .map(s => ({ filePath: s.filePath.trim(), content: s.content }))

    // Map deploy targets — only those with deployDir filled
    const deployTargets = form.value.deployTargets
      .filter(t => t.deployDir.trim())
      .map(t => ({
        serverId: t.serverId,
        deployDir: t.deployDir.trim(),
        serviceName: t.serviceName.trim() || null,
        port: t.port ? Number(t.port) : null,
        healthCheckUrl: t.healthCheckUrl.trim() || null,
      }))

    const payload = {
      name: form.value.name,
      type: form.value.type,
      repoUrl: form.value.repoUrl,
      branch: form.value.branch,
      buildCmd: form.value.buildCmd || currentType.value?.buildCmd || null,
      outputDir: form.value.outputDir || currentType.value?.outputDir || null,
      entryFile: form.value.entryFile || currentType.value?.entryFile || null,
      trigger: form.value.trigger,
      token: form.value.token || null,
      envVars: cleanEnvVars.length ? cleanEnvVars : null,
      settingFiles: cleanSettingFiles.length ? cleanSettingFiles : null,
      scheduleCron: form.value.scheduleCron || null,
      scheduleEnabled: form.value.scheduleEnabled,
      notifyEmail: form.value.notifyEmail,
      notifyEmailAddress: form.value.notifyEmailAddress || null,
      notifyTelegram: form.value.notifyTelegram,
      notifyTelegramChatId: form.value.notifyTelegramChatId || null,
      deployTargets: deployTargets.length ? deployTargets : null,
    }

    if (props.app) {
      await appsCtx.updateApp(props.app.id, payload)
    } else {
      await appsCtx.createApp(payload)
    }
    success.value = true
    emit('saved')
  } catch (e) {
    alert(e.data?.message || e.message)
  } finally {
    loading.value = false
  }
}


const resetForm = () => {
  success.value = false
  currentStep.value = 1
  form.value = {
    type: 'dotnet',
    name: '',
    repoUrl: '',
    branch: 'main',
    trigger: 'manual',
    token: '',
    buildCmd: APP_TYPES.find(t => t.id === 'dotnet')?.buildCmd || '',
    outputDir: APP_TYPES.find(t => t.id === 'dotnet')?.outputDir || '',
    entryFile: APP_TYPES.find(t => t.id === 'dotnet')?.entryFile || '',
    envVars: [{ key: '', val: '' }],
    settingFiles: [],
    scheduleEnabled: false,
    scheduleCron: '0 2 * * *',
    notifyEmail: false,
    notifyEmailAddress: '',
    notifyTelegram: false,
    notifyTelegramChatId: '',
    deployTargets: [],
  }
}
</script>

<style>
.modal-overlay {
  position: fixed; inset: 0; z-index: 1000;
  background: rgba(0, 0, 0, 0.6); backdrop-filter: blur(4px);
  display: flex; align-items: center; justify-content: center;
}
.modal {
  background: var(--bg1); border: 1px solid var(--border2);
  border-radius: 14px; width: 600px; max-width: calc(100vw - 32px);
  max-height: calc(100vh - 48px); display: flex; flex-direction: column; overflow: hidden;
  box-shadow: 0 10px 40px rgba(0,0,0,0.5);
}

.modal-header {
  padding: 18px 20px 14px; border-bottom: 1px solid var(--border);
  display: flex; align-items: flex-start; gap: 12px; flex-shrink: 0;
}
.modal-header-icon {
  width: 36px; height: 36px; border-radius: 9px; flex-shrink: 0;
  display: flex; align-items: center; justify-content: center;
}
.icon-add { background: linear-gradient(135deg, var(--accent), var(--accent2)); }
.icon-edit { background: linear-gradient(135deg, var(--purple), var(--accent2)); }

.modal-title { font-size: 15px; font-weight: 600; line-height: 1.3; }
.modal-subtitle { font-size: 11px; color: var(--text3); margin-top: 2px; }
.modal-close {
  margin-left: auto; cursor: pointer; color: var(--text3);
  width: 28px; height: 28px; border-radius: 6px;
  display: flex; align-items: center; justify-content: center;
  transition: all 0.15s;
}
.modal-close:hover { background: var(--bg3); color: var(--text1); }

.modal-steps {
  display: flex; align-items: center; padding: 12px 20px; border-bottom: 1px solid var(--border);
  flex-shrink: 0; overflow-x: auto; background: var(--bg2);
}
.mstep { display: flex; align-items: center; gap: 7px; font-size: 11px; font-weight: 500; color: var(--text3); white-space: nowrap; }
.mstep.active { color: var(--text1); }
.mstep.done { color: var(--accent); }
.mstep-num {
  width: 20px; height: 20px; border-radius: 50%; display: flex; align-items: center; justify-content: center;
  font-size: 10px; font-weight: 600; background: var(--bg3); color: var(--text3); border: 1px solid var(--border2);
}
.mstep.active .mstep-num { background: var(--accent); color: #0d1814; border-color: transparent; }
.mstep.done .mstep-num { background: rgba(0,201,167,0.15); color: var(--accent); border-color: var(--accent); }
.mstep-sep { width: 20px; height: 1px; background: var(--border2); margin: 0 4px; }

.modal-body { flex: 1; overflow-y: auto; padding: 20px; }
.scrollable { scrollbar-width: thin; }

.msec-label {
  font-size: 10px; font-weight: 600; letter-spacing: 1px; text-transform: uppercase;
  color: var(--text3); margin-bottom: 12px; margin-top: 20px;
}
.msec-label:first-child { margin-top: 0; }

.type-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 10px; margin-bottom: 20px; }
.type-card {
  border: 1.5px solid var(--border2); border-radius: var(--radius);
  padding: 14px 12px; cursor: pointer; transition: all 0.15s;
  display: flex; flex-direction: column; align-items: center; gap: 8px; text-align: center;
}
.type-card:hover { border-color: var(--border2); background: var(--bg2); }
.type-card.selected { border-color: var(--accent); background: rgba(0, 201, 167, 0.06); }
.type-card-icon {
  width: 36px; height: 36px; border-radius: 8px;
  display: flex; align-items: center; justify-content: center;
  font-size: 13px; font-weight: 700; font-family: var(--mono);
}
.type-card-name { font-size: 12px; font-weight: 600; }
.type-card-desc { font-size: 10px; color: var(--text3); line-height: 1.4; }

.form-group { margin-bottom: 14px; }
.form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.form-label { display: block; font-size: 11px; font-weight: 500; color: var(--text3); margin-bottom: 5px; text-transform: uppercase; letter-spacing: 0.5px; }
.form-label span { color: var(--danger); margin-left: 2px; }

.form-input, .form-select {
  background: var(--bg2); border: 1px solid var(--border2);
  border-radius: 6px; padding: 7px 10px;
  color: var(--text1); font-size: 12px; font-family: var(--mono);
  outline: none; transition: border-color 0.15s; width: 100%;
}
.form-input:focus, .form-select:focus { border-color: var(--accent); background: var(--bg1); }
.form-hint { font-size: 10px; color: var(--text3); margin-top: 4px; }

.env-list { display: flex; flex-direction: column; gap: 6px; }
.env-row { display: grid; grid-template-columns: 1fr 1fr 28px; gap: 6px; align-items: center; }
.env-del {
  width: 28px; height: 28px; border-radius: 6px; border: none; background: transparent;
  color: var(--text3); cursor: pointer; font-size: 16px; display: flex; align-items: center; justify-content: center;
}
.env-del:hover { background: rgba(240, 80, 96, 0.1); color: var(--danger); }
.add-row-btn {
  display: inline-flex; align-items: center; gap: 6px; font-size: 11px; font-weight: 500;
  color: var(--accent2); cursor: pointer; padding: 8px 0; border: none; background: none; margin-top: 4px;
}

.toggle-row { display: flex; align-items: center; gap: 10px; cursor: pointer; }
.toggle-track { width: 34px; height: 18px; border-radius: 99px; background: var(--bg3); border: 1px solid var(--border2); position: relative; transition: background 0.2s; }
.toggle-track.enabled { background: var(--accent); }
.toggle-thumb { position: absolute; top: 2px; left: 2px; width: 12px; height: 12px; border-radius: 50%; background: #fff; transition: transform 0.2s; }
.toggle-thumb.enabled { transform: translateX(16px); }
.toggle-label { font-size: 12px; color: var(--text2); }

.cron-presets { display: flex; flex-wrap: wrap; gap: 6px; margin-bottom: 12px; }
.cron-preset {
  padding: 3px 10px; border-radius: 99px; font-size: 10px; font-weight: 500;
  border: 1px solid var(--border2); color: var(--text2); cursor: pointer; background: transparent;
}
.cron-preset:hover { border-color: var(--accent); color: var(--accent); }

.cron-fields { display: grid; grid-template-columns: repeat(5, 1fr); gap: 6px; margin-bottom: 10px; }
.cron-field { display: flex; flex-direction: column; gap: 3px; }
.cron-flabel { font-size: 9px; color: var(--text3); text-align: center; text-transform: uppercase; }
.cron-input { text-align: center; padding: 6px 0; }

.cron-preview-box {
  background: var(--bg2); border: 1px solid var(--border); border-radius: 6px; padding: 8px 12px;
  display: flex; justify-content: space-between; align-items: center;
}
.cron-expr { font-family: var(--mono); font-size: 12px; color: var(--accent2); }
.cron-human { font-size: 11px; color: var(--text3); }

.review-section-title { font-size: 10px; font-weight: 600; letter-spacing: 0.8px; text-transform: uppercase; color: var(--text3); margin: 14px 0 6px; }
.review-block { background: var(--bg2); border: 1px solid var(--border); border-radius: 8px; padding: 12px; }
.review-row { display: flex; justify-content: space-between; padding: 3px 0; font-size: 11px; }
.review-key { color: var(--text3); }
.review-val { font-family: var(--mono); color: var(--text1); }
.review-val.accent { color: var(--accent); }

.modal-footer {
  padding: 14px 20px; border-top: 1px solid var(--border);
  display: flex; align-items: center; gap: 10px; flex-shrink: 0;
}
.modal-footer-left { flex: 1; font-size: 11px; color: var(--text3); }

.modal-success {
  display: flex; flex-direction: column; align-items: center; justify-content: center;
  padding: 30px 20px; gap: 12px; text-align: center;
}
.success-icon {
  width: 48px; height: 48px; border-radius: 50%;
  background: rgba(0,201,167,0.12); border: 2px solid var(--accent);
  display: flex; align-items: center; justify-content: center;
}
.success-title { font-size: 16px; font-weight: 600; }
.success-sub { font-size: 12px; color: var(--text3); max-width: 300px; line-height: 1.5; }

.icon-dotnet { background: rgba(139, 127, 255, 0.15); color: var(--purple); }
.icon-nuxt { background: rgba(0, 201, 167, 0.12); color: var(--accent); }
.icon-vue { background: rgba(0, 201, 167, 0.12); color: var(--accent); }
.icon-react { background: rgba(0, 151, 255, 0.15); color: #61dafb; }
.icon-node { background: rgba(131, 205, 41, 0.15); color: #83cd29; }
.icon-php { background: rgba(119, 123, 179, 0.15); color: #777bb3; }
.icon-static { background: rgba(0, 151, 255, 0.12); color: var(--accent2); }

.step-panel { animation: fadeUp 0.2s ease both; }
@keyframes fadeUp { from { opacity: 0; transform: translateY(6px); } to { opacity: 1; transform: translateY(0); } }

/* ── Multi-server deploy targets ──────────────────────────────────── */
.target-badge {
  display: inline-flex; align-items: center;
  background: rgba(0,201,167,0.1); color: var(--accent);
  border: 1px solid rgba(0,201,167,0.25); border-radius: 99px;
  font-size: 9px; font-weight: 600; padding: 1px 7px;
  margin-left: 8px; vertical-align: middle;
}
.servers-loading, .servers-empty {
  font-size: 12px; color: var(--text3); padding: 12px 0; text-align: center;
}
.server-target-list { display: flex; flex-direction: column; gap: 8px; }
.server-target-item {
  border: 1.5px solid var(--border2); border-radius: 8px;
  overflow: hidden; transition: border-color 0.15s;
}
.server-target-item.selected { border-color: var(--accent); }
.sti-header {
  display: flex; align-items: center; gap: 10px;
  padding: 10px 12px; cursor: pointer; transition: background 0.15s;
}
.sti-header:hover { background: var(--bg2); }
.sti-check {
  width: 18px; height: 18px; border-radius: 5px; flex-shrink: 0;
  border: 1.5px solid var(--border2); background: var(--bg3);
  display: flex; align-items: center; justify-content: center;
  transition: all 0.15s;
}
.sti-check.checked { border-color: var(--accent); background: rgba(0,201,167,0.15); color: var(--accent); }
.sti-info { flex: 1; min-width: 0; }
.sti-name { font-size: 12px; font-weight: 600; color: var(--text1); }
.sti-ip { font-size: 10px; color: var(--text3); font-family: var(--mono); margin-top: 1px; }
.sti-configured { font-size: 10px; color: var(--accent); font-weight: 500; white-space: nowrap; }
.sti-config {
  padding: 10px 12px 12px; border-top: 1px solid var(--border);
  background: var(--bg2); animation: fadeUp 0.15s ease both;
}
.form-input-sm { font-size: 11px; padding: 6px 8px; }

/* ── Settings Files ─────────────────────────────────────────────── */
.setting-files-list { display: flex; flex-direction: column; gap: 12px; }
.setting-file-row {
  border: 1px solid var(--border); border-radius: 8px; background: var(--bg2);
  overflow: hidden; display: flex; flex-direction: column;
}
.setting-file-header {
  display: flex; gap: 10px; align-items: center; padding: 8px 10px;
  background: rgba(0,0,0,0.1); border-bottom: 1px solid var(--border);
}
.setting-file-header .form-input {
  background: var(--bg1); border-color: transparent; flex: 1;
}
.setting-file-header .form-input:focus { border-color: var(--accent); }
.form-textarea {
  min-height: 120px; resize: vertical; border: none; border-radius: 0;
  font-size: 11px; line-height: 1.5; background: transparent;
}
.form-textarea:focus { border-color: transparent !important; background: rgba(0,0,0,0.2) !important; }

</style>
