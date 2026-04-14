"""
Unit tests for supervisor routing tools.

RouteToDocumentsTool and RouteToWorkflowTool both call the A2AClient
internally.  We patch A2AClient.send_task so no real HTTP calls happen.
"""
from __future__ import annotations

import pytest
from unittest.mock import AsyncMock, MagicMock, patch

from src.agents.core.a2a.schemas import TaskOutput
from src.agents.agents.supervisor.tools import (
    RouteToDocumentsTool,
    RouteToWorkflowTool,
    _run_async,
)


# ── _run_async helper ─────────────────────────────────────────────────────────

class TestRunAsync:
    def test_runs_coroutine_and_returns_result(self):
        async def _coro():
            return 42

        assert _run_async(_coro()) == 42

    def test_propagates_exception(self):
        async def _failing():
            raise ValueError("boom")

        with pytest.raises(ValueError, match="boom"):
            _run_async(_failing())

    def test_works_inside_running_event_loop(self):
        """Simulates being called from within an async context (e.g. FastAPI)."""
        import asyncio

        async def _outer():
            return _run_async(_inner())

        async def _inner():
            return "nested"

        assert asyncio.run(_outer()) == "nested"


# ── RouteToDocumentsTool ──────────────────────────────────────────────────────

def _make_task_output(answer: str = "doc answer", thread_id: str = "t1") -> TaskOutput:
    return TaskOutput(answer=answer, thread_id=thread_id, agent="documents")


class TestRouteToDocumentsTool:
    @pytest.fixture(autouse=True)
    def tool(self, settings):
        with patch("src.agents.agents.supervisor.tools.A2AClient") as MockClient:
            mock_instance = MockClient.return_value
            mock_instance.send_task = AsyncMock(return_value=_make_task_output("doc answer"))
            self._mock_client = mock_instance
            yield RouteToDocumentsTool(settings=settings)

    def test_run_returns_answer_string(self, tool):
        result = tool._run(question="What is diabetes?", thread_id="t1", user_id="alice")
        assert result == "doc answer"

    def test_run_calls_send_task_with_documents_url(self, tool, settings):
        tool._run(question="explain X", thread_id="t1", user_id="alice")
        call_kwargs = self._mock_client.send_task.call_args.kwargs
        assert call_kwargs["base_url"] == settings.documents_base_url
        assert call_kwargs["question"] == "explain X"
        assert call_kwargs["thread_id"] == "t1"
        assert call_kwargs["user_id"] == "alice"

    def test_run_default_user_id_is_anonymous(self, tool):
        tool._run(question="q", thread_id="t1")
        call_kwargs = self._mock_client.send_task.call_args.kwargs
        assert call_kwargs["user_id"] == "anonymous"

    def test_tool_name_and_description_not_empty(self, tool):
        assert tool.name
        assert tool.description

    def test_tool_has_args_schema(self, tool):
        assert tool.args_schema is not None


# ── RouteToWorkflowTool ───────────────────────────────────────────────────────

class TestRouteToWorkflowTool:
    @pytest.fixture(autouse=True)
    def tool(self, settings):
        with patch("src.agents.agents.supervisor.tools.A2AClient") as MockClient:
            mock_instance = MockClient.return_value
            mock_instance.send_task = AsyncMock(return_value=_make_task_output("wf answer"))
            self._mock_client = mock_instance
            yield RouteToWorkflowTool(settings=settings)

    def test_run_returns_answer_string(self, tool):
        result = tool._run(question="change my password", thread_id="t2", user_id="bob")
        assert result == "wf answer"

    def test_run_calls_send_task_with_workflow_url(self, tool, settings):
        tool._run(question="update profile", thread_id="t2", user_id="bob")
        call_kwargs = self._mock_client.send_task.call_args.kwargs
        assert call_kwargs["base_url"] == settings.workflow_base_url
        assert call_kwargs["user_id"] == "bob"

    def test_run_propagates_a2a_error(self, tool):
        self._mock_client.send_task = AsyncMock(side_effect=ValueError("A2A error 404"))
        with pytest.raises(ValueError, match="A2A error"):
            tool._run(question="q", thread_id="t1", user_id="u1")

    def test_different_user_ids_are_forwarded(self, tool):
        for uid in ("alice", "bob", "charlie"):
            self._mock_client.send_task = AsyncMock(return_value=_make_task_output("ok"))
            tool._run(question="q", thread_id="t", user_id=uid)
            call_kwargs = self._mock_client.send_task.call_args.kwargs
            assert call_kwargs["user_id"] == uid
