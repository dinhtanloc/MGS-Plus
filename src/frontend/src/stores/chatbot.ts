import { defineStore } from 'pinia'
import { ref } from 'vue'
import { chatApi } from '@/services/api'

export interface Message {
  id?: number
  role: 'user' | 'assistant'
  content: string
  createdAt: string
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

    if (!sessionId.value) {
      // Use quick chat for guests
      messages.value.push({ role: 'user', content, createdAt: new Date().toISOString() })
      loading.value = true
      error.value = null
      try {
        const { data } = await chatApi.quickChat(content)
        sessionId.value = data.sessionId
        messages.value.push({
          role: 'assistant',
          content: data.response.assistantMessage.content,
          createdAt: data.response.assistantMessage.createdAt
        })
      } catch (e: any) {
        error.value = 'Không thể kết nối tới trợ lý. Vui lòng thử lại.'
      } finally {
        loading.value = false
      }
      return
    }

    messages.value.push({ role: 'user', content, createdAt: new Date().toISOString() })
    loading.value = true
    error.value = null

    try {
      const { data } = await chatApi.sendMessage(sessionId.value, { content })
      messages.value.push({
        id: data.assistantMessage.id,
        role: 'assistant',
        content: data.assistantMessage.content,
        createdAt: data.assistantMessage.createdAt
      })
    } catch (e: any) {
      error.value = 'Không thể kết nối tới trợ lý. Vui lòng thử lại.'
    } finally {
      loading.value = false
    }
  }

  function reset() {
    sessionId.value = null
    messages.value = []
    isOpen.value = false
  }

  return { isOpen, sessionId, messages, loading, error, toggle, open, close, initSession, sendMessage, reset }
})
