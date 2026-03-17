export default defineNuxtRouteMiddleware((to, from) => {
  const auth = useAuth()
  if (import.meta.client) {
    if (!auth.accessToken && !localStorage.getItem('cekok_refresh')) {
      return navigateTo('/login')
    }
  }
})
