<template>
  <div>
    <!-- Hero Section -->
    <section class="bg-gradient-to-br from-blue-700 via-blue-600 to-blue-800 text-white py-20">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="grid md:grid-cols-2 gap-12 items-center">
          <div>
            <div class="inline-flex items-center gap-2 bg-blue-500/30 rounded-full px-4 py-1.5 text-sm mb-6">
              <span class="w-2 h-2 bg-green-400 rounded-full animate-pulse"></span>
              Hệ thống y tế thông minh 24/7
            </div>
            <h1 class="text-4xl md:text-5xl font-bold leading-tight mb-6">
              Chăm sóc sức khỏe <br/>
              <span class="text-blue-200">mọi lúc, mọi nơi</span>
            </h1>
            <p class="text-blue-100 text-lg mb-8 leading-relaxed">
              Tư vấn y tế, đặt lịch khám, hỗ trợ bảo hiểm y tế và pháp luật với trợ lý AI thông minh.
            </p>
            <div class="flex flex-wrap gap-4">
              <RouterLink to="/appointment" class="bg-white text-blue-700 font-semibold px-6 py-3 rounded-xl hover:bg-blue-50 transition-colors shadow-lg">
                Đăng ký khám ngay
              </RouterLink>
              <RouterLink to="/services" class="border border-white/40 text-white font-semibold px-6 py-3 rounded-xl hover:bg-white/10 transition-colors">
                Xem dịch vụ
              </RouterLink>
            </div>
          </div>
          <div class="hidden md:grid grid-cols-2 gap-4">
            <div v-for="stat in stats" :key="stat.label"
              class="bg-white/10 backdrop-blur rounded-2xl p-5 border border-white/20">
              <div class="text-3xl font-bold mb-1">{{ stat.value }}</div>
              <div class="text-blue-200 text-sm">{{ stat.label }}</div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- Services -->
    <section class="py-16 bg-white">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="text-center mb-12">
          <h2 class="text-3xl font-bold text-gray-900 mb-3">Dịch vụ nổi bật</h2>
          <p class="text-gray-500">Chúng tôi cung cấp đầy đủ các dịch vụ y tế hiện đại</p>
        </div>
        <div class="grid sm:grid-cols-2 lg:grid-cols-4 gap-6">
          <RouterLink v-for="svc in services" :key="svc.title" :to="svc.link"
            class="group card hover:shadow-md hover:border-blue-200 transition-all cursor-pointer text-center">
            <div :class="`w-14 h-14 ${svc.color} rounded-2xl flex items-center justify-center mx-auto mb-4 group-hover:scale-110 transition-transform`">
              <svg class="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" :d="svc.icon"/>
              </svg>
            </div>
            <h3 class="font-semibold text-gray-900 mb-2">{{ svc.title }}</h3>
            <p class="text-gray-500 text-sm leading-relaxed">{{ svc.desc }}</p>
          </RouterLink>
        </div>
      </div>
    </section>

    <!-- Latest News -->
    <section class="py-16 bg-gray-50">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex items-center justify-between mb-10">
          <div>
            <h2 class="text-3xl font-bold text-gray-900 mb-1">Tin tức y tế mới nhất</h2>
            <p class="text-gray-500 text-sm">Cập nhật thông tin sức khỏe hàng ngày</p>
          </div>
          <RouterLink to="/news" class="text-blue-600 hover:text-blue-700 font-medium text-sm flex items-center gap-1">
            Xem tất cả
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
            </svg>
          </RouterLink>
        </div>

        <div v-if="loadingNews" class="grid md:grid-cols-3 gap-6">
          <div v-for="i in 3" :key="i" class="card animate-pulse">
            <div class="bg-gray-200 h-48 rounded-lg mb-4"></div>
            <div class="bg-gray-200 h-4 rounded mb-2"></div>
            <div class="bg-gray-200 h-4 rounded w-3/4"></div>
          </div>
        </div>

        <div v-else class="grid md:grid-cols-3 gap-6">
          <RouterLink v-for="news in latestNews" :key="news.id"
            :to="`/news/${news.id}`"
            class="card hover:shadow-md hover:border-gray-200 transition-all group">
            <div class="bg-gradient-to-br from-blue-100 to-blue-50 h-48 rounded-xl mb-4 overflow-hidden">
              <img v-if="news.imageUrl" :src="news.imageUrl" :alt="news.title"
                class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300" />
              <div v-else class="w-full h-full flex items-center justify-center">
                <svg class="w-12 h-12 text-blue-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M19 20H5a2 2 0 01-2-2V6a2 2 0 012-2h10a2 2 0 012 2v1m2 13a2 2 0 01-2-2V7m2 13a2 2 0 002-2V9a2 2 0 00-2-2h-2m-4-3H9M7 16h6M7 8h6v4H7V8z"/>
                </svg>
              </div>
            </div>
            <span v-if="news.categoryName" class="badge-info text-xs mb-2 inline-block">{{ news.categoryName }}</span>
            <h3 class="font-semibold text-gray-900 line-clamp-2 mb-2 group-hover:text-blue-600 transition-colors">{{ news.title }}</h3>
            <p class="text-gray-500 text-sm line-clamp-2">{{ news.summary }}</p>
            <p class="text-xs text-gray-400 mt-3">{{ formatDate(news.publishedAt) }}</p>
          </RouterLink>
        </div>
      </div>
    </section>

    <!-- CTA -->
    <section class="py-16 bg-blue-700 text-white">
      <div class="max-w-3xl mx-auto px-4 text-center">
        <h2 class="text-3xl font-bold mb-4">Cần tư vấn ngay?</h2>
        <p class="text-blue-100 mb-8">Trợ lý ảo AI của chúng tôi luôn sẵn sàng hỗ trợ bạn 24/7</p>
        <div class="flex flex-wrap justify-center gap-4">
          <RouterLink to="/appointment"
            class="bg-white text-blue-700 font-semibold px-8 py-3 rounded-xl hover:bg-blue-50 transition-colors shadow-lg">
            Đặt lịch khám
          </RouterLink>
          <button @click="openChat"
            class="border border-white/50 text-white font-semibold px-8 py-3 rounded-xl hover:bg-white/10 transition-colors">
            Chat với AI ngay
          </button>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { newsApi } from '@/services/api'
import { useChatbotStore } from '@/stores/chatbot'

const chatbot = useChatbotStore()
const latestNews = ref<any[]>([])
const loadingNews = ref(true)

const stats = [
  { value: '50K+', label: 'Bệnh nhân tin tưởng' },
  { value: '200+', label: 'Bác sĩ chuyên gia' },
  { value: '24/7', label: 'Hỗ trợ trực tuyến' },
  { value: '15+', label: 'Chuyên khoa' }
]

const services = [
  {
    title: 'Đăng ký khám bệnh',
    desc: 'Đặt lịch khám nhanh chóng, chọn bác sĩ và khoa phù hợp',
    icon: 'M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z',
    color: 'bg-blue-600', link: '/appointment'
  },
  {
    title: 'Hồ sơ y tế',
    desc: 'Lưu trữ và tra cứu kết quả xét nghiệm, đơn thuốc',
    icon: 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z',
    color: 'bg-green-600', link: '/medical-records'
  },
  {
    title: 'Tư vấn bảo hiểm',
    desc: 'Giải đáp thắc mắc về quyền lợi và thủ tục BHYT',
    icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z',
    color: 'bg-purple-600', link: '/services'
  },
  {
    title: 'Tin tức y tế',
    desc: 'Cập nhật thông tin sức khỏe, nghiên cứu và chính sách',
    icon: 'M19 20H5a2 2 0 01-2-2V6a2 2 0 012-2h10a2 2 0 012 2v1m2 13a2 2 0 01-2-2V7m2 13a2 2 0 002-2V9a2 2 0 00-2-2h-2m-4-3H9M7 16h6M7 8h6v4H7V8z',
    color: 'bg-orange-500', link: '/news'
  }
]

async function openChat() {
  chatbot.open()
  if (chatbot.messages.length === 0) await chatbot.initSession()
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

onMounted(async () => {
  try {
    const { data } = await newsApi.list({ page: 1, pageSize: 3 })
    latestNews.value = data.data || []
  } catch {
    latestNews.value = []
  } finally {
    loadingNews.value = false
  }
})
</script>

<style scoped>
.line-clamp-2 { display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
</style>
