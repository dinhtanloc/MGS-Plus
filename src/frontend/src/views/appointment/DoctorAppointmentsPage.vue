<template>
  <div class="min-h-screen bg-gray-50">
    <!-- Header -->
    <div class="bg-white shadow-sm border-b">
      <div class="max-w-7xl mx-auto px-4 py-6">
        <h1 class="text-2xl font-bold text-gray-900">Quản lý lịch hẹn</h1>
        <p class="text-gray-500 mt-1">Xem và xử lý các lịch hẹn khám của bệnh nhân</p>
      </div>
    </div>

    <div class="max-w-7xl mx-auto px-4 py-6 space-y-6">

      <!-- Stats bar -->
      <div class="grid grid-cols-2 sm:grid-cols-4 gap-4">
        <div v-for="stat in stats" :key="stat.label"
          class="bg-white rounded-xl border p-4 flex items-center gap-3">
          <div :class="['w-10 h-10 rounded-full flex items-center justify-center text-lg', stat.bg]">
            {{ stat.icon }}
          </div>
          <div>
            <div class="text-2xl font-bold text-gray-900">{{ stat.count }}</div>
            <div class="text-xs text-gray-500">{{ stat.label }}</div>
          </div>
        </div>
      </div>

      <!-- Filters -->
      <div class="bg-white rounded-xl border p-4 flex flex-wrap gap-2">
        <button
          v-for="tab in statusTabs"
          :key="tab.value"
          @click="activeStatus = tab.value; loadAppointments()"
          :class="[
            'px-4 py-2 rounded-lg text-sm font-medium transition-colors',
            activeStatus === tab.value
              ? 'bg-blue-600 text-white'
              : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
          ]"
        >
          {{ tab.label }}
          <span v-if="tab.badge" class="ml-1.5 bg-red-500 text-white text-xs rounded-full px-1.5">
            {{ tab.badge }}
          </span>
        </button>
      </div>

      <!-- Appointments table -->
      <div class="bg-white rounded-xl border overflow-hidden">
        <div v-if="loading" class="p-12 text-center text-gray-400">
          <div class="animate-spin w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full mx-auto mb-3"></div>
          Đang tải...
        </div>

        <div v-else-if="appointments.length === 0" class="p-12 text-center text-gray-400">
          <div class="text-4xl mb-3">📋</div>
          <p>Không có lịch hẹn nào</p>
        </div>

        <div v-else>
          <div class="overflow-x-auto">
            <table class="w-full">
              <thead class="bg-gray-50 border-b">
                <tr>
                  <th class="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Bệnh nhân</th>
                  <th class="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Lý do khám</th>
                  <th class="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Ngày hẹn</th>
                  <th class="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Trạng thái</th>
                  <th class="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Hành động</th>
                </tr>
              </thead>
              <tbody class="divide-y">
                <tr v-for="apt in appointments" :key="apt.id" class="hover:bg-gray-50 transition-colors">
                  <td class="px-4 py-4">
                    <div class="font-medium text-gray-900">{{ apt.userName }}</div>
                    <div class="text-sm text-gray-500">{{ apt.userEmail }}</div>
                    <div v-if="apt.userPhone" class="text-sm text-gray-500">{{ apt.userPhone }}</div>
                  </td>
                  <td class="px-4 py-4">
                    <div class="text-sm text-gray-700 max-w-xs">
                      {{ apt.description || '—' }}
                    </div>
                    <div v-if="apt.department" class="text-xs text-blue-600 mt-1">{{ apt.department }}</div>
                    <div v-if="apt.cancelReason" class="text-xs text-red-600 mt-1 bg-red-50 px-2 py-1 rounded">
                      Từ chối: {{ apt.cancelReason }}
                    </div>
                    <div v-if="apt.rescheduleReason" class="text-xs text-orange-600 mt-1 bg-orange-50 px-2 py-1 rounded">
                      Dời lịch: {{ apt.rescheduleReason }}
                    </div>
                  </td>
                  <td class="px-4 py-4">
                    <div class="text-sm font-medium text-gray-900">
                      {{ formatDate(apt.scheduledAt) }}
                    </div>
                    <div class="text-xs text-gray-500">{{ formatTime(apt.scheduledAt) }}</div>
                    <div v-if="apt.queueNumber" class="text-xs text-purple-600 mt-1">
                      STT: #{{ apt.queueNumber }}
                    </div>
                  </td>
                  <td class="px-4 py-4">
                    <StatusBadge :status="apt.status" />
                  </td>
                  <td class="px-4 py-4">
                    <div v-if="apt.status === 'Pending' || apt.status === 'Rescheduled'"
                      class="flex flex-col gap-2">
                      <button @click="openAccept(apt)"
                        class="px-3 py-1.5 bg-green-600 text-white text-xs rounded-lg hover:bg-green-700 font-medium">
                        ✓ Chấp nhận
                      </button>
                      <button @click="openReschedule(apt)"
                        class="px-3 py-1.5 bg-orange-500 text-white text-xs rounded-lg hover:bg-orange-600 font-medium">
                        ↺ Dời lịch
                      </button>
                      <button @click="openReject(apt)"
                        class="px-3 py-1.5 bg-red-600 text-white text-xs rounded-lg hover:bg-red-700 font-medium">
                        ✕ Từ chối
                      </button>
                    </div>
                    <div v-else>
                      <button @click="openChat(apt)"
                        class="px-3 py-1.5 bg-blue-100 text-blue-700 text-xs rounded-lg hover:bg-blue-200 font-medium">
                        💬 Nhắn tin
                      </button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Pagination -->
          <div v-if="total > pageSize" class="border-t px-4 py-3 flex items-center justify-between">
            <span class="text-sm text-gray-500">{{ total }} lịch hẹn</span>
            <div class="flex gap-2">
              <button @click="page--; loadAppointments()" :disabled="page <= 1"
                class="px-3 py-1 rounded-lg border text-sm disabled:opacity-40 hover:bg-gray-100">←</button>
              <span class="px-3 py-1 text-sm">{{ page }} / {{ totalPages }}</span>
              <button @click="page++; loadAppointments()" :disabled="page >= totalPages"
                class="px-3 py-1 rounded-lg border text-sm disabled:opacity-40 hover:bg-gray-100">→</button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Accept Modal -->
    <Teleport to="body">
      <div v-if="acceptModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
        <div class="bg-white rounded-2xl w-full max-w-md shadow-2xl">
          <div class="p-6">
            <h3 class="text-lg font-bold text-gray-900 mb-2">Xác nhận chấp nhận lịch hẹn</h3>
            <p class="text-gray-600 mb-4">
              Bạn có muốn chấp nhận lịch hẹn của <strong>{{ acceptModal.userName }}</strong> vào
              <strong>{{ formatDate(acceptModal.scheduledAt) }}</strong>?
            </p>
            <div class="flex gap-3 justify-end">
              <button @click="acceptModal = null"
                class="px-4 py-2 rounded-lg border text-gray-700 hover:bg-gray-50">Hủy</button>
              <button @click="doAccept" :disabled="actionLoading"
                class="px-4 py-2 rounded-lg bg-green-600 text-white hover:bg-green-700 font-medium disabled:opacity-60">
                {{ actionLoading ? 'Đang xử lý...' : 'Xác nhận' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Reschedule Modal -->
    <Teleport to="body">
      <div v-if="rescheduleModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
        <div class="bg-white rounded-2xl w-full max-w-md shadow-2xl">
          <div class="p-6 space-y-4">
            <h3 class="text-lg font-bold text-gray-900">Dời lịch hẹn</h3>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Thời gian mới <span class="text-red-500">*</span></label>
              <input type="datetime-local" v-model="rescheduleForm.newDate"
                :min="minDate"
                class="w-full border rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-orange-500" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Lý do dời lịch <span class="text-red-500">*</span></label>
              <textarea v-model="rescheduleForm.reason" rows="3"
                placeholder="VD: Bác sĩ có lịch phẫu thuật đột xuất..."
                class="w-full border rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-orange-500 resize-none"></textarea>
            </div>
            <div v-if="actionError" class="text-sm text-red-600 bg-red-50 p-3 rounded-lg">{{ actionError }}</div>
            <div class="flex gap-3 justify-end">
              <button @click="rescheduleModal = null; actionError = ''"
                class="px-4 py-2 rounded-lg border text-gray-700 hover:bg-gray-50">Hủy</button>
              <button @click="doReschedule" :disabled="actionLoading"
                class="px-4 py-2 rounded-lg bg-orange-500 text-white hover:bg-orange-600 font-medium disabled:opacity-60">
                {{ actionLoading ? 'Đang xử lý...' : 'Dời lịch' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Reject Modal -->
    <Teleport to="body">
      <div v-if="rejectModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
        <div class="bg-white rounded-2xl w-full max-w-md shadow-2xl">
          <div class="p-6 space-y-4">
            <h3 class="text-lg font-bold text-gray-900">Từ chối lịch hẹn</h3>
            <p class="text-sm text-gray-600">
              Từ chối lịch hẹn của <strong>{{ rejectModal.userName }}</strong>.
              Lý do sẽ được gửi đến bệnh nhân.
            </p>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Lý do từ chối <span class="text-red-500">*</span></label>
              <textarea v-model="rejectReason" rows="3"
                placeholder="VD: Bác sĩ không có lịch trống trong khung giờ này..."
                class="w-full border rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-red-500 resize-none"></textarea>
            </div>
            <div v-if="actionError" class="text-sm text-red-600 bg-red-50 p-3 rounded-lg">{{ actionError }}</div>
            <div class="flex gap-3 justify-end">
              <button @click="rejectModal = null; actionError = ''"
                class="px-4 py-2 rounded-lg border text-gray-700 hover:bg-gray-50">Hủy</button>
              <button @click="doReject" :disabled="actionLoading"
                class="px-4 py-2 rounded-lg bg-red-600 text-white hover:bg-red-700 font-medium disabled:opacity-60">
                {{ actionLoading ? 'Đang xử lý...' : 'Xác nhận từ chối' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, defineComponent, h } from 'vue'
import { useRouter } from 'vue-router'
import { doctorApi, directChatApi } from '@/services/api'
import type { AppointmentDto } from '@/types/api'

const router = useRouter()

// ── Inline StatusBadge ────────────────────────────────────────────────────────
const StatusBadge = defineComponent({
  props: { status: String },
  setup(props) {
    const map: Record<string, { label: string; cls: string }> = {
      Pending:     { label: 'Chờ xử lý',  cls: 'bg-yellow-100 text-yellow-700' },
      Confirmed:   { label: 'Đã xác nhận', cls: 'bg-green-100 text-green-700' },
      Rescheduled: { label: 'Dời lịch',   cls: 'bg-orange-100 text-orange-700' },
      Cancelled:   { label: 'Từ chối',    cls: 'bg-red-100 text-red-700' },
      Completed:   { label: 'Hoàn thành', cls: 'bg-blue-100 text-blue-700' },
    }
    return () => {
      const s = props.status ?? ''
      const info = map[s] ?? { label: s, cls: 'bg-gray-100 text-gray-600' }
      return h('span', { class: `inline-block px-2 py-1 rounded-full text-xs font-medium ${info.cls}` }, info.label)
    }
  }
})

// ── State ─────────────────────────────────────────────────────────────────────
const appointments = ref<AppointmentDto[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = 20
const loading = ref(false)
const activeStatus = ref('')

const acceptModal = ref<AppointmentDto | null>(null)
const rescheduleModal = ref<AppointmentDto | null>(null)
const rejectModal = ref<AppointmentDto | null>(null)
const rejectReason = ref('')
const rescheduleForm = ref({ newDate: '', reason: '' })
const actionLoading = ref(false)
const actionError = ref('')

const totalPages = computed(() => Math.ceil(total.value / pageSize))

const statusTabs = computed(() => [
  { value: '',            label: 'Tất cả' },
  { value: 'Pending',     label: 'Chờ xử lý', badge: pendingCount.value || undefined },
  { value: 'Confirmed',   label: 'Đã xác nhận' },
  { value: 'Rescheduled', label: 'Dời lịch' },
  { value: 'Completed',   label: 'Hoàn thành' },
  { value: 'Cancelled',   label: 'Đã từ chối' },
])

const pendingCount = ref(0)
const stats = computed(() => [
  { label: 'Chờ xử lý',  count: pendingCount.value,   icon: '⏳', bg: 'bg-yellow-100' },
  { label: 'Hôm nay',    count: todayCount.value,      icon: '📅', bg: 'bg-blue-100' },
  { label: 'Đã xác nhận', count: confirmedCount.value, icon: '✅', bg: 'bg-green-100' },
  { label: 'Tổng cộng',  count: total.value,            icon: '📋', bg: 'bg-purple-100' },
])

const todayCount = ref(0)
const confirmedCount = ref(0)
const minDate = computed(() => new Date().toISOString().slice(0, 16))

// ── Data Loading ──────────────────────────────────────────────────────────────
async function loadAppointments() {
  loading.value = true
  try {
    const res = await doctorApi.listAppointments({
      status: activeStatus.value || undefined,
      page: page.value,
      pageSize
    })
    appointments.value = res.data.data
    total.value = res.data.total
  } catch (e) {
    console.error(e)
  } finally {
    loading.value = false
  }
}

async function loadStats() {
  try {
    const [pendRes, confRes, todRes] = await Promise.all([
      doctorApi.listAppointments({ status: 'Pending', pageSize: 1 }),
      doctorApi.listAppointments({ status: 'Confirmed', pageSize: 1 }),
      doctorApi.listAppointments({ pageSize: 200 }),
    ])
    pendingCount.value = pendRes.data.total
    confirmedCount.value = confRes.data.total
    const today = new Date().toDateString()
    todayCount.value = todRes.data.data.filter(
      a => new Date(a.scheduledAt).toDateString() === today
    ).length
  } catch { /* ignore */ }
}

// ── Actions ───────────────────────────────────────────────────────────────────
function openAccept(apt: AppointmentDto) { acceptModal.value = apt }
function openReschedule(apt: AppointmentDto) {
  rescheduleModal.value = apt
  rescheduleForm.value = { newDate: '', reason: '' }
  actionError.value = ''
}
function openReject(apt: AppointmentDto) {
  rejectModal.value = apt
  rejectReason.value = ''
  actionError.value = ''
}

async function doAccept() {
  if (!acceptModal.value) return
  actionLoading.value = true
  try {
    await doctorApi.takeAction(acceptModal.value.id, 'accept')
    acceptModal.value = null
    await loadAppointments()
    await loadStats()
  } catch (e: any) {
    console.error(e)
  } finally {
    actionLoading.value = false
  }
}

async function doReschedule() {
  if (!rescheduleModal.value) return
  if (!rescheduleForm.value.newDate) { actionError.value = 'Vui lòng chọn thời gian mới'; return }
  if (!rescheduleForm.value.reason) { actionError.value = 'Vui lòng nhập lý do'; return }

  actionLoading.value = true
  actionError.value = ''
  try {
    await doctorApi.takeAction(
      rescheduleModal.value.id,
      'reschedule',
      rescheduleForm.value.reason,
      new Date(rescheduleForm.value.newDate).toISOString()
    )
    rescheduleModal.value = null
    await loadAppointments()
    await loadStats()
  } catch (e: any) {
    actionError.value = e?.response?.data?.message || 'Lỗi không xác định'
  } finally {
    actionLoading.value = false
  }
}

async function doReject() {
  if (!rejectModal.value) return
  if (!rejectReason.value.trim()) { actionError.value = 'Vui lòng nhập lý do từ chối'; return }

  actionLoading.value = true
  actionError.value = ''
  try {
    await doctorApi.takeAction(rejectModal.value.id, 'reject', rejectReason.value)
    rejectModal.value = null
    await loadAppointments()
    await loadStats()
  } catch (e: any) {
    actionError.value = e?.response?.data?.message || 'Lỗi không xác định'
  } finally {
    actionLoading.value = false
  }
}

async function openChat(apt: AppointmentDto) {
  if (!apt.doctorId) return
  try {
    const res = await directChatApi.getOrCreate(apt.doctorId)
    router.push({ name: 'direct-chat-session', params: { sessionId: res.data.id } })
  } catch (e) {
    console.error(e)
  }
}

// ── Formatting ────────────────────────────────────────────────────────────────
function formatDate(dt: string) {
  return new Date(dt).toLocaleDateString('vi-VN', { weekday: 'short', day: '2-digit', month: '2-digit', year: 'numeric' })
}
function formatTime(dt: string) {
  return new Date(dt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
}

onMounted(() => {
  loadAppointments()
  loadStats()
})
</script>
