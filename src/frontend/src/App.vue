<template>
  <div class="min-h-screen flex flex-col">
    <AppNavbar />
    <main class="flex-1">
      <RouterView />
    </main>
    <AppFooter />
    <!-- Chatbot widget always visible bottom-right -->
    <ChatbotWidget />
  </div>
</template>

<script setup lang="ts">
import AppNavbar from '@/components/layout/AppNavbar.vue'
import AppFooter from '@/components/layout/AppFooter.vue'
import ChatbotWidget from '@/components/chatbot/ChatbotWidget.vue'
</script>
