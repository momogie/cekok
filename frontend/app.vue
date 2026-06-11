<template>
  <NuxtLayout>
    <AppLoader v-if="loading" />
    <NuxtPage v-else />
  </NuxtLayout>
</template>

<script setup>
const auth = useAuth()
const loading = ref(true)

onMounted(async () => {
  if (import.meta.client) {
    const hasToken = localStorage.getItem('cekok_refresh')
    if (hasToken) {
      await auth.refresh()
    }
    loading.value = false
  }
})
</script>
