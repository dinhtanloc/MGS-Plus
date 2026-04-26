import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'
import path from 'node:path'
import { readFileSync, existsSync } from 'node:fs'
import yaml from 'js-yaml'

// Project root = two levels up from src/frontend/
const projectRoot = path.resolve(fileURLToPath(new URL('.', import.meta.url)), '../..')

function loadYaml(filename: string): Record<string, any> {
  const p = path.join(projectRoot, 'configs', filename)
  if (!existsSync(p)) return {}
  return (yaml.load(readFileSync(p, 'utf8')) as Record<string, any>) ?? {}
}

function getNestedStr(obj: Record<string, any>, keys: string[], fallback: string): string {
  let node: any = obj
  for (const k of keys) {
    if (node == null || typeof node !== 'object') return fallback
    node = node[k]
  }
  return node != null ? String(node) : fallback
}

export default defineConfig(({ mode }) => {
  // Load env vars (Docker build args land here as VITE_* env vars)
  const env = loadEnv(mode, projectRoot, '')

  // Non-secret defaults from YAML (env vars take priority)
  const frontendCfg = loadYaml('frontend-config.yml')
  const backendCfg  = loadYaml('backend-config.yml')

  const frontendPort = parseInt(
    env.FRONTEND_PORT ?? getNestedStr(frontendCfg, ['service', 'port'], '3000')
  )
  const backendUrl = (env.VITE_BACKEND_URL
    ?? getNestedStr(frontendCfg, ['api', 'backend_url'], 'http://localhost:5001'))
    .replace('localhost', '127.0.0.1')
  const apiBaseUrl = env.VITE_API_BASE_URL
    ?? getNestedStr(frontendCfg, ['api', 'base_url'], '/api')

  return {
    envDir: projectRoot,
    plugins: [vue()],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url))
      }
    },
    server: {
      port: frontendPort,
      proxy: {
        '/api': { target: backendUrl, changeOrigin: true, secure: false },
        '/swagger': { target: backendUrl, changeOrigin: true, secure: false },
        '/health': { target: backendUrl, changeOrigin: true, secure: false }
      }
    },
    define: {
      // Make VITE_API_BASE_URL available via import.meta.env in the app
      'import.meta.env.VITE_API_BASE_URL': JSON.stringify(apiBaseUrl)
    },
    build: {
      sourcemap: mode === 'development',
      chunkSizeWarningLimit: 600,
      rollupOptions: {
        output: {
          manualChunks: {
            'apexcharts': ['apexcharts', 'vue3-apexcharts'],
            'signalr':    ['@microsoft/signalr'],
          }
        }
      }
    }
  }
})
