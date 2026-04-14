<template>
  <div class="max-w-4xl mx-auto px-4 py-10">
    <RouterLink to="/blog" class="inline-flex items-center gap-2 text-blue-600 hover:text-blue-700 text-sm mb-6">
      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
      </svg>
      Quay lại blog
    </RouterLink>

    <div v-if="loading" class="animate-pulse space-y-4">
      <div class="bg-gray-200 h-8 rounded w-3/4"></div>
      <div class="bg-gray-200 h-64 rounded-2xl"></div>
      <div class="space-y-2">
        <div class="bg-gray-200 h-4 rounded"></div>
        <div class="bg-gray-200 h-4 rounded w-5/6"></div>
      </div>
    </div>

    <article v-else-if="post">
      <div class="mb-4 flex items-center gap-3">
        <span v-if="post.categoryName" class="badge-success text-xs">{{ post.categoryName }}</span>
        <span class="text-xs text-gray-400">{{ formatDate(post.publishedAt) }}</span>
        <span v-if="post.authorName" class="text-xs text-gray-400">· {{ post.authorName }}</span>
      </div>
      <h1 class="text-3xl font-bold text-gray-900 mb-6 leading-tight">{{ post.title }}</h1>
      <p v-if="post.summary" class="text-lg text-gray-500 mb-6 leading-relaxed">{{ post.summary }}</p>
      <div v-if="post.thumbnailUrl" class="rounded-2xl overflow-hidden mb-8">
        <img :src="post.thumbnailUrl" :alt="post.title" class="w-full h-72 object-cover" />
      </div>
      <div class="prose prose-blue max-w-none text-gray-700 leading-relaxed whitespace-pre-line">
        {{ post.content }}
      </div>
    </article>

    <div v-else class="text-center text-gray-500 py-20">Không tìm thấy bài viết</div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { blogApi } from '@/services/api'

const route = useRoute()
const post = ref<any>(null)
const loading = ref(true)

function formatDate(d: string) {
  return new Date(d).toLocaleDateString('vi-VN', { day: '2-digit', month: 'long', year: 'numeric' })
}

onMounted(async () => {
  try {
    const { data } = await blogApi.getBySlug(route.params.slug as string)
    post.value = data
  } catch { post.value = null }
  finally { loading.value = false }
})
</script>
