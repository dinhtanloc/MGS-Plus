<template>
  <div>
    <!-- Header -->
    <section class="bg-gradient-to-br from-blue-700 to-blue-800 text-white py-16">
      <div class="max-w-7xl mx-auto px-4 text-center">
        <h1 class="text-4xl font-bold mb-4">Dịch vụ y tế MGSPlus</h1>
        <p class="text-blue-100 text-lg max-w-2xl mx-auto">
          Hệ thống dịch vụ y tế toàn diện — từ khám bệnh, tư vấn đến quản lý hồ sơ sức khỏe
        </p>
      </div>
    </section>

    <!-- Services grid -->
    <section class="py-16 bg-white">
      <div class="max-w-7xl mx-auto px-4">
        <div class="grid sm:grid-cols-2 lg:grid-cols-3 gap-8">
          <div v-for="svc in services" :key="svc.title"
            class="card hover:shadow-lg hover:border-blue-100 transition-all group">
            <div :class="`w-16 h-16 ${svc.color} rounded-2xl flex items-center justify-center mb-5 group-hover:scale-105 transition-transform`">
              <svg class="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" :d="svc.icon"/>
              </svg>
            </div>
            <h3 class="text-lg font-bold text-gray-900 mb-2">{{ svc.title }}</h3>
            <p class="text-gray-500 text-sm leading-relaxed mb-4">{{ svc.desc }}</p>
            <RouterLink :to="svc.link" class="text-blue-600 text-sm font-medium hover:text-blue-700 inline-flex items-center gap-1">
              Tìm hiểu thêm
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
              </svg>
            </RouterLink>
          </div>
        </div>
      </div>
    </section>

    <!-- BHYT section -->
    <section class="py-16 bg-blue-50">
      <div class="max-w-4xl mx-auto px-4 text-center">
        <h2 class="text-2xl font-bold text-gray-900 mb-4">Hỗ trợ Bảo hiểm Y tế</h2>
        <p class="text-gray-500 mb-8">Giải đáp mọi thắc mắc về quyền lợi BHYT, thủ tục thanh toán và danh sách bệnh viện tuyến</p>
        <div class="grid sm:grid-cols-3 gap-6 text-left">
          <div v-for="item in bhytItems" :key="item.title" class="bg-white rounded-2xl p-6 shadow-sm">
            <div class="text-3xl mb-3">{{ item.emoji }}</div>
            <h4 class="font-semibold text-gray-900 mb-1">{{ item.title }}</h4>
            <p class="text-gray-500 text-sm">{{ item.desc }}</p>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
const services = [
  {
    title: 'Đăng ký khám bệnh',
    desc: 'Đặt lịch khám online nhanh chóng, chọn bác sĩ và chuyên khoa phù hợp với nhu cầu của bạn.',
    icon: 'M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z',
    color: 'bg-blue-600', link: '/appointment'
  },
  {
    title: 'Hồ sơ y tế điện tử',
    desc: 'Lưu trữ toàn bộ lịch sử khám bệnh, đơn thuốc, kết quả xét nghiệm an toàn trên nền tảng số.',
    icon: 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z',
    color: 'bg-green-600', link: '/medical-records'
  },
  {
    title: 'Tư vấn AI 24/7',
    desc: 'Trợ lý AI thông minh giải đáp thắc mắc sức khỏe, gợi ý chuyên khoa và hỗ trợ đặt lịch.',
    icon: 'M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z',
    color: 'bg-purple-600', link: '/'
  },
  {
    title: 'Bảo hiểm y tế',
    desc: 'Hỗ trợ tra cứu quyền lợi BHYT, danh sách cơ sở y tế được bảo hiểm và thủ tục thanh toán.',
    icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z',
    color: 'bg-orange-500', link: '/services'
  },
  {
    title: 'Tin tức y tế',
    desc: 'Cập nhật thông tin sức khỏe, nghiên cứu khoa học và chính sách y tế mới nhất mỗi ngày.',
    icon: 'M19 20H5a2 2 0 01-2-2V6a2 2 0 012-2h10a2 2 0 012 2v1m2 13a2 2 0 01-2-2V7m2 13a2 2 0 002-2V9a2 2 0 00-2-2h-2m-4-3H9M7 16h6M7 8h6v4H7V8z',
    color: 'bg-red-500', link: '/news'
  },
  {
    title: 'Blog sức khỏe',
    desc: 'Kho bài viết chuyên sâu về dinh dưỡng, luyện tập, phòng bệnh và chăm sóc sức khỏe toàn diện.',
    icon: 'M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253',
    color: 'bg-teal-600', link: '/blog'
  }
]

const bhytItems = [
  { emoji: '🏥', title: 'Tra cứu cơ sở KCB', desc: 'Danh sách bệnh viện, phòng khám theo tuyến và địa bàn toàn quốc' },
  { emoji: '📋', title: 'Quyền lợi BHYT', desc: 'Hướng dẫn chi tiết các dịch vụ được BHYT chi trả theo mức phần trăm' },
  { emoji: '💊', title: 'Thủ tục thanh toán', desc: 'Hỗ trợ hoàn thiện hồ sơ và thủ tục thanh toán BHYT nhanh chóng' }
]
</script>
