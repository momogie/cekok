// useAuth.ts — login, logout, refresh token composable
import { defineStore } from 'pinia'

interface User {
  id: string
  username: string
  displayName: string
  role: 'admin' | 'operator' | 'viewer'
}

interface AuthState {
  user: User | null
  accessToken: string | null
  refreshToken: string | null
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    user: null,
    accessToken: null,
    refreshToken: null,
  }),

  getters: {
    isLoggedIn: (s) => !!s.accessToken,
    isAdmin: (s) => s.user?.role === 'admin',
  },

  actions: {
    async login(username: string, password: string) {
      const config = useRuntimeConfig()
      const data = await $fetch<{ accessToken: string; refreshToken: string; user: User }>(
        `${config.public.apiBase}/api/auth/login`,
        { method: 'POST', body: { username, password } }
      )
      this.accessToken = data.accessToken
      this.refreshToken = data.refreshToken
      this.user = data.user
      if (import.meta.client) {
        localStorage.setItem('cekok_refresh', data.refreshToken)
      }
    },

    async refresh() {
      const token = this.refreshToken ?? (import.meta.client ? localStorage.getItem('cekok_refresh') : null)
      if (!token) return false
      try {
        const config = useRuntimeConfig()
        const data = await $fetch<{ accessToken: string; refreshToken: string; user: User }>(
          `${config.public.apiBase}/api/auth/refresh`,
          { method: 'POST', body: { refreshToken: token } }
        )
        this.accessToken = data.accessToken
        this.refreshToken = data.refreshToken
        this.user = data.user
        if (import.meta.client) {
          localStorage.setItem('cekok_refresh', data.refreshToken)
        }
        return true
      } catch {
        this.logout()
        return false
      }
    },

    async logout() {
      if (this.refreshToken) {
        const config = useRuntimeConfig()
        try {
          await $fetch(`${config.public.apiBase}/api/auth/logout`, {
            method: 'POST',
            body: { refreshToken: this.refreshToken },
            headers: { Authorization: `Bearer ${this.accessToken}` }
          })
        } catch { /* ignore */ }
      }
      this.user = null
      this.accessToken = null
      this.refreshToken = null
      if (import.meta.client) localStorage.removeItem('cekok_refresh')
      navigateTo('/login')
    },

    authHeaders(): Record<string, string> {
      return this.accessToken
        ? { Authorization: `Bearer ${this.accessToken}` }
        : {}
    },

    handleError(error: any) {
      if (error?.response?.status === 401) {
        this.logout()
      }
    }
  }
})

export const useAuth = () => useAuthStore()
