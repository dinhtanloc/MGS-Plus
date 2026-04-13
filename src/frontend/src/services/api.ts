import axios from 'axios'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' }
})

// Attach JWT token to every request
api.interceptors.request.use(config => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Handle 401 — redirect to login
api.interceptors.response.use(
  res => res,
  err => {
    if (err.response?.status === 401) {
      localStorage.removeItem('token')
      window.location.href = '/login'
    }
    return Promise.reject(err)
  }
)

// ── Auth ─────────────────────────────────────────────────────
export const authApi = {
  register: (data: RegisterRequest) => api.post('/auth/register', data),
  login: (data: LoginRequest) => api.post<AuthResponse>('/auth/login', data),
  me: () => api.get('/auth/me'),
  changePassword: (data: { currentPassword: string; newPassword: string }) =>
    api.post('/auth/change-password', data)
}

// ── User / Profile ───────────────────────────────────────────
export const userApi = {
  getProfile: () => api.get('/users/profile'),
  updateProfile: (data: Partial<UserProfile>) => api.put('/users/profile', data)
}

// ── Appointments ─────────────────────────────────────────────
export const appointmentApi = {
  list: (params?: { status?: string; page?: number }) => api.get('/appointments', { params }),
  create: (data: CreateAppointmentRequest) => api.post('/appointments', data),
  get: (id: number) => api.get(`/appointments/${id}`),
  update: (id: number, data: Partial<UpdateAppointmentRequest>) => api.patch(`/appointments/${id}`, data),
  getDoctors: (specialty?: string) => api.get('/appointments/doctors', { params: { specialty } })
}

// ── Blog ─────────────────────────────────────────────────────
export const blogApi = {
  list: (params?: { categoryId?: number; search?: string; page?: number }) => api.get('/blog', { params }),
  getBySlug: (slug: string) => api.get(`/blog/${slug}`),
  getCategories: () => api.get('/blog/categories'),
  create: (data: CreateBlogPostRequest) => api.post('/blog', data),
  update: (id: number, data: Partial<CreateBlogPostRequest>) => api.put(`/blog/${id}`, data)
}

// ── News ─────────────────────────────────────────────────────
export const newsApi = {
  list: (params?: { categoryId?: number; search?: string; page?: number }) => api.get('/news', { params }),
  get: (id: number) => api.get(`/news/${id}`),
  featured: (limit?: number) => api.get('/news/featured', { params: { limit } }),
  getCategories: () => api.get('/news/categories')
}

// ── Chatbot ──────────────────────────────────────────────────
export const chatApi = {
  createSession: (data: { title?: string; sessionType?: string }) => api.post('/chatbot/sessions', data),
  getSessions: () => api.get('/chatbot/sessions'),
  getSession: (id: number) => api.get(`/chatbot/sessions/${id}`),
  sendMessage: (sessionId: number, data: { content: string; contextType?: string }) =>
    api.post(`/chatbot/sessions/${sessionId}/messages`, data),
  quickChat: (content: string) => api.post('/chatbot/quick', { content })
}

// ── Medical Records ───────────────────────────────────────────
export const medicalApi = {
  list: (params?: { page?: number }) => api.get('/medicalrecords', { params }),
  get: (id: number) => api.get(`/medicalrecords/${id}`)
}

// ── Types ────────────────────────────────────────────────────
export interface RegisterRequest { email: string; password: string; firstName: string; lastName: string; phoneNumber?: string }
export interface LoginRequest { email: string; password: string }
export interface AuthResponse { token: string; tokenType: string; expiresIn: number; user: UserDto }
export interface UserDto { id: number; email: string; firstName: string; lastName: string; phoneNumber?: string; role: string; createdAt: string }
export interface UserProfile { firstName?: string; lastName?: string; phoneNumber?: string; dateOfBirth?: string; address?: string; insuranceNumber?: string; bloodType?: string; allergies?: string }
export interface CreateAppointmentRequest { scheduledAt: string; doctorId?: number; department?: string; description?: string }
export interface UpdateAppointmentRequest { scheduledAt?: string; status?: string; notes?: string; department?: string }
export interface CreateBlogPostRequest { title: string; content: string; summary?: string; categoryId?: number; tags?: string; thumbnailUrl?: string; isPublished?: boolean }
