"""
Unit tests for Documents agent tools.

QdrantSearchTool  — patches QdrantService.search (async)
MCPSearchTool     — patches MCPClient.list_tools + call_tool (async)
"""
from __future__ import annotations

import pytest
from unittest.mock import AsyncMock, MagicMock, patch

from src.agents.agents.documents.tools import (
    MCPSearchTool,
    QdrantSearchTool,
    _run_async,
)


# ── QdrantSearchTool ──────────────────────────────────────────────────────────

class TestQdrantSearchTool:
    def _make_tool(self, settings, search_return):
        with patch("src.agents.agents.documents.tools.get_qdrant_service") as mock_svc_fn:
            mock_svc = MagicMock()
            mock_svc.search = AsyncMock(return_value=search_return)
            mock_svc_fn.return_value = mock_svc
            tool = QdrantSearchTool(settings=settings)
            tool._qdrant = mock_svc  # inject directly for test isolation
            return tool, mock_svc

    def test_returns_formatted_results(self, settings):
        hits = [
            {"id": "1", "score": 0.92, "payload": {"text": "Diabetes is a chronic condition."}},
            {"id": "2", "score": 0.85, "payload": {"text": "Type 2 diabetes affects insulin."}},
        ]
        tool, _ = self._make_tool(settings, hits)
        result = tool._run("diabetes", "knowledge_shared", 5)
        assert "Diabetes is a chronic condition." in result
        assert "0.920" in result
        assert "Type 2 diabetes affects insulin." in result

    def test_returns_no_results_message_on_empty(self, settings):
        tool, _ = self._make_tool(settings, [])
        result = tool._run("unknown query")
        assert result == "No relevant documents found."

    def test_search_exception_returns_empty_message(self, settings):
        with patch("src.agents.agents.documents.tools.get_qdrant_service") as mock_svc_fn:
            mock_svc = MagicMock()
            mock_svc.search = AsyncMock(side_effect=ConnectionError("Qdrant down"))
            mock_svc_fn.return_value = mock_svc
            tool = QdrantSearchTool(settings=settings)
            tool._qdrant = mock_svc
            # The tool catches exceptions and returns empty list → "No relevant documents"
            result = tool._run("query")
        assert result == "No relevant documents found."

    def test_default_collection_is_knowledge_shared(self, settings):
        hits = [{"id": "1", "score": 0.9, "payload": {"text": "info"}}]
        tool, mock_svc = self._make_tool(settings, hits)
        tool._run("query")
        call_kwargs = mock_svc.search.call_args.kwargs
        assert call_kwargs["collection_name"] == "knowledge_shared"

    def test_custom_collection_is_passed_through(self, settings):
        hits = [{"id": "1", "score": 0.8, "payload": {"text": "user doc"}}]
        tool, mock_svc = self._make_tool(settings, hits)
        tool._run("query", collection="user_alice_docs", limit=3)
        call_kwargs = mock_svc.search.call_args.kwargs
        assert call_kwargs["collection_name"] == "user_alice_docs"
        assert call_kwargs["limit"] == 3

    def test_tool_name_and_description(self, settings):
        with patch("src.agents.agents.documents.tools.get_qdrant_service"):
            tool = QdrantSearchTool(settings=settings)
        assert tool.name == "qdrant_search"
        assert "vector" in tool.description.lower() or "qdrant" in tool.description.lower()


# ── MCPSearchTool ─────────────────────────────────────────────────────────────

class TestMCPSearchTool:
    def _make_tool(self, settings, mcp_wiki_url="http://mcp.local"):
        """Return an MCPSearchTool with mcp_wiki_url configured."""
        patched_settings = MagicMock(wraps=settings)
        patched_settings.mcp_wiki_url = mcp_wiki_url
        patched_settings.mcp_zalo_url = None
        patched_settings.mcp_messenger_url = None
        return MCPSearchTool(settings=patched_settings), patched_settings

    def test_returns_mcp_result_for_wiki(self, settings):
        tool, _ = self._make_tool(settings)
        with patch("src.agents.agents.documents.tools.MCPClient") as MockMCP:
            mock_client = MockMCP.return_value
            search_tool = MagicMock()
            search_tool.name = "search"
            mock_client.list_tools = AsyncMock(return_value=[search_tool])
            mock_client.call_tool = AsyncMock(return_value="Wikipedia result here")
            result = tool._run("dengue fever", "wiki")
        assert result == "Wikipedia result here"

    def test_returns_error_when_source_not_configured(self, settings):
        tool, _ = self._make_tool(settings, mcp_wiki_url=None)
        result = tool._run("query", "wiki")
        assert "not configured" in result.lower() or "mcp" in result.lower()

    def test_returns_no_results_when_no_search_tool_on_server(self, settings):
        tool, _ = self._make_tool(settings)
        with patch("src.agents.agents.documents.tools.MCPClient") as MockMCP:
            mock_client = MockMCP.return_value
            mock_client.list_tools = AsyncMock(return_value=[])  # empty tool list
            mock_client.call_tool = AsyncMock(return_value=None)
            result = tool._run("query", "wiki")
        assert "no search tool" in result.lower()

    def test_mcp_exception_is_caught_gracefully(self, settings):
        tool, _ = self._make_tool(settings)
        with patch("src.agents.agents.documents.tools.MCPClient") as MockMCP:
            mock_client = MockMCP.return_value
            mock_client.list_tools = AsyncMock(side_effect=ConnectionError("MCP unreachable"))
            result = tool._run("query", "wiki")
        assert "mcp call failed" in result.lower() or "failed" in result.lower()

    def test_list_result_joined_with_newlines(self, settings):
        tool, _ = self._make_tool(settings)
        with patch("src.agents.agents.documents.tools.MCPClient") as MockMCP:
            mock_client = MockMCP.return_value
            search_tool = MagicMock()
            search_tool.name = "search"
            mock_client.list_tools = AsyncMock(return_value=[search_tool])
            mock_client.call_tool = AsyncMock(return_value=["result A", "result B"])
            result = tool._run("query", "wiki")
        assert "result A" in result
        assert "result B" in result

    def test_tool_name_and_description(self, settings):
        tool, _ = self._make_tool(settings)
        assert tool.name == "mcp_search"
        assert "mcp" in tool.description.lower()

    def test_unknown_source_returns_not_configured(self, settings):
        tool, _ = self._make_tool(settings)
        result = tool._run("query", "unknown_source")
        assert "not configured" in result.lower()
