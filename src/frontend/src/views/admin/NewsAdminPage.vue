<template>
  <div class="space-y-5">

    <!-- Header actions -->
    <div class="flex items-center justify-between gap-3 flex-wrap">
      <div class="relative flex-1 min-w-52 max-w-sm">
        <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
        </svg>
        <input v-model="search" @input="debouncedLoad" type="text"
          class="input-field pl-9" placeholder="Tìm tiêu đề..." />
      </div>
      <button @click="openCreate" class="btn-primary flex items-center gap-2 text-sm">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
        </svg>
        Thêm tin tức
      </button>
    </div>

    <!-- Table -->
    <div class="bg-white rounded-2xl border border-gray-100 overflow-hidden">
      <table class="w-full text-sm">
        <thead class="bg-gray-50 border-b border-gray-100">
          <tr>
            <th class="text-left px-5 py-3 text-xs font-medium text-gray-500 uppercase">Tiêu đề</th>
            <th class="text-left px-5 py-3 text-xs font-medium text-gray-500 uppercase hidden md:table-cell">Danh mục</th>
            <th class="text-left px-5 py-3 text-xs font-medium text-gray-500 uppercase hidden lg:table-cell">Trạng thái</th>
            <th class="text-left px-5 py-3 text-xs font-medium text-gray-500 uppercase hidden lg:table-cell">Lượt xem</th>
            <th class="text-left px-5 py-3 text-xs font-medium text-gray-500 uppercase hidden lg:table-cell">Ngày đăng</th>
            <th class="px-5 py-3"></th>
          </tr>
        </thead>
        <tbody v-if="loading">
          <tr v-for="i in 8" :key="i">
            <td colspan="6" class="px-5 py-3">
              <div class="h-5 bg-gray-100 rounded animate-pulse"></div>
            </td>
          </tr>
        </tbody>
        <tbody v-else class="divide-y divide-gray-50">
          <tr v-for="item in items" :key="item.id" class="hover:bg-gray-50 transition-colors">
            <td class="px-5 py-3">
              <div class="flex items-center gap-3">
                <img v-if="item.imageUrl" :src="item.imageUrl" class="w-10 h-10 rounded-lg object-cover shrink-0" />
                <div v-else class="w-10 h-10 bg-gray-100 rounded-lg flex items-center justify-center shrink-0 text-gray-300">
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"/>
                  </svg>
                </div>
                <div class="min-w-0">
                  <p class="font-medium text-gray-900 truncate max-w-xs">{{ item.title }}</p>
                  <p v-if="item.source" class="text-xs text-gray-400 truncate">{{ item.source }}</p>
                </div>
              </div>
            </td>
            <td class="px-5 py-3 hidden md:table-cell text-sm text-gray-500">{{ item.categoryName ?? '—' }}</td>
            <td class="px-5 py-3 hidden lg:table-cell">
              <span :class="item.isPublished ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'"
                class="text-xs font-medium px-2 py-1 rounded-full">
                {{ item.isPublished ? 'Đã đăng' : 'Nháp' }}
              </span>
            </td>
            <td class="px-5 py-3 hidden lg:table-cell text-sm text-gray-500">{{ item.viewCount }}</td>
            <td class="px-5 py-3 hidden lg:table-cell text-xs text-gray-400">{{ formatDate(item.publishedAt) }}</td>
            <td class="px-5 py-3">
              <div class="flex items-center justify-end gap-1">
                <button @click="openEdit(item)"
                  class="text-xs px-2.5 py-1.5 rounded-lg bg-blue-50 text-blue-700 hover:bg-blue-100 transition-colors">
                  Sửa
                </button>
                <button @click="togglePublish(item)"
                  class="text-xs px-2.5 py-1.5 rounded-lg transition-colors"
                  :class="item.isPublished ? 'bg-gray-100 text-gray-600 hover:bg-gray-200' : 'bg-green-50 text-green-700 hover:bg-green-100'">
                  {{ item.isPublished ? 'Ẩn' : 'Đăng' }}
                </button>
                <button @click="confirmDelete(item)"
                  class="text-xs px-2.5 py-1.5 rounded-lg bg-red-50 text-red-600 hover:bg-red-100 transition-colors">
                  Xóa
                </button>
              </div>
            </td>
          </tr>
          <tr v-if="!items.length && !loading">
            <td colspan="6" class="text-center py-16 text-gray-400">
              <div class="text-3xl mb-2">📰</div>
              <p>Chưa có tin tức nào</p>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Pagination -->
    <div v-if="total > pageSize" class="flex items-center justify-between text-sm text-gray-600">
      <p>{{ (page - 1) * pageSize + 1 }}–{{ Math.min(page * pageSize, total) }} / {{ total }}</p>
      <div class="flex gap-2">
        <button @click="prevPage" :disabled="page <= 1"
          class="px-3 py-1.5 rounded-lg border border-gray-200 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed">
          ← Trước
        </button>
        <button @click="nextPage" :disabled="page * pageSize >= total"
          class="px-3 py-1.5 rounded-lg border border-gray-200 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed">
          Sau →
        </button>
      </div>
    </div>

    <!-- Create / Edit modal -->
    <Teleport to="body">
      <div v-if="modal" class="fixed inset-0 bg-black/50 flex items-start justify-center z-50 p-4 overflow-y-auto">
        <div class="bg-white rounded-2xl shadow-2xl w-full max-w-2xl my-6">
          <div class="px-6 py-4 border-b flex items-center justify-between">
            <h2 class="font-bold text-gray-900">{{ modal.id ? 'Sửa tin tức' : 'Thêm tin tức mới' }}</h2>
            <button @click="modal = null" class="text-gray-400 hover:text-gray-600 p-1">✕</button>
          </div>
          <div class="p-6 space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1.5">Tiêu đề <span class="text-red-500">*</span></label>
              <input v-model="modal.title" type="text" class="input-field" placeholder="Tiêu đề tin tức..." />
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1.5">Nguồn</label>
                <input v-model="modal.source" type="text" class="input-field" placeholder="VnExpress, Tuổi Trẻ..." />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1.5">URL ảnh đại diện</label>
                <input v-model="modal.imageUrl" type="url" class="input-field" placeholder="https://..." />
              </div>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1.5">Tóm tắt</label>
              <textarea v-model="modal.summary" rows="2" class="input-field resize-none" placeholder="Mô tả ngắn..."></textarea>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1.5">Nội dung <span class="text-red-500">*</span></label>
              <textarea v-model="modal.content" rows="8" class="input-field resize-none font-mono text-xs" placeholder="Nội dung HTML hoặc văn bản..."></textarea>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1.5">Tags (phân cách bằng dấu phẩy)</label>
              <input v-model="modal.tags" type="text" class="input-field" placeholder="y tế, sức khỏe, dinh dưỡng" />
            </div>
            <div class="flex items-center gap-2">
              <input v-model="modal.isPublished" type="checkbox" id="pub" class="rounded" />
              <label for="pub" class="text-sm text-gray-700">Đăng ngay</label>
            </div>
          </div>
          <div class="px-6 py-4 border-t flex justify-end gap-3">
            <button @click="modal = null" class="text-sm text-gray-600 px-4 py-2 rounded-xl hover:bg-gray-100">Hủy</button>
            <button @click="saveModal" :disabled="saving || !modal.title || !modal.content"
              class="btn-primary disabled:opacity-50">
              {{ saving ? 'Đang lưu...' : (modal.id ? 'Cập nhật' : 'Tạo mới') }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Delete confirm -->
    <div v-if="deleteTarget" class="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-2xl shadow-xl p-6 max-w-sm w-full">
        <h3 class="font-semibold text-gray-900 mb-2">Xóa tin tức</h3>
        <p class="text-sm text-gray-600 mb-5">Xóa "<strong>{{ deleteTarget.title }}</strong>"? Hành động này không thể hoàn tác.</p>
        <div class="flex gap-3 justify-end">
          <button @click="deleteTarget = null" class="text-sm text-gray-600 px-4 py-2 rounded-xl hover:bg-gray-100">Hủy</button>
          <button @click="doDelete" :disabled="saving"
            class="text-sm px-4 py-2 rounded-xl text-white bg-red-600 hover:bg-red-700 disabled:opacity-50">
            {{ saving ? 'Đang xóa...' : 'Xóa' }}
          </button>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { newsApi } from '@/services/api'
import { useToastStore } from '@/stores/toast'

const toast = useToastStore()
const items   = ref<any[]>([])
const total   = ref(0)
const page    = ref(1)
const pageSize = 15
const search  = ref('')
const loading = ref(true)
const saving  = ref(false)

const modal       = ref<any>(null)
const deleteTarget = ref<any>(null)

let debounceTimer: ReturnType<typeof setTimeout>
function debouncedLoad() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => { page.value = 1; load() }, 350)
}

onMounted(load)

async function load() {
  loading.value = true
  try {
    const { data } = await newsApi.adminList({ search: search.value || undefined, page: page.value, pageSize })
    items.value = data.data
    total.value = data.total
  } catch {
    toast.error('Không thể tải danh sách tin tức.')
  } finally {
    loading.value = false
  }
}

function prevPage() { if (page.value > 1) { page.value--; load() } }
function nextPage() { if (page.value * pageSize < total.value) { page.value++; load() } }

function openCreate() {
  modal.value = { title: '', content: '', summary: '', source: '', imageUrl: '', tags: '', isPublished: true }
}

function openEdit(item: any) {
  modal.value = { ...item }
}

async function saveModal() {
  if (!modal.value) return
  saving.value = true
  try {
    if (modal.value.id) {
      await newsApi.update(modal.value.id, modal.value)
      toast.success('Đã cập nhật tin tức.')
    } else {
      await newsApi.create(modal.value)
      toast.success('Đã tạo tin tức mới.')
    }
    modal.value = null
    await load()
  } catch (e: any) {
    toast.error(e.response?.data?.message || 'Thao tác thất bại.')
  } finally {
    saving.value = false
  }
}

async function togglePublish(item: any) {
  try {
    await newsApi.update(item.id, { isPublished: !item.isPublished })
    item.isPublished = !item.isPublished
    toast.success(item.isPublished ? 'Đã đăng tin.' : 'Đã ẩn tin.')
  } catch {
    toast.error('Thao tác thất bại.')
  }
}

function confirmDelete(item: any) {
  deleteTarget.value = item
}

async function doDelete() {
  if (!deleteTarget.value) return
  saving.value = true
  try {
    await newsApi.delete(deleteTarget.value.id)
    toast.success('Đã xóa tin tức.')
    deleteTarget.value = null
    await load()
  } catch {
    toast.error('Xóa thất bại.')
  } finally {
    saving.value = false
  }
}

function formatDate(d: string | null) {
  if (!d) return '—'
  return new Date(d).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
}
</script>
