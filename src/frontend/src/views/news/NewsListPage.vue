<template>
  <div class="max-w-7xl mx-auto px-4 py-10">
    <div class="mb-8">
      <h1 class="text-3xl font-bold text-gray-900 mb-2">Tin tức y tế</h1>
      <p class="text-gray-500">Cập nhật thông tin sức khỏe, dịch bệnh và chính sách y tế mới nhất</p>
    </div>

    <!-- Search + filter -->
    <div class="flex flex-wrap gap-3 mb-8">
      <div class="relative flex-1 min-w-60">
        <input v-model="search" @input="debounceSearch" type="text" placeholder="Tìm kiếm tin tức..."
          class="input-field pl-10" />
        <svg class="w-4 h-4 text-gray-400 absolute left-3 top-1/2 -translate-y-1/2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
        </svg>
      </div>
      <select v-model="selectedCategory" @change="loadNews" class="input-field w-auto">
        <option :value="undefined">Tất cả danh mục</option>
        <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
    </div>

    <div v-if="loading" class="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
      <div v-for="i in 6" :key="i" class="card animate-pulse">
        <div class="bg-gray-200 h-44 rounded-xl mb-4"></div>
        <div class="bg-gray-200 h-4 rounded mb-2"></div>
        <div class="bg-gray-200 h-4 rounded w-2/3"></div>
      </div>
    </div>

    <div v-else>
      <div class="grid md:grid-cols-2 lg:grid-cols-3 gap-6 mb-10">
        <RouterLink v-for="item in newsList" :key="item.id" :to="`/news/${item.id}`"
          class="card hover:shadow-md hover:border-gray-200 transition-all group">
          <div class="bg-gradient-to-br from-blue-50 to-indigo-50 h-44 rounded-xl mb-4 overflow-hidden">
            <img v-if="item.imageUrl" :src="item.imageUrl" :alt="item.title"
              class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300" />
            <div v-else class="w-full h-full flex items-center justify-center">
              <svg class="w-10 h-10 text-blue-200" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M19 20H5a2 2 0 01-2-2V6a2 2 0 012-2h10a2 2 0 012 2v1m2 13a2 2 0 01-2-2V7m2 13a2 2 0 002-2V9a2 2 0 00-2-2h-2m-4-3H9M7 16h6M7 8h6v4H7V8z"/>
              </svg>
            </div>
          </div>
          <div class="flex items-center gap-2 mb-2">
            <span v-if="item.categoryName" class="badge-info text-xs">{{ item.categoryName }}</span>
            <span class="text-xs text-gray-400">{{ formatDate(item.publishedAt) }}</span>
          </div>
          <h3 class="font-semibold text-gray-900 line-clamp-2 mb-2 group-hover:text-blue-600 transition-colors">{{ item.title }}</h3>
          <p class="text-gray-500 text-sm line-clamp-2">{{ item.summary }}</p>
          <div class="flex items-center gap-1 mt-3 text-xs text-gray-400">
            <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/>
            </svg>
            {{ item.viewCount }} lượt xem
          </div>
        </RouterLink>
      </div>

      <!-- Pagination -->
      <div v-if="totalPages > 1" class="flex justify-center gap-2">
        <button v-for="p in totalPages" :key="p" @click="page = p; loadNews()"
          :class="['w-9 h-9 rounded-lg text-sm font-medium transition-colors', page === p ? 'bg-blue-600 text-white' : 'bg-white border border-gray-200 text-gray-600 hover:bg-gray-50']">
          {{ p }}
        </button>
      </div>

      <p v-if="newsList.length === 0" class="text-center text-gray-500 py-12">
        Không tìm thấy tin tức phù hợp
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { newsApi } from '@/services/api'

const newsList = ref<any[]>([])
const categories = ref<any[]>([])
const loading = ref(true)
const search = ref('')
const selectedCategory = ref<number | undefined>()
const page = ref(1)
const total = ref(0)
const pageSize = 9
let searchTimer: ReturnType<typeof setTimeout>

const totalPages = computed(() => Math.ceil(total.value / pageSize))

function formatDate(d: string) {
  return new Date(d).toLocaleDateString('vi-VN')
}

function debounceSearch() {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => { page.value = 1; loadNews() }, 400)
}

async function loadNews() {
  loading.value = true
  try {
    const { data } = await newsApi.list({ categoryId: selectedCategory.value, search: search.value || undefined, page: page.value })
    newsList.value = data.data || []
    total.value = data.total || 0
  } catch { newsList.value = [] }
  finally { loading.value = false }
}

onMounted(async () => {
  await Promise.all([
    loadNews(),
    newsApi.getCategories().then(r => { categories.value = r.data })
  ])
})

import { computed } from 'vue'
</script>

<style scoped>
.line-clamp-2 { display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
</style>
