<template>
  <div class="space-y-6">

    <!-- KPI Cards -->
    <div class="grid grid-cols-2 xl:grid-cols-4 gap-4">
      <div v-for="card in kpiCards" :key="card.label"
        class="bg-white rounded-2xl border p-5 flex items-center gap-4 hover:shadow-md transition-shadow">
        <div :class="['w-12 h-12 rounded-xl flex items-center justify-center text-2xl shrink-0', card.bg]">
          {{ card.icon }}
        </div>
        <div class="min-w-0">
          <p class="text-2xl font-bold text-gray-900 tabular-nums">
            {{ loading ? '—' : card.value }}
          </p>
          <p class="text-xs text-gray-500 truncate">{{ card.label }}</p>
          <p v-if="card.sub" :class="['text-xs font-medium mt-0.5', card.subColor ?? 'text-gray-400']">
            {{ card.sub }}
          </p>
        </div>
      </div>
    </div>

    <!-- Row 1: Registrations line + Appointments bar -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">

      <!-- User registrations (30 days) -->
      <div class="bg-white rounded-2xl border p-5">
        <div class="flex items-center justify-between mb-1">
          <h2 class="font-semibold text-gray-800">Đăng ký người dùng</h2>
          <span class="text-xs text-gray-400 bg-gray-100 px-2 py-1 rounded-full">30 ngày gần nhất</span>
        </div>
        <p class="text-xs text-gray-400 mb-4">Số lượt đăng ký mới theo ngày</p>
        <div v-if="analyticsLoading" class="h-52 bg-gray-50 rounded-xl animate-pulse"></div>
        <apexchart v-else type="area" height="210" :options="regChartOpts" :series="regSeries" />
      </div>

      <!-- Appointments 6 months bar -->
      <div class="bg-white rounded-2xl border p-5">
        <div class="flex items-center justify-between mb-1">
          <h2 class="font-semibold text-gray-800">Lịch hẹn theo tháng</h2>
          <span class="text-xs text-gray-400 bg-gray-100 px-2 py-1 rounded-full">6 tháng</span>
        </div>
        <p class="text-xs text-gray-400 mb-4">Tổng số lịch hẹn khám được đặt</p>
        <div v-if="loading" class="h-52 bg-gray-50 rounded-xl animate-pulse"></div>
        <apexchart v-else type="bar" height="210" :options="apptChartOpts" :series="apptSeries" />
      </div>
    </div>

    <!-- Row 2: AI Chat area + Role donut -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">

      <!-- AI chat activity area -->
      <div class="lg:col-span-2 bg-white rounded-2xl border p-5">
        <div class="flex items-center justify-between mb-1">
          <h2 class="font-semibold text-gray-800">Hoạt động Tư vấn AI</h2>
          <span class="text-xs text-gray-400 bg-gray-100 px-2 py-1 rounded-full">30 ngày</span>
        </div>
        <p class="text-xs text-gray-400 mb-4">Số phiên chat với trợ lý AI theo ngày</p>
        <div v-if="analyticsLoading" class="h-52 bg-gray-50 rounded-xl animate-pulse"></div>
        <apexchart v-else type="area" height="210" :options="chatChartOpts" :series="chatSeries" />
      </div>

      <!-- Role distribution donut -->
      <div class="bg-white rounded-2xl border p-5">
        <h2 class="font-semibold text-gray-800 mb-1">Phân bổ vai trò</h2>
        <p class="text-xs text-gray-400 mb-2">Người dùng theo nhóm</p>
        <div v-if="analyticsLoading" class="h-52 bg-gray-50 rounded-xl animate-pulse"></div>
        <apexchart v-else type="donut" height="210" :options="roleChartOpts" :series="roleSeries" />
      </div>
    </div>

    <!-- Row 3: Appointment status + DM 7 days + Prescription stats -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">

      <!-- Appointment status horizontal bar -->
      <div class="bg-white rounded-2xl border p-5">
        <h2 class="font-semibold text-gray-800 mb-1">Trạng thái lịch hẹn</h2>
        <p class="text-xs text-gray-400 mb-4">Phân loại toàn bộ lịch hẹn</p>
        <div v-if="analyticsLoading" class="h-48 bg-gray-50 rounded-xl animate-pulse"></div>
        <apexchart v-else type="bar" height="200" :options="statusChartOpts" :series="statusSeries" />
      </div>

      <!-- Direct messages 7 days -->
      <div class="bg-white rounded-2xl border p-5">
        <h2 class="font-semibold text-gray-800 mb-1">Tin nhắn bác sĩ - bệnh nhân</h2>
        <p class="text-xs text-gray-400 mb-4">Số tin nhắn trực tiếp mỗi ngày (7 ngày)</p>
        <div v-if="analyticsLoading" class="h-48 bg-gray-50 rounded-xl animate-pulse"></div>
        <apexchart v-else type="bar" height="200" :options="dmChartOpts" :series="dmSeries" />
      </div>

      <!-- Prescription OCR stats -->
      <div class="bg-white rounded-2xl border p-5">
        <h2 class="font-semibold text-gray-800 mb-1">Đơn thuốc OCR</h2>
        <p class="text-xs text-gray-400 mb-4">Tỉ lệ xử lý đơn thuốc tải lên</p>
        <div v-if="analyticsLoading" class="h-48 bg-gray-50 rounded-xl animate-pulse"></div>
        <apexchart v-else type="radialBar" height="200" :options="rxChartOpts" :series="rxSeries" />
      </div>
    </div>

    <!-- Row 4: AI Token usage table -->
    <div class="bg-white rounded-2xl border p-5">
      <div class="flex items-center justify-between mb-4">
        <div>
          <h2 class="font-semibold text-gray-800">Sử dụng AI Token</h2>
          <p class="text-xs text-gray-400 mt-0.5">Thống kê token theo model (30 ngày gần nhất)</p>
        </div>
        <div v-if="analytics" class="text-right">
          <p class="text-2xl font-bold text-purple-600 tabular-nums">
            {{ analytics.totalTokens.toLocaleString('vi-VN') }}
          </p>
          <p class="text-xs text-gray-400">Tổng token</p>
        </div>
      </div>

      <div v-if="analyticsLoading" class="space-y-2">
        <div v-for="i in 3" :key="i" class="h-10 bg-gray-50 rounded-xl animate-pulse"></div>
      </div>
      <div v-else-if="!analytics?.tokenByModel?.length" class="text-center py-8 text-gray-400 text-sm">
        Chưa có dữ liệu token
      </div>
      <div v-else class="space-y-3">
        <div v-for="t in analytics.tokenByModel" :key="t.model" class="flex items-center gap-3">
          <div class="w-8 h-8 bg-purple-100 rounded-lg flex items-center justify-center text-sm">🤖</div>
          <div class="flex-1 min-w-0">
            <div class="flex items-center justify-between mb-1">
              <span class="text-sm font-medium text-gray-700 truncate">{{ t.model }}</span>
              <span class="text-sm font-bold text-gray-900 tabular-nums ml-2">{{ t.tokens.toLocaleString('vi-VN') }} tokens</span>
            </div>
            <div class="w-full bg-gray-100 rounded-full h-1.5">
              <div class="bg-purple-500 h-1.5 rounded-full transition-all duration-700"
                :style="{ width: `${Math.round((t.tokens / (analytics?.totalTokens || 1)) * 100)}%` }">
              </div>
            </div>
          </div>
          <span class="text-xs text-gray-400 shrink-0">{{ t.calls }} lần</span>
        </div>
      </div>
    </div>

    <!-- Row 5: Recent chats side by side -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">

      <!-- Doctor-patient chat history -->
      <div class="bg-white rounded-2xl border overflow-hidden">
        <div class="px-5 py-4 border-b flex items-center justify-between">
          <div>
            <h2 class="font-semibold text-gray-800">Lịch sử chat Bác sĩ - Bệnh nhân</h2>
            <p class="text-xs text-gray-400 mt-0.5">10 phiên hoạt động gần nhất</p>
          </div>
          <div class="flex items-center gap-3">
            <div class="text-right">
              <p class="text-sm font-bold text-teal-600">{{ analytics?.directChat?.total ?? '—' }}</p>
              <p class="text-xs text-gray-400">phiên</p>
            </div>
            <RouterLink to="/admin/chats"
              class="text-xs text-blue-600 hover:underline bg-blue-50 px-2 py-1 rounded-lg">
              Xem tất cả
            </RouterLink>
          </div>
        </div>
        <div v-if="analyticsLoading" class="p-4 space-y-3">
          <div v-for="i in 4" :key="i" class="h-12 bg-gray-50 rounded-xl animate-pulse"></div>
        </div>
        <div v-else-if="!analytics?.recentDirectChats?.length" class="p-8 text-center text-gray-400 text-sm">
          Chưa có cuộc trò chuyện nào
        </div>
        <div v-else class="divide-y">
          <div v-for="chat in analytics.recentDirectChats" :key="chat.id"
            class="px-5 py-3 flex items-center gap-3 hover:bg-gray-50 transition-colors">
            <div class="w-8 h-8 bg-teal-100 rounded-full flex items-center justify-center text-teal-700 font-bold text-xs shrink-0">
              {{ initials(chat.patientName) }}
            </div>
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-1 text-sm">
                <span class="font-medium text-gray-900 truncate">{{ chat.patientName }}</span>
                <span class="text-gray-400">↔</span>
                <span class="text-blue-600 truncate">BS. {{ chat.doctorName }}</span>
              </div>
              <div class="text-xs text-gray-400 flex items-center gap-2 mt-0.5">
                <span>{{ chat.specialty }}</span>
                <span>·</span>
                <span>{{ chat.msgCount }} tin nhắn</span>
              </div>
            </div>
            <div class="text-right shrink-0">
              <span :class="['text-xs px-2 py-0.5 rounded-full', chat.status === 'Active' ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500']">
                {{ chat.status === 'Active' ? 'Đang hoạt động' : 'Đã đóng' }}
              </span>
              <p class="text-xs text-gray-400 mt-1">{{ formatRelative(chat.updatedAt) }}</p>
            </div>
          </div>
        </div>
      </div>

      <!-- AI chat sessions -->
      <div class="bg-white rounded-2xl border overflow-hidden">
        <div class="px-5 py-4 border-b flex items-center justify-between">
          <div>
            <h2 class="font-semibold text-gray-800">Phiên tư vấn AI gần đây</h2>
            <p class="text-xs text-gray-400 mt-0.5">10 cuộc hội thoại AI mới nhất</p>
          </div>
          <div class="text-right">
            <p class="text-sm font-bold text-indigo-600">{{ stats?.totalAppointments ?? '—' }}</p>
            <p class="text-xs text-gray-400">tổng phiên</p>
          </div>
        </div>
        <div v-if="analyticsLoading" class="p-4 space-y-3">
          <div v-for="i in 4" :key="i" class="h-12 bg-gray-50 rounded-xl animate-pulse"></div>
        </div>
        <div v-else-if="!analytics?.recentAiChats?.length" class="p-8 text-center text-gray-400 text-sm">
          Chưa có phiên chat AI nào
        </div>
        <div v-else class="divide-y">
          <div v-for="chat in analytics.recentAiChats" :key="chat.id"
            class="px-5 py-3 flex items-center gap-3 hover:bg-gray-50 transition-colors">
            <div class="w-8 h-8 bg-indigo-100 rounded-full flex items-center justify-center text-indigo-600 font-bold text-xs shrink-0">
              {{ initials(chat.userName) }}
            </div>
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium text-gray-900 truncate">{{ chat.title }}</p>
              <div class="text-xs text-gray-400 flex items-center gap-2 mt-0.5">
                <span>{{ chat.userName }}</span>
                <span>·</span>
                <span>{{ sessionTypeLabel(chat.sessionType) }}</span>
                <span>·</span>
                <span>{{ chat.msgCount }} tin</span>
              </div>
            </div>
            <p class="text-xs text-gray-400 shrink-0">{{ formatRelative(chat.updatedAt) }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Pending doctors banner -->
    <div v-if="stats && stats.pendingDoctors > 0"
      class="flex items-center gap-4 bg-yellow-50 border border-yellow-200 rounded-2xl px-5 py-4">
      <span class="text-2xl">⚠️</span>
      <p class="text-sm text-yellow-800 flex-1">
        Có <strong>{{ stats.pendingDoctors }}</strong> đơn đăng ký bác sĩ đang chờ duyệt.
      </p>
      <RouterLink to="/admin/doctors"
        class="text-sm font-medium text-yellow-700 hover:text-yellow-900 bg-yellow-100 px-3 py-1.5 rounded-lg">
        Xem ngay →
      </RouterLink>
    </div>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import VueApexCharts from 'vue3-apexcharts'
import { adminApi } from '@/services/api'
import type { AdminStatsDto } from '@/types/api'

// Register component locally
const apexchart = VueApexCharts

// ── State ─────────────────────────────────────────────────────────────────────
const stats     = ref<AdminStatsDto | null>(null)
const analytics = ref<any | null>(null)
const loading   = ref(true)
const analyticsLoading = ref(true)

// ── Load data ─────────────────────────────────────────────────────────────────
onMounted(async () => {
  try {
    const [sRes, aRes] = await Promise.all([
      adminApi.getStats(),
      adminApi.getAnalytics(),
    ])
    stats.value     = sRes.data
    analytics.value = aRes.data
  } finally {
    loading.value          = false
    analyticsLoading.value = false
  }
})

// ── KPI cards ─────────────────────────────────────────────────────────────────
const kpiCards = computed(() => {
  const s = stats.value
  const a = analytics.value
  return [
    {
      icon: '👥', label: 'Tổng người dùng',       bg: 'bg-blue-50',
      value: s?.totalUsers ?? 0,
      sub: `+${a?.registrations?.slice(-7).reduce((n: number, r: any) => n + r.count, 0) ?? 0} tuần này`,
      subColor: 'text-green-600'
    },
    {
      icon: '🩺', label: 'Bác sĩ đã duyệt',       bg: 'bg-teal-50',
      value: s?.totalDoctors ?? 0,
      sub: s?.pendingDoctors ? `${s.pendingDoctors} chờ duyệt` : 'Đã cập nhật',
      subColor: s?.pendingDoctors ? 'text-yellow-600' : 'text-gray-400'
    },
    {
      icon: '📅', label: 'Lịch hẹn tháng này',   bg: 'bg-purple-50',
      value: s?.appointmentsThisMonth ?? 0,
      sub: `Tổng: ${s?.totalAppointments ?? 0}`,
      subColor: 'text-gray-400'
    },
    {
      icon: '🤖', label: 'Tổng token AI (30 ngày)', bg: 'bg-indigo-50',
      value: (a?.totalTokens ?? 0).toLocaleString('vi-VN'),
      sub: `${a?.recentAiChats?.length ?? 0} phiên gần nhất`,
      subColor: 'text-indigo-500'
    },
    {
      icon: '💬', label: 'Tin nhắn bác sĩ hôm nay', bg: 'bg-green-50',
      value: a?.directChat?.todayMessages ?? 0,
      sub: `Tổng: ${a?.directChat?.totalMessages ?? 0}`,
      subColor: 'text-gray-400'
    },
    {
      icon: '📋', label: 'Đơn thuốc OCR',           bg: 'bg-orange-50',
      value: a?.prescriptions?.total ?? 0,
      sub: `${a?.prescriptions?.processed ?? 0} đã xử lý`,
      subColor: 'text-green-600'
    },
    {
      icon: '📝', label: 'Bài viết Blog',            bg: 'bg-pink-50',
      value: s?.totalBlogPosts ?? 0,
      sub: `Tin tức: ${s?.totalNews ?? 0}`,
      subColor: 'text-gray-400'
    },
    {
      icon: '🏥', label: 'Phiên chat doctor-patient', bg: 'bg-cyan-50',
      value: a?.directChat?.total ?? 0,
      sub: 'Tổng phiên trực tiếp',
      subColor: 'text-cyan-600'
    },
  ]
})

// ── Chart: Registrations (area) ───────────────────────────────────────────────
const regSeries = computed(() => [{
  name: 'Đăng ký mới',
  data: analytics.value?.registrations?.map((r: any) => r.count) ?? []
}])

const regChartOpts = computed(() => ({
  chart:  { toolbar: { show: false }, sparkline: { enabled: false }, background: 'transparent' },
  stroke: { curve: 'smooth', width: 2 },
  fill:   { type: 'gradient', gradient: { shadeIntensity: 1, opacityFrom: 0.4, opacityTo: 0.05 } },
  colors: ['#3b82f6'],
  xaxis:  {
    categories: analytics.value?.registrations?.map((r: any) => r.date.slice(5)) ?? [],
    labels: { rotate: -30, style: { fontSize: '10px' }, formatter: (v: string) => v },
    tickAmount: 6,
  },
  yaxis:  { labels: { style: { fontSize: '11px' } }, min: 0 },
  tooltip: { x: { show: true } },
  grid:   { borderColor: '#f3f4f6', strokeDashArray: 4 },
  dataLabels: { enabled: false },
}))

// ── Chart: Appointments by month (bar) ────────────────────────────────────────
const apptSeries = computed(() => [{
  name: 'Lịch hẹn',
  data: stats.value?.appointmentsByMonth?.map(m => m.count) ?? []
}])

const apptChartOpts = computed(() => ({
  chart:  { toolbar: { show: false }, background: 'transparent' },
  colors: ['#8b5cf6'],
  plotOptions: { bar: { borderRadius: 6, columnWidth: '50%' } },
  xaxis:  {
    categories: stats.value?.appointmentsByMonth?.map(m => `T${m.month}/${m.year}`) ?? [],
    labels: { style: { fontSize: '11px' } }
  },
  yaxis:  { labels: { style: { fontSize: '11px' } }, min: 0 },
  grid:   { borderColor: '#f3f4f6', strokeDashArray: 4 },
  dataLabels: { enabled: false },
  tooltip: { y: { formatter: (v: number) => `${v} lịch hẹn` } },
}))

// ── Chart: AI chat activity (area) ────────────────────────────────────────────
const chatSeries = computed(() => [{
  name: 'Phiên chat AI',
  data: analytics.value?.chatActivity?.map((r: any) => r.count) ?? []
}])

const chatChartOpts = computed(() => ({
  chart:  { toolbar: { show: false }, background: 'transparent' },
  stroke: { curve: 'smooth', width: 2 },
  fill:   { type: 'gradient', gradient: { shadeIntensity: 1, opacityFrom: 0.35, opacityTo: 0.02 } },
  colors: ['#6366f1'],
  xaxis:  {
    categories: analytics.value?.chatActivity?.map((r: any) => r.date.slice(5)) ?? [],
    labels: { rotate: -30, style: { fontSize: '10px' } },
    tickAmount: 6,
  },
  yaxis:  { labels: { style: { fontSize: '11px' } }, min: 0 },
  grid:   { borderColor: '#f3f4f6', strokeDashArray: 4 },
  dataLabels: { enabled: false },
  tooltip: { y: { formatter: (v: number) => `${v} phiên` } },
}))

// ── Chart: Role distribution (donut) ─────────────────────────────────────────
const roleSeries = computed(() =>
  analytics.value?.roleDistribution?.map((r: any) => r.count) ?? []
)
const roleChartOpts = computed(() => ({
  chart:  { background: 'transparent' },
  colors: ['#3b82f6', '#10b981', '#f59e0b', '#6366f1'],
  labels: analytics.value?.roleDistribution?.map((r: any) => r.role) ?? [],
  legend: { position: 'bottom', fontSize: '12px' },
  plotOptions: { pie: { donut: { size: '65%', labels: { show: true, total: { show: true, label: 'Tổng', fontSize: '13px' } } } } },
  dataLabels: { enabled: false },
  tooltip: { y: { formatter: (v: number) => `${v} người` } },
}))

// ── Chart: Appointment status (horizontal bar) ────────────────────────────────
const statusOrder = ['Pending', 'Confirmed', 'Rescheduled', 'Completed', 'Cancelled']
const statusLabel: Record<string, string> = {
  Pending: 'Chờ xử lý', Confirmed: 'Đã xác nhận', Rescheduled: 'Dời lịch',
  Completed: 'Hoàn thành', Cancelled: 'Đã hủy'
}
const statusColors = ['#f59e0b', '#10b981', '#f97316', '#3b82f6', '#ef4444']

const statusSeries = computed(() => [{
  name: 'Số lịch hẹn',
  data: statusOrder.map(s =>
    analytics.value?.apptByStatus?.find((a: any) => a.status === s)?.count ?? 0
  )
}])

const statusChartOpts = computed(() => ({
  chart:  { toolbar: { show: false }, background: 'transparent' },
  plotOptions: { bar: { horizontal: true, borderRadius: 4, barHeight: '60%',
    distributed: true } },
  colors: statusColors,
  xaxis:  { categories: statusOrder.map(s => statusLabel[s] ?? s), labels: { style: { fontSize: '11px' } } },
  yaxis:  { labels: { style: { fontSize: '11px' } } },
  grid:   { borderColor: '#f3f4f6', strokeDashArray: 4 },
  legend: { show: false },
  dataLabels: { enabled: true, style: { fontSize: '11px' } },
  tooltip: { y: { formatter: (v: number) => `${v} lịch hẹn` } },
}))

// ── Chart: Direct messages 7 days (bar) ──────────────────────────────────────
const dmSeries = computed(() => [{
  name: 'Tin nhắn',
  data: analytics.value?.directMessages7d?.map((r: any) => r.count) ?? []
}])
const dmChartOpts = computed(() => ({
  chart:  { toolbar: { show: false }, background: 'transparent' },
  colors: ['#14b8a6'],
  plotOptions: { bar: { borderRadius: 5, columnWidth: '55%' } },
  xaxis:  {
    categories: analytics.value?.directMessages7d?.map((r: any) => r.date) ?? [],
    labels: { style: { fontSize: '11px' } }
  },
  yaxis:  { labels: { style: { fontSize: '11px' } }, min: 0 },
  grid:   { borderColor: '#f3f4f6', strokeDashArray: 4 },
  dataLabels: { enabled: false },
  tooltip: { y: { formatter: (v: number) => `${v} tin nhắn` } },
}))

// ── Chart: Prescription radialBar ─────────────────────────────────────────────
const rxSeries = computed(() => {
  const p = analytics.value?.prescriptions
  if (!p || !p.total) return [0]
  return [Math.round((p.processed / p.total) * 100)]
})
const rxChartOpts = computed(() => ({
  chart:   { background: 'transparent' },
  colors:  ['#f97316'],
  plotOptions: {
    radialBar: {
      hollow: { size: '55%' },
      dataLabels: {
        name:  { offsetY: -6, fontSize: '12px', color: '#6b7280' },
        value: { fontSize: '22px', fontWeight: 700, color: '#111827',
                 formatter: (v: number) => `${v}%` }
      },
      track: { background: '#f3f4f6' }
    }
  },
  labels: ['Thành công'],
  stroke: { lineCap: 'round' },
}))

// ── Helpers ───────────────────────────────────────────────────────────────────
function initials(name: string) {
  return (name || '?').split(' ').slice(-2).map((n: string) => n[0]).join('').toUpperCase()
}

function formatRelative(dt: string) {
  const diff = Date.now() - new Date(dt).getTime()
  if (diff < 60000)    return 'vừa xong'
  if (diff < 3600000)  return `${Math.floor(diff / 60000)} phút trước`
  if (diff < 86400000) return `${Math.floor(diff / 3600000)} giờ trước`
  return `${Math.floor(diff / 86400000)} ngày trước`
}

function sessionTypeLabel(t: string) {
  const map: Record<string, string> = {
    General: 'Tổng quát', MedicalRecord: 'Hồ sơ y tế',
    Insurance: 'Bảo hiểm', Appointment: 'Đặt lịch'
  }
  return map[t] ?? t
}
</script>
