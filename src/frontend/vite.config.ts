import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const backendUrl = env.VITE_BACKEND_URL || 'http://localhost:5000'

  return {
    plugins: [vue()],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url))
      }
    },
    server: {
      port: parseInt(env.FRONTEND_PORT || '3000'),
      // Dev proxy: /api/* → backend (avoids CORS in development)
      proxy: {
        '/api': {
          target: backendUrl,
          changeOrigin: true,
          secure: false
        },
        '/swagger': {
          target: backendUrl,
          changeOrigin: true,
          secure: false
        },
        '/health': {
          target: backendUrl,
          changeOrigin: true,
          secure: false
        }
      }
    },
    build: {
      sourcemap: mode === 'development'
    }
  }
})
