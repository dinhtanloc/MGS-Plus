import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi } from '@/services/api'
import type { UserDto } from '@/types/api'

export const useAuthStore = defineStore('auth', () => {
  const token        = ref<string | null>(localStorage.getItem('token'))
  const refreshToken = ref<string | null>(localStorage.getItem('refreshToken'))
  const user         = ref<UserDto | null>(null)

  const isLoggedIn = computed(() => !!token.value)
  const isAdmin    = computed(() => user.value?.role === 'Admin')
  const isDoctor   = computed(() => user.value?.role === 'Doctor')

  async function login(email: string, password: string) {
    const { data } = await authApi.login({ email, password })
    _applyAuthResponse(data)
  }

  async function register(payload: { email: string; password: string; firstName: string; lastName: string; phoneNumber?: string }) {
    const { data } = await authApi.register(payload)
    _applyAuthResponse(data)
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

  async function logout() {
    if (refreshToken.value) {
      try { await authApi.logout(refreshToken.value) } catch { /* ignore */ }
    }
    _clearSession()
  }

  function _applyAuthResponse(data: { token: string; user: UserDto; refreshToken?: string }) {
    token.value = data.token
    user.value  = data.user
    localStorage.setItem('token', data.token)
    if (data.refreshToken) {
      refreshToken.value = data.refreshToken
      localStorage.setItem('refreshToken', data.refreshToken)
    }
  }

  function _clearSession() {
    token.value        = null
    refreshToken.value = null
    user.value         = null
    localStorage.removeItem('token')
    localStorage.removeItem('refreshToken')
  }

  return { token, refreshToken, user, isLoggedIn, isAdmin, isDoctor, login, register, fetchMe, logout }
})
