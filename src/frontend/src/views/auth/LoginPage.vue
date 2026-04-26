<template>
  <div class="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-white py-12 px-4">
    <div class="w-full max-w-md">
      <div class="text-center mb-8">
        <RouterLink to="/" class="inline-flex items-center gap-2 mb-6">
          <div class="w-10 h-10 bg-blue-600 rounded-xl flex items-center justify-center">
            <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"/>
            </svg>
          </div>
          <span class="text-2xl font-bold text-blue-700">MGSPlus</span>
        </RouterLink>
        <h1 class="text-2xl font-bold text-gray-900">Đăng nhập</h1>
        <p class="text-gray-500 mt-1 text-sm">Chào mừng trở lại!</p>
      </div>

      <div class="card shadow-lg">
        <form @submit.prevent="handleLogin" class="space-y-5">
          <!-- Email -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1.5">Email</label>
            <div class="relative">
              <input
                v-model="form.email"
                type="email"
                class="input-field transition-colors"
                :class="fieldErrors.email ? 'border-red-400 bg-red-50 pr-10 focus:ring-red-300' : ''"
                placeholder="email@example.com"
                required
                @input="fieldErrors.email = ''"
              />
              <svg v-if="fieldErrors.email" class="absolute right-3 top-1/2 -translate-y-1/2 w-5 h-5 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
              </svg>
            </div>
            <p v-if="fieldErrors.email" class="text-xs text-red-600 mt-1 flex items-center gap-1">
              <svg class="w-3 h-3 shrink-0" fill="currentColor" viewBox="0 0 20 20"><circle cx="10" cy="10" r="9"/><path fill="white" d="M10 6v4m0 2.5v.5" stroke="white" stroke-width="1.5" stroke-linecap="round"/></svg>
              {{ fieldErrors.email }}
            </p>
          </div>

          <!-- Password -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1.5">Mật khẩu</label>
            <div class="relative">
              <input
                v-model="form.password"
                :type="showPass ? 'text' : 'password'"
                class="input-field pr-10 transition-colors"
                :class="fieldErrors.password ? 'border-red-400 bg-red-50 focus:ring-red-300' : ''"
                placeholder="••••••••"
                required
                @input="fieldErrors.password = ''"
              />
              <button type="button" @click="showPass = !showPass"
                class="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    :d="showPass
                      ? 'M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21'
                      : 'M15 12a3 3 0 11-6 0 3 3 0 016 0z M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z'"
                  />
                </svg>
              </button>
            </div>
            <p v-if="fieldErrors.password" class="text-xs text-red-600 mt-1 flex items-center gap-1">
              <svg class="w-3 h-3 shrink-0" fill="currentColor" viewBox="0 0 20 20"><circle cx="10" cy="10" r="9"/><path fill="white" d="M10 6v4m0 2.5v.5" stroke="white" stroke-width="1.5" stroke-linecap="round"/></svg>
              {{ fieldErrors.password }}
            </p>
          </div>

          <button type="submit" :disabled="loading" class="btn-primary w-full flex items-center justify-center gap-2">
            <svg v-if="loading" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
            </svg>
            {{ loading ? 'Đang đăng nhập...' : 'Đăng nhập' }}
          </button>
        </form>

        <p class="text-center text-sm text-gray-500 mt-5">
          Chưa có tài khoản?
          <RouterLink to="/register" class="text-blue-600 font-medium hover:text-blue-700">Đăng ký ngay</RouterLink>
        </p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'

const auth     = useAuthStore()
const toast    = useToastStore()
const router   = useRouter()
const route    = useRoute()

const form         = ref({ email: '', password: '' })
const loading      = ref(false)
const showPass     = ref(false)
const fieldErrors  = ref<Record<string, string>>({})

// Map backend field key → display label
const fieldLabels: Record<string, string> = {
  email:    'Email',
  password: 'Mật khẩu',
}

async function handleLogin() {
  loading.value    = true
  fieldErrors.value = {}

  try {
    await auth.login(form.value.email, form.value.password)
    const redirect = route.query.redirect as string || '/'
    router.push(redirect)
  } catch (e: any) {
    const response = e?.response
    const data     = response?.data
    const errors   = data?.errors as Record<string, string> | undefined

    if (errors && Object.keys(errors).length > 0) {
      fieldErrors.value = errors
      const lines = Object.entries(errors).map(([key, msg]) => {
        const label = fieldLabels[key] ?? key
        return `${label}: ${msg}`
      })
      toast.error(lines.join('\n'), 5000)
    } else if (!response) {
      toast.error('Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối mạng.', 5000)
    } else if (response.status === 401 && data?.message) {
      toast.error(data.message, 4000)
    } else if (response.status >= 500) {
      toast.error('Máy chủ đang gặp sự cố. Vui lòng thử lại sau ít phút.', 5000)
    } else {
      toast.error(data?.message || 'Đăng nhập thất bại. Vui lòng kiểm tra lại thông tin.', 4000)
    }
  } finally {
    loading.value = false
  }
}
</script>
