# Custom Qdrant image with Prometheus monitoring support
FROM qdrant/qdrant:latest

# Build arguments for sensitive data (pass via --build-arg)
ARG QDRANT_API_KEY=""
ARG QDRANT_READ_ONLY_API_KEY=""

# Install additional monitoring and debugging tools
RUN apt-get update && apt-get install -y \
    curl \
    wget \
    git \
    prometheus-node-exporter \
    && rm -rf /var/lib/apt/lists/*

# Create directories for configuration and data
RUN mkdir -p /qdrant/storage/config

# Copy Qdrant configuration
COPY config.yml /qdrant/storage/config/config.yml

# Export build args as environment variables (available at runtime)
ENV QDRANT_API_KEY=${QDRANT_API_KEY}
ENV QDRANT_READ_ONLY_API_KEY=${QDRANT_READ_ONLY_API_KEY}

# Expose Qdrant API port and Prometheus metrics port
EXPOSE 6333 9090

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:6333/health || exit 1

# Run Qdrant server
CMD ["./qdrant"]
