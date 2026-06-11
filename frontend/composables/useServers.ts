import { defineStore } from 'pinia'
import type { ServerSummary } from '~/types'

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
