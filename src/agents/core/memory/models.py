from __future__ import annotations

from datetime import datetime

from sqlalchemy import Column, DateTime, ForeignKey, Integer, String, Text, Index
from sqlalchemy.orm import relationship

from src.agents.core.db.sqlalchemy_engine import Base


class ShortTermMemory(Base):
    """Per-thread message history. Pruned after TTL or explicit clear."""

    __tablename__ = "short_term_memory"

    id = Column(Integer, primary_key=True, autoincrement=True)
    thread_id = Column(String(128), nullable=False, index=True)
    user_id = Column(String(128), nullable=False, index=True)
    role = Column(String(32), nullable=False)   # "user" | "assistant" | "system"
    content = Column(Text, nullable=False)
    created_at = Column(DateTime, default=datetime.utcnow, nullable=False)

    __table_args__ = (
        Index("ix_stm_thread_created", "thread_id", "created_at"),
    )

    def to_dict(self) -> dict:
        return {
            "id": self.id,
            "thread_id": self.thread_id,
            "user_id": self.user_id,
            "role": self.role,
            "content": self.content,
            "created_at": self.created_at.isoformat() if self.created_at else None,
        }


class LongTermMemory(Base):
    """Per-user summarised memory persisted across sessions."""

    __tablename__ = "long_term_memory"

    id = Column(Integer, primary_key=True, autoincrement=True)
    user_id = Column(String(128), nullable=False, index=True)
    # Plain-text summary written by the LLM
    summary = Column(Text, nullable=False)
    # ID of the embedding stored in Qdrant (for semantic retrieval)
    embedding_id = Column(String(256), nullable=True)
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow, nullable=False)

    __table_args__ = (
        Index("ix_ltm_user_updated", "user_id", "updated_at"),
    )

    def to_dict(self) -> dict:
        return {
            "id": self.id,
            "user_id": self.user_id,
            "summary": self.summary,
            "embedding_id": self.embedding_id,
            "updated_at": self.updated_at.isoformat() if self.updated_at else None,
        }
