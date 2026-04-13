from __future__ import annotations

from fastapi import Depends, FastAPI, HTTPException

from src.agents.core.a2a.schemas import (
    A2ARequest,
    A2AResponse,
    AgentCapability,
    AgentCard,
    TaskOutput,
)
from src.agents.core.config import Settings, get_settings
from src.agents.api.deps import get_main_crew
from src.agents.api.schemas.chat import ChatRequest, ChatResponse
from src.agents.crews.main_crew import MainCrew

app = FastAPI(
    title="MGSPlus Supervisor Agent",
    description="Entry point for all user queries — routes to Documents or Workflow agent.",
    version="0.1.0",
)


# ── A2A protocol ──────────────────────────────────────────────────────────────

@app.get("/.well-known/agent.json", response_model=AgentCard, tags=["A2A"])
async def agent_card(settings: Settings = Depends(get_settings)) -> AgentCard:
    return AgentCard(
        name="supervisor",
        description="Receives user questions and routes them to specialist agents.",
        endpoint=settings.supervisor_base_url,
        capabilities=[
            AgentCapability(name="tasks/send", description="Accept a question and return an answer"),
            AgentCapability(name="routing", description="Route to documents or workflow agents"),
        ],
    )


@app.post("/a2a", response_model=A2AResponse, tags=["A2A"])
async def a2a_endpoint(
    request: A2ARequest,
    crew: MainCrew = Depends(get_main_crew),
) -> A2AResponse:
    if request.method != "tasks/send":
        return A2AResponse.err(request.id, -32601, f"Method '{request.method}' not found")
    try:
        answer = crew.kickoff(
            question=request.params.question,
            thread_id=request.params.thread_id,
            user_id=request.params.user_id,
        )
        return A2AResponse.ok(
            request.id,
            TaskOutput(
                answer=answer,
                thread_id=request.params.thread_id,
                agent="supervisor",
            ),
        )
    except Exception as exc:
        return A2AResponse.err(request.id, -32000, str(exc))


# ── REST chat endpoint (for direct callers, e.g. frontend) ───────────────────

@app.post("/chat", response_model=ChatResponse, tags=["Chat"])
async def chat(
    body: ChatRequest,
    crew: MainCrew = Depends(get_main_crew),
) -> ChatResponse:
    try:
        answer = crew.kickoff(
            question=body.question,
            thread_id=body.thread_id,
            user_id=body.user_id,
        )
        return ChatResponse(answer=answer, thread_id=body.thread_id)
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc))


# ── Health ────────────────────────────────────────────────────────────────────

@app.get("/health", tags=["Health"])
async def health() -> dict:
    return {"status": "ok", "agent": "supervisor"}
