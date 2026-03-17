// useServers.ts — simple composable to fetch server list
import { defineStore } from 'pinia'

export interface ServerSummary {
  id: string
  name: string
  ip: string
  sshPort: number
  role: string
  hostname?: string
  tags?: string
  nginxInstalled: boolean
  createdAt: string
}

export const useServersStore = defineStore('servers', {
  state: () => ({
    servers: [] as ServerSummary[],
    loading: false,
  }),

  actions: {
    async fetchServers() {
      if (this.servers.length > 0) return // cached
      const config = useRuntimeConfig()
      const auth = useAuth()
      this.loading = true
      try {
        const data = await $fetch<ServerSummary[]>(`${config.public.apiBase}/api/servers`, {
          headers: auth.authHeaders() as Record<string, string>
        })
        this.servers = data
      } finally {
        this.loading = false
      }
    }
  }
})

export const useServers = () => useServersStore()
