<template>
  <div class="max-w-2xl mx-auto px-4 py-10">
    <button @click="$router.back()" class="flex items-center gap-1 text-sm text-gray-500 hover:text-blue-600 mb-6 transition-colors">
      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
      </svg>
      Quay lại
    </button>

    <div v-if="loading" class="card space-y-4 animate-pulse">
      <div class="bg-gray-200 h-6 rounded w-1/2"></div>
      <div class="bg-gray-200 h-4 rounded w-1/3"></div>
      <div class="bg-gray-200 h-20 rounded"></div>
    </div>

    <div v-else-if="!appt" class="card text-center py-16 text-gray-400">
      Không tìm thấy lịch hẹn.
    </div>

    <div v-else class="space-y-4">
      <div class="card">
        <div class="flex items-start justify-between mb-4">
          <h1 class="text-xl font-bold text-gray-900">Chi tiết lịch hẹn #{{ appt.id }}</h1>
          <span :class="statusClass(appt.status)" class="text-xs font-semibold px-3 py-1 rounded-full">
            {{ statusLabel(appt.status) }}
          </span>
        </div>

        <dl class="grid sm:grid-cols-2 gap-4 text-sm">
          <div>
            <dt class="text-gray-500 mb-0.5">Ngày & giờ</dt>
            <dd class="font-medium text-gray-900">{{ formatDate(appt.scheduledAt) }}</dd>
          </div>
          <div>
            <dt class="text-gray-500 mb-0.5">Số thứ tự</dt>
            <dd class="font-medium text-gray-900">{{ appt.queueNumber ?? '—' }}</dd>
          </div>
          <div>
            <dt class="text-gray-500 mb-0.5">Bác sĩ</dt>
            <dd class="font-medium text-gray-900">{{ appt.doctorName ?? 'Chưa phân công' }}</dd>
          </div>
          <div>
            <dt class="text-gray-500 mb-0.5">Chuyên khoa</dt>
            <dd class="font-medium text-gray-900">{{ appt.doctorSpecialty ?? appt.department ?? '—' }}</dd>
          </div>
          <div class="sm:col-span-2" v-if="appt.description">
            <dt class="text-gray-500 mb-0.5">Mô tả</dt>
            <dd class="text-gray-900">{{ appt.description }}</dd>
          </div>
          <div class="sm:col-span-2" v-if="appt.notes">
            <dt class="text-gray-500 mb-0.5">Ghi chú bác sĩ</dt>
            <dd class="text-gray-900 bg-gray-50 rounded-lg px-3 py-2">{{ appt.notes }}</dd>
          </div>
        </dl>
      </div>

      <!-- Cancel button (only Pending) -->
      <div v-if="appt.status === 'Pending'" class="flex justify-end">
        <button @click="cancelAppt" :disabled="cancelling"
          class="bg-red-50 text-red-600 border border-red-200 px-5 py-2 rounded-xl text-sm font-medium hover:bg-red-100 transition-colors disabled:opacity-50">
          {{ cancelling ? 'Đang hủy...' : 'Hủy lịch hẹn' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { appointmentApi } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { AppointmentDto } from '@/types/api'

const route   = useRoute()
const router  = useRouter()
const toast   = useToastStore()
const appt    = ref<AppointmentDto | null>(null)
const loading = ref(true)
const cancelling = ref(false)

onMounted(async () => {
  try {
    const { data } = await appointmentApi.get(Number(route.params.id))
    appt.value = data
  } catch {
    toast.error('Không thể tải thông tin lịch hẹn.')
  } finally {
    loading.value = false
  }
})

async function cancelAppt() {
  if (!appt.value) return
  cancelling.value = true
  try {
    await appointmentApi.update(appt.value.id, { status: 'Cancelled' })
    appt.value.status = 'Cancelled'
    toast.success('Đã hủy lịch hẹn.')
  } catch {
    toast.error('Hủy lịch hẹn thất bại.')
  } finally {
    cancelling.value = false
  }
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleString('vi-VN', { dateStyle: 'full', timeStyle: 'short' })
}
function statusLabel(s: string) {
  return { Pending: 'Chờ xác nhận', Confirmed: 'Đã xác nhận', Cancelled: 'Đã hủy', Completed: 'Hoàn thành' }[s] ?? s
}
function statusClass(s: string) {
  return {
    Pending:   'bg-yellow-100 text-yellow-700',
    Confirmed: 'bg-blue-100 text-blue-700',
    Cancelled: 'bg-red-100 text-red-700',
    Completed: 'bg-green-100 text-green-700',
  }[s] ?? 'bg-gray-100 text-gray-700'
}
</script>
