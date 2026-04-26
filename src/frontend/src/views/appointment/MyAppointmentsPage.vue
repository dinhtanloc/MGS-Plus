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
              <!-- Reschedule notice -->
              <p v-if="apt.status === 'Rescheduled' && apt.rescheduledTo"
                class="text-xs text-orange-600 mt-1 font-medium">
                Đổi lịch → {{ formatDateTime(apt.rescheduledTo) }}
                <span v-if="apt.rescheduleReason">· {{ apt.rescheduleReason }}</span>
              </p>
            </div>
          </div>
          <div class="flex flex-col gap-2 shrink-0 items-end">
            <button v-if="apt.status === 'Pending'" @click="cancelAppointment(apt.id)"
              class="btn-danger text-xs py-1.5 px-3">Hủy</button>
            <!-- Review button for completed appointments with a doctor -->
            <button v-if="apt.status === 'Completed' && apt.doctorId && !reviewedIds.has(apt.id)"
              @click="openReview(apt)"
              class="text-xs px-3 py-1.5 rounded-lg bg-yellow-50 text-yellow-700 hover:bg-yellow-100 transition-colors font-medium flex items-center gap-1">
              <svg class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20">
                <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
              </svg>
              Đánh giá
            </button>
            <span v-else-if="apt.status === 'Completed' && apt.doctorId && reviewedIds.has(apt.id)"
              class="text-xs text-green-600 flex items-center gap-1">
              <svg class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
              </svg>
              Đã đánh giá
            </span>
          </div>
        </div>
      </div>
    </div>

    <!-- Review modal -->
    <Teleport to="body">
      <div v-if="reviewTarget" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
        <div class="bg-white rounded-2xl shadow-2xl w-full max-w-md p-6">
          <h3 class="font-bold text-gray-900 text-lg mb-1">Đánh giá bác sĩ</h3>
          <p class="text-sm text-gray-500 mb-5">BS. {{ reviewTarget.doctorName }} · {{ formatDateTime(reviewTarget.scheduledAt) }}</p>

          <!-- Stars -->
          <div class="mb-4">
            <p class="text-sm font-medium text-gray-700 mb-2">Chất lượng khám bệnh</p>
            <div class="flex gap-1">
              <button v-for="i in 5" :key="i" @click="reviewForm.rating = i"
                class="transition-transform hover:scale-110">
                <svg class="w-8 h-8" :class="i <= reviewForm.rating ? 'text-yellow-400' : 'text-gray-200'"
                  fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                </svg>
              </button>
            </div>
            <p class="text-xs text-gray-400 mt-1">{{ ratingLabel }}</p>
          </div>

          <div class="mb-5">
            <label class="text-sm font-medium text-gray-700 mb-1.5 block">Nhận xét <span class="text-gray-400 font-normal">(tùy chọn)</span></label>
            <textarea v-model="reviewForm.comment" rows="3"
              class="input-field resize-none"
              placeholder="Chia sẻ trải nghiệm của bạn..."></textarea>
          </div>

          <div class="flex gap-3 justify-end">
            <button @click="reviewTarget = null" class="text-sm text-gray-600 px-4 py-2 rounded-xl hover:bg-gray-100">Bỏ qua</button>
            <button @click="submitReview" :disabled="reviewForm.rating === 0 || reviewLoading"
              class="btn-primary disabled:opacity-50">
              {{ reviewLoading ? 'Đang gửi...' : 'Gửi đánh giá' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { appointmentApi } from '@/services/api'
import { useToastStore } from '@/stores/toast'

const toast = useToastStore()
const appointments = ref<any[]>([])
const loading = ref(true)
const activeFilter = ref('')
const reviewedIds = ref<Set<number>>(new Set())

// Review
const reviewTarget = ref<any>(null)
const reviewLoading = ref(false)
const reviewForm = ref({ rating: 0, comment: '' })

const filters = [
  { label: 'Tất cả', value: '' },
  { label: 'Chờ xác nhận', value: 'Pending' },
  { label: 'Đã xác nhận', value: 'Confirmed' },
  { label: 'Hoàn thành', value: 'Completed' },
  { label: 'Đã hủy', value: 'Cancelled' }
]

const ratingLabel = computed(() => {
  const map = ['', 'Tệ', 'Không hài lòng', 'Bình thường', 'Tốt', 'Xuất sắc']
  return map[reviewForm.value.rating] ?? ''
})

function statusBadge(status: string) {
  const map: Record<string, string> = {
    Pending: 'badge-warning', Confirmed: 'badge-success',
    Completed: 'badge-info', Cancelled: 'badge-danger', Rescheduled: 'badge-warning'
  }
  return map[status] || 'badge-info'
}

function statusLabel(status: string) {
  const map: Record<string, string> = {
    Pending: 'Chờ xác nhận', Confirmed: 'Đã xác nhận',
    Completed: 'Hoàn thành', Cancelled: 'Đã hủy', Rescheduled: 'Đã đổi lịch'
  }
  return map[status] || status
}

function formatDateTime(dt: string) {
  return new Date(dt).toLocaleString('vi-VN', { weekday: 'short', year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })
}

async function loadAppointments() {
  loading.value = true
  try {
    const { data } = await appointmentApi.list({ status: activeFilter.value || undefined })
    appointments.value = data.data || []

    // Check which completed appointments are already reviewed
    const completed = appointments.value.filter(a => a.status === 'Completed' && a.doctorId)
    await Promise.all(completed.map(async a => {
      try {
        const { data: res } = await appointmentApi.checkReview(a.doctorId, a.id)
        if (res.reviewed) reviewedIds.value.add(a.id)
      } catch { /* ignore */ }
    }))
  } catch { appointments.value = [] }
  finally { loading.value = false }
}

async function cancelAppointment(id: number) {
  if (!confirm('Bạn có chắc muốn hủy lịch hẹn này?')) return
  await appointmentApi.update(id, { status: 'Cancelled' })
  await loadAppointments()
}

function openReview(apt: any) {
  reviewTarget.value = apt
  reviewForm.value = { rating: 0, comment: '' }
}

async function submitReview() {
  if (!reviewTarget.value || reviewForm.value.rating === 0) return
  reviewLoading.value = true
  try {
    await appointmentApi.submitReview(reviewTarget.value.doctorId, {
      appointmentId: reviewTarget.value.id,
      rating: reviewForm.value.rating,
      comment: reviewForm.value.comment || undefined
    })
    reviewedIds.value.add(reviewTarget.value.id)
    reviewTarget.value = null
    toast.success('Cảm ơn bạn đã đánh giá!')
  } catch (e: any) {
    toast.error(e.response?.data?.message || 'Gửi đánh giá thất bại')
  } finally {
    reviewLoading.value = false
  }
}

onMounted(loadAppointments)
</script>
