from __future__ import annotations

from typing import Any, Dict, List, Literal, Optional
from uuid import uuid4

from pydantic import BaseModel, Field


# ── AgentCard ─────────────────────────────────────────────────────────────────

class AgentCapability(BaseModel):
    name: str
    description: str


class AgentCard(BaseModel):
    """Describes an agent — served at GET /.well-known/agent.json"""

    name: str
    description: str
    version: str = "0.1.0"
    endpoint: str                        # base URL of this agent's A2A service
    capabilities: List[AgentCapability]
    auth: Dict[str, Any] = Field(default_factory=lambda: {"type": "bearer"})


# ── A2A Task (input) ──────────────────────────────────────────────────────────

class TaskInput(BaseModel):
    """The payload sent inside an A2A JSON-RPC request."""

    question: str
    thread_id: str = Field(default_factory=lambda: str(uuid4()))
    user_id: str = "anonymous"
    context: Dict[str, Any] = Field(default_factory=dict)


# ── A2A JSON-RPC envelopes ────────────────────────────────────────────────────

class A2ARequest(BaseModel):
    jsonrpc: Literal["2.0"] = "2.0"
    method: str                          # e.g. "tasks/send"
    params: TaskInput
    id: str = Field(default_factory=lambda: str(uuid4()))


class A2AError(BaseModel):
    code: int
    message: str
    data: Optional[Any] = None


class TaskOutput(BaseModel):
    """The result payload inside a successful A2A response."""

    answer: str
    thread_id: str
    agent: str
    metadata: Dict[str, Any] = Field(default_factory=dict)


class A2AResponse(BaseModel):
    jsonrpc: Literal["2.0"] = "2.0"
    result: Optional[TaskOutput] = None
    error: Optional[A2AError] = None
    id: str

    @classmethod
    def ok(cls, request_id: str, output: TaskOutput) -> "A2AResponse":
        return cls(id=request_id, result=output)

    @classmethod
    def err(cls, request_id: str, code: int, message: str) -> "A2AResponse":
        return cls(id=request_id, error=A2AError(code=code, message=message))
