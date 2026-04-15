from __future__ import annotations

import asyncio
import concurrent.futures
from typing import Any, List, Optional, Type

from crewai.tools import BaseTool
from pydantic import BaseModel, Field
from qdrant_client.models import Filter, FieldCondition, MatchValue, MatchAny

from src.agents.core.config import Settings, get_settings
from src.agents.core.db.qdrant_client import QdrantService, get_qdrant_service
from src.agents.core.mcp.mcp_client import MCPClient


def _run_async(coro):
    """Run an async coroutine from a sync context, even inside a running event loop."""
    with concurrent.futures.ThreadPoolExecutor(max_workers=1) as pool:
        future = pool.submit(asyncio.run, coro)
        return future.result()


# ── Qdrant RAG search tool ────────────────────────────────────────────────────

class QdrantSearchInput(BaseModel):
    query_text: str = Field(description="Text to search for in the knowledge base")
    collection: str = Field(
        default="knowledge_shared",
        description=(
            "Qdrant collection to search. "
            "Use 'knowledge_shared' for the shared medical knowledge base. "
            "Use 'user_{user_id}_docs' for a specific user's personal documents."
        ),
    )
    limit: int = Field(default=5, ge=1, le=20, description="Number of results to return (1–20)")
    category: Optional[str] = Field(
        default=None,
        description=(
            "Filter results by document category. "
            "Options: 'disease', 'drug', 'insurance', 'nutrition', 'guideline', 'general', 'medical_qa'"
        ),
    )
    language: Optional[str] = Field(
        default=None,
        description="Filter by language: 'vi' (Vietnamese), 'en' (English), or 'mixed'",
    )
    score_threshold: float = Field(
        default=0.30,
        ge=0.0,
        le=1.0,
        description="Minimum cosine similarity score (0–1). Lower = broader results.",
    )


class QdrantSearchTool(BaseTool):
    name: str = "qdrant_search"
    description: str = (
        "Search the MGSPlus medical knowledge base using semantic vector search. "
        "Returns the most relevant passages with their sources and similarity scores. "
        "Use this tool to answer ANY question about diseases, medications, health insurance, "
        "nutrition, medical procedures, or system features. "
        "Set category='disease' for symptom/diagnosis queries, "
        "'drug' for medication queries, 'insurance' for BHYT questions."
    )
    args_schema: Type[BaseModel] = QdrantSearchInput

    _qdrant: QdrantService = None  # type: ignore
    _settings: Settings = None  # type: ignore

    def __init__(self, settings: Optional[Settings] = None, **kwargs: Any) -> None:
        super().__init__(**kwargs)
        self._settings = settings or get_settings()
        self._qdrant = get_qdrant_service(self._settings)

    # ── Public _run ───────────────────────────────────────────────────────────

    def _run(
        self,
        query_text:      str,
        collection:      str   = "knowledge_shared",
        limit:           int   = 5,
        category:        Optional[str] = None,
        language:        Optional[str] = None,
        score_threshold: float = 0.30,
    ) -> str:
        results = _run_async(
            self._search(query_text, collection, limit, category, language, score_threshold)
        )
        return self._format_results(results, query_text)

    # ── Embedding ─────────────────────────────────────────────────────────────

    async def _embed(self, text: str) -> List[float]:
        import httpx
        async with httpx.AsyncClient() as client:
            resp = await client.post(
                f"{self._settings.ollama_base_url}/api/embeddings",
                json={"model": self._settings.ollama_embedding_model, "prompt": text},
                timeout=30.0,
            )
            resp.raise_for_status()
            return resp.json()["embedding"]

    # ── Qdrant search ─────────────────────────────────────────────────────────

    async def _search(
        self,
        query_text:      str,
        collection:      str,
        limit:           int,
        category:        Optional[str],
        language:        Optional[str],
        score_threshold: float,
    ) -> List[dict]:
        # Embed the query
        try:
            vector = await self._embed(query_text)
        except Exception as exc:
            # Return empty rather than crash — agent will try fallback
            return []

        # Build metadata filter
        must_conditions = []
        if category:
            must_conditions.append(
                FieldCondition(key="category", match=MatchValue(value=category))
            )
        if language:
            must_conditions.append(
                FieldCondition(key="language", match=MatchValue(value=language))
            )

        payload_filter = Filter(must=must_conditions) if must_conditions else None

        try:
            results = await self._qdrant.search(
                collection_name=collection,
                query_vector=vector,
                limit=limit,
                payload_filter=payload_filter,
            )
        except Exception:
            return []

        # Apply score threshold
        return [r for r in results if r["score"] >= score_threshold]

    # ── Result formatter ──────────────────────────────────────────────────────

    @staticmethod
    def _format_results(results: List[dict], query: str) -> str:
        if not results:
            return (
                "No relevant documents found in the knowledge base for this query. "
                "Try rephrasing, lowering score_threshold, or removing category/language filters."
            )

        lines = [f"Found {len(results)} relevant passage(s) for: '{query}'\n"]
        for i, r in enumerate(results, 1):
            payload  = r["payload"]
            score    = r["score"]
            text     = payload.get("text", "").strip()
            source   = payload.get("source", "unknown")
            category = payload.get("category", "")
            language = payload.get("language", "")
            chunk_i  = payload.get("chunk_index", 0)
            total    = payload.get("total_chunks", 1)

            meta_parts = [f"score={score:.3f}"]
            if category:
                meta_parts.append(f"category={category}")
            if language:
                meta_parts.append(f"lang={language}")
            if total > 1:
                meta_parts.append(f"chunk {chunk_i+1}/{total}")

            lines.append(
                f"[{i}] [{', '.join(meta_parts)}] Source: {source}\n"
                f"{text}\n"
            )

        return "\n".join(lines)


# ── MCP connector tool ────────────────────────────────────────────────────────

class MCPSearchInput(BaseModel):
    query: str = Field(description="Search query to send to the MCP server")
    source: str = Field(
        default="wiki",
        description=(
            "MCP source to query: "
            "'wiki' (Wikipedia — medical definitions, background), "
            "'zalo', 'messenger' (social platforms)"
        ),
    )


class MCPSearchTool(BaseTool):
    name: str = "mcp_search"
    description: str = (
        "Search external knowledge sources via MCP (Model Context Protocol). "
        "Use 'wiki' to look up medical definitions, drug information, or background context "
        "that may not be in the internal knowledge base. "
        "Only call this when qdrant_search returns no useful results."
    )
    args_schema: Type[BaseModel] = MCPSearchInput

    _settings: Settings = None  # type: ignore

    def __init__(self, settings: Optional[Settings] = None, **kwargs: Any) -> None:
        super().__init__(**kwargs)
        self._settings = settings or get_settings()

    def _get_mcp_url(self, source: str) -> Optional[str]:
        mapping = {
            "wiki":      self._settings.mcp_wiki_url,
            "zalo":      self._settings.mcp_zalo_url,
            "messenger": self._settings.mcp_messenger_url,
        }
        return mapping.get(source)

    def _run(self, query: str, source: str = "wiki") -> str:
        url = self._get_mcp_url(source)
        if not url:
            return (
                f"MCP source '{source}' is not configured. "
                f"Set mcp_{source}_url in settings or environment."
            )
        result = _run_async(self._call_mcp(url, query))
        return result or f"No results returned from MCP source '{source}'."

    async def _call_mcp(self, url: str, query: str) -> str:
        client = MCPClient(url)
        try:
            tools = await client.list_tools()
            search_tool = next(
                (t for t in tools if "search" in t.name.lower()), None
            )
            if not search_tool:
                return "MCP server has no search tool registered."
            result = await client.call_tool(search_tool.name, {"query": query})
            if isinstance(result, list):
                # Each item may be a dict with {"type": "text", "text": "..."}
                texts = []
                for item in result:
                    if isinstance(item, dict):
                        texts.append(item.get("text", str(item)))
                    else:
                        texts.append(str(item))
                return "\n\n".join(texts)
            return str(result)
        except Exception as exc:
            return f"MCP call failed: {exc}"
