<template>
  <div class="max-w-4xl mx-auto px-4 py-10">

    <!-- Loading skeleton -->
    <div v-if="loading" class="space-y-4">
      <div class="card animate-pulse flex gap-5">
        <div class="w-24 h-24 bg-gray-200 rounded-full shrink-0"></div>
        <div class="flex-1 space-y-3 pt-2">
          <div class="h-5 bg-gray-200 rounded w-1/3"></div>
          <div class="h-3 bg-gray-200 rounded w-1/4"></div>
          <div class="h-3 bg-gray-200 rounded w-2/3"></div>
        </div>
      </div>
    </div>

    <div v-else-if="doctor">

      <!-- Profile header -->
      <div class="card flex flex-col sm:flex-row gap-6 mb-6">
        <div class="w-24 h-24 bg-blue-100 rounded-full flex items-center justify-center text-blue-700 font-bold text-3xl shrink-0 self-start">
          {{ initial }}
        </div>
        <div class="flex-1 min-w-0">
          <div class="flex flex-wrap items-start gap-3 mb-1">
            <h1 class="text-2xl font-bold text-gray-900">BS. {{ doctor.name }}</h1>
            <span v-if="doctor.isAvailable"
              class="text-xs bg-green-100 text-green-700 font-medium px-2 py-1 rounded-full">Đang nhận khám</span>
            <span v-else
              class="text-xs bg-gray-100 text-gray-500 font-medium px-2 py-1 rounded-full">Tạm ngừng</span>
          </div>
          <p class="text-blue-600 font-medium mb-2">{{ doctor.specialty }}</p>
          <p v-if="doctor.bio" class="text-gray-600 text-sm leading-relaxed mb-3">{{ doctor.bio }}</p>
          <div class="flex flex-wrap gap-4 text-sm text-gray-500">
            <!-- Rating -->
            <span class="flex items-center gap-1.5">
              <div class="flex">
                <svg v-for="i in 5" :key="i" class="w-4 h-4"
                  :class="i <= Math.round(doctor.rating) ? 'text-yellow-400' : 'text-gray-200'"
                  fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                </svg>
              </div>
              <strong class="text-gray-900">{{ doctor.rating.toFixed(1) }}</strong>
              <span>({{ doctor.reviewCount }} đánh giá)</span>
            </span>
            <!-- Fee -->
            <span class="flex items-center gap-1">
              <svg class="w-4 h-4 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
              </svg>
              {{ formatFee(doctor.consultationFee) }}
            </span>
            <!-- Clinic -->
            <span v-if="doctor.clinicAddress" class="flex items-center gap-1">
              <svg class="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"/>
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"/>
              </svg>
              {{ doctor.clinicAddress }}
            </span>
          </div>
        </div>
        <div class="shrink-0">
          <RouterLink :to="`/appointment?doctorId=${doctor.id}`"
            class="btn-primary whitespace-nowrap">
            Đặt lịch khám
          </RouterLink>
        </div>
      </div>

      <div class="grid md:grid-cols-3 gap-6">

        <!-- Left: schedule -->
        <div class="md:col-span-1 space-y-6">
          <div class="card">
            <h2 class="font-semibold text-gray-900 mb-3">Lịch làm việc</h2>
            <div v-if="doctor.schedule?.length" class="space-y-1.5">
              <div v-for="s in doctor.schedule" :key="s.dayOfWeek"
                class="flex items-center justify-between text-sm">
                <span class="text-gray-600 font-medium">{{ dayName(s.dayOfWeek) }}</span>
                <span class="text-gray-900 text-xs bg-blue-50 px-2 py-0.5 rounded-lg">{{ s.startTime }} – {{ s.endTime }}</span>
              </div>
            </div>
            <p v-else class="text-sm text-gray-400">Chưa cập nhật lịch làm việc</p>
          </div>
        </div>

        <!-- Right: reviews -->
        <div class="md:col-span-2">
          <div class="card">
            <div class="flex items-center justify-between mb-4">
              <h2 class="font-semibold text-gray-900">Đánh giá từ bệnh nhân</h2>
              <div class="flex items-center gap-1.5 text-sm text-gray-500">
                <span class="text-2xl font-bold text-gray-900">{{ doctor.rating.toFixed(1) }}</span>
                <span>/ 5</span>
              </div>
            </div>

            <div v-if="doctor.reviews?.length" class="space-y-4">
              <div v-for="r in doctor.reviews" :key="r.id"
                class="border-b border-gray-50 last:border-0 pb-4 last:pb-0">
                <div class="flex items-center justify-between mb-1">
                  <span class="text-sm font-medium text-gray-800">{{ r.userName }}</span>
                  <span class="text-xs text-gray-400">{{ formatDate(r.createdAt) }}</span>
                </div>
                <div class="flex gap-0.5 mb-1.5">
                  <svg v-for="i in 5" :key="i" class="w-3.5 h-3.5"
                    :class="i <= r.rating ? 'text-yellow-400' : 'text-gray-200'"
                    fill="currentColor" viewBox="0 0 20 20">
                    <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                  </svg>
                </div>
                <p v-if="r.comment" class="text-sm text-gray-600">{{ r.comment }}</p>
              </div>
            </div>

            <div v-else class="text-center py-8 text-gray-400">
              <div class="text-3xl mb-2">⭐</div>
              <p class="text-sm">Chưa có đánh giá nào</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div v-else class="text-center py-20 text-gray-400">
      <div class="text-4xl mb-3">👨‍⚕️</div>
      <p>Không tìm thấy bác sĩ</p>
      <RouterLink to="/doctors" class="text-blue-600 text-sm mt-2 inline-block">← Quay lại danh sách</RouterLink>
    </div>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { appointmentApi } from '@/services/api'

const route = useRoute()
const loading = ref(true)
const doctor  = ref<any>(null)

onMounted(async () => {
  try {
    const id = Number(route.params.id)
    const { data } = await appointmentApi.getDoctor(id)
    doctor.value = data
  } catch {
    doctor.value = null
  } finally {
    loading.value = false
  }
})

const initial = computed(() => {
  if (!doctor.value?.name) return '?'
  return doctor.value.name.split(' ').pop()?.[0]?.toUpperCase() ?? '?'
})

const dayNames = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7']
function dayName(d: number) { return dayNames[d] ?? `Ngày ${d}` }

function formatFee(fee: number) {
  if (!fee) return 'Miễn phí'
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(fee)
}
function formatDate(d: string) {
  return new Date(d).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
}
</script>
