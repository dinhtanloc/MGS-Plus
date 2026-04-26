<template>
  <div class="min-h-screen bg-gray-50 flex">
    <!-- Sidebar -->
    <aside class="w-64 shrink-0 bg-white border-r border-gray-200 flex flex-col">
      <div class="h-16 flex items-center gap-2 px-5 border-b border-gray-100">
        <div class="w-7 h-7 bg-blue-600 rounded-lg flex items-center justify-center">
          <svg class="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"/>
          </svg>
        </div>
        <span class="font-bold text-gray-900">MGSPlus <span class="text-blue-600 text-xs font-semibold bg-blue-50 px-1.5 py-0.5 rounded-md ml-1">Admin</span></span>
      </div>

      <nav class="flex-1 py-4 px-3 space-y-0.5">
        <RouterLink v-for="item in navItems" :key="item.to" :to="item.to"
          class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-colors"
          :class="isActive(item.to)
            ? 'bg-blue-50 text-blue-700'
            : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'">
          <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="item.icon"/>
          </svg>
          {{ item.label }}
          <span v-if="item.badge" class="ml-auto text-xs bg-yellow-100 text-yellow-700 font-semibold px-1.5 py-0.5 rounded-full">
            {{ item.badge }}
          </span>
        </RouterLink>
      </nav>

      <div class="px-3 pb-4 border-t border-gray-100 pt-3">
        <RouterLink to="/"
          class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-gray-500 hover:bg-gray-50 hover:text-gray-700 transition-colors">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"/>
          </svg>
          Về trang chủ
        </RouterLink>
      </div>
    </aside>

    <!-- Main content -->
    <div class="flex-1 flex flex-col min-w-0">
      <!-- Top bar -->
      <header class="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-6">
        <h1 class="text-lg font-semibold text-gray-900">{{ pageTitle }}</h1>
        <div class="flex items-center gap-2">
          <div class="w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center text-blue-700 font-semibold text-xs">
            {{ userInitials }}
          </div>
          <span class="text-sm font-medium text-gray-700">{{ auth.user?.firstName }}</span>
        </div>
      </header>

      <main class="flex-1 p-6 overflow-auto">
        <RouterView />
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { adminApi } from '@/services/api'

const auth = useAuthStore()
const route = useRoute()
const pendingCount = ref(0)

adminApi.getDoctorApplications('Pending').then(({ data }) => {
  pendingCount.value = data.length
}).catch(() => {})

const navItems = computed(() => [
  { to: '/admin', label: 'Dashboard', icon: 'M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6' },
  { to: '/admin/doctors', label: 'Duyệt bác sĩ', icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2', badge: pendingCount.value > 0 ? pendingCount.value : undefined },
  { to: '/admin/users', label: 'Người dùng', icon: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z' },
  { to: '/admin/blog',  label: 'Blog',          icon: 'M19 20H5a2 2 0 01-2-2V6a2 2 0 012-2h10a2 2 0 012 2v1m2 13a2 2 0 01-2-2V7m2 13a2 2 0 002-2V9a2 2 0 00-2-2h-2m-4-3H9M7 16h6M7 8h6v4H7V8z' },
  { to: '/admin/chats', label: 'Lịch sử chat',  icon: 'M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z' },
  { to: '/admin/news',  label: 'Tin tức',        icon: 'M19 20H5a2 2 0 01-2-2V6a2 2 0 012-2h10a2 2 0 012 2v1m2 13a2 2 0 01-2-2V7m2 13a2 2 0 002-2V9a2 2 0 00-2-2h-2m-4-3H9M7 16h6M7 8h6v4H7V8z' },
])

const pageTitle = computed(() => {
  const map: Record<string, string> = {
    'admin-dashboard': 'Dashboard',
    'admin-doctors':   'Duyệt đơn bác sĩ',
    'admin-users':     'Quản lý người dùng',
    'admin-blog':      'Quản lý Blog',
    'admin-chats':     'Lịch sử Chat',
    'admin-news':      'Quản lý Tin tức',
  }
  return map[route.name as string] ?? 'Admin Panel'
})

const userInitials = computed(() => {
  if (!auth.user) return 'A'
  return `${auth.user.firstName[0]}${auth.user.lastName[0]}`.toUpperCase()
})

function isActive(to: string) {
  if (to === '/admin') return route.path === '/admin'
  return route.path.startsWith(to)
}
</script>
