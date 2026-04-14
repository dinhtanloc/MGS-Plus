<template>
  <div class="max-w-7xl mx-auto px-4 py-10">
    <div class="mb-8">
      <h1 class="text-3xl font-bold text-gray-900 mb-2">Blog sức khỏe</h1>
      <p class="text-gray-500">Chia sẻ kiến thức, kinh nghiệm và câu chuyện về sức khỏe</p>
    </div>

    <div class="flex flex-wrap gap-3 mb-8">
      <div class="relative flex-1 min-w-60">
        <input v-model="search" @input="debounceSearch" type="text" placeholder="Tìm kiếm bài viết..."
          class="input-field pl-10" />
        <svg class="w-4 h-4 text-gray-400 absolute left-3 top-1/2 -translate-y-1/2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
        </svg>
      </div>
      <select v-model="selectedCategory" @change="loadPosts" class="input-field w-auto">
        <option :value="undefined">Tất cả chủ đề</option>
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
        <RouterLink v-for="post in posts" :key="post.id" :to="`/blog/${post.slug}`"
          class="card hover:shadow-md hover:border-gray-200 transition-all group">
          <div class="bg-gradient-to-br from-green-50 to-teal-50 h-44 rounded-xl mb-4 overflow-hidden">
            <img v-if="post.thumbnailUrl" :src="post.thumbnailUrl" :alt="post.title"
              class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300" />
            <div v-else class="w-full h-full flex items-center justify-center">
              <svg class="w-10 h-10 text-green-200" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"/>
              </svg>
            </div>
          </div>
          <div class="flex items-center gap-2 mb-2">
            <span v-if="post.categoryName" class="badge-success text-xs">{{ post.categoryName }}</span>
            <span class="text-xs text-gray-400">{{ formatDate(post.publishedAt) }}</span>
          </div>
          <h3 class="font-semibold text-gray-900 line-clamp-2 mb-2 group-hover:text-green-600 transition-colors">{{ post.title }}</h3>
          <p class="text-gray-500 text-sm line-clamp-2">{{ post.summary }}</p>
          <div class="flex items-center gap-1 mt-3 text-xs text-gray-400">
            <span>{{ post.authorName || 'MGSPlus' }}</span>
          </div>
        </RouterLink>
      </div>

      <div v-if="totalPages > 1" class="flex justify-center gap-2">
        <button v-for="p in totalPages" :key="p" @click="page = p; loadPosts()"
          :class="['w-9 h-9 rounded-lg text-sm font-medium transition-colors', page === p ? 'bg-blue-600 text-white' : 'bg-white border border-gray-200 text-gray-600 hover:bg-gray-50']">
          {{ p }}
        </button>
      </div>

      <p v-if="posts.length === 0" class="text-center text-gray-500 py-12">
        Chưa có bài viết nào
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { blogApi } from '@/services/api'

const posts = ref<any[]>([])
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
  searchTimer = setTimeout(() => { page.value = 1; loadPosts() }, 400)
}

async function loadPosts() {
  loading.value = true
  try {
    const { data } = await blogApi.list({ categoryId: selectedCategory.value, search: search.value || undefined, page: page.value })
    posts.value = data.data || []
    total.value = data.total || 0
  } catch { posts.value = [] }
  finally { loading.value = false }
}

onMounted(async () => {
  await Promise.all([
    loadPosts(),
    blogApi.getCategories().then(r => { categories.value = r.data })
  ])
})
</script>

<style scoped>
.line-clamp-2 { display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
</style>
