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
        <h1 class="text-2xl font-bold text-gray-900">Tạo tài khoản</h1>
        <p class="text-gray-500 mt-1 text-sm">Đăng ký miễn phí và bắt đầu ngay hôm nay</p>
      </div>

      <div class="card shadow-lg">
        <form @submit.prevent="handleRegister" class="space-y-4">
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1.5">Họ</label>
              <input v-model="form.lastName" type="text" class="input-field" placeholder="Nguyễn" required />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1.5">Tên</label>
              <input v-model="form.firstName" type="text" class="input-field" placeholder="Văn A" required />
            </div>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1.5">Email</label>
            <input v-model="form.email" type="email" class="input-field" placeholder="email@example.com" required />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1.5">Số điện thoại</label>
            <input v-model="form.phoneNumber" type="tel" class="input-field" placeholder="0901234567" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1.5">Mật khẩu</label>
            <input v-model="form.password" type="password" class="input-field" placeholder="Tối thiểu 8 ký tự" required minlength="8" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1.5">Xác nhận mật khẩu</label>
            <input v-model="confirmPassword" type="password" class="input-field" placeholder="Nhập lại mật khẩu" required />
          </div>

          <p v-if="error" class="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-lg">{{ error }}</p>

          <button type="submit" :disabled="loading" class="btn-primary w-full flex items-center justify-center gap-2">
            <svg v-if="loading" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
            </svg>
            {{ loading ? 'Đang đăng ký...' : 'Đăng ký' }}
          </button>
        </form>

        <p class="text-center text-sm text-gray-500 mt-5">
          Đã có tài khoản?
          <RouterLink to="/login" class="text-blue-600 font-medium hover:text-blue-700">Đăng nhập</RouterLink>
        </p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()

const form = ref({ email: '', password: '', firstName: '', lastName: '', phoneNumber: '' })
const confirmPassword = ref('')
const loading = ref(false)
const error = ref('')

async function handleRegister() {
  if (form.value.password !== confirmPassword.value) {
    error.value = 'Mật khẩu xác nhận không khớp'
    return
  }
  loading.value = true
  error.value = ''
  try {
    await auth.register(form.value)
    router.push('/')
  } catch (e: any) {
    error.value = e.response?.data?.message || 'Đăng ký thất bại. Vui lòng thử lại.'
  } finally {
    loading.value = false
  }
}
</script>
