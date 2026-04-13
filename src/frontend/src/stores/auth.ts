import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi, type UserDto } from '@/services/api'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'))
  const user = ref<UserDto | null>(null)

  const isLoggedIn = computed(() => !!token.value)
  const isAdmin = computed(() => user.value?.role === 'Admin')
  const isDoctor = computed(() => user.value?.role === 'Doctor')

  async function login(email: string, password: string) {
    const { data } = await authApi.login({ email, password })
    token.value = data.token
    user.value = data.user
    localStorage.setItem('token', data.token)
  }

  async function register(payload: { email: string; password: string; firstName: string; lastName: string; phoneNumber?: string }) {
    const { data } = await authApi.register(payload)
    token.value = data.token
    user.value = data.user
    localStorage.setItem('token', data.token)
  }

  async function fetchMe() {
    if (!token.value) return
    try {
      const { data } = await authApi.me()
      user.value = data
    } catch {
      logout()
    }
  }

  function logout() {
    token.value = null
    user.value = null
    localStorage.removeItem('token')
  }

  return { token, user, isLoggedIn, isAdmin, isDoctor, login, register, fetchMe, logout }
})
