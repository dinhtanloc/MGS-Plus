# Neo4j 5 — Graph database cho MGSPlus (GraphRAG, knowledge graph)
FROM neo4j:5.18-community

# Build args (override at build time, runtime uses ENV)
ARG NEO4J_USER=neo4j
ARG NEO4J_PASSWORD=your-neo4j-password
ARG NEO4J_DB=mgsplus

# ── Plugins: APOC (stored procedures & utilities) ────────────────────────────
# APOC is required for graph algorithms, date functions, import/export
ENV NEO4J_PLUGINS='["apoc"]'

# ── Authentication ────────────────────────────────────────────────────────────
ENV NEO4J_AUTH=${NEO4J_USER}/${NEO4J_PASSWORD}

# ── Memory tuning (adjust for available RAM) ──────────────────────────────────
ENV NEO4J_server_memory_heap_initial__size=512m
ENV NEO4J_server_memory_heap_max__size=1G
ENV NEO4J_server_memory_pagecache__size=512m

# ── Network: allow connections from any host ──────────────────────────────────
ENV NEO4J_server_default__listen__address=0.0.0.0
ENV NEO4J_server_bolt_listen__address=0.0.0.0:7687
ENV NEO4J_server_http_listen__address=0.0.0.0:7474

# ── APOC config ───────────────────────────────────────────────────────────────
ENV NEO4J_dbms_security_procedures_unrestricted=apoc.*
ENV NEO4J_dbms_security_procedures_allowlist=apoc.*
ENV apoc_export_file_enabled=true
ENV apoc_import_file_enabled=true
ENV apoc_import_file_use__neo4j__config=true

# ── Logging ──────────────────────────────────────────────────────────────────
ENV NEO4J_server_logs_query_enabled=INFO
ENV NEO4J_server_logs_query_threshold=2000ms

# Persistent data & logs volumes
VOLUME ["/data", "/logs", "/var/lib/neo4j/import"]

# HTTP browser: 7474 | HTTPS: 7473 | Bolt: 7687
EXPOSE 7474 7473 7687

# Health check via HTTP API
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=5 \
    CMD wget -qO- http://localhost:7474 || exit 1

CMD ["neo4j"]
