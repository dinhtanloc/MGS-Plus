<template>
  <!-- Fixed bottom-right chatbot button + panel -->
  <div class="fixed bottom-6 right-6 z-50 flex flex-col items-end gap-3">
    <!-- Chat panel -->
    <Transition name="chatbot">
      <div v-if="chatbot.isOpen"
        class="w-80 sm:w-96 h-[500px] bg-white rounded-2xl shadow-2xl border border-gray-200 flex flex-col overflow-hidden">
        <!-- Header -->
        <div class="bg-gradient-to-r from-blue-600 to-blue-700 px-4 py-3 flex items-center justify-between shrink-0">
          <div class="flex items-center gap-2">
            <div class="w-8 h-8 bg-white/20 rounded-full flex items-center justify-center">
              <svg class="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"/>
              </svg>
            </div>
            <div>
              <p class="text-white font-semibold text-sm leading-none">Trợ lý ảo MGSPlus</p>
              <p class="text-blue-200 text-xs mt-0.5">Tư vấn y tế • Bảo hiểm • Lịch khám</p>
            </div>
          </div>
          <button @click="chatbot.close()" class="text-white/70 hover:text-white transition-colors p-1">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>
        </div>

        <!-- Quick actions -->
        <div class="px-3 py-2 bg-blue-50 border-b border-blue-100 flex gap-2 overflow-x-auto shrink-0">
          <button v-for="q in quickQuestions" :key="q"
            @click="sendQuick(q)"
            class="shrink-0 text-xs bg-white border border-blue-200 text-blue-700 px-2.5 py-1 rounded-full hover:bg-blue-600 hover:text-white hover:border-blue-600 transition-all">
            {{ q }}
          </button>
        </div>

        <!-- Messages -->
        <div ref="messagesContainer" class="flex-1 overflow-y-auto px-4 py-3 space-y-3">
          <div v-for="(msg, i) in chatbot.messages" :key="i"
            :class="['flex', msg.role === 'user' ? 'justify-end' : 'justify-start']">
            <div v-if="msg.role === 'assistant'" class="flex items-end gap-2">
              <div class="w-6 h-6 bg-blue-100 rounded-full flex items-center justify-center shrink-0">
                <svg class="w-3 h-3 text-blue-600" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M12 2a10 10 0 100 20A10 10 0 0012 2zm0 3a1 1 0 110 2 1 1 0 010-2zm0 4a1 1 0 011 1v6a1 1 0 11-2 0v-6a1 1 0 011-1z"/>
                </svg>
              </div>
              <div class="max-w-[80%] bg-gray-100 text-gray-800 rounded-2xl rounded-bl-none px-3 py-2 text-sm leading-relaxed">
                {{ msg.content }}
              </div>
            </div>
            <div v-else class="max-w-[80%] bg-blue-600 text-white rounded-2xl rounded-br-none px-3 py-2 text-sm leading-relaxed">
              {{ msg.content }}
            </div>
          </div>

          <!-- Typing indicator -->
          <div v-if="chatbot.loading" class="flex justify-start">
            <div class="bg-gray-100 rounded-2xl rounded-bl-none px-4 py-3 flex items-center gap-1">
              <span class="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce" style="animation-delay:0ms"></span>
              <span class="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce" style="animation-delay:150ms"></span>
              <span class="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce" style="animation-delay:300ms"></span>
            </div>
          </div>

          <!-- Error -->
          <p v-if="chatbot.error" class="text-xs text-red-500 text-center">{{ chatbot.error }}</p>
        </div>

        <!-- Input -->
        <div class="px-3 py-3 border-t border-gray-100 shrink-0">
          <div class="flex items-end gap-2">
            <textarea
              v-model="inputText"
              @keydown.enter.exact.prevent="handleSend"
              placeholder="Nhập câu hỏi... (Enter để gửi)"
              rows="1"
              class="flex-1 resize-none px-3 py-2 text-sm border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent min-h-[38px] max-h-24"
              :disabled="chatbot.loading"
            />
            <button @click="handleSend"
              :disabled="!inputText.trim() || chatbot.loading"
              class="shrink-0 w-9 h-9 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-300 text-white rounded-xl flex items-center justify-center transition-colors">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8"/>
              </svg>
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- Toggle button -->
    <button @click="toggleChat"
      class="w-14 h-14 bg-blue-600 hover:bg-blue-700 text-white rounded-full shadow-lg hover:shadow-xl flex items-center justify-center transition-all duration-200 active:scale-95">
      <svg v-if="!chatbot.isOpen" class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z"/>
      </svg>
      <svg v-else class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
      </svg>
    </button>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, nextTick } from 'vue'
import { useChatbotStore } from '@/stores/chatbot'

const chatbot = useChatbotStore()
const inputText = ref('')
const messagesContainer = ref<HTMLElement>()

const quickQuestions = [
  'Tư vấn BHYT',
  'Đặt lịch khám',
  'Đọc hồ sơ y tế',
  'Pháp luật y tế',
  'Triệu chứng bệnh'
]

async function toggleChat() {
  chatbot.toggle()
  if (chatbot.isOpen && chatbot.messages.length === 0) {
    await chatbot.initSession()
  }
  await scrollToBottom()
}

async function handleSend() {
  const text = inputText.value.trim()
  if (!text) return
  inputText.value = ''
  await chatbot.sendMessage(text)
  await scrollToBottom()
}

function sendQuick(question: string) {
  inputText.value = question
  handleSend()
}

async function scrollToBottom() {
  await nextTick()
  if (messagesContainer.value) {
    messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
  }
}

watch(() => chatbot.messages.length, scrollToBottom)
</script>
