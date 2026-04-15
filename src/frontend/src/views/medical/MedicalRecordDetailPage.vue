<template>
  <div class="max-w-2xl mx-auto px-4 py-10">
    <button @click="$router.back()" class="flex items-center gap-1 text-sm text-gray-500 hover:text-blue-600 mb-6 transition-colors">
      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
      </svg>
      Quay lại
    </button>

    <div v-if="loading" class="card space-y-4 animate-pulse">
      <div class="bg-gray-200 h-6 rounded w-1/2"></div>
      <div v-for="i in 5" :key="i" class="bg-gray-200 h-4 rounded"></div>
    </div>

    <div v-else-if="!record" class="card text-center py-16 text-gray-400">
      Không tìm thấy hồ sơ y tế.
    </div>

    <div v-else class="card space-y-5">
      <div class="flex items-center justify-between">
        <h1 class="text-xl font-bold text-gray-900">Hồ sơ y tế #{{ record.id }}</h1>
        <span class="text-xs text-gray-400">{{ formatDate(record.recordDate) }}</span>
      </div>

      <div class="grid sm:grid-cols-2 gap-4 text-sm">
        <div>
          <p class="text-gray-500 mb-0.5">Bác sĩ phụ trách</p>
          <p class="font-medium text-gray-900">{{ record.doctorName ?? 'Không rõ' }}</p>
        </div>
        <div>
          <p class="text-gray-500 mb-0.5">Chuyên khoa</p>
          <p class="font-medium text-gray-900">{{ record.specialty ?? '—' }}</p>
        </div>
      </div>

      <template v-if="record.diagnosis">
        <hr class="border-gray-100"/>
        <div>
          <p class="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1">Chẩn đoán</p>
          <p class="text-gray-900 whitespace-pre-wrap">{{ record.diagnosis }}</p>
        </div>
      </template>

      <template v-if="record.prescription">
        <hr class="border-gray-100"/>
        <div>
          <p class="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1">Đơn thuốc</p>
          <p class="text-gray-900 whitespace-pre-wrap">{{ record.prescription }}</p>
        </div>
      </template>

      <template v-if="record.labResults">
        <hr class="border-gray-100"/>
        <div>
          <p class="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1">Kết quả xét nghiệm</p>
          <p class="text-gray-900 whitespace-pre-wrap">{{ record.labResults }}</p>
        </div>
      </template>

      <template v-if="record.notes">
        <hr class="border-gray-100"/>
        <div>
          <p class="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1">Ghi chú</p>
          <p class="text-gray-900 whitespace-pre-wrap">{{ record.notes }}</p>
        </div>
      </template>

      <template v-if="record.attachmentUrl">
        <hr class="border-gray-100"/>
        <div>
          <p class="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1">Tệp đính kèm</p>
          <a :href="record.attachmentUrl" target="_blank" rel="noopener"
            class="text-blue-600 hover:underline text-sm">Xem tệp đính kèm</a>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { medicalApi } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { MedicalRecordDto } from '@/types/api'

const route   = useRoute()
const toast   = useToastStore()
const record  = ref<MedicalRecordDto | null>(null)
const loading = ref(true)

onMounted(async () => {
  try {
    const { data } = await medicalApi.get(Number(route.params.id))
    record.value = data
  } catch {
    toast.error('Không thể tải hồ sơ y tế.')
  } finally {
    loading.value = false
  }
})

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('vi-VN', { dateStyle: 'long' })
}
</script>
