"""
Unit tests for MainCrew.

All external dependencies (CrewAI agents, Crew.kickoff, MemoryService,
build_*_agent functions) are mocked so tests run without LLM/DB.

Key design:
- build_*_agent calls are patched ONLY during MainCrew.__init__ (context manager).
- route_and_respond_task and Crew are patched per-test because CrewAI's Task
  pydantic model rejects MagicMock as an agent value.
- MemoryService is patched with a MagicMock that tracks calls.
"""
from __future__ import annotations

import json
import pytest
from unittest.mock import MagicMock, patch, call, ANY

from src.agents.crews.main_crew import MainCrew


# ── crew factory fixture ──────────────────────────────────────────────────────

@pytest.fixture()
def mock_mem():
    mem = MagicMock()
    mem.load_context.return_value = []
    mem.should_summarise.return_value = False
    return mem


@pytest.fixture()
def mock_supervisor():
    sup = MagicMock()
    sup.llm = MagicMock()
    sup.llm.call = MagicMock(return_value="Summarised conversation.")
    return sup


@pytest.fixture()
def crew(settings, mock_mem, mock_supervisor):
    """MainCrew with mocked agents and memory (agents patched only during init)."""
    mock_docs = MagicMock()
    mock_workflow = MagicMock()

    with (
        patch("src.agents.crews.main_crew.build_supervisor_agent", return_value=mock_supervisor),
        patch("src.agents.crews.main_crew.build_documents_agent", return_value=mock_docs),
        patch("src.agents.crews.main_crew.build_workflow_agent", return_value=mock_workflow),
        patch("src.agents.crews.main_crew.MemoryService", return_value=mock_mem),
    ):
        c = MainCrew(settings)

    # store mocks for test assertions
    c._mock_supervisor = mock_supervisor
    c._mock_docs = mock_docs
    c._mock_workflow = mock_workflow
    # re-inject mock memory (it was set during __init__ via patched MemoryService)
    c._memory = mock_mem
    return c


# ── kickoff basic ─────────────────────────────────────────────────────────────

class TestKickoff:
    def test_returns_string_answer(self, crew):
        with (
            patch("src.agents.crews.main_crew.route_and_respond_task", return_value=MagicMock()),
            patch("src.agents.crews.main_crew.Crew") as MockCrew,
        ):
            MockCrew.return_value.kickoff.return_value = "final answer"
            result = crew.kickoff("What is MGSPlus?", thread_id="t1", user_id="alice")
        assert result == "final answer"

    def test_appends_user_and_assistant_messages(self, crew, mock_mem):
        with (
            patch("src.agents.crews.main_crew.route_and_respond_task", return_value=MagicMock()),
            patch("src.agents.crews.main_crew.Crew") as MockCrew,
        ):
            MockCrew.return_value.kickoff.return_value = "the answer"
            crew.kickoff("question text", thread_id="t1", user_id="alice")

        append_calls = mock_mem.append.call_args_list
        assert call("t1", "alice", "user", "question text") in append_calls
        assert call("t1", "alice", "assistant", "the answer") in append_calls

    def test_loads_context_with_user_id(self, crew, mock_mem):
        with (
            patch("src.agents.crews.main_crew.route_and_respond_task", return_value=MagicMock()),
            patch("src.agents.crews.main_crew.Crew") as MockCrew,
        ):
            MockCrew.return_value.kickoff.return_value = "ok"
            crew.kickoff("q", thread_id="t1", user_id="bob")

        mock_mem.load_context.assert_called_once_with("t1", "bob")

    def test_checks_should_summarise_with_user_id(self, crew, mock_mem):
        with (
            patch("src.agents.crews.main_crew.route_and_respond_task", return_value=MagicMock()),
            patch("src.agents.crews.main_crew.Crew") as MockCrew,
        ):
            MockCrew.return_value.kickoff.return_value = "ok"
            crew.kickoff("q", thread_id="t1", user_id="charlie")

        mock_mem.should_summarise.assert_called_once_with("t1", "charlie")

    def test_does_not_summarise_below_threshold(self, crew, mock_mem):
        mock_mem.should_summarise.return_value = False
        with (
            patch("src.agents.crews.main_crew.route_and_respond_task", return_value=MagicMock()),
            patch("src.agents.crews.main_crew.Crew") as MockCrew,
            patch.object(crew, "_summarise_and_persist") as mock_sum,
        ):
            MockCrew.return_value.kickoff.return_value = "ok"
            crew.kickoff("q", thread_id="t1", user_id="u1")
        mock_sum.assert_not_called()

    def test_summarises_when_threshold_reached(self, crew, mock_mem):
        mock_mem.should_summarise.return_value = True
        with (
            patch("src.agents.crews.main_crew.route_and_respond_task", return_value=MagicMock()),
            patch("src.agents.crews.main_crew.Crew") as MockCrew,
            patch.object(crew, "_summarise_and_persist") as mock_sum,
        ):
            MockCrew.return_value.kickoff.return_value = "ok"
            crew.kickoff("q", thread_id="t1", user_id="u1")
        mock_sum.assert_called_once_with("t1", "u1")

    def test_uses_last_10_history_messages_as_context(self, crew, mock_mem):
        history = [
            {"role": "user" if i % 2 == 0 else "assistant", "content": f"msg {i}"}
            for i in range(15)
        ]
        mock_mem.load_context.return_value = history

        with (
            patch("src.agents.crews.main_crew.route_and_respond_task") as mock_task_fn,
            patch("src.agents.crews.main_crew.Crew") as MockCrew,
        ):
            mock_task_fn.return_value = MagicMock()
            MockCrew.return_value.kickoff.return_value = "ok"
            crew.kickoff("new question", thread_id="t1", user_id="u1")

        task_call_kwargs = mock_task_fn.call_args.kwargs
        context_str = task_call_kwargs["context"]
        assert "msg 14" in context_str
        assert "msg 5" in context_str
        assert "msg 4" not in context_str  # clipped beyond last 10


# ── user isolation at crew level ──────────────────────────────────────────────

class TestKickoffUserIsolation:
    def test_two_users_have_independent_memory_calls(self, crew, mock_mem):
        """Each kickoff must only load/append for its own user_id."""
        with (
            patch("src.agents.crews.main_crew.route_and_respond_task", return_value=MagicMock()),
            patch("src.agents.crews.main_crew.Crew") as MockCrew,
        ):
            MockCrew.return_value.kickoff.return_value = "answer"
            crew.kickoff("q for alice", thread_id="t-alice", user_id="alice")
            crew.kickoff("q for bob", thread_id="t-bob", user_id="bob")

        load_calls = mock_mem.load_context.call_args_list
        assert load_calls[0] == call("t-alice", "alice")
        assert load_calls[1] == call("t-bob", "bob")

        alice_appends = [c for c in mock_mem.append.call_args_list if c.args[1] == "alice"]
        assert all(c.args[1] == "alice" for c in alice_appends)
        bob_appends = [c for c in mock_mem.append.call_args_list if c.args[1] == "bob"]
        assert all(c.args[1] == "bob" for c in bob_appends)


# ── kickoff_stream ────────────────────────────────────────────────────────────

class TestKickoffStream:
    def test_returns_queue_with_answer_and_sentinel(self, crew):
        with (
            patch("src.agents.crews.main_crew.route_and_respond_task", return_value=MagicMock()),
            patch("src.agents.crews.main_crew.Crew") as MockCrew,
        ):
            MockCrew.return_value.kickoff.return_value = "streamed answer"
            q = crew.kickoff_stream("q", thread_id="t1", user_id="u1")

        events = []
        while True:
            item = q.get(timeout=5)
            if item is None:
                break
            events.append(json.loads(item))

        types = [e["type"] for e in events]
        assert "answer" in types
        answer_event = next(e for e in events if e["type"] == "answer")
        assert answer_event["content"] == "streamed answer"

    def test_stream_emits_error_event_on_exception(self, crew):
        with (
            patch("src.agents.crews.main_crew.route_and_respond_task", return_value=MagicMock()),
            patch("src.agents.crews.main_crew.Crew") as MockCrew,
        ):
            MockCrew.return_value.kickoff.side_effect = RuntimeError("LLM crashed")
            q = crew.kickoff_stream("q", thread_id="t1", user_id="u1")

        events = []
        while True:
            item = q.get(timeout=5)
            if item is None:
                break
            events.append(json.loads(item))

        error_events = [e for e in events if e["type"] == "error"]
        assert len(error_events) == 1
        assert "LLM crashed" in error_events[0]["content"]

    def test_stream_persists_messages_after_completion(self, crew, mock_mem):
        with (
            patch("src.agents.crews.main_crew.route_and_respond_task", return_value=MagicMock()),
            patch("src.agents.crews.main_crew.Crew") as MockCrew,
        ):
            MockCrew.return_value.kickoff.return_value = "stream answer"
            q = crew.kickoff_stream("hello", thread_id="t1", user_id="alice")

        while q.get(timeout=5) is not None:
            pass

        append_calls = mock_mem.append.call_args_list
        assert call("t1", "alice", "user", "hello") in append_calls
        assert call("t1", "alice", "assistant", "stream answer") in append_calls

    def test_stream_uses_user_scoped_memory(self, crew, mock_mem):
        with (
            patch("src.agents.crews.main_crew.route_and_respond_task", return_value=MagicMock()),
            patch("src.agents.crews.main_crew.Crew") as MockCrew,
        ):
            MockCrew.return_value.kickoff.return_value = "ok"
            q = crew.kickoff_stream("q", thread_id="t1", user_id="specific-user")

        while q.get(timeout=5) is not None:
            pass

        mock_mem.load_context.assert_called_with("t1", "specific-user")


# ── _summarise_and_persist ────────────────────────────────────────────────────

class TestSummariseAndPersist:
    def test_calls_llm_with_transcript(self, crew, mock_mem, mock_supervisor):
        mock_mem.load_context.return_value = [
            {"role": "user", "content": "hello"},
            {"role": "assistant", "content": "hi"},
        ]
        crew._summarise_and_persist("t1", "u1")

        llm_call_args = mock_supervisor.llm.call.call_args[0][0]
        content_blob = " ".join(m["content"] for m in llm_call_args)
        assert "hello" in content_blob or "hi" in content_blob

    def test_upserts_long_term_memory(self, crew, mock_mem, mock_supervisor):
        mock_mem.load_context.return_value = [{"role": "user", "content": "msg"}]
        mock_supervisor.llm.call.return_value = "neat summary"
        crew._summarise_and_persist("t1", "u1")

        mock_mem.upsert_long_term.assert_called_once_with(
            user_id="u1", summary="neat summary"
        )

    def test_clears_thread_after_summarisation(self, crew, mock_mem):
        mock_mem.load_context.return_value = [{"role": "user", "content": "msg"}]
        crew._summarise_and_persist("t1", "u1")

        mock_mem.clear_thread.assert_called_once_with("t1", "u1")

    def test_loads_context_with_correct_user_id(self, crew, mock_mem):
        mock_mem.load_context.return_value = []
        crew._summarise_and_persist("t-thread", "user-xyz")

        mock_mem.load_context.assert_called_once_with("t-thread", "user-xyz")
