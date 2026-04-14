#!/bin/bash
# Build all Docker images for MGSPlus.
# Env vars are sourced from the single project root .env (no per-service .env).
#
# Usage:
#   ./infra/scripts/build.sh           # build all
#   ./infra/scripts/build.sh qdrant    # build one service
#
# Run from project root OR infra/scripts/ — both work.

set -euo pipefail

# ── Resolve project root ───────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INFRA_DIR="$(dirname "$SCRIPT_DIR")"
PROJECT_ROOT="$(dirname "$INFRA_DIR")"

# ── Load root .env (single source of truth) ────────────────────────────────────
ROOT_ENV="${PROJECT_ROOT}/.env"
ROOT_ENV_EXAMPLE="${PROJECT_ROOT}/.env.example"

if [ -f "$ROOT_ENV" ]; then
    echo "[INFO] Loading env: $ROOT_ENV"
    # shellcheck disable=SC1090
    set -a; source "$ROOT_ENV"; set +a
elif [ -f "$ROOT_ENV_EXAMPLE" ]; then
    echo "[WARN] .env not found — loading example: $ROOT_ENV_EXAMPLE"
    # shellcheck disable=SC1090
    set -a; source "$ROOT_ENV_EXAMPLE"; set +a
else
    echo "[ERROR] No .env found at project root: $PROJECT_ROOT"
    echo "        Copy .env.example to .env and fill in values."
    exit 1
fi

PROJECT_NAME="${COMPOSE_PROJECT_NAME:-mgsplus}"
COMPOSE_FILE="${INFRA_DIR}/docker-compose.yml"

# ── Helper ─────────────────────────────────────────────────────────────────────
build_service() {
    local svc="$1"
    echo ""
    echo "[INFO] ─── Building: $svc ───────────────────────────────"
    docker compose -f "$COMPOSE_FILE" --project-directory "$PROJECT_ROOT" \
        build "$svc"
    echo "[OK]  $svc built successfully"
}

# ── Main ───────────────────────────────────────────────────────────────────────
SERVICES=(sqlserver qdrant neo4j backend agents-supervisor agents-documents agents-workflow frontend)

if [ $# -gt 0 ]; then
    # Build specific services passed as arguments
    for svc in "$@"; do
        build_service "$svc"
    done
else
    # Build all
    echo "[INFO] Building all services for project: $PROJECT_NAME"
    for svc in "${SERVICES[@]}"; do
        build_service "$svc"
    done
fi

echo ""
echo "[OK] Build complete."
echo ""
echo "[INFO] Images:"
docker images | grep "^${PROJECT_NAME}" || echo "  (none found matching '${PROJECT_NAME}')"
