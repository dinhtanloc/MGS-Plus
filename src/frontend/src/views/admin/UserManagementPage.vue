<template>
  <div>
    <!-- Filters bar -->
    <div class="flex flex-wrap gap-3 mb-5">
      <div class="relative flex-1 min-w-52">
        <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
        </svg>
        <input v-model="search" @input="debouncedLoad" type="text"
          class="input-field pl-9" placeholder="Tìm theo tên, email..." />
      </div>
      <select v-model="filterRole" @change="() => { page = 1; load() }"
        class="border border-gray-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white">
        <option value="">Tất cả vai trò</option>
        <option value="Patient">Bệnh nhân</option>
        <option value="Doctor">Bác sĩ</option>
        <option value="Admin">Admin</option>
      </select>
      <select v-model="filterStatus" @change="() => { page = 1; load() }"
        class="border border-gray-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white">
        <option value="">Tất cả trạng thái</option>
        <option value="verified">Đã xác thực</option>
        <option value="pending">Chờ xác thực</option>
        <option value="locked">Bị khóa</option>
      </select>
    </div>

    <!-- Table -->
    <div class="bg-white rounded-2xl border border-gray-100 overflow-hidden">
      <table class="w-full text-sm">
        <thead class="bg-gray-50 border-b border-gray-100">
          <tr>
            <th class="text-left px-5 py-3 text-xs font-medium text-gray-500 uppercase">Người dùng</th>
            <th class="text-left px-5 py-3 text-xs font-medium text-gray-500 uppercase hidden md:table-cell">Vai trò</th>
            <th class="text-left px-5 py-3 text-xs font-medium text-gray-500 uppercase hidden lg:table-cell">Trạng thái</th>
            <th class="text-left px-5 py-3 text-xs font-medium text-gray-500 uppercase hidden lg:table-cell">Ngày tham gia</th>
            <th class="text-right px-5 py-3 text-xs font-medium text-gray-500 uppercase">Thao tác</th>
          </tr>
        </thead>
        <tbody v-if="loading">
          <tr v-for="i in 8" :key="i">
            <td colspan="5" class="px-5 py-3">
              <div class="h-5 bg-gray-100 rounded animate-pulse"></div>
            </td>
          </tr>
        </tbody>
        <tbody v-else class="divide-y divide-gray-50">
          <tr v-for="user in filteredUsers" :key="user.id" class="hover:bg-gray-50 transition-colors">
            <td class="px-5 py-3">
              <div class="flex items-center gap-3">
                <div class="w-9 h-9 rounded-full flex items-center justify-center text-xs font-bold shrink-0"
                  :class="roleAvatarClass(user.role)">
                  {{ (user.firstName[0] + user.lastName[0]).toUpperCase() }}
                </div>
                <div>
                  <p class="font-medium text-gray-900">{{ user.firstName }} {{ user.lastName }}</p>
                  <p class="text-gray-400 text-xs">{{ user.email }}</p>
                </div>
              </div>
            </td>
            <td class="px-5 py-3 hidden md:table-cell">
              <span :class="['text-xs font-semibold px-2.5 py-1 rounded-full', roleBadgeClass(user.role)]">
                {{ user.role }}
              </span>
            </td>
            <td class="px-5 py-3 hidden lg:table-cell">
              <div class="flex flex-col gap-1">
                <!-- Email verification badge -->
                <span v-if="user.isEmailVerified"
                  class="inline-flex items-center gap-1 text-xs font-medium text-green-700 bg-green-50 px-2 py-0.5 rounded-full w-fit">
                  <svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
                    <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
                  </svg>
                  Đã xác thực
                </span>
                <span v-else
                  class="inline-flex items-center gap-1 text-xs font-medium text-amber-700 bg-amber-50 px-2 py-0.5 rounded-full w-fit">
                  <svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
                    <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clip-rule="evenodd"/>
                  </svg>
                  Chờ xác thực
                </span>
                <!-- Active badge -->
                <span v-if="!user.isActive"
                  class="inline-flex items-center gap-1 text-xs font-medium text-red-700 bg-red-50 px-2 py-0.5 rounded-full w-fit">
                  <svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
                    <path fill-rule="evenodd" d="M13.477 14.89A6 6 0 015.11 6.524l8.367 8.368zm1.414-1.414L6.524 5.11a6 6 0 018.367 8.367zM18 10a8 8 0 11-16 0 8 8 0 0116 0z" clip-rule="evenodd"/>
                  </svg>
                  Bị khóa
                </span>
              </div>
            </td>
            <td class="px-5 py-3 text-gray-400 text-xs hidden lg:table-cell">{{ formatDate(user.createdAt) }}</td>
            <td class="px-5 py-3">
              <div class="flex items-center justify-end gap-1 flex-wrap">
                <!-- Email verification actions -->
                <button v-if="!user.isEmailVerified" @click="confirmAction('verify', user)"
                  class="text-xs px-2.5 py-1.5 rounded-lg bg-green-50 text-green-700 hover:bg-green-100 transition-colors whitespace-nowrap">
                  Xác thực
                </button>
                <button v-if="!user.isEmailVerified" @click="confirmAction('resend', user)"
                  class="text-xs px-2.5 py-1.5 rounded-lg bg-blue-50 text-blue-700 hover:bg-blue-100 transition-colors whitespace-nowrap">
                  Gửi lại
                </button>
                <!-- Role actions -->
                <button v-if="user.role !== 'Admin'" @click="confirmAction('grant', user)"
                  class="text-xs px-2.5 py-1.5 rounded-lg bg-purple-50 text-purple-700 hover:bg-purple-100 transition-colors">
                  +Admin
                </button>
                <button v-else @click="confirmAction('revoke', user)"
                  class="text-xs px-2.5 py-1.5 rounded-lg bg-gray-100 text-gray-600 hover:bg-gray-200 transition-colors">
                  -Admin
                </button>
                <!-- Lock/Unlock -->
                <button @click="confirmAction('toggle', user)"
                  class="text-xs px-2.5 py-1.5 rounded-lg transition-colors"
                  :class="user.isActive
                    ? 'bg-red-50 text-red-600 hover:bg-red-100'
                    : 'bg-green-50 text-green-700 hover:bg-green-100'">
                  {{ user.isActive ? 'Khóa' : 'Mở' }}
                </button>
              </div>
            </td>
          </tr>
          <tr v-if="!filteredUsers.length">
            <td colspan="5" class="text-center py-16 text-gray-400">
              <div class="text-3xl mb-2">👤</div>
              <p>Không tìm thấy người dùng</p>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Pagination -->
    <div v-if="total > pageSize" class="flex items-center justify-between mt-4 text-sm text-gray-600">
      <p>{{ (page - 1) * pageSize + 1 }}–{{ Math.min(page * pageSize, total) }} / {{ total }}</p>
      <div class="flex gap-2">
        <button @click="prevPage" :disabled="page <= 1"
          class="px-3 py-1.5 rounded-lg border border-gray-200 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed">
          ← Trước
        </button>
        <button @click="nextPage" :disabled="page * pageSize >= total"
          class="px-3 py-1.5 rounded-lg border border-gray-200 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed">
          Sau →
        </button>
      </div>
    </div>

    <!-- Confirm modal -->
    <div v-if="confirmModal" class="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-2xl shadow-xl p-6 max-w-sm w-full">
        <h3 class="font-semibold text-gray-900 mb-2">{{ confirmModal.title }}</h3>
        <p class="text-sm text-gray-600 mb-5">{{ confirmModal.message }}</p>
        <div class="flex gap-3 justify-end">
          <button @click="confirmModal = null" class="text-sm text-gray-600 px-4 py-2 rounded-xl hover:bg-gray-100">Hủy</button>
          <button @click="confirmModal.action()" :disabled="actionLoading"
            class="text-sm px-4 py-2 rounded-xl text-white disabled:opacity-50 transition-colors"
            :class="confirmModal.danger ? 'bg-red-600 hover:bg-red-700' : 'bg-blue-600 hover:bg-blue-700'">
            {{ actionLoading ? 'Đang xử lý...' : 'Xác nhận' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { adminApi } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { AdminUserDto } from '@/types/api'

const toast = useToastStore()
const users = ref<AdminUserDto[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = 20
const search = ref('')
const filterRole = ref('')
const filterStatus = ref('')
const loading = ref(true)
const actionLoading = ref(false)
const confirmModal = ref<{ title: string; message: string; danger: boolean; action: () => void } | null>(null)

let debounceTimer: ReturnType<typeof setTimeout>
function debouncedLoad() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => { page.value = 1; load() }, 350)
}

onMounted(load)

async function load() {
  loading.value = true
  try {
    const { data } = await adminApi.getUsers({ search: search.value || undefined, page: page.value, pageSize })
    users.value = data.data
    total.value = data.total
  } catch {
    toast.error('Không thể tải danh sách người dùng.')
  } finally {
    loading.value = false
  }
}

// Client-side filter by role / status (backend handles search + pagination)
const filteredUsers = computed(() => {
  let list = users.value
  if (filterRole.value) list = list.filter(u => u.role === filterRole.value)
  if (filterStatus.value === 'verified')  list = list.filter(u => u.isEmailVerified && u.isActive)
  if (filterStatus.value === 'pending')   list = list.filter(u => !u.isEmailVerified)
  if (filterStatus.value === 'locked')    list = list.filter(u => !u.isActive)
  return list
})

function prevPage() { if (page.value > 1) { page.value--; load() } }
function nextPage() { if (page.value * pageSize < total.value) { page.value++; load() } }

type ActionType = 'verify' | 'resend' | 'grant' | 'revoke' | 'toggle'

function confirmAction(type: ActionType, user: AdminUserDto) {
  const actions: Record<ActionType, { title: string; message: string; danger: boolean; fn: () => Promise<void> }> = {
    verify: {
      title: 'Xác thực email thủ công',
      message: `Đánh dấu email của ${user.firstName} ${user.lastName} là đã xác thực?`,
      danger: false,
      fn: async () => {
        await adminApi.verifyUserEmail(user.id)
        user.isEmailVerified = true
        toast.success('Đã xác thực email.')
      }
    },
    resend: {
      title: 'Gửi lại email xác thực',
      message: `Gửi lại email xác thực đến ${user.email}?`,
      danger: false,
      fn: async () => {
        await adminApi.resendVerification(user.id)
        toast.success('Đã gửi lại email xác thực.')
      }
    },
    grant: {
      title: 'Cấp quyền Admin',
      message: `Cấp quyền Admin cho ${user.firstName} ${user.lastName}?`,
      danger: false,
      fn: async () => {
        await adminApi.grantAdmin(user.id)
        user.role = 'Admin'
        toast.success('Đã cấp quyền Admin.')
      }
    },
    revoke: {
      title: 'Thu hồi quyền Admin',
      message: `Thu hồi quyền Admin của ${user.firstName} ${user.lastName}?`,
      danger: true,
      fn: async () => {
        await adminApi.revokeAdmin(user.id)
        user.role = 'Patient'
        toast.success('Đã thu hồi quyền Admin.')
      }
    },
    toggle: {
      title: user.isActive ? 'Khóa tài khoản' : 'Mở khóa tài khoản',
      message: user.isActive
        ? `Khóa tài khoản ${user.firstName} ${user.lastName}? Người dùng sẽ không thể đăng nhập.`
        : `Mở khóa tài khoản ${user.firstName} ${user.lastName}?`,
      danger: user.isActive,
      fn: async () => {
        const { data } = await adminApi.toggleUserActive(user.id)
        user.isActive = data.isActive
        toast.success(data.isActive ? 'Đã mở khóa.' : 'Đã khóa tài khoản.')
      }
    }
  }

  const cfg = actions[type]
  confirmModal.value = {
    title:   cfg.title,
    message: cfg.message,
    danger:  cfg.danger,
    action: async () => {
      actionLoading.value = true
      try {
        await cfg.fn()
        confirmModal.value = null
      } catch (e: any) {
        toast.error(e.response?.data?.message || 'Thao tác thất bại.')
      } finally {
        actionLoading.value = false
      }
    }
  }
}

function formatDate(d: string) {
  return new Date(d).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function roleAvatarClass(role: string) {
  return role === 'Admin' ? 'bg-purple-100 text-purple-700'
       : role === 'Doctor' ? 'bg-green-100 text-green-700'
       : 'bg-blue-100 text-blue-700'
}

function roleBadgeClass(role: string) {
  return role === 'Admin' ? 'bg-purple-100 text-purple-700'
       : role === 'Doctor' ? 'bg-green-100 text-green-700'
       : 'bg-blue-100 text-blue-700'
}
</script>
