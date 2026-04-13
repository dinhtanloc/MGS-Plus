<template>
  <nav class="bg-white shadow-sm border-b border-gray-200 sticky top-0 z-40">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <div class="flex items-center justify-between h-16">
        <!-- Logo -->
        <RouterLink to="/" class="flex items-center gap-2 shrink-0">
          <div class="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
            <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"/>
            </svg>
          </div>
          <span class="text-xl font-bold text-blue-700">MGSPlus</span>
        </RouterLink>

        <!-- Desktop nav -->
        <div class="hidden md:flex items-center gap-1">
          <RouterLink v-for="item in navItems" :key="item.path"
            :to="item.path"
            class="px-3 py-2 text-sm font-medium text-gray-600 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition-colors"
            active-class="text-blue-600 bg-blue-50">
            {{ item.label }}
          </RouterLink>
        </div>

        <!-- Right actions -->
        <div class="flex items-center gap-3">
          <button v-if="!auth.isLoggedIn" @click="$router.push('/login')"
            class="btn-secondary text-sm py-2 px-4">
            Đăng nhập
          </button>
          <button v-if="!auth.isLoggedIn" @click="$router.push('/register')"
            class="btn-primary text-sm py-2 px-4">
            Đăng ký
          </button>

          <!-- User menu -->
          <div v-else class="relative" ref="userMenuRef">
            <button @click="userMenuOpen = !userMenuOpen"
              class="flex items-center gap-2 text-sm font-medium text-gray-700 hover:text-blue-600 focus:outline-none">
              <div class="w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center text-blue-700 font-semibold text-xs">
                {{ userInitials }}
              </div>
              <span class="hidden md:block">{{ auth.user?.firstName }}</span>
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
              </svg>
            </button>

            <div v-if="userMenuOpen"
              class="absolute right-0 mt-2 w-52 bg-white rounded-xl shadow-lg border border-gray-100 py-1 z-50">
              <RouterLink to="/profile" @click="userMenuOpen = false"
                class="flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/>
                </svg>
                Hồ sơ cá nhân
              </RouterLink>
              <RouterLink to="/appointments" @click="userMenuOpen = false"
                class="flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/>
                </svg>
                Lịch hẹn của tôi
              </RouterLink>
              <RouterLink to="/medical-records" @click="userMenuOpen = false"
                class="flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                </svg>
                Hồ sơ y tế
              </RouterLink>
              <hr class="my-1 border-gray-100" />
              <button @click="handleLogout"
                class="flex items-center gap-2 w-full px-4 py-2 text-sm text-red-600 hover:bg-red-50">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/>
                </svg>
                Đăng xuất
              </button>
            </div>
          </div>

          <!-- Mobile menu button -->
          <button class="md:hidden p-2 text-gray-500 hover:text-blue-600" @click="mobileOpen = !mobileOpen">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"/>
            </svg>
          </button>
        </div>
      </div>

      <!-- Mobile nav -->
      <div v-if="mobileOpen" class="md:hidden pb-3 space-y-1">
        <RouterLink v-for="item in navItems" :key="item.path"
          :to="item.path" @click="mobileOpen = false"
          class="block px-3 py-2 text-sm text-gray-700 hover:bg-blue-50 hover:text-blue-600 rounded-lg">
          {{ item.label }}
        </RouterLink>
      </div>
    </div>
  </nav>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { onClickOutside } from '@vueuse/core'

const auth = useAuthStore()
const router = useRouter()
const userMenuOpen = ref(false)
const mobileOpen = ref(false)
const userMenuRef = ref<HTMLElement>()

onClickOutside(userMenuRef, () => { userMenuOpen.value = false })

const navItems = [
  { path: '/', label: 'Trang chủ' },
  { path: '/news', label: 'Tin tức y tế' },
  { path: '/blog', label: 'Blog sức khỏe' },
  { path: '/services', label: 'Dịch vụ' },
  { path: '/appointment', label: 'Đăng ký khám' }
]

const userInitials = computed(() => {
  if (!auth.user) return '?'
  return `${auth.user.firstName[0]}${auth.user.lastName[0]}`.toUpperCase()
})

function handleLogout() {
  auth.logout()
  userMenuOpen.value = false
  router.push('/')
}
</script>
