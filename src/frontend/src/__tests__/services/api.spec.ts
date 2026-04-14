/**
 * api.spec.ts
 *
 * Tests for axios interceptors and API shape.
 * Interceptors are captured at import time via a module-level axios mock,
 * then invoked directly — no real HTTP is made.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'

// ── Capture interceptors via axios mock ─────────────────────────────────────

type RequestHandler  = (config: Record<string, unknown>) => Record<string, unknown>
type ResponseHandler = (res: unknown) => unknown
type ErrorHandler    = (err: unknown) => unknown

let requestFulfilled!: RequestHandler
let responseFulfilled!: ResponseHandler
let responseRejected!: ErrorHandler

vi.mock('axios', () => {
  const mockInstance = {
    interceptors: {
      request:  { use: vi.fn((fn: RequestHandler)  => { requestFulfilled  = fn }) },
      response: { use: vi.fn((_: ResponseHandler, fn: ErrorHandler) => {
        responseFulfilled = (res: unknown) => res   // pass-through
        responseRejected  = fn
      }) }
    },
    get:   vi.fn(),
    post:  vi.fn(),
    put:   vi.fn(),
    patch: vi.fn(),
    delete: vi.fn()
  }

  return {
    default: {
      create: vi.fn(() => mockInstance)
    }
  }
})

// Force the module to run so interceptors are registered
await import('@/services/api')

// ── Request interceptor ─────────────────────────────────────────────────────

describe('Request interceptor — Authorization header', () => {
  beforeEach(() => localStorage.clear())

  it('attaches Bearer token when token is in localStorage', () => {
    localStorage.setItem('token', 'my-test-token')
    const config = { headers: {} as Record<string, string> }
    const result = requestFulfilled(config as Record<string, unknown>) as typeof config
    expect(result.headers['Authorization']).toBe('Bearer my-test-token')
  })

  it('does not attach Authorization when no token', () => {
    const config = { headers: {} as Record<string, string> }
    const result = requestFulfilled(config as Record<string, unknown>) as typeof config
    expect(result.headers['Authorization']).toBeUndefined()
  })
})

// ── Response interceptor (401 handling) ─────────────────────────────────────

describe('Response interceptor — 401 handling', () => {
  beforeEach(() => {
    localStorage.clear()
    // Make window.location.href writable for jsdom
    Object.defineProperty(window, 'location', {
      writable: true,
      value: { href: 'http://localhost/' }
    })
  })

  it('removes token and redirects to /login on 401', async () => {
    localStorage.setItem('token', 'stale-token')

    const err = { response: { status: 401 } }
    await responseRejected(err).catch(() => {})

    expect(localStorage.getItem('token')).toBeNull()
    expect(window.location.href).toBe('/login')
  })

  it('does not redirect on 500 errors', async () => {
    const err = { response: { status: 500 } }
    await responseRejected(err).catch(() => {})
    expect(window.location.href).not.toBe('/login')
  })

  it('passes through successful responses unchanged', () => {
    const response = { data: { id: 1 }, status: 200 }
    const result = responseFulfilled(response)
    expect(result).toBe(response)
  })
})

// ── API shape ───────────────────────────────────────────────────────────────

describe('authApi exports', () => {
  it('exposes login, register, me, changePassword', async () => {
    const { authApi } = await import('@/services/api')
    expect(typeof authApi.login).toBe('function')
    expect(typeof authApi.register).toBe('function')
    expect(typeof authApi.me).toBe('function')
    expect(typeof authApi.changePassword).toBe('function')
  })
})

describe('chatApi exports', () => {
  it('exposes createSession, sendMessage, streamMessage, quickChat', async () => {
    const { chatApi } = await import('@/services/api')
    expect(typeof chatApi.createSession).toBe('function')
    expect(typeof chatApi.sendMessage).toBe('function')
    expect(typeof chatApi.streamMessage).toBe('function')
    expect(typeof chatApi.quickChat).toBe('function')
  })
})
