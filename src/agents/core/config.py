from __future__ import annotations

from functools import lru_cache
from pathlib import Path
from typing import Any, Optional

import yaml
from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict

# ── Project root resolution ────────────────────────────────────────────────────
# src/agents/core/config.py → parents: [core(0), agents(1), src(2), MGSPlus(3)]
_ROOT = Path(__file__).resolve().parents[3]
_CONFIGS = _ROOT / "configs"


def _load_yml(filename: str) -> dict[str, Any]:
    """Load a yml file from configs/. Returns empty dict if not found."""
    path = _CONFIGS / filename
    if path.exists():
        with open(path) as f:
            return yaml.safe_load(f) or {}
    return {}


def _get(d: dict[str, Any], *keys: str, default: Any = None) -> Any:
    """Traverse nested dict by key path, return default if any key is missing."""
    node: Any = d
    for key in keys:
        if not isinstance(node, dict):
            return default
        node = node.get(key, default)
    return node


# Load the two config files relevant to agents
_infra = _load_yml("infra-config.yml")
_agents = _load_yml("agents-config.yml")


# ── Settings ───────────────────────────────────────────────────────────────────

class Settings(BaseSettings):
    """
    All runtime configuration for the agents service.

    Priority (highest → lowest):
      1. Environment variables (from runtime or docker-compose)
      2. Root .env file
      3. configs/agents-config.yml  (non-secret defaults)
      4. configs/infra-config.yml   (DB/infra defaults)
    """

    model_config = SettingsConfigDict(
        env_file=[
            str(_ROOT / ".env"),
            str(_ROOT / ".env.example"),   # fallback for missing vars
        ],
        env_file_encoding="utf-8",
        extra="ignore",
    )

    # ── LLM ──────────────────────────────────────────────────────────────────
    openai_api_key: str = Field(default="", alias="OPENAI_API_KEY")

    # Provider selection: "ollama" (default) or "openai"
    llm_provider: str = Field(
        default=_get(_agents, "llm", "provider", default="ollama"),
        alias="LLM_PROVIDER",
    )

    # OpenAI model (used only when llm_provider=openai)
    llm_model: str = Field(
        default=_get(_agents, "llm", "model", default="gpt-4o-mini"),
        alias="LLM_MODEL",
    )

    # Ollama settings (used when llm_provider=ollama)
    ollama_model: str = Field(
        default=_get(_agents, "llm", "ollama", "model", default="qwen2.5:7b"),
        alias="OLLAMA_MODEL",
    )
    ollama_base_url: str = Field(
        default=_get(_agents, "llm", "ollama", "base_url", default="http://localhost:11434"),
        alias="OLLAMA_BASE_URL",
    )
    ollama_embedding_model: str = Field(
        default=_get(_agents, "llm", "ollama", "embedding_model", default="nomic-embed-text"),
        alias="OLLAMA_EMBEDDING_MODEL",
    )

    # ── Qdrant (from infra-config.yml) ───────────────────────────────────────
    qdrant_host: str = Field(
        default=_get(_infra, "databases", "qdrant", "host", default="localhost"),
        alias="QDRANT_HOST",
    )
    qdrant_port: int = Field(
        default=_get(_infra, "databases", "qdrant", "port", default=6333),
        alias="QDRANT_PORT",
    )
    qdrant_grpc_port: int = Field(
        default=_get(_infra, "databases", "qdrant", "grpc_port", default=6334),
        alias="QDRANT_GRPC_PORT",
    )
    qdrant_api_key: str = Field(default="", alias="QDRANT_API_KEY")
    qdrant_vector_size: int = Field(
        default=_get(_infra, "databases", "qdrant", "vector_size", default=1536),
    )
    qdrant_shared_collection: str = Field(
        default=_get(_infra, "databases", "qdrant", "shared_collection", default="knowledge_shared"),
    )

    # ── SQL Server (from infra-config.yml) ────────────────────────────────────
    sqlserver_host: str = Field(
        default=_get(_infra, "databases", "sqlserver", "host", default="localhost"),
        alias="SQLSERVER_HOST",
    )
    sqlserver_port: int = Field(
        default=_get(_infra, "databases", "sqlserver", "port", default=1433),
        alias="SQLSERVER_PORT",
    )
    sa_password: str = Field(default="", alias="SA_PASSWORD")
    sql_admin_user: str = Field(default="sa", alias="SQL_ADMIN_USER")
    sqlserver_db: str = Field(
        default=_get(_infra, "databases", "sqlserver", "name", default="mgsplus_db"),
        alias="SQLSERVER_DB",
    )

    # ── Neo4j (from infra-config.yml) ─────────────────────────────────────────
    neo4j_uri: str = Field(default="bolt://localhost:7687", alias="NEO4J_URI")
    neo4j_user: str = Field(default="neo4j", alias="NEO4J_USER")
    neo4j_password: str = Field(default="", alias="NEO4J_PASSWORD")

    # ── Agent service ports (from agents-config.yml) ─────────────────────────
    supervisor_port: int = Field(
        default=_get(_agents, "services", "supervisor", "port", default=8010),
        alias="SUPERVISOR_PORT",
    )
    documents_port: int = Field(
        default=_get(_agents, "services", "documents", "port", default=8011),
        alias="DOCUMENTS_PORT",
    )
    workflow_port: int = Field(
        default=_get(_agents, "services", "workflow", "port", default=8012),
        alias="WORKFLOW_PORT",
    )

    # ── MCP (from agents-config.yml) ─────────────────────────────────────────
    mcp_zalo_url: Optional[str] = Field(
        default=_get(_agents, "mcp", "zalo_url") or None,
        alias="MCP_ZALO_URL",
    )
    mcp_messenger_url: Optional[str] = Field(
        default=_get(_agents, "mcp", "messenger_url") or None,
        alias="MCP_MESSENGER_URL",
    )
    mcp_wiki_url: Optional[str] = Field(
        default=_get(_agents, "mcp", "wiki_url") or None,
        alias="MCP_WIKI_URL",
    )

    # ── Memory (from agents-config.yml) ──────────────────────────────────────
    short_term_ttl_seconds: int = Field(
        default=_get(_agents, "memory", "short_term_ttl_seconds", default=3600),
        alias="SHORT_TERM_TTL_SECONDS",
    )
    long_term_summary_threshold: int = Field(
        default=_get(_agents, "memory", "long_term_summary_threshold", default=20),
        alias="LONG_TERM_SUMMARY_THRESHOLD",
    )

    # ── Derived properties ────────────────────────────────────────────────────
    @property
    def sqlserver_url(self) -> str:
        return (
            f"mssql+pymssql://{self.sql_admin_user}:{self.sa_password}"
            f"@{self.sqlserver_host}:{self.sqlserver_port}/{self.sqlserver_db}"
        )

    @property
    def supervisor_base_url(self) -> str:
        return f"http://localhost:{self.supervisor_port}"

    @property
    def documents_base_url(self) -> str:
        return f"http://localhost:{self.documents_port}"

    @property
    def workflow_base_url(self) -> str:
        return f"http://localhost:{self.workflow_port}"


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    return Settings()


def build_llm(settings: Optional[Settings] = None):
    """Return a CrewAI ``LLM`` instance configured for the active provider.

    Provider selection (highest-priority first):
      1. ``LLM_PROVIDER`` env var
      2. ``configs/agents-config.yml → llm.provider``
      3. Default: ``"ollama"``

    Ollama (``provider=ollama``):
      Uses LiteLLM's ``ollama/`` prefix.  No API key required.
      Set ``OLLAMA_MODEL`` (e.g. ``qwen2.5:7b``) and
      ``OLLAMA_BASE_URL`` (default ``http://localhost:11434``).

    OpenAI (``provider=openai``):
      Uses ``LLM_MODEL`` and ``OPENAI_API_KEY``.
    """
    from crewai import LLM  # local import to avoid circular issues at module load

    cfg = settings or get_settings()

    if cfg.llm_provider == "ollama":
        return LLM(
            model=f"ollama/{cfg.ollama_model}",
            base_url=cfg.ollama_base_url,
        )

    # Fallback — OpenAI-compatible
    return LLM(
        model=cfg.llm_model,
        api_key=cfg.openai_api_key,
    )
