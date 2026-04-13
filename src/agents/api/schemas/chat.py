from __future__ import annotations

from typing import Any, Dict, Optional
from uuid import uuid4

from pydantic import BaseModel, Field


class ChatRequest(BaseModel):
    question: str = Field(..., description="User's question or request")
    thread_id: Optional[str] = Field(
        default_factory=lambda: str(uuid4()),
        description="Conversation thread ID — creates new thread if omitted",
    )
    user_id: str = Field(default="anonymous", description="Authenticated user ID")
    context: Dict[str, Any] = Field(
        default_factory=dict,
        description="Optional extra context key-value pairs",
    )


class ChatResponse(BaseModel):
    answer: str
    thread_id: str
    agent: str = "supervisor"
    metadata: Dict[str, Any] = Field(default_factory=dict)
