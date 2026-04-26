<template>
  <div class="min-h-screen bg-gray-50">
    <!-- Header -->
    <div class="bg-white shadow-sm border-b">
      <div class="max-w-4xl mx-auto px-4 py-6">
        <h1 class="text-2xl font-bold text-gray-900">Đơn thuốc OCR</h1>
        <p class="text-gray-500 mt-1">Tải lên hình ảnh đơn thuốc để trích xuất danh sách thuốc tự động</p>
      </div>
    </div>

    <div class="max-w-4xl mx-auto px-4 py-6 space-y-6">

      <!-- Upload card -->
      <div class="bg-white rounded-2xl border p-6">
        <h2 class="text-lg font-semibold text-gray-900 mb-4">Tải lên đơn thuốc</h2>

        <!-- Drop zone -->
        <div
          @dragover.prevent="dragOver = true"
          @dragleave="dragOver = false"
          @drop.prevent="handleDrop"
          @click="fileInput?.click()"
          :class="[
            'border-2 border-dashed rounded-2xl p-10 text-center cursor-pointer transition-all',
            dragOver
              ? 'border-blue-500 bg-blue-50'
              : previewUrl
                ? 'border-green-400 bg-green-50'
                : 'border-gray-300 hover:border-blue-400 hover:bg-gray-50'
          ]">
          <input ref="fileInput" type="file" class="hidden"
            accept="image/jpeg,image/png,image/webp,image/bmp,image/tiff"
            @change="handleFileChange" />

          <div v-if="previewUrl" class="flex flex-col items-center gap-3">
            <img :src="previewUrl" alt="Preview"
              class="max-h-48 rounded-xl shadow-md object-contain" />
            <p class="text-sm text-green-700 font-medium">{{ selectedFile?.name }}</p>
            <button @click.stop="clearFile" class="text-xs text-red-600 hover:underline">Xóa & chọn lại</button>
          </div>

          <div v-else class="flex flex-col items-center gap-3">
            <div class="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center text-3xl">📋</div>
            <div>
              <p class="text-gray-700 font-medium">Kéo & thả ảnh đơn thuốc vào đây</p>
              <p class="text-sm text-gray-400 mt-1">hoặc nhấn để chọn file</p>
            </div>
            <p class="text-xs text-gray-400">Hỗ trợ: JPG, PNG, WEBP, BMP, TIFF · Tối đa 10MB</p>
          </div>
        </div>

        <div v-if="uploadError" class="mt-3 text-sm text-red-600 bg-red-50 border border-red-200 px-4 py-3 rounded-xl">
          {{ uploadError }}
        </div>

        <div class="mt-4 flex gap-3 justify-end">
          <button @click="clearFile" :disabled="!selectedFile || uploading"
            class="px-4 py-2 border rounded-xl text-gray-700 hover:bg-gray-50 disabled:opacity-40">
            Hủy
          </button>
          <button @click="uploadFile" :disabled="!selectedFile || uploading"
            class="px-6 py-2 bg-blue-600 text-white rounded-xl font-medium hover:bg-blue-700 disabled:opacity-60 flex items-center gap-2">
            <svg v-if="uploading" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
            </svg>
            {{ uploading ? 'Đang xử lý...' : '🔍 Phân tích OCR' }}
          </button>
        </div>
      </div>

      <!-- Active result card -->
      <div v-if="activeResult" class="bg-white rounded-2xl border overflow-hidden">
        <div class="p-5 border-b flex items-center justify-between">
          <div>
            <h2 class="text-lg font-semibold text-gray-900">Kết quả phân tích</h2>
            <p class="text-sm text-gray-500">{{ activeResult.originalFileName }}</p>
          </div>
          <StatusBadge :status="activeResult.status" />
        </div>

        <div v-if="activeResult.status === 'Pending'" class="p-8 text-center text-gray-400">
          <div class="animate-spin w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full mx-auto mb-3"></div>
          <p>Đang nhận dạng văn bản... Kết quả sẽ có sau vài giây</p>
          <button @click="pollResult(activeResult.id)" class="mt-3 text-sm text-blue-600 hover:underline">
            Kiểm tra ngay
          </button>
        </div>

        <div v-else-if="activeResult.status === 'Failed'" class="p-6">
          <div class="bg-red-50 border border-red-200 rounded-xl p-4 text-red-700 text-sm">
            <p class="font-medium mb-1">Xử lý thất bại</p>
            <p>{{ activeResult.errorMessage || 'Không thể nhận dạng văn bản từ ảnh này' }}</p>
          </div>
          <p class="text-sm text-gray-500 mt-3">Thử tải lên ảnh rõ ràng hơn, đủ sáng và không bị mờ.</p>
        </div>

        <div v-else class="p-5 space-y-5">
          <!-- Medications table -->
          <div v-if="activeResult.medications && activeResult.medications.length > 0">
            <h3 class="text-sm font-semibold text-gray-700 mb-3 flex items-center gap-2">
              💊 Danh sách thuốc ({{ activeResult.medications.length }} loại)
            </h3>
            <div class="overflow-x-auto">
              <table class="w-full">
                <thead class="bg-blue-50">
                  <tr>
                    <th class="text-left px-4 py-2.5 text-xs font-medium text-blue-700 uppercase">Tên thuốc</th>
                    <th class="text-left px-4 py-2.5 text-xs font-medium text-blue-700 uppercase">Liều dùng</th>
                    <th class="text-left px-4 py-2.5 text-xs font-medium text-blue-700 uppercase">Tần suất</th>
                    <th class="text-left px-4 py-2.5 text-xs font-medium text-blue-700 uppercase">Thời gian</th>
                  </tr>
                </thead>
                <tbody class="divide-y">
                  <tr v-for="(med, idx) in activeResult.medications" :key="idx"
                    class="hover:bg-gray-50 transition-colors">
                    <td class="px-4 py-3 font-medium text-gray-900">{{ med.name }}</td>
                    <td class="px-4 py-3 text-gray-600">{{ med.dosage || '—' }}</td>
                    <td class="px-4 py-3 text-gray-600">{{ med.frequency || '—' }}</td>
                    <td class="px-4 py-3 text-gray-600">{{ med.duration || '—' }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <div v-else class="text-sm text-amber-600 bg-amber-50 border border-amber-200 p-4 rounded-xl">
            Không trích xuất được danh sách thuốc từ ảnh này.
            Ảnh cần rõ ràng, không bị che khuất chữ.
          </div>

          <!-- Raw OCR text (collapsible) -->
          <div v-if="activeResult.rawOcrText">
            <button @click="showRawText = !showRawText"
              class="text-sm text-gray-500 hover:text-gray-700 flex items-center gap-1">
              <span>{{ showRawText ? '▾' : '▸' }}</span>
              Văn bản thô OCR
            </button>
            <div v-if="showRawText"
              class="mt-2 bg-gray-50 border rounded-xl p-4 text-xs text-gray-600 font-mono whitespace-pre-wrap max-h-48 overflow-y-auto">
              {{ activeResult.rawOcrText }}
            </div>
          </div>
        </div>
      </div>

      <!-- History -->
      <div class="bg-white rounded-2xl border">
        <div class="p-5 border-b flex items-center justify-between">
          <h2 class="text-lg font-semibold text-gray-900">Lịch sử đơn thuốc</h2>
          <button @click="loadHistory" class="text-sm text-blue-600 hover:underline">Làm mới</button>
        </div>

        <div v-if="loadingHistory" class="p-8 text-center text-gray-400">
          <div class="animate-spin w-6 h-6 border-2 border-blue-500 border-t-transparent rounded-full mx-auto mb-2"></div>
          Đang tải...
        </div>

        <div v-else-if="history.length === 0" class="p-8 text-center text-gray-400">
          <div class="text-4xl mb-2">📋</div>
          <p>Chưa có đơn thuốc nào</p>
        </div>

        <div v-else>
          <div v-for="item in history" :key="item.id"
            class="flex items-center gap-4 px-5 py-4 border-b last:border-0 hover:bg-gray-50 transition-colors cursor-pointer"
            @click="viewHistoryItem(item.id)">
            <div :class="[
              'w-10 h-10 rounded-xl flex items-center justify-center text-xl flex-shrink-0',
              item.status === 'Processed' ? 'bg-green-100' :
              item.status === 'Failed' ? 'bg-red-100' : 'bg-yellow-100'
            ]">
              {{ item.status === 'Processed' ? '✅' : item.status === 'Failed' ? '❌' : '⏳' }}
            </div>
            <div class="flex-1 min-w-0">
              <div class="font-medium text-gray-900 truncate">{{ item.originalFileName }}</div>
              <div class="text-sm text-gray-500">
                {{ item.medicationCount }} loại thuốc ·
                {{ new Date(item.createdAt).toLocaleDateString('vi-VN') }}
              </div>
            </div>
            <StatusBadge :status="item.status" />
          </div>

          <div v-if="historyTotal > 10" class="p-4 text-center">
            <button @click="historyPage++; loadHistory()"
              class="text-sm text-blue-600 hover:underline">Xem thêm</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, defineComponent, h } from 'vue'
import { prescriptionApi } from '@/services/api'
import type { PrescriptionDto } from '@/types/api'

// Inline StatusBadge
const StatusBadge = defineComponent({
  props: { status: String },
  setup(props) {
    const map: Record<string, { label: string; cls: string }> = {
      Pending:   { label: 'Đang xử lý', cls: 'bg-yellow-100 text-yellow-700' },
      Processed: { label: 'Hoàn thành', cls: 'bg-green-100 text-green-700' },
      Failed:    { label: 'Lỗi',        cls: 'bg-red-100 text-red-700' },
    }
    return () => {
      const s = props.status ?? ''
      const info = map[s] ?? { label: s, cls: 'bg-gray-100 text-gray-600' }
      return h('span', { class: `inline-block px-2.5 py-1 rounded-full text-xs font-medium ${info.cls}` }, info.label)
    }
  }
})

// ── State ─────────────────────────────────────────────────────────────────────
const fileInput = ref<HTMLInputElement | null>(null)
const selectedFile = ref<File | null>(null)
const previewUrl = ref<string | null>(null)
const dragOver = ref(false)
const uploading = ref(false)
const uploadError = ref('')

const activeResult = ref<PrescriptionDto | null>(null)
const showRawText = ref(false)

const history = ref<any[]>([])
const historyTotal = ref(0)
const historyPage = ref(1)
const loadingHistory = ref(false)

let pollInterval: ReturnType<typeof setInterval> | null = null

// ── File handling ─────────────────────────────────────────────────────────────
function handleFileChange(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (file) setFile(file)
}

function handleDrop(e: DragEvent) {
  dragOver.value = false
  const file = e.dataTransfer?.files[0]
  if (file) setFile(file)
}

function setFile(file: File) {
  if (file.size > 10 * 1024 * 1024) {
    uploadError.value = 'File quá lớn. Vui lòng chọn file nhỏ hơn 10MB'
    return
  }
  selectedFile.value = file
  uploadError.value = ''
  const reader = new FileReader()
  reader.onload = (e) => { previewUrl.value = e.target?.result as string }
  reader.readAsDataURL(file)
}

function clearFile() {
  selectedFile.value = null
  previewUrl.value = null
  uploadError.value = ''
  if (fileInput.value) fileInput.value.value = ''
}

// ── Upload & OCR ──────────────────────────────────────────────────────────────
async function uploadFile() {
  if (!selectedFile.value) return
  uploading.value = true
  uploadError.value = ''
  try {
    const { data } = await prescriptionApi.upload(selectedFile.value)
    clearFile()
    await loadHistory()
    // Start polling for result
    await pollResult(data.id)
    if (data.status === 'Pending') {
      pollInterval = setInterval(() => pollResult(data.id), 2000)
    }
  } catch (e: any) {
    uploadError.value = e?.response?.data?.message || 'Tải lên thất bại. Vui lòng thử lại.'
  } finally {
    uploading.value = false
  }
}

async function pollResult(id: number) {
  try {
    const { data } = await prescriptionApi.get(id)
    activeResult.value = data
    showRawText.value = false
    if (data.status !== 'Pending') {
      if (pollInterval) { clearInterval(pollInterval); pollInterval = null }
      await loadHistory()
    }
  } catch { /* ignore */ }
}

async function viewHistoryItem(id: number) {
  try {
    const { data } = await prescriptionApi.get(id)
    activeResult.value = data
    showRawText.value = false
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } catch { /* ignore */ }
}

// ── History ───────────────────────────────────────────────────────────────────
async function loadHistory() {
  loadingHistory.value = true
  try {
    const { data } = await prescriptionApi.list(historyPage.value)
    history.value = historyPage.value === 1 ? data.data : [...history.value, ...data.data]
    historyTotal.value = data.total
  } catch { /* ignore */ }
  finally { loadingHistory.value = false }
}

onMounted(() => { loadHistory() })
</script>
