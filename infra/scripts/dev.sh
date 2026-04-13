#!/bin/bash
# Run backend + frontend concurrently in development
set -e

ROOT="$(cd "$(dirname "$0")" && pwd)"

# Load shared .env
if [ -f "$ROOT/.env" ]; then
  export $(grep -v '^#' "$ROOT/.env" | xargs)
fi

echo "Starting MGSPlus dev environment..."
echo "  Backend  → http://localhost:${BACKEND_PORT:-5000}"
echo "  Frontend → http://localhost:${FRONTEND_PORT:-3000}"
echo "  Swagger  → http://localhost:${BACKEND_PORT:-5000}/swagger"

# Trap ctrl-c to kill all
trap 'kill 0' SIGINT SIGTERM

# Backend
(cd "$ROOT/src/backend" && dotnet watch run) &

# Frontend
(cd "$ROOT/src/frontend" && npm run dev) &

wait
