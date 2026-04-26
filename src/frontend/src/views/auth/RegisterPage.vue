<template>
  <div class="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-white py-12 px-4">
    <div class="w-full max-w-md">
      <div class="text-center mb-8">
        <RouterLink to="/" class="inline-flex items-center gap-2 mb-6">
          <div class="w-10 h-10 bg-blue-600 rounded-xl flex items-center justify-center">
            <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"/>
            </svg>
          </div>
          <span class="text-2xl font-bold text-blue-700">MGSPlus</span>
        </RouterLink>
        <h1 class="text-2xl font-bold text-gray-900">Tạo tài khoản</h1>
        <p class="text-gray-500 mt-1 text-sm">Đăng ký miễn phí và bắt đầu ngay hôm nay</p>
      </div>

      <!-- Step 1: Role selection -->
      <div v-if="step === 1" class="card shadow-lg">
        <p class="text-sm font-medium text-gray-700 mb-4 text-center">Bạn muốn đăng ký với tư cách?</p>
        <div class="grid grid-cols-2 gap-4">
          <button @click="selectRole('Patient')"
            class="flex flex-col items-center gap-3 p-5 rounded-xl border-2 transition-all hover:border-blue-500 hover:bg-blue-50"
            :class="form.requestedRole === 'Patient' ? 'border-blue-500 bg-blue-50' : 'border-gray-200'">
            <div class="w-12 h-12 rounded-full bg-blue-100 flex items-center justify-center">
              <svg class="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/>
              </svg>
            </div>
            <div class="text-center">
              <p class="font-semibold text-gray-900 text-sm">Bệnh nhân</p>
              <p class="text-xs text-gray-500 mt-0.5">Đặt lịch khám, tư vấn AI</p>
            </div>
          </button>

          <button @click="selectRole('Doctor')"
            class="flex flex-col items-center gap-3 p-5 rounded-xl border-2 transition-all hover:border-green-500 hover:bg-green-50"
            :class="form.requestedRole === 'Doctor' ? 'border-green-500 bg-green-50' : 'border-gray-200'">
            <div class="w-12 h-12 rounded-full bg-green-100 flex items-center justify-center">
              <svg class="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/>
              </svg>
            </div>
            <div class="text-center">
              <p class="font-semibold text-gray-900 text-sm">Bác sĩ</p>
              <p class="text-xs text-gray-500 mt-0.5">Cần duyệt bởi Admin</p>
            </div>
          </button>
        </div>

        <button @click="step = 2" :disabled="!form.requestedRole"
          class="btn-primary w-full mt-5 disabled:opacity-40 disabled:cursor-not-allowed">
          Tiếp tục
        </button>

        <p class="text-center text-sm text-gray-500 mt-5">
          Đã có tài khoản?
          <RouterLink to="/login" class="text-blue-600 font-medium hover:text-blue-700">Đăng nhập</RouterLink>
        </p>
      </div>

      <!-- Step 2: Fill in details -->
      <div v-else-if="step === 2" class="card shadow-lg">
        <button @click="step = 1" class="flex items-center gap-1 text-sm text-gray-500 hover:text-blue-600 mb-4 -mt-1">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
          </svg>
          Quay lại
        </button>

        <div class="flex items-center gap-2 mb-5 p-3 rounded-xl"
          :class="form.requestedRole === 'Doctor' ? 'bg-green-50' : 'bg-blue-50'">
          <span class="text-sm font-medium" :class="form.requestedRole === 'Doctor' ? 'text-green-700' : 'text-blue-700'">
            Đăng ký: {{ form.requestedRole === 'Doctor' ? 'Bác sĩ (chờ duyệt)' : 'Bệnh nhân' }}
          </span>
        </div>

        <form @submit.prevent="handleRegister" class="space-y-4">
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1.5">Họ</label>
              <input v-model="form.lastName" type="text" 
                class="input-field transition-colors"
                :class="fieldErrors.lastName ? 'border-red-500 bg-red-50' : ''"
                placeholder="Nguyễn" required />
              <p v-if="fieldErrors.lastName" class="text-xs text-red-600 mt-1 flex items-center gap-1">
                <svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M18.101 12.93a1 1 0 00-1.414-1.414L10 15.586 7.313 12.899a1 1 0 00-1.414 1.414l3.536 3.536a1 1 0 001.414 0l8.536-8.536z"/></svg>
                {{ fieldErrors.lastName }}
              </p>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1.5">Tên</label>
              <input v-model="form.firstName" type="text" 
                class="input-field transition-colors"
                :class="fieldErrors.firstName ? 'border-red-500 bg-red-50' : ''"
                placeholder="Văn A" required />
              <p v-if="fieldErrors.firstName" class="text-xs text-red-600 mt-1 flex items-center gap-1">
                <svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M18.101 12.93a1 1 0 00-1.414-1.414L10 15.586 7.313 12.899a1 1 0 00-1.414 1.414l3.536 3.536a1 1 0 001.414 0l8.536-8.536z"/></svg>
                {{ fieldErrors.firstName }}
              </p>
            </div>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1.5">Email</label>
            <div class="relative">
              <input v-model="form.email" type="email" 
                class="input-field transition-colors"
                :class="fieldErrors.email ? 'border-red-500 bg-red-50 pr-10' : ''"
                placeholder="email@example.com" required />
              <svg v-if="fieldErrors.email" class="absolute right-3 top-1/2 -translate-y-1/2 w-5 h-5 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4v.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
              </svg>
            </div>
            <p v-if="fieldErrors.email" class="text-xs text-red-600 mt-1 flex items-center gap-1">
              <svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M18.101 12.93a1 1 0 00-1.414-1.414L10 15.586 7.313 12.899a1 1 0 00-1.414 1.414l3.536 3.536a1 1 0 001.414 0l8.536-8.536z"/></svg>
              {{ fieldErrors.email }}
            </p>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1.5">Số điện thoại</label>
            <input v-model="form.phoneNumber" type="tel" 
              class="input-field transition-colors"
              :class="fieldErrors.phoneNumber ? 'border-red-500 bg-red-50' : ''"
              placeholder="0901234567" />
            <p v-if="fieldErrors.phoneNumber" class="text-xs text-red-600 mt-1 flex items-center gap-1">
              <svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M18.101 12.93a1 1 0 00-1.414-1.414L10 15.586 7.313 12.899a1 1 0 00-1.414 1.414l3.536 3.536a1 1 0 001.414 0l8.536-8.536z"/></svg>
              {{ fieldErrors.phoneNumber }}
            </p>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1.5">Mật khẩu</label>
            <input v-model="form.password" type="password" 
              class="input-field transition-colors"
              :class="fieldErrors.password ? 'border-red-500 bg-red-50' : ''"
              placeholder="Tối thiểu 8 ký tự" required minlength="8" />
            <p v-if="fieldErrors.password" class="text-xs text-red-600 mt-1 flex items-center gap-1">
              <svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M18.101 12.93a1 1 0 00-1.414-1.414L10 15.586 7.313 12.899a1 1 0 00-1.414 1.414l3.536 3.536a1 1 0 001.414 0l8.536-8.536z"/></svg>
              {{ fieldErrors.password }}
            </p>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1.5">Xác nhận mật khẩu</label>
            <input v-model="confirmPassword" type="password" 
              class="input-field transition-colors"
              :class="fieldErrors.confirmPassword ? 'border-red-500 bg-red-50' : ''"
              placeholder="Nhập lại mật khẩu" required />
            <p v-if="fieldErrors.confirmPassword" class="text-xs text-red-600 mt-1 flex items-center gap-1">
              <svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M18.101 12.93a1 1 0 00-1.414-1.414L10 15.586 7.313 12.899a1 1 0 00-1.414 1.414l3.536 3.536a1 1 0 001.414 0l8.536-8.536z"/></svg>
              {{ fieldErrors.confirmPassword }}
            </p>
          </div>

          <!-- Doctor-specific fields -->
          <template v-if="form.requestedRole === 'Doctor'">
            <hr class="border-gray-100" />
            <p class="text-xs font-semibold text-gray-500 uppercase tracking-wide">Thông tin hành nghề</p>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1.5">Chuyên khoa <span class="text-red-500">*</span></label>
              <input v-model="form.specialty" type="text" 
                class="input-field transition-colors"
                :class="fieldErrors.specialty ? 'border-red-500 bg-red-50' : ''"
                placeholder="Tim mạch, Nội tiết, ..." required />
              <p v-if="fieldErrors.specialty" class="text-xs text-red-600 mt-1 flex items-center gap-1">
                <svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M18.101 12.93a1 1 0 00-1.414-1.414L10 15.586 7.313 12.899a1 1 0 00-1.414 1.414l3.536 3.536a1 1 0 001.414 0l8.536-8.536z"/></svg>
                {{ fieldErrors.specialty }}
              </p>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1.5">Số giấy phép hành nghề <span class="text-red-500">*</span></label>
              <input v-model="form.licenseNumber" type="text" 
                class="input-field transition-colors"
                :class="fieldErrors.licenseNumber ? 'border-red-500 bg-red-50' : ''"
                placeholder="GP-12345" required />
              <p v-if="fieldErrors.licenseNumber" class="text-xs text-red-600 mt-1 flex items-center gap-1">
                <svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M18.101 12.93a1 1 0 00-1.414-1.414L10 15.586 7.313 12.899a1 1 0 00-1.414 1.414l3.536 3.536a1 1 0 001.414 0l8.536-8.536z"/></svg>
                {{ fieldErrors.licenseNumber }}
              </p>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1.5">Phí tư vấn (VNĐ)</label>
              <input v-model.number="form.consultationFee" type="number" min="0" class="input-field" placeholder="150000" />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1.5">Giới thiệu bản thân</label>
              <textarea v-model="form.bio" class="input-field resize-none" rows="3" placeholder="Kinh nghiệm, chuyên môn..."></textarea>
            </div>
          </template>

          <button type="submit" :disabled="loading" class="btn-primary w-full flex items-center justify-center gap-2">
            <svg v-if="loading" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
            </svg>
            {{ loading ? 'Đang đăng ký...' : 'Đăng ký' }}
          </button>
        </form>

        <p class="text-center text-sm text-gray-500 mt-5">
          Đã có tài khoản?
          <RouterLink to="/login" class="text-blue-600 font-medium hover:text-blue-700">Đăng nhập</RouterLink>
        </p>
      </div>

      <!-- Step 3: Pending approval (doctor) -->
      <div v-else-if="step === 3" class="card shadow-lg text-center">
        <div class="w-16 h-16 bg-yellow-100 rounded-full flex items-center justify-center mx-auto mb-4">
          <svg class="w-8 h-8 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
          </svg>
        </div>
        <h2 class="text-xl font-bold text-gray-900 mb-2">Đang chờ duyệt</h2>
        <p class="text-gray-500 text-sm mb-4">
          Hồ sơ bác sĩ của bạn đã được gửi thành công.<br>
          Admin sẽ xem xét và phê duyệt trong vòng 1–3 ngày làm việc.
        </p>
        <div class="bg-blue-50 rounded-xl p-4 mb-6 text-left">
          <p class="text-sm text-blue-800 font-medium mb-1">Kiểm tra email của bạn</p>
          <p class="text-xs text-blue-600">Chúng tôi đã gửi email xác thực đến <strong>{{ registeredEmail }}</strong>. Vui lòng xác thực email để kích hoạt tài khoản.</p>
        </div>
        <RouterLink to="/" class="btn-primary inline-block">Về trang chủ</RouterLink>
      </div>

      <!-- Step 4: Check email (patient) -->
      <div v-else-if="step === 4" class="card shadow-lg text-center">
        <div class="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4">
          <svg class="w-8 h-8 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/>
          </svg>
        </div>
        <h2 class="text-xl font-bold text-gray-900 mb-2">Kiểm tra email của bạn</h2>
        <p class="text-gray-500 text-sm mb-2">Chúng tôi đã gửi email xác thực đến</p>
        <p class="font-semibold text-blue-700 text-sm mb-4">{{ registeredEmail }}</p>
        <p class="text-gray-400 text-xs mb-6">Nhấn vào liên kết trong email để xác thực tài khoản. Liên kết có hiệu lực trong 24 giờ.</p>

        <div class="space-y-3">
          <RouterLink to="/" class="btn-primary w-full block text-center">Về trang chủ</RouterLink>
          <button @click="resendEmail" :disabled="resendLoading || resendCooldown > 0"
            class="w-full text-sm text-blue-600 hover:text-blue-700 disabled:opacity-50 disabled:cursor-not-allowed py-2">
            <span v-if="resendLoading">Đang gửi...</span>
            <span v-else-if="resendCooldown > 0">Gửi lại sau {{ resendCooldown }}s</span>
            <span v-else>Gửi lại email xác thực</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import { authApi } from '@/services/api'

const auth  = useAuthStore()
const toast = useToastStore()
const router = useRouter()

const step = ref<1 | 2 | 3 | 4>(1)
const registeredEmail = ref('')
const resendLoading = ref(false)
const resendCooldown = ref(0)

const form = ref({
  email: '',
  password: '',
  firstName: '',
  lastName: '',
  phoneNumber: '',
  requestedRole: '' as 'Patient' | 'Doctor' | '',
  specialty: '',
  licenseNumber: '',
  bio: '',
  consultationFee: undefined as number | undefined,
})
const confirmPassword = ref('')
const loading     = ref(false)
const fieldErrors = ref<Record<string, string>>({})

function selectRole(role: 'Patient' | 'Doctor') {
  form.value.requestedRole = role
}

// Map backend field key → Vietnamese label for toast messages
const fieldLabels: Record<string, string> = {
  firstName:       'Tên',
  lastName:        'Họ',
  email:           'Email',
  phoneNumber:     'Số điện thoại',
  password:        'Mật khẩu',
  confirmPassword: 'Xác nhận mật khẩu',
  specialty:       'Chuyên khoa',
  licenseNumber:   'Số giấy phép',
}

async function handleRegister() {
  fieldErrors.value = {}

  if (form.value.password !== confirmPassword.value) {
    fieldErrors.value.confirmPassword = 'Mật khẩu xác nhận không khớp'
    toast.error('Xác nhận mật khẩu: Mật khẩu xác nhận không khớp', 4000)
    return
  }

  loading.value = true
  try {
    await auth.register({
      email:           form.value.email,
      password:        form.value.password,
      firstName:       form.value.firstName,
      lastName:        form.value.lastName,
      phoneNumber:     form.value.phoneNumber || undefined,
      requestedRole:   form.value.requestedRole as 'Patient' | 'Doctor',
      specialty:       form.value.specialty || undefined,
      licenseNumber:   form.value.licenseNumber || undefined,
      bio:             form.value.bio || undefined,
      consultationFee: form.value.consultationFee,
    })
    registeredEmail.value = form.value.email
    step.value = auth.pendingDoctor ? 3 : 4
  } catch (e: any) {
    const response = e?.response
    const data     = response?.data
    const errors   = data?.errors as Record<string, string> | undefined

    if (errors && Object.keys(errors).length > 0) {
      fieldErrors.value = errors
      const lines = Object.entries(errors).map(([key, msg]) => {
        const label = fieldLabels[key] ?? key
        return `${label}: ${msg}`
      })
      toast.error(lines.join('\n'), 6000)
    } else if (!response) {
      toast.error('Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối mạng.', 5000)
    } else if (response.status >= 500) {
      toast.error('Máy chủ đang gặp sự cố. Vui lòng thử lại sau ít phút.', 5000)
    } else {
      toast.error(data?.message || 'Đăng ký thất bại. Vui lòng kiểm tra lại thông tin.', 5000)
    }
  } finally {
    loading.value = false
  }
}

async function resendEmail() {
  resendLoading.value = true
  try {
    await authApi.sendVerification()
    resendCooldown.value = 60
    const timer = setInterval(() => {
      resendCooldown.value--
      if (resendCooldown.value <= 0) clearInterval(timer)
    }, 1000)
  } catch {
    // ignore
  } finally {
    resendLoading.value = false
  }
}
</script>
