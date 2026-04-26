<template>
  <div>
    <!-- Filter tabs -->
    <div class="flex gap-1 mb-5 bg-gray-100 p-1 rounded-xl w-fit">
      <button v-for="tab in tabs" :key="tab.value"
        @click="activeTab = tab.value; load()"
        class="px-4 py-1.5 rounded-lg text-sm font-medium transition-colors"
        :class="activeTab === tab.value
          ? 'bg-white text-gray-900 shadow-sm'
          : 'text-gray-500 hover:text-gray-700'">
        {{ tab.label }}
        <span v-if="tab.value === 'Pending' && pendingCount > 0"
          class="ml-1.5 text-xs bg-yellow-100 text-yellow-700 font-semibold px-1.5 py-0.5 rounded-full">
          {{ pendingCount }}
        </span>
      </button>
    </div>

    <div v-if="loading" class="space-y-3">
      <div v-for="i in 4" :key="i" class="card animate-pulse h-20"></div>
    </div>

    <div v-else-if="applications.length" class="space-y-3">
      <div v-for="app in applications" :key="app.doctorId"
        class="card hover:shadow-md transition-shadow">
        <div class="flex items-start gap-4">
          <div class="w-10 h-10 bg-green-100 rounded-xl flex items-center justify-center shrink-0 text-green-700 font-semibold text-sm">
            {{ app.fullName.charAt(0) }}
          </div>
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2 flex-wrap">
              <p class="font-semibold text-gray-900">{{ app.fullName }}</p>
              <StatusBadge :status="app.applicationStatus" />
            </div>
            <p class="text-sm text-gray-500 mt-0.5">{{ app.email }}</p>
            <div class="flex flex-wrap gap-4 mt-2 text-sm text-gray-600">
              <span><strong>Chuyên khoa:</strong> {{ app.specialty }}</span>
              <span><strong>Số GP:</strong> {{ app.licenseNumber }}</span>
              <span><strong>Phí tư vấn:</strong> {{ formatCurrency(app.consultationFee) }}</span>
            </div>
            <p v-if="app.bio" class="text-sm text-gray-500 mt-1 line-clamp-2">{{ app.bio }}</p>
            <p v-if="app.rejectionReason" class="text-sm text-red-600 mt-1 bg-red-50 px-2 py-1 rounded-lg">
              Lý do từ chối: {{ app.rejectionReason }}
            </p>
            <p class="text-xs text-gray-400 mt-1.5">Đăng ký: {{ formatDate(app.createdAt) }}</p>
          </div>

          <!-- Actions (only for Pending) -->
          <div v-if="app.applicationStatus === 'Pending'" class="flex gap-2 shrink-0">
            <button @click="openApprove(app)"
              class="px-3 py-1.5 bg-green-600 text-white text-xs font-medium rounded-lg hover:bg-green-700 transition-colors">
              Duyệt
            </button>
            <button @click="openReject(app)"
              class="px-3 py-1.5 bg-red-50 text-red-600 text-xs font-medium rounded-lg hover:bg-red-100 transition-colors">
              Từ chối
            </button>
          </div>
        </div>
      </div>
    </div>

    <div v-else class="text-center py-20 text-gray-400">
      <svg class="w-12 h-12 mx-auto mb-3 opacity-40" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/>
      </svg>
      Không có đơn nào
    </div>

    <!-- Approve modal -->
    <div v-if="approving" class="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-2xl shadow-xl p-6 max-w-sm w-full">
        <h3 class="font-semibold text-gray-900 mb-2">Duyệt đơn bác sĩ</h3>
        <p class="text-sm text-gray-600 mb-5">
          Xác nhận phê duyệt đơn đăng ký của <strong>{{ approving.fullName }}</strong>?
          Tài khoản sẽ được nâng lên quyền Bác sĩ.
        </p>
        <div class="flex gap-3 justify-end">
          <button @click="approving = null" class="text-sm text-gray-600 px-4 py-2 rounded-xl hover:bg-gray-100">Hủy</button>
          <button @click="doApprove" :disabled="actionLoading"
            class="text-sm bg-green-600 text-white px-4 py-2 rounded-xl hover:bg-green-700 disabled:opacity-50">
            {{ actionLoading ? 'Đang xử lý...' : 'Duyệt' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Reject modal -->
    <div v-if="rejecting" class="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-2xl shadow-xl p-6 max-w-sm w-full">
        <h3 class="font-semibold text-gray-900 mb-2">Từ chối đơn bác sĩ</h3>
        <p class="text-sm text-gray-600 mb-3">
          Từ chối đơn của <strong>{{ rejecting.fullName }}</strong>. Vui lòng nhập lý do:
        </p>
        <textarea v-model="rejectionReason" class="input-field resize-none w-full" rows="3"
          placeholder="Lý do từ chối..." required></textarea>
        <div class="flex gap-3 justify-end mt-4">
          <button @click="rejecting = null; rejectionReason = ''" class="text-sm text-gray-600 px-4 py-2 rounded-xl hover:bg-gray-100">Hủy</button>
          <button @click="doReject" :disabled="actionLoading || !rejectionReason.trim()"
            class="text-sm bg-red-600 text-white px-4 py-2 rounded-xl hover:bg-red-700 disabled:opacity-50">
            {{ actionLoading ? 'Đang xử lý...' : 'Từ chối' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, defineComponent, h } from 'vue'
import { adminApi } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { DoctorApplicationDto } from '@/types/api'

const toast = useToastStore()
const applications = ref<DoctorApplicationDto[]>([])
const loading = ref(true)
const activeTab = ref('Pending')
const approving = ref<DoctorApplicationDto | null>(null)
const rejecting = ref<DoctorApplicationDto | null>(null)
const rejectionReason = ref('')
const actionLoading = ref(false)

const tabs = [
  { label: 'Chờ duyệt', value: 'Pending' },
  { label: 'Đã duyệt',  value: 'Approved' },
  { label: 'Từ chối',   value: 'Rejected' },
  { label: 'Tất cả',    value: '' },
]

const pendingCount = computed(() =>
  activeTab.value === 'Pending' ? applications.value.length : 0
)

onMounted(load)

async function load() {
  loading.value = true
  try {
    const { data } = await adminApi.getDoctorApplications(activeTab.value || undefined)
    applications.value = data
  } catch {
    toast.error('Không thể tải danh sách đơn.')
  } finally {
    loading.value = false
  }
}

function openApprove(app: DoctorApplicationDto) { approving.value = app }
function openReject(app: DoctorApplicationDto)  { rejecting.value = app }

async function doApprove() {
  if (!approving.value) return
  actionLoading.value = true
  try {
    await adminApi.reviewApplication(approving.value.doctorId, 'approve')
    toast.success(`Đã duyệt đơn của ${approving.value.fullName}.`)
    approving.value = null
    await load()
  } catch (e: any) {
    toast.error(e.response?.data?.message || 'Duyệt thất bại.')
  } finally {
    actionLoading.value = false
  }
}

async function doReject() {
  if (!rejecting.value || !rejectionReason.value.trim()) return
  actionLoading.value = true
  try {
    await adminApi.reviewApplication(rejecting.value.doctorId, 'reject', rejectionReason.value)
    toast.success(`Đã từ chối đơn của ${rejecting.value.fullName}.`)
    rejecting.value = null
    rejectionReason.value = ''
    await load()
  } catch (e: any) {
    toast.error(e.response?.data?.message || 'Từ chối thất bại.')
  } finally {
    actionLoading.value = false
  }
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value)
}
function formatDate(d: string) {
  return new Date(d).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

// Inline status badge component
const StatusBadge = defineComponent({
  props: { status: String },
  setup(props) {
    return () => {
      const map: Record<string, { label: string; cls: string }> = {
        Pending:  { label: 'Chờ duyệt', cls: 'bg-yellow-100 text-yellow-700' },
        Approved: { label: 'Đã duyệt',  cls: 'bg-green-100 text-green-700' },
        Rejected: { label: 'Từ chối',   cls: 'bg-red-100 text-red-600' },
      }
      const info = map[props.status ?? ''] ?? { label: props.status, cls: 'bg-gray-100 text-gray-600' }
      return h('span', { class: `text-xs font-medium px-2 py-0.5 rounded-full ${info.cls}` }, info.label)
    }
  }
})
</script>
