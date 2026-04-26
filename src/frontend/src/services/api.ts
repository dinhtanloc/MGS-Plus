import axios, { type AxiosRequestConfig } from 'axios'
import type {
  RegisterRequest, LoginRequest, AuthResponse, UserDto,
  UserProfile, CreateAppointmentRequest, UpdateAppointmentRequest,
  CreateBlogPostRequest, StreamEvent,
  PaginatedResponse, AppointmentDto, MedicalRecordDto, DoctorDto, NewsDto, BlogPostDto,
  AdminStatsDto, DoctorApplicationDto, AdminUsersResponse
} from '@/types/api'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' }
})

// ── Token refresh queue ────────────────────────────────────────────────────────
let isRefreshing = false
let refreshSubscribers: Array<(token: string) => void> = []

function subscribeTokenRefresh(cb: (token: string) => void) {
  refreshSubscribers.push(cb)
}
function onTokenRefreshed(newToken: string) {
  refreshSubscribers.forEach(cb => cb(newToken))
  refreshSubscribers = []
}

// ── Request interceptor: attach JWT ───────────────────────────────────────────
api.interceptors.request.use(config => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// ── Response interceptor: silent refresh on 401 ───────────────────────────────
api.interceptors.response.use(
  res => res,
  async err => {
    const original = err.config as AxiosRequestConfig & { _retry?: boolean }

    // Skip refresh for auth endpoints — 401 there means wrong credentials, not expired token
    const isAuthEndpoint = (original.url ?? '').includes('/auth/login')
      || (original.url ?? '').includes('/auth/register')

    if (err.response?.status === 401 && !original._retry && !isAuthEndpoint) {
      const refreshToken = localStorage.getItem('refreshToken')

      if (!refreshToken) {
        // No refresh token — redirect to login
        localStorage.removeItem('token')
        import('@/router').then(m => m.default.push('/login'))
        return Promise.reject(err)
      }

      if (isRefreshing) {
        // Queue the request until refresh completes
        return new Promise(resolve => {
          subscribeTokenRefresh(token => {
            original.headers = { ...original.headers, Authorization: `Bearer ${token}` }
            resolve(api(original))
          })
        })
      }

      original._retry = true
      isRefreshing = true

      try {
        const { data } = await axios.post<AuthResponse>(
          `${import.meta.env.VITE_API_BASE_URL || '/api'}/auth/refresh`,
          { refreshToken }
        )
        localStorage.setItem('token', data.token)
        if (data.refreshToken) localStorage.setItem('refreshToken', data.refreshToken)

        onTokenRefreshed(data.token)
        original.headers = { ...original.headers, Authorization: `Bearer ${data.token}` }
        return api(original)
      } catch {
        // Refresh failed — clear session and redirect
        localStorage.removeItem('token')
        localStorage.removeItem('refreshToken')
        import('@/router').then(m => m.default.push('/login'))
        return Promise.reject(err)
      } finally {
        isRefreshing = false
      }
    }

    return Promise.reject(err)
  }
)

// ── Auth ──────────────────────────────────────────────────────────────────────
export const authApi = {
  register:      (data: RegisterRequest)                                  => api.post<AuthResponse>('/auth/register', data),
  login:         (data: LoginRequest)                                     => api.post<AuthResponse>('/auth/login', data),
  me:            ()                                                       => api.get<UserDto>('/auth/me'),
  changePassword:(data: { currentPassword: string; newPassword: string }) => api.post('/auth/change-password', data),
  refresh:       (refreshToken: string)                                   => api.post<AuthResponse>('/auth/refresh', { refreshToken }),
  logout:        (refreshToken: string)                                   => api.post('/auth/logout', { refreshToken }),
  sendVerification: ()                                                    => api.post('/auth/send-verification-email'),
}

// ── User / Profile ────────────────────────────────────────────────────────────
export const userApi = {
  getProfile:    ()                       => api.get('/users/profile'),
  updateProfile: (data: Partial<UserProfile>) => api.put('/users/profile', data)
}

// ── Appointments ──────────────────────────────────────────────────────────────
export const appointmentApi = {
  list:       (params?: { status?: string; page?: number })        => api.get<PaginatedResponse<AppointmentDto>>('/appointments', { params }),
  create:     (data: CreateAppointmentRequest)                     => api.post<AppointmentDto>('/appointments', data),
  get:        (id: number)                                         => api.get<AppointmentDto>(`/appointments/${id}`),
  update:     (id: number, data: Partial<UpdateAppointmentRequest>)=> api.patch(`/appointments/${id}`, data),
  getDoctors: (specialty?: string)                                 => api.get<DoctorDto[]>('/appointments/doctors', { params: { specialty } }),
  getDoctor:  (id: number)                                         => api.get(`/appointments/doctors/${id}`),
  getDoctorSchedule: (id: number)                                  => api.get(`/appointments/doctors/${id}/schedule`),
  getDoctorSlots:    (id: number, date: string)                    => api.get(`/appointments/doctors/${id}/slots`, { params: { date } }),
  submitReview:      (doctorId: number, data: { appointmentId: number; rating: number; comment?: string }) =>
    api.post(`/appointments/doctors/${doctorId}/reviews`, data),
  checkReview:       (doctorId: number, appointmentId: number)     =>
    api.get<{ reviewed: boolean }>(`/appointments/doctors/${doctorId}/reviews/check`, { params: { appointmentId } }),
}

// ── Blog ──────────────────────────────────────────────────────────────────────
export const blogApi = {
  list:          (params?: { categoryId?: number; search?: string; page?: number }) => api.get<PaginatedResponse<BlogPostDto>>('/blog', { params }),
  getBySlug:     (slug: string)                                                     => api.get<BlogPostDto>(`/blog/${slug}`),
  getCategories: ()                                                                 => api.get('/blog/categories'),
  create:        (data: CreateBlogPostRequest)                                      => api.post<BlogPostDto>('/blog', data),
  update:        (id: number, data: Partial<CreateBlogPostRequest>)                 => api.put(`/blog/${id}`, data),
  delete:        (id: number)                                                       => api.delete(`/blog/${id}`),
}

// ── News ──────────────────────────────────────────────────────────────────────
export const newsApi = {
  list:          (params?: { categoryId?: number; search?: string; page?: number }) => api.get<PaginatedResponse<NewsDto>>('/news', { params }),
  get:           (id: number)                                                        => api.get<NewsDto>(`/news/${id}`),
  featured:      (limit?: number)                                                    => api.get<NewsDto[]>('/news/featured', { params: { limit } }),
  getCategories: ()                                                                  => api.get('/news/categories'),
  create:        (data: any)                                                         => api.post('/news', data),
  update:        (id: number, data: any)                                             => api.put(`/news/${id}`, data),
  delete:        (id: number)                                                        => api.delete(`/news/${id}`),
  adminList:     (params?: { search?: string; page?: number; pageSize?: number })   => api.get('/news/admin', { params }),
}

// ── Chatbot ───────────────────────────────────────────────────────────────────
export const chatApi = {
  createSession: (data: { title?: string; sessionType?: string })               => api.post('/chatbot/sessions', data),
  getSessions:   ()                                                              => api.get('/chatbot/sessions'),
  getSession:    (id: number)                                                    => api.get(`/chatbot/sessions/${id}`),
  sendMessage:   (sessionId: number, data: { content: string; contextType?: string }) =>
    api.post(`/chatbot/sessions/${sessionId}/messages`, data),
  quickChat:     (content: string)                                               => api.post('/chatbot/quick', { content }),

  streamMessage(
    sessionId: number,
    content: string,
    onEvent: (event: StreamEvent) => void,
    signal?: AbortSignal
  ): Promise<void> {
    const token   = localStorage.getItem('token')
    const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api'
    return fetch(`${baseUrl}/chatbot/sessions/${sessionId}/messages/stream`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      },
      body: JSON.stringify({ content }),
      signal
    }).then(async response => {
      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      const reader  = response.body!.getReader()
      const decoder = new TextDecoder()
      let buffer = ''
      try {
        while (true) {
          const { done, value } = await reader.read()
          if (done) break
          buffer += decoder.decode(value, { stream: true })
          const lines = buffer.split('\n')
          buffer = lines.pop() ?? ''
          for (const line of lines) {
            if (line.startsWith('data: ')) {
              try { onEvent(JSON.parse(line.slice(6))) } catch { /* skip malformed */ }
            }
          }
        }
      } finally {
        reader.releaseLock()
      }
    })
  }
}

// ── Medical Records ───────────────────────────────────────────────────────────
export const medicalApi = {
  list: (params?: { page?: number }) => api.get<PaginatedResponse<MedicalRecordDto>>('/medicalrecords', { params }),
  get:  (id: number)                 => api.get<MedicalRecordDto>(`/medicalrecords/${id}`)
}

// ── Appointments (doctor-side) ────────────────────────────────────────────────
export const doctorApi = {
  listAppointments: (params?: { status?: string; page?: number; pageSize?: number }) =>
    api.get<{ total: number; page: number; pageSize: number; data: AppointmentDto[] }>('/appointments/doctor', { params }),
  takeAction: (id: number, action: 'accept' | 'reschedule' | 'reject', reason?: string, rescheduledTo?: string) =>
    api.post<AppointmentDto>(`/appointments/${id}/action`, { action, reason, rescheduledTo }),
}

// ── Direct Chat ───────────────────────────────────────────────────────────────
export const directChatApi = {
  getSessions:  ()                        => api.get('/direct-chat/sessions'),
  getOrCreate:  (doctorId: number)        => api.post('/direct-chat/sessions', { doctorId }),
  getSession:   (id: number)              => api.get(`/direct-chat/sessions/${id}`),
  getMessages:  (id: number, page = 1)    => api.get(`/direct-chat/sessions/${id}/messages`, { params: { page } }),
}

// ── Prescriptions ─────────────────────────────────────────────────────────────
export const prescriptionApi = {
  upload:  (file: File) => {
    const form = new FormData()
    form.append('file', file)
    return api.post('/prescriptions/upload', form, { headers: { 'Content-Type': 'multipart/form-data' } })
  },
  get:     (id: number) => api.get(`/prescriptions/${id}`),
  list:    (page = 1)   => api.get('/prescriptions', { params: { page } }),
}

// ── Admin ─────────────────────────────────────────────────────────────────────
export const adminApi = {
  getStats:               ()                                                         => api.get<AdminStatsDto>('/admin/stats'),
  getAnalytics:           ()                                                         => api.get('/admin/analytics'),
  getDoctorApplications:  (status?: string)                                          => api.get<DoctorApplicationDto[]>('/admin/doctor-applications', { params: { status } }),
  reviewApplication:      (id: number, action: 'approve' | 'reject', rejectionReason?: string) =>
    api.post(`/admin/doctor-applications/${id}/review`, { action, rejectionReason }),
  getUsers:               (params?: { search?: string; page?: number; pageSize?: number }) => api.get<AdminUsersResponse>('/admin/users', { params }),
  grantAdmin:             (id: number)                                               => api.post(`/admin/users/${id}/grant-admin`),
  revokeAdmin:            (id: number)                                               => api.post(`/admin/users/${id}/revoke-admin`),
  toggleUserActive:       (id: number)                                               => api.put<{ isActive: boolean }>(`/admin/users/${id}/toggle-active`),
  verifyUserEmail:        (id: number)                                               => api.post(`/admin/users/${id}/verify-email`),
  resendVerification:     (id: number)                                               => api.post(`/admin/users/${id}/resend-verification`),
}

export default api

// ── Re-export types for convenience ──────────────────────────────────────────
export type {
  RegisterRequest, LoginRequest, AuthResponse, UserDto, UserProfile,
  CreateAppointmentRequest, UpdateAppointmentRequest,
  CreateBlogPostRequest, StreamEvent,
  PaginatedResponse, AppointmentDto, MedicalRecordDto, DoctorDto, NewsDto, BlogPostDto,
  AdminStatsDto, DoctorApplicationDto, AdminUsersResponse
}
