<template>
  <div class="max-w-5xl mx-auto px-4 py-10">
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-2xl font-bold text-gray-900">Quản lý Blog</h1>
      <RouterLink to="/admin/blog/new"
        class="bg-blue-600 text-white px-4 py-2 rounded-xl text-sm font-medium hover:bg-blue-700 transition-colors">
        + Bài viết mới
      </RouterLink>
    </div>

    <div v-if="loading" class="space-y-3">
      <div v-for="i in 5" :key="i" class="card animate-pulse flex gap-4">
        <div class="bg-gray-200 h-5 rounded flex-1"></div>
        <div class="bg-gray-200 h-5 rounded w-20"></div>
      </div>
    </div>

    <div v-else-if="posts.length" class="card overflow-hidden p-0">
      <table class="w-full text-sm">
        <thead class="bg-gray-50 border-b border-gray-100">
          <tr>
            <th class="text-left px-4 py-3 text-gray-500 font-medium">Tiêu đề</th>
            <th class="text-left px-4 py-3 text-gray-500 font-medium hidden md:table-cell">Danh mục</th>
            <th class="text-left px-4 py-3 text-gray-500 font-medium hidden md:table-cell">Trạng thái</th>
            <th class="text-left px-4 py-3 text-gray-500 font-medium hidden md:table-cell">Lượt xem</th>
            <th class="text-right px-4 py-3 text-gray-500 font-medium">Thao tác</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-100">
          <tr v-for="post in posts" :key="post.id" class="hover:bg-gray-50">
            <td class="px-4 py-3">
              <p class="font-medium text-gray-900 truncate max-w-xs">{{ post.title }}</p>
              <p class="text-gray-400 text-xs mt-0.5">{{ post.slug }}</p>
            </td>
            <td class="px-4 py-3 text-gray-600 hidden md:table-cell">{{ post.categoryName ?? '—' }}</td>
            <td class="px-4 py-3 hidden md:table-cell">
              <span :class="post.isPublished ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600'"
                class="text-xs font-medium px-2 py-0.5 rounded-full">
                {{ post.isPublished ? 'Đã đăng' : 'Nháp' }}
              </span>
            </td>
            <td class="px-4 py-3 text-gray-600 hidden md:table-cell">{{ post.viewCount }}</td>
            <td class="px-4 py-3 text-right">
              <div class="flex items-center justify-end gap-2">
                <RouterLink :to="`/admin/blog/edit/${post.id}`"
                  class="text-xs text-blue-600 hover:underline">Sửa</RouterLink>
                <button @click="confirmDelete(post)" class="text-xs text-red-500 hover:underline">Xóa</button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-else class="text-center py-20 text-gray-400">Chưa có bài viết nào.</div>

    <!-- Delete confirm modal -->
    <div v-if="deleting" class="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl shadow-xl p-6 mx-4 max-w-sm w-full">
        <h3 class="font-semibold text-gray-900 mb-2">Xác nhận xóa</h3>
        <p class="text-sm text-gray-600 mb-5">
          Bạn có chắc muốn xóa bài viết "<strong>{{ deleting.title }}</strong>"? Hành động này không thể hoàn tác.
        </p>
        <div class="flex gap-3 justify-end">
          <button @click="deleting = null" class="text-sm text-gray-600 px-4 py-2 rounded-xl hover:bg-gray-100">Hủy</button>
          <button @click="doDelete" :disabled="deleteLoading"
            class="text-sm bg-red-600 text-white px-4 py-2 rounded-xl hover:bg-red-700 disabled:opacity-50">
            {{ deleteLoading ? 'Đang xóa...' : 'Xóa' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { blogApi } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { BlogPostDto } from '@/types/api'

const toast       = useToastStore()
const posts       = ref<BlogPostDto[]>([])
const loading     = ref(true)
const deleting    = ref<BlogPostDto | null>(null)
const deleteLoading = ref(false)

onMounted(load)

async function load() {
  loading.value = true
  try {
    // fetch all (including unpublished for admin — backend returns admin view)
    const { data } = await blogApi.list({ page: 1 })
    posts.value = data.data
  } catch {
    toast.error('Không thể tải danh sách bài viết.')
  } finally {
    loading.value = false
  }
}

function confirmDelete(post: BlogPostDto) {
  deleting.value = post
}

async function doDelete() {
  if (!deleting.value) return
  deleteLoading.value = true
  try {
    await blogApi.delete(deleting.value.id)
    posts.value = posts.value.filter(p => p.id !== deleting.value!.id)
    toast.success('Đã xóa bài viết.')
    deleting.value = null
  } catch {
    toast.error('Xóa bài viết thất bại.')
  } finally {
    deleteLoading.value = false
  }
}
</script>
