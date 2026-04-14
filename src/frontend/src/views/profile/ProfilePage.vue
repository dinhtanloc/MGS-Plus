<template>
  <div class="max-w-3xl mx-auto px-4 py-10">
    <h1 class="text-2xl font-bold text-gray-900 mb-8">Hồ sơ cá nhân</h1>

    <div v-if="loading" class="card animate-pulse space-y-4">
      <div class="bg-gray-200 h-6 rounded w-1/3"></div>
      <div class="bg-gray-200 h-10 rounded"></div>
      <div class="bg-gray-200 h-10 rounded"></div>
    </div>

    <div v-else class="space-y-6">
      <!-- Profile card -->
      <div class="card">
        <div class="flex items-center gap-4 mb-6">
          <div class="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center text-2xl font-bold text-blue-600">
            {{ initials }}
          </div>
          <div>
            <h2 class="text-xl font-semibold text-gray-900">{{ auth.user?.firstName }} {{ auth.user?.lastName }}</h2>
            <p class="text-gray-500 text-sm">{{ auth.user?.email }}</p>
            <span class="badge-info text-xs mt-1 inline-block">{{ auth.user?.role }}</span>
          </div>
        </div>

        <form @submit.prevent="saveProfile" class="grid sm:grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Họ</label>
            <input v-model="form.firstName" type="text" class="input-field" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Tên</label>
            <input v-model="form.lastName" type="text" class="input-field" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Số điện thoại</label>
            <input v-model="form.phoneNumber" type="tel" class="input-field" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Ngày sinh</label>
            <input v-model="form.dateOfBirth" type="date" class="input-field" />
          </div>
          <div class="sm:col-span-2">
            <label class="block text-sm font-medium text-gray-700 mb-1">Địa chỉ</label>
            <input v-model="form.address" type="text" class="input-field" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Số BHYT</label>
            <input v-model="form.insuranceNumber" type="text" class="input-field" placeholder="VD: DN4050..." />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Nhóm máu</label>
            <select v-model="form.bloodType" class="input-field">
              <option value="">Chưa xác định</option>
              <option v-for="bt in bloodTypes" :key="bt">{{ bt }}</option>
            </select>
          </div>
          <div class="sm:col-span-2">
            <label class="block text-sm font-medium text-gray-700 mb-1">Dị ứng</label>
            <input v-model="form.allergies" type="text" class="input-field" placeholder="Ghi rõ các loại thuốc/thực phẩm dị ứng..." />
          </div>
          <div class="sm:col-span-2 flex items-center justify-between">
            <p v-if="saved" class="text-green-600 text-sm">Đã lưu thành công!</p>
            <p v-if="error" class="text-red-500 text-sm">{{ error }}</p>
            <button type="submit" :disabled="saving"
              class="ml-auto bg-blue-600 text-white px-6 py-2 rounded-xl font-medium hover:bg-blue-700 transition-colors disabled:opacity-50">
              {{ saving ? 'Đang lưu...' : 'Lưu thay đổi' }}
            </button>
          </div>
        </form>
      </div>

      <!-- Change password -->
      <div class="card">
        <h3 class="font-semibold text-gray-900 mb-4">Đổi mật khẩu</h3>
        <form @submit.prevent="changePassword" class="space-y-3">
          <input v-model="pwd.current" type="password" placeholder="Mật khẩu hiện tại" class="input-field" />
          <input v-model="pwd.newPwd" type="password" placeholder="Mật khẩu mới (ít nhất 6 ký tự)" class="input-field" />
          <input v-model="pwd.confirm" type="password" placeholder="Xác nhận mật khẩu mới" class="input-field" />
          <div class="flex items-center justify-between">
            <p v-if="pwdMsg" :class="pwdOk ? 'text-green-600' : 'text-red-500'" class="text-sm">{{ pwdMsg }}</p>
            <button type="submit" :disabled="pwdSaving"
              class="ml-auto bg-gray-800 text-white px-6 py-2 rounded-xl font-medium hover:bg-gray-900 transition-colors disabled:opacity-50">
              {{ pwdSaving ? 'Đang đổi...' : 'Đổi mật khẩu' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { userApi, authApi } from '@/services/api'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const loading = ref(true)
const saving = ref(false)
const saved = ref(false)
const error = ref('')
const pwdSaving = ref(false)
const pwdMsg = ref('')
const pwdOk = ref(false)

const form = ref({ firstName: '', lastName: '', phoneNumber: '', dateOfBirth: '', address: '', insuranceNumber: '', bloodType: '', allergies: '' })
const pwd = ref({ current: '', newPwd: '', confirm: '' })
const bloodTypes = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-']

const initials = computed(() => {
  const f = auth.user?.firstName?.[0] || ''
  const l = auth.user?.lastName?.[0] || ''
  return (f + l).toUpperCase() || '?'
})

async function saveProfile() {
  saving.value = true; saved.value = false; error.value = ''
  try {
    await userApi.updateProfile(form.value)
    saved.value = true
    setTimeout(() => { saved.value = false }, 3000)
  } catch { error.value = 'Lưu thất bại, thử lại sau.' }
  finally { saving.value = false }
}

async function changePassword() {
  if (pwd.value.newPwd !== pwd.value.confirm) { pwdMsg.value = 'Mật khẩu xác nhận không khớp'; pwdOk.value = false; return }
  pwdSaving.value = true; pwdMsg.value = ''
  try {
    await authApi.changePassword({ currentPassword: pwd.value.current, newPassword: pwd.value.newPwd })
    pwdMsg.value = 'Đổi mật khẩu thành công!'
    pwdOk.value = true
    pwd.value = { current: '', newPwd: '', confirm: '' }
  } catch { pwdMsg.value = 'Đổi mật khẩu thất bại. Kiểm tra mật khẩu hiện tại.'; pwdOk.value = false }
  finally { pwdSaving.value = false }
}

onMounted(async () => {
  try {
    const { data } = await userApi.getProfile()
    Object.assign(form.value, data)
  } catch { /* use auth store data */ }
  finally {
    if (auth.user) {
      form.value.firstName = form.value.firstName || auth.user.firstName
      form.value.lastName = form.value.lastName || auth.user.lastName
      form.value.phoneNumber = form.value.phoneNumber || auth.user.phoneNumber || ''
    }
    loading.value = false
  }
})
</script>
