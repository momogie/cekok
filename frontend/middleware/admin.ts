export default defineNuxtRouteMiddleware((to, from) => {
  const auth = useAuth()
  if (auth.user?.role !== 'admin') {
    return navigateTo('/dashboard')
  }
})
