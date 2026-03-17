export default defineNuxtConfig({
  devtools: { enabled: true },

  modules: ['@pinia/nuxt'],

  devServer: {
    port: 4010
  },

  runtimeConfig: {
    public: {
      apiBase: process.env.NUXT_PUBLIC_API_BASE || 'http://localhost:54388'
    }
  },

  app: {
    head: {
      title: 'Cekok — Self-hosted Deploy Orchestrator',
      meta: [
        { charset: 'utf-8' },
        { name: 'viewport', content: 'width=device-width, initial-scale=1' },
        { name: 'description', content: 'Cekok — CI/CD ringan self-hosted. Build sekali di master, distribusi ke semua server.' }
      ],
      link: [
        { rel: 'preconnect', href: 'https://fonts.googleapis.com' },
        { rel: 'stylesheet', href: 'https://fonts.googleapis.com/css2?family=JetBrains+Mono:wght@400;500;600&family=Sora:wght@300;400;500;600&display=swap' }
      ]
    }
  },

  routeRules: {
    '/': { redirect: '/dashboard' }
  },

  compatibilityDate: '2025-01-01'
})
