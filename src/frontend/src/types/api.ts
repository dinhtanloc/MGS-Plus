// ── Auth ──────────────────────────────────────────────────────────────────────
export interface RegisterRequest {
  email: string
  password: string
  firstName: string
  lastName: string
  phoneNumber?: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface AuthResponse {
  token: string
  tokenType: string
  expiresIn: number
  user: UserDto
  refreshToken?: string
}

export interface UserDto {
  id: number
  email: string
  firstName: string
  lastName: string
  phoneNumber?: string
  role: string
  createdAt: string
}

export interface UserProfile {
  firstName?: string
  lastName?: string
  phoneNumber?: string
  dateOfBirth?: string
  address?: string
  insuranceNumber?: string
  insuranceProvider?: string
  bloodType?: string
  allergies?: string
  chronicDiseases?: string
}

// ── Appointments ──────────────────────────────────────────────────────────────
export interface AppointmentDto {
  id: number
  userId: number
  userName: string | null
  doctorId: number | null
  doctorName: string | null
  doctorSpecialty: string | null
  scheduledAt: string
  status: string
  description: string | null
  notes: string | null
  department: string | null
  queueNumber: number | null
  createdAt: string
}

export interface CreateAppointmentRequest {
  scheduledAt: string
  doctorId?: number
  department?: string
  description?: string
}

export interface UpdateAppointmentRequest {
  scheduledAt?: string
  status?: string
  notes?: string
  department?: string
}

export interface DoctorDto {
  id: number
  specialty: string
  bio: string | null
  consultationFee: number
  rating: number
  reviewCount: number
  name: string
  email: string
}

export interface DoctorScheduleSlot {
  dayOfWeek: number
  startTime: string
  endTime: string
  isAvailable: boolean
}

// ── Medical Records ───────────────────────────────────────────────────────────
export interface MedicalRecordDto {
  id: number
  diagnosis: string | null
  prescription: string | null
  notes: string | null
  labResults: string | null
  attachmentUrl: string | null
  recordDate: string
  doctorName: string | null
  specialty: string | null
}

// ── Blog ──────────────────────────────────────────────────────────────────────
export interface BlogPostDto {
  id: number
  title: string
  slug: string
  summary: string | null
  content: string
  thumbnailUrl: string | null
  tags: string | null
  isPublished: boolean
  publishedAt: string | null
  viewCount: number
  createdAt: string
  updatedAt: string
  authorName?: string
  categoryId?: number
  categoryName?: string
}

export interface CreateBlogPostRequest {
  title: string
  content: string
  summary?: string
  categoryId?: number
  tags?: string
  thumbnailUrl?: string
  isPublished?: boolean
}

// ── News ──────────────────────────────────────────────────────────────────────
export interface NewsDto {
  id: number
  title: string
  slug: string
  content: string
  imageUrl: string | null
  isFeatured: boolean
  viewCount: number
  publishedAt: string | null
  createdAt: string
  updatedAt: string
  categoryId?: number
  categoryName?: string
}

// ── Chatbot ───────────────────────────────────────────────────────────────────
export interface ChatSessionDto {
  id: number
  title: string
  sessionType: string
  messageCount: number
  createdAt: string
  updatedAt: string
}

export interface ChatMessageDto {
  id: number
  role: string
  content: string
  createdAt: string
}

export interface StreamEvent {
  type: 'start' | 'session' | 'reasoning' | 'tool_call' | 'response_chunk' | 'answer' | 'complete' | 'error'
  content?: string
  agent?: string
  tool?: string
  messageId?: number
  userMessageId?: number
  sessionId?: number
  thread_id?: string
}

// ── Pagination ────────────────────────────────────────────────────────────────
export interface PaginatedResponse<T> {
  total: number
  page: number
  pageSize: number
  data: T[]
}
