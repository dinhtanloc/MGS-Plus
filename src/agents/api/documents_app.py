from __future__ import annotations

from fastapi import Depends, FastAPI

from src.agents.core.a2a.schemas import (
    A2ARequest,
    A2AResponse,
    AgentCapability,
    AgentCard,
    TaskOutput,
)
from src.agents.core.config import Settings, get_settings
from src.agents.agents.documents.agent import build_documents_agent
from src.agents.crews.tasks.document_tasks import retrieve_knowledge_task

from crewai import Crew, Process

app = FastAPI(
    title="MGSPlus Documents Agent",
    description="Retrieves information from Qdrant and external MCP sources.",
    version="0.1.0",
)


def _run_documents_task(question: str, user_id: str) -> str:
    agent = build_documents_agent()
    task = retrieve_knowledge_task(agent, question, user_id)
    crew = Crew(agents=[agent], tasks=[task], process=Process.sequential, verbose=True)
    return str(crew.kickoff())


# ── A2A protocol ──────────────────────────────────────────────────────────────

@app.get("/.well-known/agent.json", response_model=AgentCard, tags=["A2A"])
async def agent_card(settings: Settings = Depends(get_settings)) -> AgentCard:
    return AgentCard(
        name="documents",
        description="Retrieves documents, images, and knowledge from Qdrant and MCP sources.",
        endpoint=settings.documents_base_url,
        capabilities=[
            AgentCapability(name="tasks/send", description="Answer knowledge retrieval questions"),
            AgentCapability(name="qdrant_search", description="Search internal vector database"),
            AgentCapability(name="mcp_search", description="Search external sources via MCP"),
        ],
    )


@app.post("/a2a", response_model=A2AResponse, tags=["A2A"])
async def a2a_endpoint(request: A2ARequest) -> A2AResponse:
    if request.method != "tasks/send":
        return A2AResponse.err(request.id, -32601, f"Method '{request.method}' not found")
    try:
        answer = _run_documents_task(
            request.params.question,
            request.params.user_id,
        )
        return A2AResponse.ok(
            request.id,
            TaskOutput(
                answer=answer,
                thread_id=request.params.thread_id,
                agent="documents",
            ),
        )
    except Exception as exc:
        return A2AResponse.err(request.id, -32000, str(exc))


@app.get("/health", tags=["Health"])
async def health() -> dict:
    return {"status": "ok", "agent": "documents"}
