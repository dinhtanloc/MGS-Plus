from __future__ import annotations

from functools import lru_cache
from typing import Optional

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=["infra/.env.local", "infra/.env.example"],
        env_file_encoding="utf-8",
        extra="ignore",
    )

    # ── LLM ──────────────────────────────────────────────────────────────────
    openai_api_key: str = Field(default="", alias="OPENAI_API_KEY")
    llm_model: str = "gpt-4o-mini"

    # ── Qdrant ───────────────────────────────────────────────────────────────
    qdrant_host: str = Field(default="localhost", alias="QDRANT_HOST")
    qdrant_port: int = Field(default=6333, alias="QDRANT_PORT")
    qdrant_grpc_port: int = Field(default=6334, alias="QDRANT_GRPC_PORT")
    qdrant_api_key: str = Field(default="", alias="QDRANT_API_KEY")
    # Default embedding dimension (e.g. OpenAI text-embedding-3-small = 1536)
    qdrant_vector_size: int = 1536

    # Collection names
    qdrant_shared_collection: str = "knowledge_shared"

    # ── SQL Server ────────────────────────────────────────────────────────────
    sqlserver_host: str = Field(default="localhost", alias="SQLSERVER_HOST")
    sqlserver_port: int = Field(default=1433, alias="SQLSERVER_PORT")
    sa_password: str = Field(default="", alias="SA_PASSWORD")
    sql_admin_user: str = Field(default="sa", alias="SQL_ADMIN_USER")
    sqlserver_db: str = Field(default="mgsplus_agents", alias="SQLSERVER_DB")

    # ── Neo4j ─────────────────────────────────────────────────────────────────
    neo4j_uri: str = Field(default="bolt://localhost:7687", alias="NEO4J_URI")
    neo4j_user: str = Field(default="neo4j", alias="NEO4J_USER")
    neo4j_password: str = Field(default="", alias="NEO4J_PASSWORD")

    # ── Agent service ports ──────────────────────────────────────────────────
    supervisor_port: int = 8010
    documents_port: int = 8011
    workflow_port: int = 8012

    # ── MCP server URLs ──────────────────────────────────────────────────────
    mcp_zalo_url: Optional[str] = None
    mcp_messenger_url: Optional[str] = None
    mcp_wiki_url: Optional[str] = None

    # ── Memory TTL ───────────────────────────────────────────────────────────
    short_term_ttl_seconds: int = 3600  # 1 hour
    long_term_summary_threshold: int = 20  # messages before summarisation

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
