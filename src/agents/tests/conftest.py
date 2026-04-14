"""
Shared pytest fixtures for the agents test suite.

Design principles:
- No real network calls (Qdrant, SQL Server, Neo4j, Ollama are all mocked/stubbed).
- MemoryService tests use an in-memory SQLite engine so ORM logic is exercised
  without a real SQL Server instance.
- Settings are overridden via a lightweight fixture that bypasses .env loading.
"""
from __future__ import annotations

import pytest
from unittest.mock import MagicMock, AsyncMock, patch
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker

# ---------------------------------------------------------------------------
# Settings fixture — minimal in-memory config (no .env required)
# ---------------------------------------------------------------------------

@pytest.fixture(scope="session")
def settings():
    """Return a Settings instance with safe in-memory / dummy values."""
    from src.agents.core.config import Settings

    return Settings(
        openai_api_key="sk-test",
        llm_provider="ollama",
        ollama_model="qwen2.5:7b",
        ollama_base_url="http://localhost:11434",
        ollama_embedding_model="nomic-embed-text",
        qdrant_host="localhost",
        qdrant_port=6333,
        qdrant_grpc_port=6334,
        qdrant_api_key="",
        sqlserver_host="localhost",
        sqlserver_port=1433,
        sa_password="Test!Pass0",
        sql_admin_user="sa",
        sqlserver_db="mgsplus_test",
        neo4j_uri="bolt://localhost:7687",
        neo4j_user="neo4j",
        neo4j_password="",
        supervisor_port=8010,
        documents_port=8011,
        workflow_port=8012,
        short_term_ttl_seconds=3600,
        long_term_summary_threshold=20,
    )


# ---------------------------------------------------------------------------
# In-memory SQLite engine + session (replaces SQL Server in unit tests)
# ---------------------------------------------------------------------------

@pytest.fixture(scope="function")
def sqlite_engine():
    """SQLite in-memory engine with all ORM tables created."""
    from src.agents.core.db.sqlalchemy_engine import Base

    engine = create_engine("sqlite:///:memory:", connect_args={"check_same_thread": False})
    Base.metadata.create_all(engine)
    yield engine
    Base.metadata.drop_all(engine)
    engine.dispose()


@pytest.fixture(scope="function")
def sqlite_session_factory(sqlite_engine):
    """Session factory bound to the in-memory SQLite engine."""
    return sessionmaker(bind=sqlite_engine, expire_on_commit=False)


@pytest.fixture(scope="function")
def memory_service(settings, sqlite_session_factory):
    """MemoryService wired to SQLite in-memory (no SQL Server needed)."""
    from src.agents.core.memory.memory_service import MemoryService

    svc = MemoryService(settings)
    # Patch the internal session factory to use SQLite
    svc._session = sqlite_session_factory
    return svc


# ---------------------------------------------------------------------------
# Mock CrewAI Agent helper
# ---------------------------------------------------------------------------

def make_mock_agent(role: str = "mock") -> MagicMock:
    """Return a MagicMock that satisfies the CrewAI Agent interface."""
    agent = MagicMock()
    agent.role = role
    agent.llm = MagicMock()
    agent.llm.call = MagicMock(return_value="Mocked LLM summary")
    return agent


# ---------------------------------------------------------------------------
# Mock MainCrew fixture
# ---------------------------------------------------------------------------

@pytest.fixture()
def mock_crew_kickoff():
    """Patch MainCrew so crew.kickoff() returns a fixed string."""
    with patch("src.agents.crews.main_crew.Crew") as MockCrew:
        instance = MockCrew.return_value
        instance.kickoff.return_value = "mocked answer"
        yield MockCrew


# ---------------------------------------------------------------------------
# httpx mock transport (for A2A / MCP HTTP calls)
# ---------------------------------------------------------------------------

@pytest.fixture()
def httpx_mock_transport():
    """
    Returns a factory: ``httpx_mock_transport(json_body)`` → httpx.MockTransport.
    Use with ``httpx.AsyncClient(transport=...)`` or patch httpx.AsyncClient.
    """
    import httpx

    def _factory(json_body: dict, status_code: int = 200):
        def handler(request):
            return httpx.Response(status_code, json=json_body)
        return httpx.MockTransport(handler)

    return _factory
