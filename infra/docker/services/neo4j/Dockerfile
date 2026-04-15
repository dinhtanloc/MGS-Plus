# Neo4j 5 — Graph database for MGSPlus (GraphRAG, knowledge graph)
# Secrets (NEO4J_AUTH) are injected at RUNTIME via docker-compose env_file.
# No secrets baked into the image.
FROM neo4j:5.18-community

# ── Plugins ───────────────────────────────────────────────────────────────────
ENV NEO4J_PLUGINS='["apoc"]'

# ── Network: accept connections from any host ─────────────────────────────────
ENV NEO4J_server_default__listen__address=0.0.0.0
ENV NEO4J_server_bolt_listen__address=0.0.0.0:7687
ENV NEO4J_server_http_listen__address=0.0.0.0:7474

# ── APOC ──────────────────────────────────────────────────────────────────────
ENV NEO4J_dbms_security_procedures_unrestricted=apoc.*
ENV NEO4J_dbms_security_procedures_allowlist=apoc.*
ENV apoc_export_file_enabled=true
ENV apoc_import_file_enabled=true
ENV apoc_import_file_use__neo4j__config=true

# ── Memory tuning ─────────────────────────────────────────────────────────────
ENV NEO4J_server_memory_heap_initial__size=512m
ENV NEO4J_server_memory_heap_max__size=1G
# pagecache_size was removed from Neo4j 5.18+ strict validation; use heap only

# ── Logging ───────────────────────────────────────────────────────────────────
ENV NEO4J_server_logs_query_enabled=INFO
ENV NEO4J_server_logs_query_threshold=2000ms

# ── Disable strict config validation to ignore unrecognized env vars ─────────
ENV NEO4J_server_config_strict__validation_enabled=false

# Runtime env vars (injected by docker-compose via env_file: ../../.env):
#   NEO4J_AUTH   — "neo4j/<NEO4J_PASSWORD>" — set in docker-compose environment section

VOLUME ["/data", "/logs", "/var/lib/neo4j/import"]

EXPOSE 7474 7473 7687

HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=5 \
    CMD wget -qO- http://localhost:7474 || exit 1

CMD ["neo4j"]
