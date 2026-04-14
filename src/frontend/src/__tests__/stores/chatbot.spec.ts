import { setActivePinia, createPinia } from 'pinia'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { useChatbotStore } from '@/stores/chatbot'
import * as apiModule from '@/services/api'

// ── Mock chatApi ─────────────────────────────────────────────────────────────
vi.mock('@/services/api', async () => {
  const actual = await vi.importActual<typeof apiModule>('@/services/api')
  return {
    ...actual,
    chatApi: {
      createSession: vi.fn(),
      getSessions: vi.fn(),
      getSession: vi.fn(),
      sendMessage: vi.fn(),
      quickChat: vi.fn(),
      streamMessage: vi.fn()
    }
  }
})

const mockChatApi = apiModule.chatApi as {
  createSession: ReturnType<typeof vi.fn>
  getSessions: ReturnType<typeof vi.fn>
  getSession: ReturnType<typeof vi.fn>
  sendMessage: ReturnType<typeof vi.fn>
  quickChat: ReturnType<typeof vi.fn>
  streamMessage: ReturnType<typeof vi.fn>
}

beforeEach(() => {
  setActivePinia(createPinia())
  localStorage.clear()
})

// ── open / close / toggle ────────────────────────────────────────────────────

describe('visibility controls', () => {
  it('starts closed', () => {
    const store = useChatbotStore()
    expect(store.isOpen).toBe(false)
  })

  it('open() sets isOpen to true', () => {
    const store = useChatbotStore()
    store.open()
    expect(store.isOpen).toBe(true)
  })

  it('close() sets isOpen to false', () => {
    const store = useChatbotStore()
    store.open()
    store.close()
    expect(store.isOpen).toBe(false)
  })

  it('toggle() flips isOpen', () => {
    const store = useChatbotStore()
    store.toggle()
    expect(store.isOpen).toBe(true)
    store.toggle()
    expect(store.isOpen).toBe(false)
  })
})

// ── initSession() ────────────────────────────────────────────────────────────

describe('initSession', () => {
  it('creates a session and sets sessionId + welcome message', async () => {
    mockChatApi.createSession.mockResolvedValue({ data: { id: 42 } })

    const store = useChatbotStore()
    await store.initSession()

    expect(store.sessionId).toBe(42)
    expect(store.messages).toHaveLength(1)
    expect(store.messages[0].role).toBe('assistant')
  })

  it('does not create a second session if one exists', async () => {
    mockChatApi.createSession.mockResolvedValue({ data: { id: 42 } })

    const store = useChatbotStore()
    await store.initSession()
    await store.initSession()   // second call — should be a no-op

    expect(mockChatApi.createSession).toHaveBeenCalledTimes(1)
  })
})

// ── reset() ──────────────────────────────────────────────────────────────────

describe('reset', () => {
  it('clears sessionId, messages, and closes the widget', async () => {
    mockChatApi.createSession.mockResolvedValue({ data: { id: 99 } })

    const store = useChatbotStore()
    await store.initSession()
    store.open()
    store.reset()

    expect(store.sessionId).toBeNull()
    expect(store.messages).toHaveLength(0)
    expect(store.isOpen).toBe(false)
  })
})

// ── sendMessage() ────────────────────────────────────────────────────────────

describe('sendMessage', () => {
  it('ignores empty / whitespace messages', async () => {
    const store = useChatbotStore()
    store.sessionId = 1
    await store.sendMessage('   ')
    expect(mockChatApi.streamMessage).not.toHaveBeenCalled()
  })

  it('ignores calls while already loading', async () => {
    const store = useChatbotStore()
    store.sessionId = 1
    store.loading = true
    await store.sendMessage('hello')
    expect(mockChatApi.streamMessage).not.toHaveBeenCalled()
  })

  it('auto-creates session if none exists', async () => {
    mockChatApi.createSession.mockResolvedValue({ data: { id: 77 } })
    mockChatApi.streamMessage.mockResolvedValue(undefined)

    const store = useChatbotStore()
    await store.sendMessage('hello')

    expect(mockChatApi.createSession).toHaveBeenCalledOnce()
    expect(store.sessionId).toBe(77)
  })

  it('pushes user + streaming assistant messages', async () => {
    mockChatApi.streamMessage.mockResolvedValue(undefined)

    const store = useChatbotStore()
    store.sessionId = 5
    await store.sendMessage('xin chào')

    expect(store.messages).toHaveLength(2)
    expect(store.messages[0].role).toBe('user')
    expect(store.messages[0].content).toBe('xin chào')
    expect(store.messages[1].role).toBe('assistant')
  })

  it('clears loading flag after stream completes', async () => {
    mockChatApi.streamMessage.mockResolvedValue(undefined)

    const store = useChatbotStore()
    store.sessionId = 5
    await store.sendMessage('test')

    expect(store.loading).toBe(false)
  })

  it('sets error message when stream fails', async () => {
    mockChatApi.streamMessage.mockRejectedValue(new Error('Network error'))

    const store = useChatbotStore()
    store.sessionId = 5
    await store.sendMessage('test')

    expect(store.error).toBe('Kết nối bị gián đoạn.')
    expect(store.loading).toBe(false)
  })

  it('does not set error for AbortError', async () => {
    const abort = Object.assign(new Error('Aborted'), { name: 'AbortError' })
    mockChatApi.streamMessage.mockRejectedValue(abort)

    const store = useChatbotStore()
    store.sessionId = 5
    await store.sendMessage('test')

    expect(store.error).toBeNull()
  })
})

// ── _handleStreamEvent (via sendMessage callback) ───────────────────────────

describe('stream event handling', () => {
  async function sendAndCapture(events: apiModule.StreamEvent[]) {
    mockChatApi.streamMessage.mockImplementation(
      (_id: number, _content: string, onEvent: (e: apiModule.StreamEvent) => void) => {
        events.forEach(e => onEvent(e))
        return Promise.resolve()
      }
    )
    const store = useChatbotStore()
    store.sessionId = 1
    await store.sendMessage('hello')
    return store
  }

  it('session event updates sessionId', async () => {
    const store = await sendAndCapture([{ type: 'session', sessionId: 200 }])
    expect(store.sessionId).toBe(200)
  })

  it('response_chunk appends to content', async () => {
    const store = await sendAndCapture([
      { type: 'response_chunk', content: 'Xin ' },
      { type: 'response_chunk', content: 'chào!' }
    ])
    expect(store.messages[1].content).toBe('Xin chào!')
  })

  it('answer event replaces content', async () => {
    const store = await sendAndCapture([
      { type: 'response_chunk', content: 'partial' },
      { type: 'answer', content: 'Final answer.' }
    ])
    expect(store.messages[1].content).toBe('Final answer.')
  })

  it('reasoning event adds a reasoning step', async () => {
    const store = await sendAndCapture([
      { type: 'reasoning', content: 'Thinking...', agent: 'supervisor' }
    ])
    expect(store.messages[1].reasoning).toHaveLength(1)
    expect(store.messages[1].reasoning![0].type).toBe('reasoning')
  })

  it('tool_call event adds a tool step', async () => {
    const store = await sendAndCapture([
      { type: 'tool_call', content: 'Calling Qdrant', tool: 'qdrant_search', agent: 'documents' }
    ])
    const step = store.messages[1].reasoning![0]
    expect(step.type).toBe('tool_call')
    expect(step.tool).toBe('qdrant_search')
  })

  it('complete event stops streaming flag', async () => {
    const store = await sendAndCapture([{ type: 'complete', messageId: 99 }])
    expect(store.messages[1].isStreaming).toBe(false)
    expect(store.messages[1].id).toBe(99)
  })

  it('error event sets fallback content', async () => {
    const store = await sendAndCapture([{ type: 'error' }])
    expect(store.messages[1].content).toBeTruthy()
    expect(store.messages[1].isStreaming).toBe(false)
  })
})

// ── toggleReasoning() ────────────────────────────────────────────────────────

describe('toggleReasoning', () => {
  it('flips reasoningOpen for the given message index', async () => {
    mockChatApi.streamMessage.mockResolvedValue(undefined)

    const store = useChatbotStore()
    store.sessionId = 1
    await store.sendMessage('hi')   // assistant msg at index 1

    const before = store.messages[1].reasoningOpen
    store.toggleReasoning(1)
    expect(store.messages[1].reasoningOpen).toBe(!before)
  })

  it('does nothing for out-of-range index', () => {
    const store = useChatbotStore()
    expect(() => store.toggleReasoning(99)).not.toThrow()
  })
})
