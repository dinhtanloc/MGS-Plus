# MGSPlus Agents — Python multi-agent service (CrewAI + FastAPI)
# Build context: project root (needs pyproject.toml, uv.lock, src/, configs/)
#
# Usage (from project root):
#   docker build -f infra/docker/services/agents/Dockerfile \
#     --build-arg AGENT=supervisor -t mgsplus-agents-supervisor .
#
# Or via docker-compose (recommended):
#   docker compose -f infra/docker-compose.yml up agents-supervisor
ARG PYTHON_VERSION=3.11

# ── Stage 1: dependency installer ─────────────────────────────────────────────
FROM python:${PYTHON_VERSION}-slim AS deps

WORKDIR /app

# Install uv (fast Python package manager)
RUN pip install --no-cache-dir uv

# Copy only dependency manifests first (cache layer)
COPY pyproject.toml uv.lock ./

# Install all dependencies into /app/.venv
RUN uv sync --frozen --no-dev --no-install-project

# ── Stage 2: runtime ───────────────────────────────────────────────────────────
FROM python:${PYTHON_VERSION}-slim AS runtime

WORKDIR /app

# Copy installed venv from deps stage
COPY --from=deps /app/.venv /app/.venv

# Ensure venv is on PATH
ENV PATH="/app/.venv/bin:$PATH"
ENV PYTHONUNBUFFERED=1
ENV PYTHONDONTWRITEBYTECODE=1

# Copy application source and config directory
COPY src/agents ./src/agents
COPY configs/ ./configs/

# Create non-root user for security
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

# Which agent to start: supervisor | documents | workflow | all (default)
ARG AGENT=all
ENV AGENT=${AGENT}

# Ports exposed depend on AGENT:
#   supervisor  → 8010
#   documents   → 8011
#   workflow    → 8012
#   all         → 8010 8011 8012
EXPOSE 8010 8011 8012

HEALTHCHECK --interval=30s --timeout=10s --start-period=20s --retries=3 \
    CMD python -c "import httpx, os; \
        port_map={'supervisor':8010,'documents':8011,'workflow':8012,'all':8010}; \
        p=port_map.get(os.getenv('AGENT','all'),8010); \
        httpx.get(f'http://localhost:{p}/health').raise_for_status()"

CMD ["python", "-m", "src.agents.server"]
