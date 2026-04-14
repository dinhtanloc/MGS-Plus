import { setActivePinia, createPinia } from 'pinia'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { useAuthStore } from '@/stores/auth'
import * as apiModule from '@/services/api'

// ── Mock authApi so no real HTTP is made ────────────────────────────────────
vi.mock('@/services/api', async () => {
  const actual = await vi.importActual<typeof apiModule>('@/services/api')
  return {
    ...actual,
    authApi: {
      login: vi.fn(),
      register: vi.fn(),
      me: vi.fn(),
      changePassword: vi.fn()
    }
  }
})

const mockAuthApi = apiModule.authApi as {
  login: ReturnType<typeof vi.fn>
  register: ReturnType<typeof vi.fn>
  me: ReturnType<typeof vi.fn>
  changePassword: ReturnType<typeof vi.fn>
}

const fakeUser: apiModule.UserDto = {
  id: 1,
  email: 'alice@example.com',
  firstName: 'Alice',
  lastName: 'Smith',
  role: 'Patient',
  createdAt: new Date().toISOString()
}

beforeEach(() => {
  setActivePinia(createPinia())
  localStorage.clear()
})

// ── isLoggedIn ──────────────────────────────────────────────────────────────

describe('isLoggedIn', () => {
  it('is false when no token', () => {
    const auth = useAuthStore()
    expect(auth.isLoggedIn).toBe(false)
  })

  it('is true when token in localStorage on mount', () => {
    localStorage.setItem('token', 'existing-jwt')
    const auth = useAuthStore()
    expect(auth.isLoggedIn).toBe(true)
  })
})

// ── isAdmin / isDoctor ──────────────────────────────────────────────────────

describe('role computed', () => {
  it('isAdmin false when user is Patient', () => {
    const auth = useAuthStore()
    auth.user = { ...fakeUser, role: 'Patient' }
    expect(auth.isAdmin).toBe(false)
  })

  it('isAdmin true when user is Admin', () => {
    const auth = useAuthStore()
    auth.user = { ...fakeUser, role: 'Admin' }
    expect(auth.isAdmin).toBe(true)
  })

  it('isDoctor true when user is Doctor', () => {
    const auth = useAuthStore()
    auth.user = { ...fakeUser, role: 'Doctor' }
    expect(auth.isDoctor).toBe(true)
  })
})

// ── login() ─────────────────────────────────────────────────────────────────

describe('login', () => {
  it('sets token and user on success', async () => {
    mockAuthApi.login.mockResolvedValue({ data: { token: 'jwt-abc', user: fakeUser } })

    const auth = useAuthStore()
    await auth.login('alice@example.com', 'secret')

    expect(auth.token).toBe('jwt-abc')
    expect(auth.user).toEqual(fakeUser)
    expect(auth.isLoggedIn).toBe(true)
  })

  it('persists token to localStorage', async () => {
    mockAuthApi.login.mockResolvedValue({ data: { token: 'jwt-abc', user: fakeUser } })

    const auth = useAuthStore()
    await auth.login('alice@example.com', 'secret')

    expect(localStorage.getItem('token')).toBe('jwt-abc')
  })

  it('propagates error on failed login', async () => {
    mockAuthApi.login.mockRejectedValue(new Error('401'))

    const auth = useAuthStore()
    await expect(auth.login('bad@example.com', 'wrong')).rejects.toThrow('401')
    expect(auth.token).toBeNull()
  })
})

// ── register() ──────────────────────────────────────────────────────────────

describe('register', () => {
  it('sets token and user after registration', async () => {
    mockAuthApi.register.mockResolvedValue({ data: { token: 'reg-token', user: fakeUser } })

    const auth = useAuthStore()
    await auth.register({ email: 'a@b.com', password: 'Pass1!', firstName: 'A', lastName: 'B' })

    expect(auth.token).toBe('reg-token')
    expect(auth.user?.email).toBe('alice@example.com')
    expect(localStorage.getItem('token')).toBe('reg-token')
  })
})

// ── fetchMe() ───────────────────────────────────────────────────────────────

describe('fetchMe', () => {
  it('does nothing when no token', async () => {
    const auth = useAuthStore()
    await auth.fetchMe()
    expect(mockAuthApi.me).not.toHaveBeenCalled()
  })

  it('populates user on success', async () => {
    localStorage.setItem('token', 'jwt-xyz')
    mockAuthApi.me.mockResolvedValue({ data: fakeUser })

    const auth = useAuthStore()
    await auth.fetchMe()

    expect(auth.user).toEqual(fakeUser)
  })

  it('calls logout on error', async () => {
    localStorage.setItem('token', 'expired')
    mockAuthApi.me.mockRejectedValue({ response: { status: 401 } })

    const auth = useAuthStore()
    auth.user = fakeUser    // simulate a previous login
    await auth.fetchMe()

    expect(auth.user).toBeNull()
    expect(auth.token).toBeNull()
    expect(localStorage.getItem('token')).toBeNull()
  })
})

// ── logout() ────────────────────────────────────────────────────────────────

describe('logout', () => {
  it('clears token, user, and localStorage', async () => {
    mockAuthApi.login.mockResolvedValue({ data: { token: 'jwt', user: fakeUser } })

    const auth = useAuthStore()
    await auth.login('alice@example.com', 'secret')
    auth.logout()

    expect(auth.token).toBeNull()
    expect(auth.user).toBeNull()
    expect(localStorage.getItem('token')).toBeNull()
    expect(auth.isLoggedIn).toBe(false)
  })
})
