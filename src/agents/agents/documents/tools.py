from __future__ import annotations

import asyncio
import concurrent.futures
from typing import Any, List, Optional, Type

from crewai.tools import BaseTool
from pydantic import BaseModel, Field

from src.agents.core.config import Settings, get_settings
from src.agents.core.db.qdrant_client import QdrantService, get_qdrant_service
from src.agents.core.mcp.mcp_client import MCPClient


def _run_async(coro):
    """Run an async coroutine safely even when an event loop is already running.

    See supervisor/tools.py for rationale.
    """
    with concurrent.futures.ThreadPoolExecutor(max_workers=1) as pool:
        future = pool.submit(asyncio.run, coro)
        return future.result()


# ── Qdrant search tool ────────────────────────────────────────────────────────

class QdrantSearchInput(BaseModel):
    query_text: str = Field(description="Text to search for in the knowledge base")
    collection: str = Field(
        default="knowledge_shared",
        description="Qdrant collection to search. Use 'knowledge_shared' for global knowledge, "
                    "'user_{user_id}_docs' for user-specific docs.",
    )
    limit: int = Field(default=5, description="Number of results to return")


class QdrantSearchTool(BaseTool):
    name: str = "qdrant_search"
    description: str = (
        "Search the Qdrant vector database for relevant documents, images, or knowledge. "
        "Use this to answer questions that require retrieving stored content."
    )
    args_schema: Type[BaseModel] = QdrantSearchInput

    _qdrant: QdrantService = None  # type: ignore
    _settings: Settings = None  # type: ignore

    def __init__(self, settings: Optional[Settings] = None, **kwargs: Any) -> None:
        super().__init__(**kwargs)
        self._settings = settings or get_settings()
        self._qdrant = get_qdrant_service(self._settings)

    def _run(self, query_text: str, collection: str = "knowledge_shared", limit: int = 5) -> str:
        results = _run_async(self._search(query_text, collection, limit))
        if not results:
            return "No relevant documents found."
        return "\n\n".join(
            f"[Score: {r['score']:.3f}] {r['payload'].get('text', r['payload'])}"
            for r in results
        )

    async def _search(self, query_text: str, collection: str, limit: int) -> list:
        # Placeholder: in production, embed query_text via OpenAI/local model
        # then pass the embedding vector to qdrant.search()
        # For now, return empty list (stub until embedder is wired)
        try:
            return await self._qdrant.search(
                collection_name=collection,
                query_vector=[0.0] * self._settings.qdrant_vector_size,  # replace with real embed
                limit=limit,
            )
        except Exception:
            return []


# ── MCP connector tools ───────────────────────────────────────────────────────

class MCPSearchInput(BaseModel):
    query: str = Field(description="Search query to send to the MCP server")
    source: str = Field(
        default="wiki",
        description="MCP source to query: 'wiki', 'zalo', 'messenger'",
    )


class MCPSearchTool(BaseTool):
    name: str = "mcp_search"
    description: str = (
        "Search external sources via MCP (Model Context Protocol). "
        "Supported sources: 'wiki' (Wikipedia, medical sites), 'zalo', 'messenger'. "
        "Use this to fetch up-to-date information from external platforms."
    )
    args_schema: Type[BaseModel] = MCPSearchInput

    _settings: Settings = None  # type: ignore

    def __init__(self, settings: Optional[Settings] = None, **kwargs: Any) -> None:
        super().__init__(**kwargs)
        self._settings = settings or get_settings()

    def _get_mcp_url(self, source: str) -> Optional[str]:
        mapping = {
            "wiki": self._settings.mcp_wiki_url,
            "zalo": self._settings.mcp_zalo_url,
            "messenger": self._settings.mcp_messenger_url,
        }
        return mapping.get(source)

    def _run(self, query: str, source: str = "wiki") -> str:
        url = self._get_mcp_url(source)
        if not url:
            return f"MCP source '{source}' is not configured. Set mcp_{source}_url in settings."

        result = _run_async(self._call_mcp(url, query))
        return result or f"No results from {source}."

    async def _call_mcp(self, url: str, query: str) -> str:
        client = MCPClient(url)
        try:
            tools = await client.list_tools()
            search_tool = next(
                (t for t in tools if "search" in t.name.lower()), None
            )
            if not search_tool:
                return "MCP server has no search tool."
            result = await client.call_tool(search_tool.name, {"query": query})
            if isinstance(result, list):
                return "\n".join(str(item) for item in result)
            return str(result)
        except Exception as exc:
            return f"MCP call failed: {exc}"
