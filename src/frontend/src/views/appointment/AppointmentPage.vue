<template>
  <div class="max-w-4xl mx-auto px-4 py-10">
    <div class="mb-8">
      <h1 class="text-3xl font-bold text-gray-900 mb-2">Đăng ký khám bệnh</h1>
      <p class="text-gray-500">Chọn thời gian và bác sĩ phù hợp. Số thứ tự sẽ được cấp tự động.</p>
    </div>

    <div class="grid md:grid-cols-3 gap-8">
      <!-- Form -->
      <div class="md:col-span-2">
        <div class="card">
          <form @submit.prevent="handleSubmit" class="space-y-6">
            <!-- Department -->
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">Chuyên khoa</label>
              <select v-model="form.department" class="input-field">
                <option value="">-- Chọn chuyên khoa --</option>
                <option v-for="d in departments" :key="d" :value="d">{{ d }}</option>
              </select>
            </div>

            <!-- Doctor -->
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">Bác sĩ (không bắt buộc)</label>
              <select v-model="form.doctorId" class="input-field">
                <option :value="undefined">-- Bất kỳ bác sĩ nào --</option>
                <option v-for="d in doctors" :key="d.id" :value="d.id">
                  BS. {{ d.name }} — {{ d.specialty }} ({{ formatFee(d.consultationFee) }})
                </option>
              </select>
            </div>

            <!-- Date + Time -->
            <div class="grid sm:grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-2">Ngày khám</label>
                <input v-model="form.date" type="date" class="input-field" :min="minDate" required />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-2">Giờ khám</label>
                <select v-model="form.time" class="input-field" required>
                  <option value="">-- Chọn giờ --</option>
                  <option v-for="t in timeSlots" :key="t" :value="t">{{ t }}</option>
                </select>
              </div>
            </div>

            <!-- Description -->
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">Mô tả triệu chứng</label>
              <textarea v-model="form.description" rows="4" class="input-field resize-none"
                placeholder="Mô tả ngắn gọn tình trạng sức khỏe hoặc lý do khám..."></textarea>
            </div>

            <p v-if="error" class="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-lg">{{ error }}</p>

            <button type="submit" :disabled="loading" class="btn-primary w-full flex items-center justify-center gap-2">
              <svg v-if="loading" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
              </svg>
              {{ loading ? 'Đang đặt lịch...' : 'Xác nhận đặt lịch' }}
            </button>
          </form>
        </div>
      </div>

      <!-- Side info -->
      <div class="space-y-4">
        <div class="card bg-blue-50 border-blue-100">
          <h3 class="font-semibold text-blue-900 mb-3">Hướng dẫn</h3>
          <ul class="space-y-2 text-sm text-blue-700">
            <li class="flex items-start gap-2">
              <span class="text-blue-500 mt-0.5">1.</span> Chọn chuyên khoa hoặc bác sĩ
            </li>
            <li class="flex items-start gap-2">
              <span class="text-blue-500 mt-0.5">2.</span> Chọn ngày và giờ khám
            </li>
            <li class="flex items-start gap-2">
              <span class="text-blue-500 mt-0.5">3.</span> Mô tả triệu chứng (không bắt buộc)
            </li>
            <li class="flex items-start gap-2">
              <span class="text-blue-500 mt-0.5">4.</span> Số thứ tự sẽ được cấp tự động
            </li>
          </ul>
        </div>

        <div class="card">
          <h3 class="font-semibold text-gray-900 mb-3">Liên hệ hỗ trợ</h3>
          <p class="text-sm text-gray-500 mb-2">Cần hỗ trợ đặt lịch?</p>
          <p class="text-blue-700 font-semibold">1800-xxxx</p>
          <p class="text-xs text-gray-400 mt-1">Miễn phí 7:00 – 21:00</p>
        </div>
      </div>
    </div>

    <!-- Success modal -->
    <div v-if="success" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-2xl p-8 max-w-sm w-full text-center shadow-2xl">
        <div class="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
          <svg class="w-8 h-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
          </svg>
        </div>
        <h2 class="text-xl font-bold text-gray-900 mb-2">Đặt lịch thành công!</h2>
        <p class="text-gray-500 text-sm mb-2">Số thứ tự của bạn:</p>
        <div class="text-4xl font-bold text-blue-600 mb-4">#{{ createdQueue }}</div>
        <p class="text-gray-500 text-sm mb-6">Vui lòng đến đúng giờ và mang theo CMND/CCCD</p>
        <div class="flex gap-3">
          <RouterLink to="/appointments" class="btn-secondary flex-1 text-sm text-center">Xem lịch hẹn</RouterLink>
          <button @click="success = false" class="btn-primary flex-1 text-sm">Đóng</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { appointmentApi } from '@/services/api'

const form = ref({ department: '', doctorId: undefined as number | undefined, date: '', time: '', description: '' })
const loading = ref(false)
const error = ref('')
const success = ref(false)
const createdQueue = ref<number | null>(null)
const doctors = ref<any[]>([])

const departments = ['Nội khoa', 'Ngoại khoa', 'Nhi khoa', 'Sản phụ khoa', 'Da liễu', 'Tai mũi họng', 'Mắt', 'Thần kinh', 'Tim mạch', 'Cơ xương khớp']
const timeSlots = ['07:30', '08:00', '08:30', '09:00', '09:30', '10:00', '10:30', '13:00', '13:30', '14:00', '14:30', '15:00', '15:30', '16:00']

const minDate = new Date().toISOString().split('T')[0]

function formatFee(fee: number) {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(fee)
}

async function handleSubmit() {
  if (!form.value.date || !form.value.time) { error.value = 'Vui lòng chọn ngày và giờ khám'; return }
  loading.value = true
  error.value = ''
  try {
    const scheduledAt = new Date(`${form.value.date}T${form.value.time}:00`).toISOString()
    const { data } = await appointmentApi.create({
      scheduledAt,
      doctorId: form.value.doctorId,
      department: form.value.department || undefined,
      description: form.value.description || undefined
    })
    createdQueue.value = data.queueNumber
    success.value = true
    form.value = { department: '', doctorId: undefined, date: '', time: '', description: '' }
  } catch (e: any) {
    error.value = e.response?.data?.message || 'Đặt lịch thất bại. Vui lòng thử lại.'
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  try {
    const { data } = await appointmentApi.getDoctors()
    doctors.value = data
  } catch { /* ignore */ }
})
</script>
