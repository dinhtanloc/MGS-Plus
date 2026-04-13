from __future__ import annotations

from datetime import datetime, timedelta
from typing import List, Optional

from sqlalchemy.orm import Session

from src.agents.core.config import Settings, get_settings
from src.agents.core.db.sqlalchemy_engine import get_db_session
from src.agents.core.memory.models import LongTermMemory, ShortTermMemory


class MemoryService:
    """Load and persist conversation memory for an agent session."""

    def __init__(self, settings: Optional[Settings] = None) -> None:
        self._settings = settings or get_settings()

    def _session(self) -> Session:
        return get_db_session(self._settings)

    # ── Short-term (per-thread) ───────────────────────────────────────────────

    def load_context(self, thread_id: str) -> List[dict]:
        """Return all messages for *thread_id* ordered chronologically."""
        with self._session() as db:
            rows = (
                db.query(ShortTermMemory)
                .filter(ShortTermMemory.thread_id == thread_id)
                .order_by(ShortTermMemory.created_at)
                .all()
            )
            return [r.to_dict() for r in rows]

    def append(self, thread_id: str, user_id: str, role: str, content: str) -> None:
        """Append a single message to the short-term store."""
        with self._session() as db:
            db.add(
                ShortTermMemory(
                    thread_id=thread_id,
                    user_id=user_id,
                    role=role,
                    content=content,
                )
            )
            db.commit()

    def clear_thread(self, thread_id: str) -> None:
        """Delete all messages for a thread (e.g. after summarisation)."""
        with self._session() as db:
            db.query(ShortTermMemory).filter(
                ShortTermMemory.thread_id == thread_id
            ).delete()
            db.commit()

    def prune_expired(self) -> int:
        """Delete messages older than short_term_ttl_seconds. Returns count deleted."""
        cutoff = datetime.utcnow() - timedelta(seconds=self._settings.short_term_ttl_seconds)
        with self._session() as db:
            count = (
                db.query(ShortTermMemory)
                .filter(ShortTermMemory.created_at < cutoff)
                .delete()
            )
            db.commit()
            return count

    # ── Long-term (per-user) ─────────────────────────────────────────────────

    def get_long_term(self, user_id: str) -> Optional[dict]:
        """Retrieve the latest long-term memory summary for a user."""
        with self._session() as db:
            row = (
                db.query(LongTermMemory)
                .filter(LongTermMemory.user_id == user_id)
                .order_by(LongTermMemory.updated_at.desc())
                .first()
            )
            return row.to_dict() if row else None

    def upsert_long_term(
        self,
        user_id: str,
        summary: str,
        embedding_id: Optional[str] = None,
    ) -> None:
        """Write or overwrite the long-term summary for a user."""
        with self._session() as db:
            row = (
                db.query(LongTermMemory)
                .filter(LongTermMemory.user_id == user_id)
                .first()
            )
            if row:
                row.summary = summary
                row.embedding_id = embedding_id
                row.updated_at = datetime.utcnow()
            else:
                db.add(
                    LongTermMemory(
                        user_id=user_id,
                        summary=summary,
                        embedding_id=embedding_id,
                    )
                )
            db.commit()

    def should_summarise(self, thread_id: str) -> bool:
        """True when the thread exceeds the summarisation threshold."""
        messages = self.load_context(thread_id)
        return len(messages) >= self._settings.long_term_summary_threshold
