<template>
  <div class="max-w-5xl mx-auto px-4 py-10">
    <div class="mb-8">
      <h1 class="text-2xl font-bold text-gray-900 mb-1">Hồ sơ y tế</h1>
      <p class="text-gray-500 text-sm">Lịch sử khám bệnh, kết quả xét nghiệm và đơn thuốc của bạn</p>
    </div>

    <div v-if="loading" class="space-y-4">
      <div v-for="i in 4" :key="i" class="card animate-pulse">
        <div class="flex gap-4">
          <div class="bg-gray-200 w-12 h-12 rounded-xl flex-shrink-0"></div>
          <div class="flex-1 space-y-2">
            <div class="bg-gray-200 h-4 rounded w-1/3"></div>
            <div class="bg-gray-200 h-4 rounded w-2/3"></div>
          </div>
        </div>
      </div>
    </div>

    <div v-else>
      <div v-if="records.length === 0" class="text-center py-20">
        <svg class="w-16 h-16 text-gray-200 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
        </svg>
        <p class="text-gray-500">Chưa có hồ sơ y tế nào</p>
        <RouterLink to="/appointment" class="mt-4 inline-block text-blue-600 text-sm hover:underline">
          Đặt lịch khám ngay →
        </RouterLink>
      </div>

      <div v-else class="space-y-4">
        <div v-for="record in records" :key="record.id" class="card hover:shadow-sm transition-shadow">
          <div class="flex items-start gap-4">
            <div class="w-12 h-12 bg-green-100 rounded-xl flex items-center justify-center flex-shrink-0">
              <svg class="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
              </svg>
            </div>
            <div class="flex-1 min-w-0">
              <div class="flex items-center justify-between gap-2 mb-1">
                <h3 class="font-semibold text-gray-900 truncate">{{ record.diagnosis || 'Khám tổng quát' }}</h3>
                <span class="text-xs text-gray-400 flex-shrink-0">{{ formatDate(record.visitDate) }}</span>
              </div>
              <p v-if="record.doctorName" class="text-sm text-gray-500 mb-1">
                Bác sĩ: {{ record.doctorName }}
              </p>
              <p v-if="record.prescription" class="text-sm text-gray-600 bg-gray-50 rounded-lg p-3 mt-2">
                <span class="font-medium text-gray-700">Đơn thuốc: </span>{{ record.prescription }}
              </p>
              <p v-if="record.notes" class="text-sm text-gray-500 mt-1">{{ record.notes }}</p>
            </div>
          </div>
        </div>

        <div v-if="totalPages > 1" class="flex justify-center gap-2 pt-4">
          <button v-for="p in totalPages" :key="p" @click="page = p; loadRecords()"
            :class="['w-9 h-9 rounded-lg text-sm font-medium transition-colors', page === p ? 'bg-blue-600 text-white' : 'bg-white border border-gray-200 text-gray-600 hover:bg-gray-50']">
            {{ p }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { medicalApi } from '@/services/api'

const records = ref<any[]>([])
const loading = ref(true)
const page = ref(1)
const total = ref(0)
const pageSize = 10

const totalPages = computed(() => Math.ceil(total.value / pageSize))

function formatDate(d: string) {
  return new Date(d).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

async function loadRecords() {
  loading.value = true
  try {
    const { data } = await medicalApi.list({ page: page.value })
    records.value = data.data || data || []
    total.value = data.total || records.value.length
  } catch { records.value = [] }
  finally { loading.value = false }
}

onMounted(loadRecords)
</script>
