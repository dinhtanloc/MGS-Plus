import { defineStore } from 'pinia'
import { ref } from 'vue'
import { chatApi } from '@/services/api'
import type { StreamEvent } from '@/services/api'

export interface ReasoningStep {
  type: 'reasoning' | 'tool_call'
  content: string
  agent?: string
  tool?: string
}

export interface Message {
  id?: number
  role: 'user' | 'assistant'
  content: string
  createdAt: string
  reasoning?: ReasoningStep[]
  isStreaming?: boolean
  reasoningOpen?: boolean
}

export const useChatbotStore = defineStore('chatbot', () => {
  const isOpen = ref(false)
  const sessionId = ref<number | null>(null)
  const messages = ref<Message[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  function toggle() { isOpen.value = !isOpen.value }
  function open() { isOpen.value = true }
  function close() { isOpen.value = false }

  async function initSession() {
    if (sessionId.value) return
    const { data } = await chatApi.createSession({ title: 'Trợ lý ảo', sessionType: 'General' })
    sessionId.value = data.id
    messages.value = [{
      role: 'assistant',
      content: 'Xin chào! Tôi là trợ lý ảo MGSPlus. Tôi có thể giúp bạn tư vấn y tế, giải đáp về bảo hiểm y tế, hỗ trợ đặt lịch khám và đọc hồ sơ y tế. Bạn cần hỗ trợ gì?',
      createdAt: new Date().toISOString()
    }]
  }

  async function sendMessage(content: string) {
    if (!content.trim() || loading.value) return

    // Ensure session exists before streaming
    if (!sessionId.value) {
      try {
        const { data } = await chatApi.createSession({ title: 'Trợ lý ảo', sessionType: 'General' })
        sessionId.value = data.id
      } catch {
        error.value = 'Không thể tạo phiên chat. Vui lòng thử lại.'
        return
      }
    }

    messages.value.push({ role: 'user', content, createdAt: new Date().toISOString() })
    loading.value = true
    error.value = null

    // Add streaming placeholder for assistant reply
    const assistantIdx = messages.value.length
    messages.value.push({
      role: 'assistant',
      content: '',
      createdAt: new Date().toISOString(),
      reasoning: [],
      isStreaming: true,
      reasoningOpen: true
    })

    const abortController = new AbortController()

    try {
      await chatApi.streamMessage(
        sessionId.value,
        content,
        (event: StreamEvent) => _handleStreamEvent(event, assistantIdx),
        abortController.signal
      )
    } catch (e: unknown) {
      if (e instanceof Error && e.name === 'AbortError') return
      // Stream failed — show fallback in the placeholder
      if (!messages.value[assistantIdx].content) {
        messages.value[assistantIdx].content = 'Không thể kết nối tới trợ lý. Vui lòng thử lại.'
      }
      error.value = 'Kết nối bị gián đoạn.'
    } finally {
      messages.value[assistantIdx].isStreaming = false
      messages.value[assistantIdx].reasoningOpen = false
      loading.value = false
    }
  }

  function _handleStreamEvent(event: StreamEvent, idx: number) {
    const msg = messages.value[idx]
    if (!msg) return

    switch (event.type) {
      case 'session':
        if (event.sessionId) sessionId.value = event.sessionId
        break

      case 'reasoning':
        msg.reasoning ??= []
        if (event.content) {
          msg.reasoning.push({ type: 'reasoning', content: event.content, agent: event.agent })
        }
        break

      case 'tool_call':
        msg.reasoning ??= []
        msg.reasoning.push({
          type: 'tool_call',
          content: event.content ?? '',
          tool: event.tool,
          agent: event.agent
        })
        break

      case 'response_chunk':
        msg.content += event.content ?? ''
        break

      case 'answer':
        msg.content = event.content ?? ''
        break

      case 'complete':
        if (event.messageId) msg.id = event.messageId
        msg.isStreaming = false
        msg.reasoningOpen = false
        break

      case 'error':
        if (!msg.content) msg.content = 'Đã có lỗi xảy ra. Vui lòng thử lại.'
        msg.isStreaming = false
        break
    }
  }

  function toggleReasoning(msgIdx: number) {
    const msg = messages.value[msgIdx]
    if (msg) msg.reasoningOpen = !msg.reasoningOpen
  }

  function reset() {
    sessionId.value = null
    messages.value = []
    isOpen.value = false
  }

  return {
    isOpen, sessionId, messages, loading, error,
    toggle, open, close, initSession, sendMessage, toggleReasoning, reset
  }
})
