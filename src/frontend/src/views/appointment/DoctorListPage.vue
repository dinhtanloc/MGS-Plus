<template>
  <div class="max-w-5xl mx-auto px-4 py-10">
    <h1 class="text-2xl font-bold text-gray-900 mb-2">Đội ngũ bác sĩ</h1>
    <p class="text-gray-500 text-sm mb-6">Tìm bác sĩ phù hợp theo chuyên khoa</p>

    <!-- Search -->
    <div class="flex gap-3 mb-6">
      <input v-model="search" type="search" placeholder="Tìm theo chuyên khoa..."
        class="input-field flex-1 max-w-xs" @input="filterDoctors"/>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
      <div v-for="i in 6" :key="i" class="card animate-pulse space-y-3">
        <div class="flex gap-3">
          <div class="w-14 h-14 bg-gray-200 rounded-full shrink-0"></div>
          <div class="flex-1 space-y-2 pt-1">
            <div class="bg-gray-200 h-4 rounded w-3/4"></div>
            <div class="bg-gray-200 h-3 rounded w-1/2"></div>
          </div>
        </div>
        <div class="bg-gray-200 h-12 rounded"></div>
      </div>
    </div>

    <!-- Results -->
    <div v-else-if="filtered.length" class="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
      <div v-for="d in filtered" :key="d.id" class="card hover:shadow-md transition-shadow">
        <div class="flex items-center gap-3 mb-3">
          <div class="w-14 h-14 bg-blue-100 rounded-full flex items-center justify-center text-blue-700 font-bold text-lg shrink-0">
            {{ d.name.split(' ').pop()?.[0] ?? '?' }}
          </div>
          <div>
            <h3 class="font-semibold text-gray-900 text-sm">BS. {{ d.name }}</h3>
            <p class="text-blue-600 text-xs font-medium">{{ d.specialty }}</p>
          </div>
        </div>

        <p v-if="d.bio" class="text-xs text-gray-500 line-clamp-2 mb-3">{{ d.bio }}</p>

        <div class="flex items-center justify-between text-xs text-gray-500 mb-3">
          <span class="flex items-center gap-1">
            <svg class="w-3.5 h-3.5 text-yellow-400" fill="currentColor" viewBox="0 0 20 20">
              <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
            </svg>
            {{ d.rating.toFixed(1) }} ({{ d.reviewCount }} đánh giá)
          </span>
          <span>{{ formatFee(d.consultationFee) }}</span>
        </div>

        <RouterLink :to="`/appointment?doctorId=${d.id}`"
          class="block w-full text-center bg-blue-600 text-white text-sm py-2 rounded-xl hover:bg-blue-700 transition-colors font-medium">
          Đặt lịch khám
        </RouterLink>
      </div>
    </div>

    <div v-else class="text-center py-20 text-gray-400">
      Không tìm thấy bác sĩ phù hợp.
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { appointmentApi } from '@/services/api'
import type { DoctorDto } from '@/types/api'

const doctors  = ref<DoctorDto[]>([])
const filtered = ref<DoctorDto[]>([])
const loading  = ref(true)
const search   = ref('')

onMounted(async () => {
  try {
    const { data } = await appointmentApi.getDoctors()
    doctors.value  = data
    filtered.value = data
  } catch {
    // show empty state
  } finally {
    loading.value = false
  }
})

function filterDoctors() {
  const q = search.value.toLowerCase()
  filtered.value = q
    ? doctors.value.filter(d =>
        d.specialty.toLowerCase().includes(q) ||
        d.name.toLowerCase().includes(q))
    : doctors.value
}

function formatFee(fee: number) {
  return fee > 0
    ? fee.toLocaleString('vi-VN', { style: 'currency', currency: 'VND' })
    : 'Miễn phí'
}
</script>
