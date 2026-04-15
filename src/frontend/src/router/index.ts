import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const routes = [
  { path: '/', name: 'home', component: () => import('@/views/home/HomePage.vue') },
  { path: '/login', name: 'login', component: () => import('@/views/auth/LoginPage.vue'), meta: { guest: true } },
  { path: '/register', name: 'register', component: () => import('@/views/auth/RegisterPage.vue'), meta: { guest: true } },
  { path: '/news', name: 'news', component: () => import('@/views/news/NewsListPage.vue') },
  { path: '/news/:id', name: 'news-detail', component: () => import('@/views/news/NewsDetailPage.vue') },
  { path: '/blog', name: 'blog', component: () => import('@/views/blog/BlogListPage.vue') },
  { path: '/blog/:slug', name: 'blog-detail', component: () => import('@/views/blog/BlogDetailPage.vue') },
  { path: '/services', name: 'services', component: () => import('@/views/home/ServicesPage.vue') },
  { path: '/doctors', name: 'doctors', component: () => import('@/views/appointment/DoctorListPage.vue') },
  {
    path: '/chat',
    name: 'chat',
    component: () => import('@/views/chat/ChatPage.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/appointment',
    name: 'appointment',
    component: () => import('@/views/appointment/AppointmentPage.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/appointments',
    name: 'my-appointments',
    component: () => import('@/views/appointment/MyAppointmentsPage.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/appointments/:id',
    name: 'appointment-detail',
    component: () => import('@/views/appointment/AppointmentDetailPage.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/profile',
    name: 'profile',
    component: () => import('@/views/profile/ProfilePage.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/medical-records',
    name: 'medical-records',
    component: () => import('@/views/medical/MedicalRecordsPage.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/medical-records/:id',
    name: 'medical-record-detail',
    component: () => import('@/views/medical/MedicalRecordDetailPage.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/admin/blog',
    name: 'admin-blog',
    component: () => import('@/views/admin/BlogAdminPage.vue'),
    meta: { requiresAuth: true, requiresRole: 'Admin' }
  },
  { path: '/:pathMatch(.*)*', name: 'not-found', component: () => import('@/views/NotFoundPage.vue') }
]

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior: () => ({ top: 0 })
})

router.beforeEach(async (to, _from, next) => {
  const auth = useAuthStore()

  if (auth.token && !auth.user) {
    await auth.fetchMe()
  }

  if (to.meta.requiresAuth && !auth.isLoggedIn) {
    return next({ name: 'login', query: { redirect: to.fullPath } })
  }
  if (to.meta.guest && auth.isLoggedIn) {
    return next({ name: 'home' })
  }
  next()
})

export default router
