<template>
  <div class="max-w-5xl mx-auto px-4 py-10">
    <div class="flex items-center justify-between mb-8">
      <div>
        <h1 class="text-3xl font-bold text-gray-900 mb-1">Lịch hẹn của tôi</h1>
        <p class="text-gray-500 text-sm">Quản lý và theo dõi lịch khám bệnh</p>
      </div>
      <RouterLink to="/appointment" class="btn-primary text-sm">+ Đặt lịch mới</RouterLink>
    </div>

    <!-- Filter -->
    <div class="flex gap-2 mb-6 flex-wrap">
      <button v-for="f in filters" :key="f.value"
        @click="activeFilter = f.value; loadAppointments()"
        :class="['px-4 py-2 rounded-lg text-sm font-medium transition-colors', activeFilter === f.value ? 'bg-blue-600 text-white' : 'bg-white border border-gray-200 text-gray-600 hover:bg-gray-50']">
        {{ f.label }}
      </button>
    </div>

    <div v-if="loading" class="space-y-4">
      <div v-for="i in 3" :key="i" class="card animate-pulse h-24"></div>
    </div>

    <div v-else-if="appointments.length === 0" class="card text-center py-16">
      <svg class="w-12 h-12 text-gray-300 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/>
      </svg>
      <p class="text-gray-500">Chưa có lịch hẹn nào</p>
      <RouterLink to="/appointment" class="btn-primary mt-4 inline-block">Đặt lịch ngay</RouterLink>
    </div>

    <div v-else class="space-y-4">
      <div v-for="apt in appointments" :key="apt.id" class="card hover:shadow-md transition-shadow">
        <div class="flex items-start justify-between gap-4">
          <div class="flex items-center gap-4">
            <div class="w-12 h-12 bg-blue-100 rounded-xl flex items-center justify-center shrink-0">
              <span class="text-blue-700 font-bold text-lg">#{{ apt.queueNumber || '—' }}</span>
            </div>
            <div>
              <div class="flex items-center gap-2 mb-1">
                <span :class="statusBadge(apt.status)">{{ statusLabel(apt.status) }}</span>
                <span v-if="apt.department" class="badge-info">{{ apt.department }}</span>
              </div>
              <p class="font-semibold text-gray-900">{{ formatDateTime(apt.scheduledAt) }}</p>
              <p v-if="apt.doctorName" class="text-sm text-gray-500">BS. {{ apt.doctorName }} • {{ apt.doctorSpecialty }}</p>
              <p v-if="apt.description" class="text-sm text-gray-400 mt-1">{{ apt.description }}</p>
            </div>
          </div>
          <div class="flex gap-2 shrink-0">
            <button v-if="apt.status === 'Pending'" @click="cancelAppointment(apt.id)"
              class="btn-danger text-xs py-1.5 px-3">Hủy</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { appointmentApi } from '@/services/api'

const appointments = ref<any[]>([])
const loading = ref(true)
const activeFilter = ref('')

const filters = [
  { label: 'Tất cả', value: '' },
  { label: 'Chờ xác nhận', value: 'Pending' },
  { label: 'Đã xác nhận', value: 'Confirmed' },
  { label: 'Hoàn thành', value: 'Completed' },
  { label: 'Đã hủy', value: 'Cancelled' }
]

function statusBadge(status: string) {
  const map: Record<string, string> = {
    Pending: 'badge-warning', Confirmed: 'badge-success',
    Completed: 'badge-info', Cancelled: 'badge-danger'
  }
  return map[status] || 'badge-info'
}

function statusLabel(status: string) {
  const map: Record<string, string> = {
    Pending: 'Chờ xác nhận', Confirmed: 'Đã xác nhận',
    Completed: 'Hoàn thành', Cancelled: 'Đã hủy'
  }
  return map[status] || status
}

function formatDateTime(dt: string) {
  return new Date(dt).toLocaleString('vi-VN', { weekday: 'long', year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })
}

async function loadAppointments() {
  loading.value = true
  try {
    const { data } = await appointmentApi.list({ status: activeFilter.value || undefined })
    appointments.value = data.data || []
  } catch { appointments.value = [] }
  finally { loading.value = false }
}

async function cancelAppointment(id: number) {
  if (!confirm('Bạn có chắc muốn hủy lịch hẹn này?')) return
  await appointmentApi.update(id, { status: 'Cancelled' })
  await loadAppointments()
}

onMounted(loadAppointments)
</script>
