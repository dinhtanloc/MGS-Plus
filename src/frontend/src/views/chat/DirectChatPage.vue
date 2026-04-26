<template>
  <div class="flex h-screen bg-gray-50 overflow-hidden">

    <!-- Sessions sidebar -->
    <div :class="[
      'flex-shrink-0 bg-white border-r flex flex-col transition-all duration-300',
      showSidebar ? 'w-72' : 'w-0 overflow-hidden'
    ]">
      <div class="p-4 border-b flex items-center justify-between">
        <h2 class="font-semibold text-gray-900">Tin nhắn</h2>
        <button @click="openNewChat = true"
          class="p-1.5 rounded-lg bg-blue-600 text-white hover:bg-blue-700 text-sm">
          ✎ Mới
        </button>
      </div>

      <div v-if="loadingSessions" class="flex-1 flex items-center justify-center text-gray-400 text-sm">
        Đang tải...
      </div>
      <div v-else-if="sessions.length === 0" class="flex-1 flex flex-col items-center justify-center text-gray-400 p-4 text-center">
        <div class="text-4xl mb-2">💬</div>
        <p class="text-sm">Chưa có cuộc trò chuyện nào</p>
      </div>
      <div v-else class="flex-1 overflow-y-auto">
        <button v-for="s in sessions" :key="s.id"
          @click="selectSession(s.id)"
          :class="[
            'w-full p-3 text-left flex items-center gap-3 hover:bg-gray-50 transition-colors border-b',
            activeSessionId === s.id ? 'bg-blue-50 border-l-4 border-l-blue-600' : ''
          ]">
          <div class="w-10 h-10 rounded-full bg-gradient-to-br from-blue-400 to-indigo-500 flex items-center justify-center text-white font-bold text-sm flex-shrink-0">
            {{ initials(isDoctor ? s.patientName : s.doctorName) }}
          </div>
          <div class="flex-1 min-w-0">
            <div class="flex items-center justify-between">
              <span class="font-medium text-gray-900 text-sm truncate">
                {{ isDoctor ? s.patientName : ('BS. ' + s.doctorName) }}
              </span>
              <span class="text-xs text-gray-400 flex-shrink-0 ml-1">
                {{ formatRelative(s.updatedAt) }}
              </span>
            </div>
            <div class="text-xs text-gray-500 truncate">{{ isDoctor ? s.doctorSpecialty : s.doctorSpecialty }}</div>
          </div>
          <div v-if="s.unreadCount > 0"
            class="w-5 h-5 bg-red-500 text-white text-xs rounded-full flex items-center justify-center flex-shrink-0">
            {{ s.unreadCount }}
          </div>
        </button>
      </div>
    </div>

    <!-- Chat area -->
    <div class="flex-1 flex flex-col min-w-0">

      <!-- Header -->
      <div class="bg-white border-b px-4 py-3 flex items-center gap-3">
        <button @click="showSidebar = !showSidebar" class="p-1.5 rounded-lg hover:bg-gray-100 text-gray-600">
          ☰
        </button>
        <div v-if="activeSession" class="flex items-center gap-3 flex-1">
          <div class="w-9 h-9 rounded-full bg-gradient-to-br from-blue-400 to-indigo-500 flex items-center justify-center text-white font-bold text-sm">
            {{ initials(isDoctor ? activeSession.patientName : activeSession.doctorName) }}
          </div>
          <div>
            <div class="font-semibold text-gray-900 text-sm">
              {{ isDoctor ? activeSession.patientName : ('BS. ' + activeSession.doctorName) }}
            </div>
            <div class="text-xs text-green-500">Đang hoạt động</div>
          </div>
        </div>
        <div v-else class="flex-1">
          <div class="font-semibold text-gray-900">Chat với bác sĩ</div>
        </div>
      </div>

      <!-- Messages -->
      <div v-if="!activeSessionId" class="flex-1 flex flex-col items-center justify-center text-gray-400 p-8 text-center">
        <div class="text-6xl mb-4">💬</div>
        <h3 class="text-lg font-semibold text-gray-700 mb-2">Chọn cuộc trò chuyện</h3>
        <p class="text-sm mb-4">Chọn từ danh sách bên trái hoặc bắt đầu cuộc trò chuyện mới</p>
        <button @click="openNewChat = true"
          class="px-5 py-2.5 bg-blue-600 text-white rounded-xl font-medium hover:bg-blue-700 transition-colors">
          + Bắt đầu trò chuyện
        </button>
      </div>

      <div v-else class="flex-1 overflow-y-auto p-4 space-y-4" ref="messagesEl">
        <div v-if="loadingMessages" class="text-center text-gray-400 py-4">Đang tải tin nhắn...</div>

        <div v-for="msg in messages" :key="msg.id"
          :class="['flex gap-3', msg.senderId === currentUserId ? 'flex-row-reverse' : '']">
          <div :class="[
            'w-8 h-8 rounded-full flex items-center justify-center text-white text-xs font-bold flex-shrink-0',
            msg.senderId === currentUserId ? 'bg-blue-500' : 'bg-indigo-500'
          ]">
            {{ initials(msg.senderName) }}
          </div>
          <div :class="['max-w-xs lg:max-w-md', msg.senderId === currentUserId ? 'items-end' : 'items-start', 'flex flex-col']">
            <div :class="[
              'px-4 py-2.5 rounded-2xl text-sm leading-relaxed shadow-sm',
              msg.senderId === currentUserId
                ? 'bg-blue-600 text-white rounded-br-md'
                : 'bg-white text-gray-900 border rounded-bl-md'
            ]">
              {{ msg.content }}
            </div>
            <div class="text-xs text-gray-400 mt-1 flex items-center gap-1">
              {{ formatTime(msg.sentAt) }}
              <span v-if="msg.senderId === currentUserId">
                {{ msg.isRead ? '✓✓' : '✓' }}
              </span>
            </div>
          </div>
        </div>

        <!-- Typing indicator -->
        <div v-if="isTyping" class="flex gap-3">
          <div class="w-8 h-8 rounded-full bg-indigo-500 flex items-center justify-center text-white text-xs font-bold">...</div>
          <div class="bg-white border px-4 py-3 rounded-2xl rounded-bl-md">
            <div class="flex gap-1">
              <div v-for="i in 3" :key="i"
                :style="{ animationDelay: `${(i-1) * 0.15}s` }"
                class="w-2 h-2 bg-gray-400 rounded-full animate-bounce"></div>
            </div>
          </div>
        </div>
      </div>

      <!-- Input -->
      <div v-if="activeSessionId" class="bg-white border-t p-4">
        <form @submit.prevent="sendMessage" class="flex gap-3">
          <input
            v-model="newMessage"
            type="text"
            placeholder="Nhập tin nhắn..."
            maxlength="4000"
            class="flex-1 border-2 rounded-xl px-4 py-2.5 focus:outline-none focus:border-blue-500 text-gray-900"
          />
          <button type="submit" :disabled="!newMessage.trim() || !isConnected"
            class="px-5 py-2.5 bg-blue-600 text-white rounded-xl font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors">
            Gửi
          </button>
        </form>
        <div v-if="!isConnected" class="text-xs text-amber-600 mt-1 text-center">
          ⚡ Đang kết nối...
        </div>
      </div>
    </div>

    <!-- New chat modal -->
    <Teleport to="body">
      <div v-if="openNewChat" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
        <div class="bg-white rounded-2xl w-full max-w-md shadow-2xl">
          <div class="p-6">
            <h3 class="text-lg font-bold text-gray-900 mb-4">Bắt đầu trò chuyện</h3>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">Chọn bác sĩ</label>
              <select v-model="newChatDoctorId"
                class="w-full border-2 rounded-xl px-3 py-2.5 focus:outline-none focus:border-blue-500">
                <option :value="undefined">-- Chọn bác sĩ --</option>
                <option v-for="d in allDoctors" :key="d.id" :value="d.id">
                  BS. {{ d.name }} — {{ d.specialty }}
                </option>
              </select>
            </div>
            <div class="flex gap-3 justify-end mt-5">
              <button @click="openNewChat = false; newChatDoctorId = undefined"
                class="px-4 py-2 rounded-xl border text-gray-700 hover:bg-gray-50">Hủy</button>
              <button @click="createNewChat" :disabled="!newChatDoctorId || creatingChat"
                class="px-4 py-2 rounded-xl bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50 font-medium">
                {{ creatingChat ? 'Đang tạo...' : 'Bắt đầu' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import * as signalR from '@microsoft/signalr'
import { directChatApi, appointmentApi } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import type { DirectChatSessionDto, DirectMessageDto } from '@/types/api'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const sessions = ref<DirectChatSessionDto[]>([])
const messages = ref<DirectMessageDto[]>([])
const activeSessionId = ref<number | null>(null)
const activeSession = computed(() => sessions.value.find(s => s.id === activeSessionId.value))

const loadingSessions = ref(false)
const loadingMessages = ref(false)
const newMessage = ref('')
const isTyping = ref(false)
const isConnected = ref(false)
const showSidebar = ref(true)
const messagesEl = ref<HTMLElement | null>(null)

const openNewChat = ref(false)
const newChatDoctorId = ref<number | undefined>(undefined)
const creatingChat = ref(false)
const allDoctors = ref<any[]>([])

const currentUserId = computed(() => auth.user?.id ?? 0)
const isDoctor = computed(() => auth.user?.role === 'Doctor')

let connection: signalR.HubConnection | null = null

// ── SignalR ───────────────────────────────────────────────────────────────────
async function connectSignalR() {
  const token = localStorage.getItem('token')
  const wsUrl = import.meta.env.VITE_WS_URL || 'ws://localhost:5001'
  const baseUrl = wsUrl.replace('ws://', 'http://').replace('wss://', 'https://')

  connection = new signalR.HubConnectionBuilder()
    .withUrl(`${baseUrl}/hubs/direct-chat`, {
      accessTokenFactory: () => token ?? ''
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Warning)
    .build()

  connection.on('ReceiveMessage', (msg: DirectMessageDto) => {
    if (msg.sessionId === activeSessionId.value) {
      messages.value.push(msg)
      scrollToBottom()
    }
    // Update session order
    const idx = sessions.value.findIndex(s => s.id === msg.sessionId)
    if (idx >= 0) {
      const s = sessions.value.splice(idx, 1)[0]
      if (msg.senderId !== currentUserId.value) s.unreadCount++
      sessions.value.unshift(s)
    }
  })

  connection.onreconnected(() => {
    isConnected.value = true
    if (activeSessionId.value) connection?.invoke('JoinSession', activeSessionId.value)
  })

  connection.onclose(() => { isConnected.value = false })

  try {
    await connection.start()
    isConnected.value = true
  } catch (e) {
    console.error('SignalR connect failed:', e)
  }
}

async function joinSession(id: number) {
  if (connection?.state === signalR.HubConnectionState.Connected) {
    await connection.invoke('JoinSession', id).catch(console.error)
  }
}

async function sendMessage() {
  if (!newMessage.value.trim() || !activeSessionId.value) return
  if (!isConnected.value) return

  const text = newMessage.value.trim()
  newMessage.value = ''
  try {
    await connection!.invoke('SendMessage', activeSessionId.value, text)
  } catch (e: any) {
    console.error(e)
    newMessage.value = text
  }
}

// ── Data ──────────────────────────────────────────────────────────────────────
async function loadSessions() {
  loadingSessions.value = true
  try {
    const { data } = await directChatApi.getSessions()
    sessions.value = data
  } catch { /* ignore */ }
  finally { loadingSessions.value = false }
}

async function selectSession(id: number) {
  activeSessionId.value = id
  router.replace({ name: 'direct-chat-session', params: { sessionId: id } })
  await loadMessages(id)
  await joinSession(id)
  const s = sessions.value.find(s => s.id === id)
  if (s) s.unreadCount = 0
}

async function loadMessages(id: number) {
  loadingMessages.value = true
  messages.value = []
  try {
    const { data } = await directChatApi.getMessages(id)
    messages.value = data.data
    await nextTick()
    scrollToBottom()
  } catch { /* ignore */ }
  finally { loadingMessages.value = false }
}

async function createNewChat() {
  if (!newChatDoctorId.value) return
  creatingChat.value = true
  try {
    const { data } = await directChatApi.getOrCreate(newChatDoctorId.value)
    openNewChat.value = false
    newChatDoctorId.value = undefined
    await loadSessions()
    await selectSession(data.id)
  } catch (e) {
    console.error(e)
  } finally {
    creatingChat.value = false
  }
}

function scrollToBottom() {
  nextTick(() => {
    if (messagesEl.value) messagesEl.value.scrollTop = messagesEl.value.scrollHeight
  })
}

function initials(name: string) {
  return (name || '?').split(' ').slice(-2).map((n: string) => n[0]).join('').toUpperCase()
}

function formatTime(dt: string) {
  return new Date(dt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
}

function formatRelative(dt: string) {
  const diff = Date.now() - new Date(dt).getTime()
  if (diff < 60000) return 'vừa xong'
  if (diff < 3600000) return `${Math.floor(diff / 60000)} phút`
  if (diff < 86400000) return `${Math.floor(diff / 3600000)} giờ`
  return new Date(dt).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' })
}

onMounted(async () => {
  await connectSignalR()
  await loadSessions()

  // Load doctors for new chat modal
  if (!isDoctor.value) {
    try {
      const { data } = await appointmentApi.getDoctors()
      allDoctors.value = data
    } catch { /* ignore */ }
  }

  // Auto-select session from route
  const sessionIdParam = route.params.sessionId
  if (sessionIdParam) {
    const id = parseInt(sessionIdParam as string)
    if (!isNaN(id)) await selectSession(id)
  }
})

onUnmounted(() => {
  connection?.stop()
})

watch(() => route.params.sessionId, async (newId) => {
  if (newId) {
    const id = parseInt(newId as string)
    if (!isNaN(id) && id !== activeSessionId.value) await selectSession(id)
  }
})
</script>
