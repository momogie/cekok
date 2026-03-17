<template>
  <div class="auth-wrap">
    <div class="auth-box">
      <div class="logo">
        <div class="logo-icon">C</div>
        <div class="logo-text">Ce<span>kok</span></div>
      </div>
      <div class="title">Welcome back</div>
      <div class="sub">Deploy harusnya semudah "cekok" obat.</div>

      <form @submit.prevent="handleLogin" class="form">
        <div class="form-group">
          <label>Username</label>
          <input type="text" v-model="username" class="form-input" required autofocus>
        </div>
        <div class="form-group">
          <label>Password</label>
          <input type="password" v-model="password" class="form-input" required>
        </div>
        
        <div v-if="error" class="error-msg">{{ error }}</div>
        
        <button type="submit" class="btn btn-primary" :disabled="loading" style="width:100%; justify-content:center; padding:10px; margin-top:10px">
          {{ loading ? 'Logging in...' : 'Sign In' }}
        </button>
      </form>
    </div>
  </div>
</template>

<script setup>
definePageMeta({ layout: 'auth' })
const auth = useAuth()
const username = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)

const handleLogin = async () => {
  error.value = ''
  loading.value = true
  try {
    await auth.login(username.value, password.value)
    navigateTo('/dashboard')
  } catch (e) {
    error.value = 'Invalid username or password'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.auth-wrap {
  display: flex; align-items: center; justify-content: center;
  min-height: 100vh; background: var(--bg0);
}
.auth-box {
  width: 100%; max-width: 360px;
  background: var(--bg1); border: 1px solid var(--border);
  border-radius: var(--radius); padding: 32px;
}
.logo { display:flex; align-items:center; gap:10px; margin-bottom:24px; justify-content:center; }
.logo-icon {
  width:36px; height:36px; border-radius:10px;
  background:linear-gradient(135deg,var(--warn),var(--danger));
  display:flex; align-items:center; justify-content:center;
  font-size:18px; font-weight:700; color:#fff;
}
.logo-text { font-size:20px; font-weight:600; }
.logo-text span { color:var(--warn); }

.title { font-size:16px; font-weight:600; text-align:center; }
.sub { font-size:12px; color:var(--text3); text-align:center; margin-bottom:24px; }

.form-group { margin-bottom:14px; display:flex; flex-direction:column; gap:6px; }
.form-group label { font-size:11px; font-weight:500; color:var(--text3); text-transform:uppercase; letter-spacing:0.5px; }
.form-input {
  background:var(--bg2); border:1px solid var(--border2);
  border-radius:var(--radius-sm); padding:10px 12px;
  color:var(--text1); font-size:13px; font-family:var(--font); outline:none; transition:border 0.2s;
}
.form-input:focus { border-color:var(--warn); }
.error-msg { font-size:12px; color:var(--danger); text-align:center; margin-top:8px; padding:8px; background:rgba(240,80,96,0.1); border-radius:6px; }
</style>
