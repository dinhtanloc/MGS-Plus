<template>
  <div class="min-h-screen flex flex-col">
    <template v-if="!isGuestPage">
      <AppNavbar />
      <main class="flex-1">
        <RouterView />
      </main>
      <AppFooter />
      <ChatbotWidget />
    </template>
    <template v-else>
      <RouterView />
    </template>
    <!-- Global toast notifications — always visible -->
    <ToastContainer />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import AppNavbar from '@/components/layout/AppNavbar.vue'
import AppFooter from '@/components/layout/AppFooter.vue'
import ChatbotWidget from '@/components/chatbot/ChatbotWidget.vue'
import ToastContainer from '@/components/common/ToastContainer.vue'

const route = useRoute()
// Routes with meta.guest = true are standalone pages (login, register, verify-email)
// — no navbar, footer, or chatbot
const isGuestPage = computed(() => !!route.meta.guest)
</script>
