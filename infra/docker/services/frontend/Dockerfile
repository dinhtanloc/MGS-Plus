# MGSPlus Frontend — Vue 3 + Vite → Nginx static server
# Build context: project root (needs src/frontend/ and root .env for VITE_* vars)
#
# VITE_* vars must be baked in at build time (Vite replaces them statically).
# They are passed as build-args from docker-compose (sourced from root .env).
ARG NODE_VERSION=20

# ── Stage 1: build ─────────────────────────────────────────────────────────────
FROM node:${NODE_VERSION}-slim AS build

WORKDIR /app

# Copy package manifests and install deps (cache layer)
COPY src/frontend/package.json src/frontend/package-lock.json* ./
RUN npm install --legacy-peer-deps

# Copy frontend source
COPY src/frontend/ ./

# VITE_* build-time vars (injected via docker-compose build.args from root .env)
ARG VITE_API_BASE_URL=/api
ARG VITE_BACKEND_URL=http://localhost:5000
ARG VITE_WS_URL=ws://localhost:5000

ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
ENV VITE_BACKEND_URL=${VITE_BACKEND_URL}
ENV VITE_WS_URL=${VITE_WS_URL}

# Build production assets (skip type-check for Docker; type-check runs in CI)
RUN npx vite build

# ── Stage 2: serve ─────────────────────────────────────────────────────────────
FROM nginx:1.27-alpine AS runtime

# Copy Nginx config
COPY infra/docker/services/frontend/nginx.conf /etc/nginx/conf.d/default.conf

# Copy built assets from build stage
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
    CMD wget -qO- http://127.0.0.1/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
