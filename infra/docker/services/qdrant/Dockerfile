# Qdrant vector database — MGSPlus
# Secrets (QDRANT_API_KEY) are injected at RUNTIME via docker-compose env_file.
# No secrets baked into the image.
FROM qdrant/qdrant:latest

# Install monitoring and debug tools
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    wget \
    && rm -rf /var/lib/apt/lists/*

# Copy static config (no secrets here — API key is set at runtime)
COPY config.yml /qdrant/config/config.yaml

# Qdrant reads API key from env var at runtime:
#   QDRANT__SERVICE__API_KEY          — main API key
#   QDRANT__SERVICE__READ_ONLY_API_KEY — read-only key
# Both are injected by docker-compose via env_file: ../../.env

EXPOSE 6333 6334

HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -sf http://localhost:6333/readyz || exit 1

CMD ["./qdrant", "--config-path", "/qdrant/config/config.yaml"]
