<template>
  <div class="min-h-screen bg-gradient-to-br from-blue-50 via-white to-indigo-50">
    <!-- Page header -->
    <div class="bg-white shadow-sm border-b">
      <div class="max-w-4xl mx-auto px-4 py-6">
        <h1 class="text-2xl font-bold text-gray-900">Đặt lịch khám bệnh</h1>
        <p class="text-gray-500 mt-1">Điền thông tin để đăng ký lịch khám với bác sĩ</p>
      </div>
    </div>

    <div class="max-w-4xl mx-auto px-4 py-8">

      <!-- Progress steps -->
      <div class="flex items-center mb-8">
        <div v-for="(step, idx) in steps" :key="idx" class="flex items-center flex-1">
          <div class="flex flex-col items-center">
            <div :class="[
              'w-10 h-10 rounded-full flex items-center justify-center text-sm font-bold transition-all duration-300',
              currentStep > idx + 1 ? 'bg-green-500 text-white' :
              currentStep === idx + 1 ? 'bg-blue-600 text-white shadow-lg shadow-blue-200' :
              'bg-gray-200 text-gray-400'
            ]">
              <span v-if="currentStep > idx + 1">✓</span>
              <span v-else>{{ idx + 1 }}</span>
            </div>
            <span :class="['text-xs mt-1 font-medium', currentStep === idx + 1 ? 'text-blue-600' : 'text-gray-400']">
              {{ step }}
            </span>
          </div>
          <div v-if="idx < steps.length - 1"
            :class="['flex-1 h-0.5 mx-2 transition-all duration-300', currentStep > idx + 1 ? 'bg-green-400' : 'bg-gray-200']">
          </div>
        </div>
      </div>

      <div class="grid md:grid-cols-3 gap-6">
        <!-- Main form card -->
        <div class="md:col-span-2">
          <div class="bg-white rounded-2xl shadow-sm border p-6">

            <!-- Step 1: Choose doctor & department -->
            <div v-if="currentStep === 1" class="space-y-5">
              <h2 class="text-lg font-semibold text-gray-900">Chọn chuyên khoa & bác sĩ</h2>

              <!-- Specialty filter -->
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-2">Chuyên khoa</label>
                <div class="flex flex-wrap gap-2">
                  <button v-for="dept in departments" :key="dept"
                    @click="form.department = dept; filterDoctors()"
                    :class="[
                      'px-3 py-1.5 rounded-full text-sm transition-colors border',
                      form.department === dept
                        ? 'bg-blue-600 text-white border-blue-600'
                        : 'bg-white text-gray-600 border-gray-300 hover:border-blue-400'
                    ]">
                    {{ dept }}
                  </button>
                  <button @click="form.department = ''; filterDoctors()"
                    :class="[
                      'px-3 py-1.5 rounded-full text-sm transition-colors border',
                      !form.department
                        ? 'bg-blue-600 text-white border-blue-600'
                        : 'bg-white text-gray-600 border-gray-300 hover:border-blue-400'
                    ]">
                    Tất cả
                  </button>
                </div>
              </div>

              <!-- Doctor list -->
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-2">Chọn bác sĩ</label>
                <div v-if="loadingDoctors" class="text-center py-8 text-gray-400">
                  <div class="animate-spin w-6 h-6 border-2 border-blue-500 border-t-transparent rounded-full mx-auto mb-2"></div>
                  Đang tải danh sách bác sĩ...
                </div>
                <div v-else class="space-y-2 max-h-80 overflow-y-auto pr-1">
                  <!-- No specific doctor option -->
                  <label :class="[
                    'flex items-center gap-3 p-3 rounded-xl border-2 cursor-pointer transition-all',
                    !form.doctorId ? 'border-blue-500 bg-blue-50' : 'border-gray-200 hover:border-gray-300'
                  ]">
                    <input type="radio" :value="undefined" v-model="form.doctorId" class="hidden" />
                    <div class="w-10 h-10 bg-gray-100 rounded-full flex items-center justify-center text-xl">🏥</div>
                    <div class="flex-1">
                      <div class="font-medium text-gray-900">Bác sĩ bất kỳ</div>
                      <div class="text-sm text-gray-500">Hệ thống sẽ phân công bác sĩ phù hợp</div>
                    </div>
                    <div v-if="!form.doctorId" class="w-5 h-5 bg-blue-600 rounded-full flex items-center justify-center">
                      <div class="w-2 h-2 bg-white rounded-full"></div>
                    </div>
                  </label>

                  <label v-for="doctor in filteredDoctors" :key="doctor.id" :class="[
                    'flex items-center gap-3 p-3 rounded-xl border-2 cursor-pointer transition-all',
                    form.doctorId === doctor.id ? 'border-blue-500 bg-blue-50' : 'border-gray-200 hover:border-gray-300'
                  ]">
                    <input type="radio" :value="doctor.id" v-model="form.doctorId" class="hidden" />
                    <div class="w-10 h-10 bg-gradient-to-br from-blue-400 to-indigo-500 rounded-full flex items-center justify-center text-white font-bold text-sm">
                      {{ doctorInitials(doctor.name) }}
                    </div>
                    <div class="flex-1 min-w-0">
                      <div class="font-medium text-gray-900">BS. {{ doctor.name }}</div>
                      <div class="text-sm text-blue-600">{{ doctor.specialty }}</div>
                      <div class="text-xs text-gray-500 flex items-center gap-2 mt-0.5">
                        <span>⭐ {{ doctor.rating.toFixed(1) }}</span>
                        <span>•</span>
                        <span>{{ formatFee(doctor.consultationFee) }}</span>
                      </div>
                    </div>
                    <div v-if="form.doctorId === doctor.id" class="w-5 h-5 bg-blue-600 rounded-full flex items-center justify-center">
                      <div class="w-2 h-2 bg-white rounded-full"></div>
                    </div>
                  </label>
                </div>
              </div>

              <div class="flex justify-end">
                <button @click="currentStep = 2"
                  class="px-6 py-2.5 bg-blue-600 text-white rounded-xl font-medium hover:bg-blue-700 transition-colors">
                  Tiếp theo →
                </button>
              </div>
            </div>

            <!-- Step 2: Date & time -->
            <div v-if="currentStep === 2" class="space-y-5">
              <h2 class="text-lg font-semibold text-gray-900">Chọn ngày & giờ khám</h2>

              <div class="grid sm:grid-cols-2 gap-4">
                <div>
                  <label class="block text-sm font-medium text-gray-700 mb-2">Ngày khám <span class="text-red-500">*</span></label>
                  <input v-model="form.date" type="date" :min="minDate"
                    @change="loadSlots"
                    class="w-full border-2 rounded-xl px-3 py-2.5 focus:outline-none focus:border-blue-500 text-gray-900" />
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-700 mb-2">Giờ khám <span class="text-red-500">*</span></label>
                  <div v-if="loadingSlots" class="text-sm text-gray-400 py-2">Đang tải khung giờ...</div>
                  <div v-else-if="availableSlots.length > 0" class="grid grid-cols-3 gap-1.5">
                    <button v-for="slot in availableSlots" :key="slot"
                      @click="form.time = slot"
                      :class="[
                        'py-1.5 rounded-lg text-sm font-medium transition-colors border',
                        form.time === slot
                          ? 'bg-blue-600 text-white border-blue-600'
                          : 'border-gray-200 text-gray-700 hover:border-blue-400 hover:text-blue-600'
                      ]">
                      {{ slot }}
                    </button>
                  </div>
                  <div v-else-if="form.date" class="space-y-1">
                    <p class="text-sm text-amber-600">Không có lịch trống từ bác sĩ, chọn giờ thủ công:</p>
                    <select v-model="form.time" class="w-full border-2 rounded-xl px-3 py-2.5 focus:outline-none focus:border-blue-500">
                      <option value="">-- Chọn giờ --</option>
                      <option v-for="t in defaultTimeSlots" :key="t" :value="t">{{ t }}</option>
                    </select>
                  </div>
                  <div v-else class="text-sm text-gray-400 py-2">Chọn ngày để xem giờ trống</div>
                </div>
              </div>

              <!-- Description -->
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-2">Mô tả triệu chứng</label>
                <textarea v-model="form.description" rows="4"
                  placeholder="VD: Đau đầu kéo dài 3 ngày, có kèm sốt nhẹ và chóng mặt..."
                  class="w-full border-2 rounded-xl px-3 py-2.5 focus:outline-none focus:border-blue-500 resize-none text-gray-900"></textarea>
              </div>

              <div class="flex justify-between">
                <button @click="currentStep = 1"
                  class="px-6 py-2.5 border-2 border-gray-300 text-gray-700 rounded-xl font-medium hover:bg-gray-50 transition-colors">
                  ← Quay lại
                </button>
                <button @click="goToStep3"
                  class="px-6 py-2.5 bg-blue-600 text-white rounded-xl font-medium hover:bg-blue-700 transition-colors">
                  Tiếp theo →
                </button>
              </div>
            </div>

            <!-- Step 3: Confirm -->
            <div v-if="currentStep === 3" class="space-y-5">
              <h2 class="text-lg font-semibold text-gray-900">Xác nhận thông tin</h2>

              <div class="bg-blue-50 border border-blue-200 rounded-xl p-5 space-y-3">
                <div class="flex items-center gap-3 pb-3 border-b border-blue-200">
                  <div class="w-12 h-12 bg-gradient-to-br from-blue-400 to-indigo-500 rounded-xl flex items-center justify-center text-white font-bold">
                    {{ selectedDoctor ? doctorInitials(selectedDoctor.name) : '🏥' }}
                  </div>
                  <div>
                    <div class="font-semibold text-gray-900">
                      {{ selectedDoctor ? 'BS. ' + selectedDoctor.name : 'Bác sĩ bất kỳ' }}
                    </div>
                    <div class="text-sm text-blue-600">
                      {{ selectedDoctor?.specialty || form.department || 'Chưa chọn chuyên khoa' }}
                    </div>
                  </div>
                </div>

                <div class="grid grid-cols-2 gap-3 text-sm">
                  <div>
                    <span class="text-gray-500">Ngày khám</span>
                    <div class="font-semibold text-gray-900 mt-0.5">{{ formatDisplayDate(form.date) }}</div>
                  </div>
                  <div>
                    <span class="text-gray-500">Giờ khám</span>
                    <div class="font-semibold text-gray-900 mt-0.5">{{ form.time }}</div>
                  </div>
                  <div v-if="form.department">
                    <span class="text-gray-500">Chuyên khoa</span>
                    <div class="font-semibold text-gray-900 mt-0.5">{{ form.department }}</div>
                  </div>
                  <div v-if="selectedDoctor">
                    <span class="text-gray-500">Phí khám</span>
                    <div class="font-semibold text-green-700 mt-0.5">{{ formatFee(selectedDoctor.consultationFee) }}</div>
                  </div>
                </div>

                <div v-if="form.description" class="pt-2 border-t border-blue-200">
                  <span class="text-gray-500 text-sm">Triệu chứng</span>
                  <div class="text-gray-900 text-sm mt-0.5">{{ form.description }}</div>
                </div>
              </div>

              <p v-if="error" class="text-sm text-red-600 bg-red-50 border border-red-200 px-4 py-3 rounded-xl">
                {{ error }}
              </p>

              <div class="flex justify-between">
                <button @click="currentStep = 2"
                  class="px-6 py-2.5 border-2 border-gray-300 text-gray-700 rounded-xl font-medium hover:bg-gray-50 transition-colors">
                  ← Quay lại
                </button>
                <button @click="handleSubmit" :disabled="loading"
                  class="px-8 py-2.5 bg-blue-600 text-white rounded-xl font-medium hover:bg-blue-700 disabled:opacity-60 transition-colors flex items-center gap-2">
                  <svg v-if="loading" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                  </svg>
                  {{ loading ? 'Đang đặt lịch...' : '✓ Xác nhận đặt lịch' }}
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Sidebar -->
        <div class="space-y-4">
          <div class="bg-white rounded-2xl border p-5">
            <h3 class="font-semibold text-gray-900 mb-3 flex items-center gap-2">
              <span class="text-blue-500">ℹ</span> Hướng dẫn
            </h3>
            <ol class="space-y-3 text-sm text-gray-600">
              <li class="flex gap-2.5">
                <span class="w-5 h-5 rounded-full bg-blue-100 text-blue-700 text-xs flex items-center justify-center font-bold flex-shrink-0 mt-0.5">1</span>
                <span>Chọn chuyên khoa và bác sĩ phù hợp với tình trạng của bạn</span>
              </li>
              <li class="flex gap-2.5">
                <span class="w-5 h-5 rounded-full bg-blue-100 text-blue-700 text-xs flex items-center justify-center font-bold flex-shrink-0 mt-0.5">2</span>
                <span>Chọn ngày và giờ khám từ lịch trống của bác sĩ</span>
              </li>
              <li class="flex gap-2.5">
                <span class="w-5 h-5 rounded-full bg-blue-100 text-blue-700 text-xs flex items-center justify-center font-bold flex-shrink-0 mt-0.5">3</span>
                <span>Mô tả triệu chứng để bác sĩ chuẩn bị tốt hơn</span>
              </li>
              <li class="flex gap-2.5">
                <span class="w-5 h-5 rounded-full bg-blue-100 text-blue-700 text-xs flex items-center justify-center font-bold flex-shrink-0 mt-0.5">4</span>
                <span>Đến đúng giờ, mang theo CMND/CCCD và bảo hiểm y tế</span>
              </li>
            </ol>
          </div>

          <div class="bg-amber-50 border border-amber-200 rounded-2xl p-5">
            <h3 class="font-semibold text-amber-900 mb-2">⚠ Lưu ý</h3>
            <ul class="text-sm text-amber-800 space-y-1">
              <li>• Đặt lịch trước ít nhất 2 giờ</li>
              <li>• Có thể hủy trước 1 giờ trong mục "Lịch hẹn của tôi"</li>
              <li>• Bác sĩ có thể điều chỉnh giờ nếu cần thiết</li>
            </ul>
          </div>

          <div class="bg-white border rounded-2xl p-5">
            <h3 class="font-semibold text-gray-900 mb-2">📞 Hỗ trợ</h3>
            <p class="text-2xl font-bold text-blue-600">1800-xxxx</p>
            <p class="text-sm text-gray-500 mt-1">Miễn phí · 7:00 – 21:00 hàng ngày</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Success modal -->
    <Teleport to="body">
      <div v-if="success" class="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
        <div class="bg-white rounded-2xl p-8 max-w-sm w-full text-center shadow-2xl">
          <div class="w-20 h-20 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-5">
            <svg class="w-10 h-10 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/>
            </svg>
          </div>
          <h2 class="text-2xl font-bold text-gray-900 mb-2">Đặt lịch thành công!</h2>
          <p class="text-gray-500 text-sm mb-2">Số thứ tự của bạn:</p>
          <div class="text-5xl font-bold text-blue-600 mb-2">#{{ createdQueue }}</div>
          <p class="text-sm text-gray-500 mb-1">
            {{ formatDisplayDate(form.date) }} lúc {{ form.time }}
          </p>
          <p class="text-gray-400 text-xs mb-6">Bác sĩ sẽ xem xét và xác nhận lịch hẹn của bạn</p>
          <div class="flex gap-3">
            <RouterLink to="/appointments"
              class="flex-1 py-2.5 border-2 border-gray-300 text-gray-700 rounded-xl font-medium hover:bg-gray-50 text-sm text-center transition-colors">
              Xem lịch hẹn
            </RouterLink>
            <RouterLink to="/"
              class="flex-1 py-2.5 bg-blue-600 text-white rounded-xl font-medium hover:bg-blue-700 text-sm text-center transition-colors">
              Về trang chủ
            </RouterLink>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { appointmentApi } from '@/services/api'
import type { DoctorDto } from '@/types/api'

const steps = ['Bác sĩ', 'Lịch hẹn', 'Xác nhận']
const currentStep = ref(1)

const form = ref({
  department: '',
  doctorId: undefined as number | undefined,
  date: '',
  time: '',
  description: ''
})

const loading = ref(false)
const loadingDoctors = ref(false)
const loadingSlots = ref(false)
const error = ref('')
const success = ref(false)
const createdQueue = ref<number | null>(null)

const allDoctors = ref<DoctorDto[]>([])
const filteredDoctors = ref<DoctorDto[]>([])
const availableSlots = ref<string[]>([])

const departments = ['Nội khoa', 'Ngoại khoa', 'Nhi khoa', 'Sản phụ khoa', 'Da liễu', 'Tai mũi họng', 'Mắt', 'Thần kinh', 'Tim mạch', 'Cơ xương khớp']
const defaultTimeSlots = ['07:30', '08:00', '08:30', '09:00', '09:30', '10:00', '10:30', '13:00', '13:30', '14:00', '14:30', '15:00', '15:30', '16:00']
const minDate = new Date().toISOString().split('T')[0]

const selectedDoctor = computed(() =>
  form.value.doctorId ? allDoctors.value.find(d => d.id === form.value.doctorId) : undefined
)

function doctorInitials(name: string) {
  return name.split(' ').slice(-2).map(n => n[0]).join('').toUpperCase()
}

function formatFee(fee: number) {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(fee)
}

function formatDisplayDate(d: string) {
  if (!d) return ''
  return new Date(d + 'T00:00:00').toLocaleDateString('vi-VN', {
    weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric'
  })
}

function filterDoctors() {
  form.value.doctorId = undefined
  filteredDoctors.value = form.value.department
    ? allDoctors.value.filter(d => d.specialty.toLowerCase().includes(form.value.department.toLowerCase()))
    : allDoctors.value
}

async function loadSlots() {
  if (!form.value.date) return
  form.value.time = ''
  availableSlots.value = []

  if (form.value.doctorId) {
    loadingSlots.value = true
    try {
      const { data } = await appointmentApi.getDoctorSlots(form.value.doctorId, form.value.date)
      availableSlots.value = data.slots || []
    } catch { /* fallback to manual select */ }
    finally { loadingSlots.value = false }
  }
}

function goToStep3() {
  if (!form.value.date) { error.value = 'Vui lòng chọn ngày khám'; return }
  if (!form.value.time) { error.value = 'Vui lòng chọn giờ khám'; return }
  error.value = ''
  currentStep.value = 3
}

async function handleSubmit() {
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
  } catch (e: any) {
    error.value = e.response?.data?.message || 'Đặt lịch thất bại. Vui lòng thử lại.'
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  loadingDoctors.value = true
  try {
    const { data } = await appointmentApi.getDoctors()
    allDoctors.value = data
    filteredDoctors.value = data
  } catch { /* ignore */ }
  finally { loadingDoctors.value = false }
})
</script>
