"""
Unit tests for the A2A (Agent-to-Agent) layer.

TestA2ASchemas   — Pydantic model validation (no I/O)
TestA2AClient    — HTTP calls mocked via httpx.MockTransport
"""
from __future__ import annotations

import json
import pytest
from uuid import UUID

import httpx
from unittest.mock import AsyncMock, patch

from src.agents.core.a2a.schemas import (
    A2AError,
    A2ARequest,
    A2AResponse,
    AgentCapability,
    AgentCard,
    TaskInput,
    TaskOutput,
)
from src.agents.core.a2a.client import A2AClient


# ── Schema tests ──────────────────────────────────────────────────────────────

class TestA2ASchemas:
    def test_task_input_defaults(self):
        ti = TaskInput(question="hello")
        assert ti.user_id == "anonymous"
        assert ti.context == {}
        # thread_id should be a valid UUID string
        UUID(ti.thread_id)  # raises ValueError if not valid UUID

    def test_a2a_request_defaults(self):
        req = A2ARequest(method="tasks/send", params=TaskInput(question="hi"))
        assert req.jsonrpc == "2.0"
        UUID(req.id)

    def test_a2a_response_ok_factory(self):
        output = TaskOutput(answer="42", thread_id="t1", agent="documents")
        resp = A2AResponse.ok("req-1", output)
        assert resp.id == "req-1"
        assert resp.result.answer == "42"
        assert resp.error is None

    def test_a2a_response_err_factory(self):
        resp = A2AResponse.err("req-2", -32601, "Method not found")
        assert resp.id == "req-2"
        assert resp.error.code == -32601
        assert resp.error.message == "Method not found"
        assert resp.result is None

    def test_agent_card_schema(self):
        card = AgentCard(
            name="documents",
            description="Retrieves docs",
            endpoint="http://localhost:8011",
            capabilities=[AgentCapability(name="search", description="search docs")],
        )
        assert card.version == "0.1.0"
        assert card.auth == {"type": "bearer"}

    def test_task_output_metadata_defaults_empty(self):
        out = TaskOutput(answer="ok", thread_id="t1", agent="supervisor")
        assert out.metadata == {}

    def test_a2a_request_serialise_round_trip(self):
        req = A2ARequest(
            method="tasks/send",
            params=TaskInput(question="test", user_id="alice"),
        )
        data = req.model_dump()
        restored = A2ARequest.model_validate(data)
        assert restored.params.user_id == "alice"
        assert restored.method == "tasks/send"


# ── A2AClient tests ───────────────────────────────────────────────────────────

BASE_URL = "http://agent.local:8011"


def _success_response(answer: str = "the answer", thread_id: str = "t1") -> dict:
    return A2AResponse.ok(
        "req-id",
        TaskOutput(answer=answer, thread_id=thread_id, agent="documents"),
    ).model_dump()


def _error_response(code: int = -32000, message: str = "internal error") -> dict:
    return A2AResponse.err("req-id", code, message).model_dump()


class TestA2AClient:
    @pytest.fixture
    def client(self):
        return A2AClient(timeout=5.0)

    # ── get_agent_card ──────────────────────────────────────────────────────

    @pytest.mark.asyncio
    async def test_get_agent_card_success(self, client):
        card_data = {
            "name": "documents",
            "description": "docs agent",
            "endpoint": BASE_URL,
            "capabilities": [{"name": "search", "description": "search"}],
            "version": "0.1.0",
            "auth": {"type": "bearer"},
        }

        def handler(request):
            return httpx.Response(200, json=card_data)

        transport = httpx.MockTransport(handler)
        with patch("httpx.AsyncClient", return_value=httpx.AsyncClient(transport=transport)):
            card = await client.get_agent_card(BASE_URL)

        assert card.name == "documents"
        assert card.endpoint == BASE_URL

    # ── send_task ────────────────────────────────────────────────────────────

    @pytest.mark.asyncio
    async def test_send_task_returns_task_output(self, client):
        def handler(request):
            return httpx.Response(200, json=_success_response("great answer", "t-99"))

        transport = httpx.MockTransport(handler)
        with patch("httpx.AsyncClient", return_value=httpx.AsyncClient(transport=transport)):
            output = await client.send_task(BASE_URL, "what is X?", thread_id="t-99", user_id="alice")

        assert output.answer == "great answer"
        assert output.thread_id == "t-99"
        assert output.agent == "documents"

    @pytest.mark.asyncio
    async def test_send_task_raises_on_a2a_error(self, client):
        def handler(request):
            return httpx.Response(200, json=_error_response(-32000, "something broke"))

        transport = httpx.MockTransport(handler)
        with patch("httpx.AsyncClient", return_value=httpx.AsyncClient(transport=transport)):
            with pytest.raises(ValueError, match="something broke"):
                await client.send_task(BASE_URL, "q", thread_id="t1")

    @pytest.mark.asyncio
    async def test_send_task_raises_on_http_error(self, client):
        def handler(request):
            return httpx.Response(503)

        transport = httpx.MockTransport(handler)
        with patch("httpx.AsyncClient", return_value=httpx.AsyncClient(transport=transport)):
            with pytest.raises(httpx.HTTPStatusError):
                await client.send_task(BASE_URL, "q", thread_id="t1")

    @pytest.mark.asyncio
    async def test_send_task_payload_includes_user_id_and_thread_id(self, client):
        captured = {}

        def handler(request):
            captured["body"] = json.loads(request.content)
            return httpx.Response(200, json=_success_response())

        transport = httpx.MockTransport(handler)
        with patch("httpx.AsyncClient", return_value=httpx.AsyncClient(transport=transport)):
            await client.send_task(BASE_URL, "hello", thread_id="t-abc", user_id="bob")

        params = captured["body"]["params"]
        assert params["user_id"] == "bob"
        assert params["thread_id"] == "t-abc"
        assert params["question"] == "hello"

    @pytest.mark.asyncio
    async def test_send_task_auto_generates_thread_id_when_none(self, client):
        captured = {}

        def handler(request):
            captured["body"] = json.loads(request.content)
            return httpx.Response(200, json=_success_response())

        transport = httpx.MockTransport(handler)
        with patch("httpx.AsyncClient", return_value=httpx.AsyncClient(transport=transport)):
            await client.send_task(BASE_URL, "q", thread_id=None)

        # Should be a valid UUID
        UUID(captured["body"]["params"]["thread_id"])

    @pytest.mark.asyncio
    async def test_send_task_raises_when_no_result_and_no_error(self, client):
        bad_response = {"jsonrpc": "2.0", "id": "x", "result": None, "error": None}

        def handler(request):
            return httpx.Response(200, json=bad_response)

        transport = httpx.MockTransport(handler)
        with patch("httpx.AsyncClient", return_value=httpx.AsyncClient(transport=transport)):
            with pytest.raises(ValueError, match="neither result nor error"):
                await client.send_task(BASE_URL, "q")
