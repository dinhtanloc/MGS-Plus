<template>
  <div class="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-white px-4">
    <div class="w-full max-w-md text-center">

      <!-- Success -->
      <div v-if="status === 'success'" class="card shadow-lg">
        <div class="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
          <svg class="w-8 h-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
          </svg>
        </div>
        <h1 class="text-2xl font-bold text-gray-900 mb-2">Email đã được xác thực!</h1>
        <p class="text-gray-500 text-sm mb-6">Tài khoản của bạn đã được kích hoạt thành công. Bạn có thể đăng nhập ngay bây giờ.</p>
        <RouterLink to="/login" class="btn-primary inline-block w-full">Đăng nhập</RouterLink>
      </div>

      <!-- Expired / invalid -->
      <div v-else-if="status === 'expired'" class="card shadow-lg">
        <div class="w-16 h-16 bg-yellow-100 rounded-full flex items-center justify-center mx-auto mb-4">
          <svg class="w-8 h-8 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
          </svg>
        </div>
        <h1 class="text-2xl font-bold text-gray-900 mb-2">Liên kết đã hết hạn</h1>
        <p class="text-gray-500 text-sm mb-6">
          Liên kết xác thực không hợp lệ hoặc đã hết hạn (sau 24 giờ).<br>
          Vui lòng đăng nhập và yêu cầu gửi lại email xác thực.
        </p>
        <div class="space-y-3">
          <RouterLink to="/login" class="btn-primary inline-block w-full">Đăng nhập</RouterLink>
          <RouterLink to="/" class="block text-sm text-gray-500 hover:text-gray-700">Về trang chủ</RouterLink>
        </div>
      </div>

      <!-- Unknown / no status param -->
      <div v-else class="card shadow-lg">
        <div class="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4">
          <svg class="w-8 h-8 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/>
          </svg>
        </div>
        <h1 class="text-2xl font-bold text-gray-900 mb-2">Xác thực email</h1>
        <p class="text-gray-500 text-sm mb-6">Nhấn vào liên kết trong email của bạn để xác thực tài khoản.</p>
        <RouterLink to="/" class="btn-primary inline-block w-full">Về trang chủ</RouterLink>
      </div>

    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const status = computed(() => route.query.status as string | undefined)
</script>
