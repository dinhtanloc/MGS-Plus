"""
Unit tests for MemoryService.

All tests use the ``memory_service`` fixture which replaces SQL Server with
SQLite in-memory, so no external DB is required.

Coverage areas:
- append / load_context (basic CRUD)
- user_id isolation (cross-user leak prevention)
- thread_id isolation (separate threads stay separate)
- clear_thread (user-scoped delete)
- prune_expired (TTL eviction)
- upsert_long_term / get_long_term
- should_summarise threshold
"""
from __future__ import annotations

import time
from datetime import datetime, timedelta

import pytest

from src.agents.core.memory.models import ShortTermMemory


# ── helpers ───────────────────────────────────────────────────────────────────

def _seed(svc, thread_id: str, user_id: str, n: int = 3):
    """Append *n* alternating user/assistant messages."""
    for i in range(n):
        role = "user" if i % 2 == 0 else "assistant"
        svc.append(thread_id, user_id, role, f"message {i}")


# ── basic CRUD ────────────────────────────────────────────────────────────────

class TestAppendAndLoadContext:
    def test_append_single_message(self, memory_service):
        memory_service.append("t1", "u1", "user", "hello")
        ctx = memory_service.load_context("t1", "u1")
        assert len(ctx) == 1
        assert ctx[0]["content"] == "hello"
        assert ctx[0]["role"] == "user"

    def test_messages_ordered_chronologically(self, memory_service):
        for i in range(5):
            memory_service.append("t1", "u1", "user", f"msg {i}")
        ctx = memory_service.load_context("t1", "u1")
        contents = [m["content"] for m in ctx]
        assert contents == [f"msg {i}" for i in range(5)]

    def test_load_returns_correct_dict_keys(self, memory_service):
        memory_service.append("t1", "u1", "assistant", "hi")
        ctx = memory_service.load_context("t1", "u1")
        assert set(ctx[0].keys()) == {"id", "thread_id", "user_id", "role", "content", "created_at"}

    def test_empty_thread_returns_empty_list(self, memory_service):
        ctx = memory_service.load_context("nonexistent", "u1")
        assert ctx == []


# ── user_id isolation (security) ─────────────────────────────────────────────

class TestUserIdIsolation:
    """A user must never see another user's messages, even if they share a thread_id."""

    def test_different_users_same_thread_are_isolated(self, memory_service):
        memory_service.append("shared-thread", "alice", "user", "alice message")
        memory_service.append("shared-thread", "bob", "user", "bob message")

        alice_ctx = memory_service.load_context("shared-thread", "alice")
        bob_ctx = memory_service.load_context("shared-thread", "bob")

        assert len(alice_ctx) == 1
        assert alice_ctx[0]["content"] == "alice message"

        assert len(bob_ctx) == 1
        assert bob_ctx[0]["content"] == "bob message"

    def test_load_context_does_not_cross_user_boundary(self, memory_service):
        memory_service.append("t-secret", "attacker", "user", "I should not see this")
        ctx = memory_service.load_context("t-secret", "victim")
        assert ctx == [], "Victim must not read attacker's messages"

    def test_clear_thread_only_deletes_own_messages(self, memory_service):
        memory_service.append("shared", "alice", "user", "alice msg")
        memory_service.append("shared", "bob", "user", "bob msg")

        # Alice clears her side of the thread
        memory_service.clear_thread("shared", "alice")

        assert memory_service.load_context("shared", "alice") == []
        assert len(memory_service.load_context("shared", "bob")) == 1

    def test_should_summarise_scoped_to_user(self, memory_service, settings):
        threshold = settings.long_term_summary_threshold
        # Alice accumulates enough messages
        for i in range(threshold):
            memory_service.append("t1", "alice", "user", f"msg {i}")
        # Bob has none
        assert memory_service.should_summarise("t1", "alice") is True
        assert memory_service.should_summarise("t1", "bob") is False


# ── thread isolation ──────────────────────────────────────────────────────────

class TestThreadIsolation:
    def test_two_threads_for_same_user_are_separate(self, memory_service):
        memory_service.append("thread-A", "u1", "user", "msg in A")
        memory_service.append("thread-B", "u1", "user", "msg in B")

        assert len(memory_service.load_context("thread-A", "u1")) == 1
        assert len(memory_service.load_context("thread-B", "u1")) == 1
        assert memory_service.load_context("thread-A", "u1")[0]["content"] == "msg in A"

    def test_clear_thread_does_not_affect_other_threads(self, memory_service):
        memory_service.append("t-keep", "u1", "user", "keep me")
        memory_service.append("t-clear", "u1", "user", "delete me")

        memory_service.clear_thread("t-clear", "u1")

        assert len(memory_service.load_context("t-keep", "u1")) == 1
        assert memory_service.load_context("t-clear", "u1") == []


# ── prune_expired ─────────────────────────────────────────────────────────────

class TestPruneExpired:
    def test_prune_removes_old_messages(self, memory_service, sqlite_session_factory):
        # Insert a message with an artificially old timestamp
        with sqlite_session_factory() as db:
            old_msg = ShortTermMemory(
                thread_id="old-thread",
                user_id="u1",
                role="user",
                content="ancient",
                created_at=datetime.utcnow() - timedelta(seconds=9999),
            )
            db.add(old_msg)
            db.commit()

        # Also add a fresh message through the service
        memory_service.append("fresh-thread", "u1", "user", "recent")

        deleted = memory_service.prune_expired()

        assert deleted == 1
        assert memory_service.load_context("old-thread", "u1") == []
        assert len(memory_service.load_context("fresh-thread", "u1")) == 1

    def test_prune_returns_zero_when_nothing_expired(self, memory_service):
        memory_service.append("t1", "u1", "user", "new msg")
        assert memory_service.prune_expired() == 0


# ── long-term memory ──────────────────────────────────────────────────────────

class TestLongTermMemory:
    def test_upsert_and_get_long_term(self, memory_service):
        memory_service.upsert_long_term("u1", "User likes short answers.")
        result = memory_service.get_long_term("u1")
        assert result is not None
        assert result["summary"] == "User likes short answers."
        assert result["user_id"] == "u1"

    def test_upsert_overwrites_existing_summary(self, memory_service):
        memory_service.upsert_long_term("u1", "First summary.")
        memory_service.upsert_long_term("u1", "Updated summary.")
        result = memory_service.get_long_term("u1")
        assert result["summary"] == "Updated summary."

    def test_get_long_term_returns_none_for_unknown_user(self, memory_service):
        assert memory_service.get_long_term("nobody") is None

    def test_long_term_is_per_user(self, memory_service):
        memory_service.upsert_long_term("alice", "Alice summary")
        memory_service.upsert_long_term("bob", "Bob summary")

        assert memory_service.get_long_term("alice")["summary"] == "Alice summary"
        assert memory_service.get_long_term("bob")["summary"] == "Bob summary"

    def test_upsert_with_embedding_id(self, memory_service):
        memory_service.upsert_long_term("u1", "summary text", embedding_id="emb-abc-123")
        result = memory_service.get_long_term("u1")
        assert result["embedding_id"] == "emb-abc-123"


# ── should_summarise ──────────────────────────────────────────────────────────

class TestShouldSummarise:
    def test_below_threshold_returns_false(self, memory_service, settings):
        threshold = settings.long_term_summary_threshold
        for i in range(threshold - 1):
            memory_service.append("t1", "u1", "user", f"msg {i}")
        assert memory_service.should_summarise("t1", "u1") is False

    def test_at_threshold_returns_true(self, memory_service, settings):
        threshold = settings.long_term_summary_threshold
        for i in range(threshold):
            memory_service.append("t1", "u1", "user", f"msg {i}")
        assert memory_service.should_summarise("t1", "u1") is True

    def test_above_threshold_returns_true(self, memory_service, settings):
        threshold = settings.long_term_summary_threshold
        for i in range(threshold + 5):
            memory_service.append("t1", "u1", "user", f"msg {i}")
        assert memory_service.should_summarise("t1", "u1") is True
