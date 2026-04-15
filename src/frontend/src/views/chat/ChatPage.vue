<template>
  <div class="flex h-[calc(100vh-4rem)] overflow-hidden">
    <!-- Sidebar: danh sách phiên chat -->
    <aside class="hidden sm:flex flex-col w-64 bg-white border-r border-gray-200 shrink-0">
      <div class="p-4 border-b border-gray-100 flex items-center justify-between">
        <h2 class="font-semibold text-gray-800 text-sm">Lịch sử chat</h2>
        <button @click="startNewSession"
          class="text-xs bg-blue-600 text-white px-3 py-1 rounded-lg hover:bg-blue-700 transition-colors">
          + Mới
        </button>
      </div>
      <div class="flex-1 overflow-y-auto">
        <div v-if="loadingSessions" class="p-4 space-y-2">
          <div v-for="i in 4" :key="i" class="bg-gray-100 rounded h-10 animate-pulse"></div>
        </div>
        <ul v-else class="divide-y divide-gray-100">
          <li v-for="s in sessions" :key="s.id"
            @click="loadSession(s.id)"
            :class="['px-4 py-3 cursor-pointer hover:bg-gray-50 transition-colors',
                     currentSessionId === s.id ? 'bg-blue-50 border-l-2 border-blue-500' : '']">
            <p class="text-sm font-medium text-gray-800 truncate">{{ s.title }}</p>
            <p class="text-xs text-gray-400 mt-0.5">{{ formatDate(s.updatedAt) }}</p>
          </li>
        </ul>
        <p v-if="!loadingSessions && sessions.length === 0" class="p-4 text-sm text-gray-400 text-center">
          Chưa có cuộc trò chuyện nào
        </p>
      </div>
    </aside>

    <!-- Main chat area -->
    <div class="flex flex-col flex-1 min-w-0 bg-gray-50">
      <!-- Header -->
      <div class="bg-white border-b border-gray-200 px-4 py-3 flex items-center gap-3 shrink-0">
        <div class="w-8 h-8 bg-blue-600 rounded-full flex items-center justify-center shrink-0">
          <svg class="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z" />
          </svg>
        </div>
        <div>
          <h1 class="font-semibold text-gray-900 text-sm">MGSPlus AI</h1>
          <p class="text-xs text-gray-400">Trợ lý y tế thông minh</p>
        </div>
        <!-- Mobile: new session button -->
        <button @click="startNewSession"
          class="sm:hidden ml-auto text-xs bg-blue-600 text-white px-3 py-1 rounded-lg hover:bg-blue-700 transition-colors">
          + Mới
        </button>
      </div>

      <!-- Messages -->
      <div ref="messagesEl" class="flex-1 overflow-y-auto px-4 py-4 space-y-4">
        <!-- Welcome state -->
        <div v-if="messages.length === 0 && !loadingMessages"
          class="flex flex-col items-center justify-center h-full text-center gap-4 pb-20">
          <div class="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center">
            <svg class="w-8 h-8 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z" />
            </svg>
          </div>
          <div>
            <h2 class="text-lg font-semibold text-gray-900">Xin chào, {{ auth.user?.firstName }}!</h2>
            <p class="text-sm text-gray-500 mt-1">Tôi có thể giúp bạn tư vấn y tế, bảo hiểm, đặt lịch khám và đọc hồ sơ bệnh án.</p>
          </div>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-2 w-full max-w-md">
            <button v-for="s in suggestions" :key="s" @click="sendSuggestion(s)"
              class="text-left text-sm bg-white border border-gray-200 rounded-xl px-4 py-3 hover:bg-blue-50 hover:border-blue-300 transition-colors text-gray-700">
              {{ s }}
            </button>
          </div>
        </div>

        <!-- Loading messages skeleton -->
        <div v-if="loadingMessages" class="space-y-3">
          <div v-for="i in 3" :key="i" :class="['flex', i % 2 === 0 ? 'justify-end' : 'justify-start']">
            <div class="bg-gray-200 rounded-2xl h-10 w-48 animate-pulse"></div>
          </div>
        </div>

        <!-- Message list -->
        <template v-for="msg in messages" :key="msg.id">
          <!-- User message -->
          <div v-if="msg.role === 'user'" class="flex justify-end">
            <div class="max-w-[75%] bg-blue-600 text-white rounded-2xl rounded-tr-sm px-4 py-2.5 text-sm">
              {{ msg.content }}
            </div>
          </div>
          <!-- Assistant message -->
          <div v-else class="flex gap-2">
            <div class="w-7 h-7 bg-blue-100 rounded-full flex items-center justify-center shrink-0 mt-0.5">
              <svg class="w-3.5 h-3.5 text-blue-600" fill="currentColor" viewBox="0 0 20 20">
                <path d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <div class="max-w-[75%] bg-white border border-gray-200 rounded-2xl rounded-tl-sm px-4 py-2.5 text-sm text-gray-800 whitespace-pre-wrap shadow-sm">
              {{ msg.content }}
            </div>
          </div>
        </template>

        <!-- Streaming indicator (reasoning / typing) -->
        <div v-if="streamingState.active" class="flex gap-2">
          <div class="w-7 h-7 bg-blue-100 rounded-full flex items-center justify-center shrink-0 mt-0.5">
            <svg class="w-3.5 h-3.5 text-blue-600 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"></path>
            </svg>
          </div>
          <div class="max-w-[75%] bg-white border border-gray-200 rounded-2xl rounded-tl-sm px-4 py-2.5 text-sm shadow-sm">
            <p v-if="streamingState.partial" class="text-gray-800 whitespace-pre-wrap">{{ streamingState.partial }}</p>
            <p v-else-if="streamingState.reasoning" class="text-gray-400 italic text-xs">{{ streamingState.reasoning }}</p>
            <span v-else class="flex gap-1 items-center text-gray-400">
              <span class="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce" style="animation-delay:0s"></span>
              <span class="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce" style="animation-delay:.15s"></span>
              <span class="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce" style="animation-delay:.3s"></span>
            </span>
          </div>
        </div>
      </div>

      <!-- Input area -->
      <div class="bg-white border-t border-gray-200 px-4 py-3 shrink-0">
        <p v-if="sendError" class="text-xs text-red-500 mb-2">{{ sendError }}</p>
        <form @submit.prevent="sendMessage" class="flex gap-2">
          <textarea
            v-model="input"
            @keydown.enter.exact.prevent="sendMessage"
            rows="1"
            placeholder="Nhập câu hỏi của bạn..."
            :disabled="streamingState.active"
            class="flex-1 resize-none rounded-xl border border-gray-300 px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent disabled:opacity-50 overflow-hidden"
            style="min-height:42px; max-height:120px"
            ref="inputEl"
            @input="autoResize"
          ></textarea>
          <button type="submit"
            :disabled="!input.trim() || streamingState.active"
            class="shrink-0 w-10 h-10 bg-blue-600 text-white rounded-xl hover:bg-blue-700 transition-colors disabled:opacity-40 flex items-center justify-center self-end">
            <svg v-if="!streamingState.active" class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8" />
            </svg>
            <svg v-else @click.prevent="cancelStream"
              class="w-4 h-4 cursor-pointer" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </form>
        <p class="text-xs text-gray-400 mt-1.5 text-center">
          Chỉ mang tính tư vấn — không thay thế ý kiến bác sĩ chuyên khoa.
        </p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, nextTick, onMounted } from 'vue'
import { chatApi, type StreamEvent } from '@/services/api'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()

// ── State ────────────────────────────────────────────────────
interface Session { id: number; title: string; updatedAt: string; messageCount: number }
interface Message { id: number; role: string; content: string; createdAt: string }

const sessions = ref<Session[]>([])
const messages = ref<Message[]>([])
const currentSessionId = ref<number | null>(null)
const loadingSessions = ref(true)
const loadingMessages = ref(false)
const input = ref('')
const sendError = ref('')

const streamingState = ref<{ active: boolean; partial: string; reasoning: string }>({
  active: false, partial: '', reasoning: ''
})

const messagesEl = ref<HTMLElement | null>(null)
const inputEl = ref<HTMLTextAreaElement | null>(null)
let abortController: AbortController | null = null

const suggestions = [
  'Quyền lợi bảo hiểm y tế BHYT?',
  'Tôi muốn đặt lịch khám bệnh',
  'Giải thích kết quả xét nghiệm',
  'Triệu chứng cúm và cách điều trị'
]

// ── Lifecycle ─────────────────────────────────────────────────
onMounted(async () => {
  await loadSessions()
})

// ── Sessions ──────────────────────────────────────────────────
async function loadSessions() {
  loadingSessions.value = true
  try {
    const { data } = await chatApi.getSessions()
    sessions.value = data
  } catch {
    // ignore — user will see empty state
  } finally {
    loadingSessions.value = false
  }
}

async function loadSession(id: number) {
  if (currentSessionId.value === id) return
  loadingMessages.value = true
  currentSessionId.value = id
  messages.value = []
  try {
    const { data } = await chatApi.getSession(id)
    messages.value = data.messages ?? []
    await scrollToBottom()
  } catch {
    sendError.value = 'Không thể tải phiên chat.'
  } finally {
    loadingMessages.value = false
  }
}

async function startNewSession() {
  try {
    const { data } = await chatApi.createSession({ title: 'Cuộc trò chuyện mới' })
    sessions.value.unshift({ id: data.id, title: data.title, updatedAt: data.updatedAt, messageCount: 0 })
    currentSessionId.value = data.id
    messages.value = []
    sendError.value = ''
    await nextTick()
    inputEl.value?.focus()
  } catch {
    sendError.value = 'Không thể tạo phiên chat mới.'
  }
}

// ── Messaging ─────────────────────────────────────────────────
async function sendMessage() {
  const content = input.value.trim()
  if (!content || streamingState.value.active) return

  // Ensure we have a session
  if (!currentSessionId.value) {
    await startNewSession()
    if (!currentSessionId.value) return
  }

  sendError.value = ''
  input.value = ''
  resetTextarea()

  // Optimistically add user message
  const tmpId = Date.now()
  messages.value.push({ id: tmpId, role: 'user', content, createdAt: new Date().toISOString() })
  await scrollToBottom()

  // Start streaming
  streamingState.value = { active: true, partial: '', reasoning: '' }
  abortController = new AbortController()

  try {
    await chatApi.streamMessage(
      currentSessionId.value,
      content,
      handleStreamEvent,
      abortController.signal
    )
  } catch (err: any) {
    if (err.name !== 'AbortError') {
      sendError.value = 'Lỗi kết nối. Vui lòng thử lại.'
      // Remove optimistic message on error
      messages.value = messages.value.filter(m => m.id !== tmpId)
    }
  } finally {
    if (streamingState.value.partial) {
      messages.value.push({
        id: Date.now(),
        role: 'assistant',
        content: streamingState.value.partial,
        createdAt: new Date().toISOString()
      })
    }
    streamingState.value = { active: false, partial: '', reasoning: '' }
    await scrollToBottom()
    // Refresh session list to update updatedAt
    loadSessions()
  }
}

function handleStreamEvent(event: StreamEvent) {
  switch (event.type) {
    case 'reasoning':
      streamingState.value.reasoning = event.content ?? ''
      break
    case 'response_chunk':
    case 'answer':
      streamingState.value.partial += event.content ?? ''
      streamingState.value.reasoning = ''
      nextTick(scrollToBottom)
      break
    case 'complete':
      // Final message committed in finally block
      break
    case 'error':
      sendError.value = event.content ?? 'Có lỗi xảy ra.'
      break
  }
}

function sendSuggestion(text: string) {
  input.value = text
  sendMessage()
}

function cancelStream() {
  abortController?.abort()
}

// ── Helpers ───────────────────────────────────────────────────
async function scrollToBottom() {
  await nextTick()
  if (messagesEl.value) {
    messagesEl.value.scrollTop = messagesEl.value.scrollHeight
  }
}

function autoResize(e: Event) {
  const el = e.target as HTMLTextAreaElement
  el.style.height = 'auto'
  el.style.height = Math.min(el.scrollHeight, 120) + 'px'
}

function resetTextarea() {
  if (inputEl.value) {
    inputEl.value.style.height = '42px'
  }
}

function formatDate(iso: string) {
  const d = new Date(iso)
  const now = new Date()
  const diffH = (now.getTime() - d.getTime()) / 3600000
  if (diffH < 24) return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
  return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' })
}
</script>
