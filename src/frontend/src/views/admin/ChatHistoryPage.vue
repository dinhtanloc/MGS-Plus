<template>
  <div class="space-y-5">

    <!-- Stats bar -->
    <div class="grid grid-cols-3 gap-4">
      <div v-for="s in summaryCards" :key="s.label"
        class="bg-white rounded-xl border p-4 flex items-center gap-3">
        <div :class="['w-10 h-10 rounded-xl flex items-center justify-center text-xl', s.bg]">{{ s.icon }}</div>
        <div>
          <p class="text-xl font-bold text-gray-900">{{ s.value }}</p>
          <p class="text-xs text-gray-500">{{ s.label }}</p>
        </div>
      </div>
    </div>

    <!-- Filters -->
    <div class="bg-white rounded-xl border p-4 flex flex-wrap items-center gap-3">
      <input v-model="search" type="text" placeholder="Tìm tên bác sĩ, bệnh nhân..."
        class="border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 flex-1 min-w-48" />
      <select v-model="filterType" class="border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
        <option value="all">Tất cả loại</option>
        <option value="direct">Chat bác sĩ - bệnh nhân</option>
        <option value="ai">Tư vấn AI</option>
      </select>
      <select v-model="filterStatus" class="border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
        <option value="">Tất cả trạng thái</option>
        <option value="Active">Đang hoạt động</option>
        <option value="Closed">Đã đóng</option>
      </select>
    </div>

    <!-- Tabs -->
    <div class="bg-white rounded-2xl border overflow-hidden">
      <div class="border-b flex">
        <button v-for="tab in tabs" :key="tab.key"
          @click="activeTab = tab.key"
          :class="['px-5 py-3 text-sm font-medium transition-colors border-b-2 -mb-px',
            activeTab === tab.key
              ? 'border-blue-600 text-blue-700 bg-blue-50'
              : 'border-transparent text-gray-500 hover:text-gray-700']">
          {{ tab.label }}
          <span class="ml-1.5 text-xs bg-gray-100 text-gray-600 px-1.5 py-0.5 rounded-full">
            {{ tab.count }}
          </span>
        </button>
      </div>

      <!-- Direct chats table -->
      <div v-if="activeTab === 'direct'">
        <div v-if="loadingDirect" class="p-8 text-center text-gray-400">
          <div class="animate-spin w-6 h-6 border-2 border-blue-500 border-t-transparent rounded-full mx-auto mb-2"></div>
          Đang tải...
        </div>
        <div v-else-if="filteredDirectChats.length === 0" class="p-8 text-center text-gray-400">
          <div class="text-4xl mb-2">💬</div>
          <p>Không có phiên chat nào</p>
        </div>
        <div v-else>
          <table class="w-full">
            <thead class="bg-gray-50 border-b">
              <tr>
                <th class="text-left px-5 py-3 text-xs font-medium text-gray-500 uppercase">Bệnh nhân</th>
                <th class="text-left px-5 py-3 text-xs font-medium text-gray-500 uppercase">Bác sĩ</th>
                <th class="text-left px-5 py-3 text-xs font-medium text-gray-500 uppercase">Tin nhắn</th>
                <th class="text-left px-5 py-3 text-xs font-medium text-gray-500 uppercase">Trạng thái</th>
                <th class="text-left px-5 py-3 text-xs font-medium text-gray-500 uppercase">Cập nhật</th>
                <th class="px-5 py-3"></th>
              </tr>
            </thead>
            <tbody class="divide-y">
              <tr v-for="chat in filteredDirectChats" :key="chat.id"
                class="hover:bg-gray-50 transition-colors">
                <td class="px-5 py-3">
                  <div class="flex items-center gap-2">
                    <div class="w-7 h-7 bg-teal-100 text-teal-700 rounded-full flex items-center justify-center text-xs font-bold">
                      {{ initials(chat.patientName) }}
                    </div>
                    <span class="text-sm font-medium text-gray-900">{{ chat.patientName }}</span>
                  </div>
                </td>
                <td class="px-5 py-3">
                  <div>
                    <p class="text-sm text-gray-900">BS. {{ chat.doctorName }}</p>
                    <p class="text-xs text-blue-600">{{ chat.specialty }}</p>
                  </div>
                </td>
                <td class="px-5 py-3">
                  <div class="flex items-center gap-1.5">
                    <span class="text-sm font-medium text-gray-900">{{ chat.msgCount }}</span>
                    <span class="text-xs text-gray-400">tin</span>
                  </div>
                </td>
                <td class="px-5 py-3">
                  <span :class="['text-xs px-2 py-1 rounded-full font-medium',
                    chat.status === 'Active' ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500']">
                    {{ chat.status === 'Active' ? 'Đang hoạt động' : 'Đã đóng' }}
                  </span>
                </td>
                <td class="px-5 py-3 text-sm text-gray-400">{{ formatDate(chat.updatedAt) }}</td>
                <td class="px-5 py-3">
                  <button @click="openDirectChat(chat)"
                    class="text-xs text-blue-600 hover:underline bg-blue-50 px-2 py-1 rounded-lg">
                    Xem
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- AI chat sessions table -->
      <div v-if="activeTab === 'ai'">
        <div v-if="loadingAi" class="p-8 text-center text-gray-400">
          <div class="animate-spin w-6 h-6 border-2 border-blue-500 border-t-transparent rounded-full mx-auto mb-2"></div>
          Đang tải...
        </div>
        <div v-else-if="filteredAiChats.length === 0" class="p-8 text-center text-gray-400">
          <div class="text-4xl mb-2">🤖</div>
          <p>Không có phiên chat AI nào</p>
        </div>
        <div v-else>
          <table class="w-full">
            <thead class="bg-gray-50 border-b">
              <tr>
                <th class="text-left px-5 py-3 text-xs font-medium text-gray-500 uppercase">Người dùng</th>
                <th class="text-left px-5 py-3 text-xs font-medium text-gray-500 uppercase">Tiêu đề phiên</th>
                <th class="text-left px-5 py-3 text-xs font-medium text-gray-500 uppercase">Loại</th>
                <th class="text-left px-5 py-3 text-xs font-medium text-gray-500 uppercase">Tin nhắn</th>
                <th class="text-left px-5 py-3 text-xs font-medium text-gray-500 uppercase">Ngày</th>
              </tr>
            </thead>
            <tbody class="divide-y">
              <tr v-for="chat in filteredAiChats" :key="chat.id"
                class="hover:bg-gray-50 transition-colors">
                <td class="px-5 py-3">
                  <div class="flex items-center gap-2">
                    <div class="w-7 h-7 bg-indigo-100 text-indigo-700 rounded-full flex items-center justify-center text-xs font-bold">
                      {{ initials(chat.userName) }}
                    </div>
                    <span class="text-sm font-medium text-gray-900">{{ chat.userName }}</span>
                  </div>
                </td>
                <td class="px-5 py-3 text-sm text-gray-700 max-w-xs truncate">{{ chat.title }}</td>
                <td class="px-5 py-3">
                  <span class="text-xs bg-indigo-100 text-indigo-700 px-2 py-1 rounded-full">
                    {{ sessionTypeLabel(chat.sessionType) }}
                  </span>
                </td>
                <td class="px-5 py-3 text-sm text-gray-900">{{ chat.msgCount }}</td>
                <td class="px-5 py-3 text-sm text-gray-400">{{ formatDate(chat.updatedAt) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Chat viewer modal (direct chats) -->
    <Teleport to="body">
      <div v-if="selectedChat" class="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
        <div class="bg-white rounded-2xl w-full max-w-lg max-h-[80vh] flex flex-col shadow-2xl overflow-hidden">
          <div class="px-5 py-4 border-b flex items-center justify-between bg-gray-50">
            <div>
              <p class="font-semibold text-gray-900">
                {{ selectedChat.patientName }} ↔ BS. {{ selectedChat.doctorName }}
              </p>
              <p class="text-xs text-gray-500">{{ selectedChat.specialty }} · {{ selectedChat.msgCount }} tin nhắn</p>
            </div>
            <button @click="selectedChat = null" class="text-gray-400 hover:text-gray-600 p-1">✕</button>
          </div>
          <div class="flex-1 overflow-y-auto p-4 space-y-3 bg-gray-50">
            <div v-if="loadingMessages" class="text-center text-gray-400 text-sm py-4">Đang tải tin nhắn...</div>
            <div v-for="msg in chatMessages" :key="msg.id"
              :class="['flex gap-2', msg.senderId === selectedChat?.patientId ? 'flex-row-reverse' : '']">
              <div :class="['w-7 h-7 rounded-full flex items-center justify-center text-white text-xs font-bold shrink-0',
                msg.senderId === selectedChat?.patientId ? 'bg-teal-500' : 'bg-blue-500']">
                {{ initials(msg.senderName) }}
              </div>
              <div :class="['max-w-xs rounded-2xl px-3.5 py-2.5 text-sm shadow-sm',
                msg.senderId === selectedChat?.patientId
                  ? 'bg-teal-600 text-white rounded-br-md'
                  : 'bg-white border text-gray-800 rounded-bl-md']">
                <p>{{ msg.content }}</p>
                <p :class="['text-xs mt-1', msg.senderId === selectedChat?.patientId ? 'text-teal-200' : 'text-gray-400']">
                  {{ formatTime(msg.sentAt) }} · {{ msg.senderName }}
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { adminApi, directChatApi } from '@/services/api'

const search       = ref('')
const filterType   = ref('all')
const filterStatus = ref('')
const activeTab    = ref<'direct' | 'ai'>('direct')

const directChats  = ref<any[]>([])
const aiChats      = ref<any[]>([])
const loadingDirect = ref(true)
const loadingAi     = ref(true)

const selectedChat    = ref<any>(null)
const chatMessages    = ref<any[]>([])
const loadingMessages = ref(false)

// ── Load ──────────────────────────────────────────────────────────────────────
onMounted(async () => {
  try {
    const { data } = await adminApi.getAnalytics()
    directChats.value = data.recentDirectChats ?? []
    aiChats.value     = data.recentAiChats ?? []
  } finally {
    loadingDirect.value = false
    loadingAi.value     = false
  }
})

// ── Computed ──────────────────────────────────────────────────────────────────
const filteredDirectChats = computed(() => {
  let list = directChats.value
  if (search.value) {
    const q = search.value.toLowerCase()
    list = list.filter((c: any) =>
      c.patientName.toLowerCase().includes(q) || c.doctorName.toLowerCase().includes(q)
    )
  }
  if (filterStatus.value) list = list.filter((c: any) => c.status === filterStatus.value)
  return list
})

const filteredAiChats = computed(() => {
  let list = aiChats.value
  if (search.value) {
    const q = search.value.toLowerCase()
    list = list.filter((c: any) =>
      c.userName.toLowerCase().includes(q) || c.title.toLowerCase().includes(q)
    )
  }
  return list
})

const tabs = computed(() => [
  { key: 'direct', label: 'Chat Bác sĩ - Bệnh nhân', count: filteredDirectChats.value.length },
  { key: 'ai',     label: 'Tư vấn AI',                count: filteredAiChats.value.length },
])

const summaryCards = computed(() => [
  { icon: '💬', label: 'Phiên chat trực tiếp', value: directChats.value.length, bg: 'bg-teal-50' },
  { icon: '🤖', label: 'Phiên tư vấn AI',      value: aiChats.value.length,     bg: 'bg-indigo-50' },
  { icon: '📨', label: 'Tổng tin nhắn (mẫu)',  value: directChats.value.reduce((s: number, c: any) => s + c.msgCount, 0), bg: 'bg-blue-50' },
])

// ── Open direct chat viewer ───────────────────────────────────────────────────
async function openDirectChat(chat: any) {
  selectedChat.value = chat
  chatMessages.value = []
  loadingMessages.value = true
  try {
    const { data } = await directChatApi.getMessages(chat.id)
    chatMessages.value = data.data
  } catch { /* ignore */ }
  finally { loadingMessages.value = false }
}

// ── Helpers ───────────────────────────────────────────────────────────────────
function initials(name: string) {
  return (name || '?').split(' ').slice(-2).map((n: string) => n[0]).join('').toUpperCase()
}
function formatDate(dt: string) {
  return new Date(dt).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}
function formatTime(dt: string) {
  return new Date(dt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
}
function sessionTypeLabel(t: string) {
  const map: Record<string, string> = { General: 'Tổng quát', MedicalRecord: 'Hồ sơ y tế', Insurance: 'Bảo hiểm', Appointment: 'Đặt lịch' }
  return map[t] ?? t
}
</script>
