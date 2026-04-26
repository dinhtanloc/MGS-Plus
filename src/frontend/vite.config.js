import { defineConfig, loadEnv } from 'vite';
import vue from '@vitejs/plugin-vue';
import { fileURLToPath, URL } from 'node:url';
import path from 'node:path';
// Project root = two levels up from src/frontend/
var projectRoot = path.resolve(fileURLToPath(new URL('.', import.meta.url)), '../..');
export default defineConfig(function (_a) {
    var mode = _a.mode;
    // Load ALL env vars from project root .env (single source of truth)
    var env = loadEnv(mode, projectRoot, '');
    var backendUrl = env.VITE_BACKEND_URL || 'http://localhost:5001';
    return {
        // envDir points to project root so VITE_* vars are read from root .env
        envDir: projectRoot,
        plugins: [vue()],
        resolve: {
            alias: {
                '@': fileURLToPath(new URL('./src', import.meta.url))
            }
        },
        server: {
            port: parseInt(env.FRONTEND_PORT || '3000'),
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
    };
});
