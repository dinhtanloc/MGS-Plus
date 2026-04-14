"""
Integration tests for the supervisor FastAPI application.

Uses httpx.AsyncClient + ASGITransport (no real network / no real LLM).
MainCrew is fully mocked so tests run without any external services.

Coverage:
- GET  /.well-known/agent.json   → AgentCard shape
- POST /a2a                      → tasks/send happy path + error paths
- POST /chat                     → ChatResponse happy path + 500 on error
- POST /chat/stream              → SSE event stream
- GET  /health                   → liveness check
"""
from __future__ import annotations

import json
import pytest
from unittest.mock import MagicMock
from starlette.testclient import TestClient

from src.agents.api.supervisor_app import app
from src.agents.api import deps


# ── fixtures ──────────────────────────────────────────────────────────────────

@pytest.fixture()
def mock_crew():
    """A MainCrew mock injected via FastAPI dependency override."""
    crew = MagicMock()
    crew.kickoff.return_value = "mocked crew answer"

    # kickoff_stream returns a queue with one answer event + sentinel
    import queue as _queue

    def _make_stream_queue(*args, **kwargs):
        q = _queue.Queue()
        q.put(json.dumps({"type": "answer", "content": "stream answer"}))
        q.put(None)
        return q

    crew.kickoff_stream.side_effect = _make_stream_queue
    return crew


@pytest.fixture()
def client(mock_crew):
    """Starlette TestClient with MainCrew dependency overridden."""
    app.dependency_overrides[deps.get_main_crew] = lambda: mock_crew
    with TestClient(app, raise_server_exceptions=False) as c:
        yield c
    app.dependency_overrides.clear()


# ── GET /.well-known/agent.json ───────────────────────────────────────────────

class TestAgentCard:
    def test_returns_200(self, client):
        resp = client.get("/.well-known/agent.json")
        assert resp.status_code == 200

    def test_response_has_required_fields(self, client):
        data = client.get("/.well-known/agent.json").json()
        assert data["name"] == "supervisor"
        assert "endpoint" in data
        assert isinstance(data["capabilities"], list)
        assert len(data["capabilities"]) >= 1

    def test_capabilities_have_name_and_description(self, client):
        data = client.get("/.well-known/agent.json").json()
        for cap in data["capabilities"]:
            assert "name" in cap
            assert "description" in cap


# ── GET /health ───────────────────────────────────────────────────────────────

class TestHealth:
    def test_returns_200_ok(self, client):
        resp = client.get("/health")
        assert resp.status_code == 200
        assert resp.json()["status"] == "ok"
        assert resp.json()["agent"] == "supervisor"


# ── POST /a2a ─────────────────────────────────────────────────────────────────

def _a2a_payload(question: str = "hello", method: str = "tasks/send",
                 user_id: str = "alice", thread_id: str = "t1") -> dict:
    return {
        "jsonrpc": "2.0",
        "id": "req-test",
        "method": method,
        "params": {
            "question": question,
            "thread_id": thread_id,
            "user_id": user_id,
            "context": {},
        },
    }


class TestA2AEndpoint:
    def test_tasks_send_returns_200(self, client):
        resp = client.post("/a2a", json=_a2a_payload())
        assert resp.status_code == 200

    def test_tasks_send_returns_answer(self, client):
        data = client.post("/a2a", json=_a2a_payload("tell me something")).json()
        assert data["result"]["answer"] == "mocked crew answer"
        assert data["result"]["agent"] == "supervisor"
        assert data["error"] is None

    def test_tasks_send_returns_thread_id(self, client):
        data = client.post("/a2a", json=_a2a_payload(thread_id="t-xyz")).json()
        assert data["result"]["thread_id"] == "t-xyz"

    def test_unknown_method_returns_error(self, client):
        payload = _a2a_payload()
        payload["method"] = "tasks/unknown"
        data = client.post("/a2a", json=payload).json()
        assert data["error"] is not None
        assert data["error"]["code"] == -32601

    def test_crew_exception_returns_a2a_error(self, client, mock_crew):
        mock_crew.kickoff.side_effect = RuntimeError("LLM unavailable")
        data = client.post("/a2a", json=_a2a_payload()).json()
        assert data["error"] is not None
        assert data["error"]["code"] == -32000
        assert "LLM unavailable" in data["error"]["message"]

    def test_user_id_forwarded_to_crew(self, client, mock_crew):
        client.post("/a2a", json=_a2a_payload(user_id="specific-user"))
        call_kwargs = mock_crew.kickoff.call_args.kwargs
        assert call_kwargs["user_id"] == "specific-user"

    def test_thread_id_forwarded_to_crew(self, client, mock_crew):
        client.post("/a2a", json=_a2a_payload(thread_id="my-thread"))
        call_kwargs = mock_crew.kickoff.call_args.kwargs
        assert call_kwargs["thread_id"] == "my-thread"


# ── POST /chat ────────────────────────────────────────────────────────────────

class TestChatEndpoint:
    def test_returns_200_with_answer(self, client):
        payload = {"question": "what is MGSPlus?", "thread_id": "t1", "user_id": "alice"}
        resp = client.post("/chat", json=payload)
        assert resp.status_code == 200
        data = resp.json()
        assert data["answer"] == "mocked crew answer"
        assert data["thread_id"] == "t1"
        assert data["agent"] == "supervisor"

    def test_missing_question_returns_422(self, client):
        resp = client.post("/chat", json={"thread_id": "t1"})
        assert resp.status_code == 422

    def test_crew_exception_returns_500(self, client, mock_crew):
        mock_crew.kickoff.side_effect = RuntimeError("broken")
        resp = client.post("/chat", json={"question": "q", "thread_id": "t1", "user_id": "u"})
        assert resp.status_code == 500
        assert "broken" in resp.json()["detail"]

    def test_user_id_forwarded_to_crew(self, client, mock_crew):
        client.post("/chat", json={"question": "q", "thread_id": "t1", "user_id": "bob"})
        call_kwargs = mock_crew.kickoff.call_args.kwargs
        assert call_kwargs["user_id"] == "bob"

    def test_default_user_id_is_anonymous(self, client, mock_crew):
        client.post("/chat", json={"question": "q", "thread_id": "t1"})
        call_kwargs = mock_crew.kickoff.call_args.kwargs
        assert call_kwargs["user_id"] == "anonymous"


# ── POST /chat/stream ─────────────────────────────────────────────────────────

class TestChatStreamEndpoint:
    def test_returns_200_event_stream(self, client):
        payload = {"question": "stream this", "thread_id": "t1", "user_id": "alice"}
        resp = client.post("/chat/stream", json=payload)
        assert resp.status_code == 200
        assert "text/event-stream" in resp.headers["content-type"]

    def test_stream_contains_start_event(self, client):
        payload = {"question": "stream this", "thread_id": "t1", "user_id": "alice"}
        resp = client.post("/chat/stream", json=payload)
        events = [
            json.loads(line[len("data: "):])
            for line in resp.text.strip().split("\n\n")
            if line.startswith("data: ")
        ]
        types = [e["type"] for e in events]
        assert "start" in types

    def test_stream_contains_answer_event(self, client):
        payload = {"question": "stream this", "thread_id": "t1", "user_id": "alice"}
        resp = client.post("/chat/stream", json=payload)
        events = [
            json.loads(line[len("data: "):])
            for line in resp.text.strip().split("\n\n")
            if line.startswith("data: ")
        ]
        answer_events = [e for e in events if e["type"] == "answer"]
        assert len(answer_events) == 1
        assert answer_events[0]["content"] == "stream answer"

    def test_stream_start_event_contains_thread_id(self, client):
        payload = {"question": "q", "thread_id": "my-thread", "user_id": "u"}
        resp = client.post("/chat/stream", json=payload)
        events = [
            json.loads(line[len("data: "):])
            for line in resp.text.strip().split("\n\n")
            if line.startswith("data: ")
        ]
        start = next(e for e in events if e["type"] == "start")
        assert start["thread_id"] == "my-thread"
