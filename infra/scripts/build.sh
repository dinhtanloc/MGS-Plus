#!/bin/bash

# Build Docker images with environment configuration
# Reads environment variables from {INFRA_HOME}/.env.local

set -e

# Script directory and paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INFRA_HOME="$(dirname "$SCRIPT_DIR")"
ENV_DIR="${INFRA_HOME}"
DOCKER_SERVICES_DIR="${INFRA_HOME}/docker/services"

# Load environment variables
ENV_LOCAL="${ENV_DIR}/.env.local"
ENV_EXAMPLE="${ENV_DIR}/.env.example"

if [ -f "$ENV_LOCAL" ]; then
    echo "[INFO] Loading environment from: $ENV_LOCAL"
    source "$ENV_LOCAL"
elif [ -f "$ENV_EXAMPLE" ]; then
    echo "[INFO] Loading environment from: $ENV_EXAMPLE"
    source "$ENV_EXAMPLE"
else
    echo "[ERROR] No environment file found in: $ENV_DIR"
    echo "[ERROR] Expected: $ENV_LOCAL or $ENV_EXAMPLE"
    exit 1
fi

echo "[INFO] Building Docker images with build arguments..."
echo ""

# Build Qdrant image
echo "[INFO] Building Qdrant image: ${COMPOSE_PROJECT_NAME}-qdrant:latest"
docker build \
    --build-arg QDRANT_API_KEY="${QDRANT_API_KEY}" \
    --build-arg QDRANT_READ_ONLY_API_KEY="${QDRANT_READ_ONLY_API_KEY}" \
    -t "${COMPOSE_PROJECT_NAME}-qdrant:latest" \
    "${DOCKER_SERVICES_DIR}/qdrant"

if [ $? -eq 0 ]; then
    echo "[SUCCESS] Qdrant image built successfully"
else
    echo "[ERROR] Qdrant build failed"
    exit 1
fi

echo ""

# Build SQL Server image
echo "[INFO] Building SQL Server image: ${COMPOSE_PROJECT_NAME}-sqlserver:latest"
docker build \
    --build-arg SA_PASSWORD="${SA_PASSWORD}" \
    --build-arg SQL_ADMIN_USER="${SQL_ADMIN_USER}" \
    --build-arg ACCEPT_EULA="${ACCEPT_EULA}" \
    -t "${COMPOSE_PROJECT_NAME}-sqlserver:latest" \
    "${DOCKER_SERVICES_DIR}/sqlserver"

if [ $? -eq 0 ]; then
    echo "[SUCCESS] SQL Server image built successfully"
else
    echo "[ERROR] SQL Server build failed"
    exit 1
fi

echo ""
echo "[SUCCESS] All Docker images built successfully"
echo ""
echo "[INFO] Image summary:"
docker images | grep "$COMPOSE_PROJECT_NAME" || echo "[WARNING] No images found matching $COMPOSE_PROJECT_NAME"
