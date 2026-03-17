export default defineNuxtPlugin((nuxtApp) => {
  const auth = useAuth()

  // Use the global fetch interceptor via nuxtApp
  // Note: $fetch in Nuxt 3 is a wrapper around ohmyfetch/ofetch
  
  // Custom fetch instance that handles 401
  const apiFetch = $fetch.create({
    onResponseError({ response }) {
      if (response.status === 401) {
        // Avoid infinite loop if logout itself fails with 401
        const route = useRoute()
        if (route.path !== '/login') {
          auth.logout()
        }
      }
    }
  })

  // We can also override the global $fetch or provide this specific one
  // For simplicity and maximum effect, we'll use a global hook if possible,
  // but Nuxt's $fetch doesn't have a simple global "default" that can be changed mid-flight easily
  // without replacing it.
  
  // Instead, let's inject it so it can be used as useNuxtApp().$apiFetch
  return {
    provide: {
      apiFetch
    }
  }
})
