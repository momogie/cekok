import { defineStore } from 'pinia'

export interface Application {
  id: string
  name: string
  type: string
  repoUrl: string
  branch: string
  buildCmd?: string
  outputDir?: string
  scheduleCron?: string
  scheduleEnabled: boolean
  lastDeployedAt?: string
  lastStatus?: string
  currentJobId?: string
}

export interface DeployJob {
  id: string
  appId: string
  status: string
  triggerBy: string
  startedAt: string
  completedAt?: string
  errorMessage?: string
  progress: number
}

export interface DeployLog {
  timestamp: string
  message: string
  type: string
}

export const useAppsStore = defineStore('apps', {
  state: () => ({
    apps: [] as Application[],
    loading: false,
    currentApp: null as Application | null,
    currentJob: null as DeployJob | null,
    logs: [] as DeployLog[]
  }),

  actions: {
    async fetchApps() {
      const config = useRuntimeConfig()
      const auth = useAuth()
      this.loading = true
      try {
        const data = await $fetch<Application[]>(`${config.public.apiBase}/api/applications`, {
          headers: auth.authHeaders()
        })
        this.apps = data
      } finally {
        this.loading = false
      }
    },

    async selectApp(app: Application) {
      this.currentApp = app
      await this.fetchStatus(app.id)
      await this.fetchLogs(app.id)
    },

    async fetchStatus(appId: string) {
      const config = useRuntimeConfig()
      const auth = useAuth()
      try {
        const job = await $fetch<DeployJob>(`${config.public.apiBase}/api/deploy/${appId}/status`, {
          headers: auth.authHeaders()
        })
        this.currentJob = job
      } catch {
        this.currentJob = null
      }
    },

    async fetchLogs(appId: string, jobId?: string) {
      const config = useRuntimeConfig()
      const auth = useAuth()
      try {
        const data = await $fetch<DeployLog[]>(`${config.public.apiBase}/api/deploy/${appId}/logs`, {
          params: jobId ? { jobId } : {},
          headers: auth.authHeaders()
        })
        this.logs = data
      } catch {
        this.logs = []
      }
    },

    async deploy(appId: string) {
      const config = useRuntimeConfig()
      const auth = useAuth()
      try {
        const job = await $fetch<DeployJob>(`${config.public.apiBase}/api/deploy/${appId}`, {
          method: 'POST',
          headers: auth.authHeaders()
        })
        this.currentJob = job
        return job
      } catch (e) {
        console.error('Deploy failed', e)
        throw e
      }
    },

    async createApp(dto: any) {
      const config = useRuntimeConfig()
      const auth = useAuth()
      const data = await $fetch<Application>(`${config.public.apiBase}/api/applications`, {
        method: 'POST',
        body: dto,
        headers: auth.authHeaders()
      })
      this.apps.push(data)
      return data
    },

    async updateApp(id: string, dto: any) {
      const config = useRuntimeConfig()
      const auth = useAuth()
      const data = await $fetch<Application>(`${config.public.apiBase}/api/applications/${id}`, {
        method: 'PUT',
        body: dto,
        headers: auth.authHeaders()
      })
      const idx = this.apps.findIndex(a => a.id === id)
      if (idx !== -1) this.apps[idx] = data
      if (this.currentApp?.id === id) this.currentApp = data
      return data
    },

    async deleteApp(id: string) {
      const config = useRuntimeConfig()
      const auth = useAuth()
      await $fetch(`${config.public.apiBase}/api/applications/${id}`, {
        method: 'DELETE',
        headers: auth.authHeaders()
      })
      this.apps = this.apps.filter(a => a.id !== id)
      if (this.currentApp?.id === id) this.currentApp = null
    }
  }
})

export const useApps = () => useAppsStore()
