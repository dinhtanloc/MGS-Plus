from __future__ import annotations

from fastapi import Depends, FastAPI
from prometheus_fastapi_instrumentator import Instrumentator

from src.agents.api.deps import verify_api_key
from src.agents.core.a2a.schemas import (
    A2ARequest,
    A2AResponse,
    AgentCapability,
    AgentCard,
    TaskOutput,
)
from src.agents.core.config import Settings, get_settings
from src.agents.agents.workflow.agent import build_workflow_agent
from src.agents.crews.tasks.workflow_tasks import execute_platform_task

from crewai import Crew, Process

app = FastAPI(
    title="MGSPlus Workflow Agent",
    description="Executes platform tasks: account management, chatbot config, and more.",
    version="0.1.0",
)

Instrumentator().instrument(app).expose(app)


def _run_workflow_task(request: str, user_id: str) -> str:
    agent = build_workflow_agent()
    task = execute_platform_task(agent, request, user_id)
    crew = Crew(agents=[agent], tasks=[task], process=Process.sequential, verbose=True)
    return str(crew.kickoff())


# ── A2A protocol ──────────────────────────────────────────────────────────────

@app.get("/.well-known/agent.json", response_model=AgentCard, tags=["A2A"])
async def agent_card(settings: Settings = Depends(get_settings)) -> AgentCard:
    return AgentCard(
        name="workflow",
        description="Handles platform workflow tasks: password changes, auth settings, web actions.",
        endpoint=settings.workflow_base_url,
        capabilities=[
            AgentCapability(name="tasks/send", description="Execute a platform workflow task"),
            AgentCapability(name="change_password", description="Initiate password reset"),
            AgentCapability(name="manage_auth_settings", description="Toggle 2FA, revoke sessions"),
            AgentCapability(name="perform_web_action", description="Execute general platform actions"),
        ],
    )


@app.post("/a2a", response_model=A2AResponse, tags=["A2A"],
          dependencies=[Depends(verify_api_key)])
async def a2a_endpoint(request: A2ARequest) -> A2AResponse:
    if request.method != "tasks/send":
        return A2AResponse.err(request.id, -32601, f"Method '{request.method}' not found")
    try:
        answer = _run_workflow_task(
            request.params.question,
            request.params.user_id,
        )
        return A2AResponse.ok(
            request.id,
            TaskOutput(
                answer=answer,
                thread_id=request.params.thread_id,
                agent="workflow",
            ),
        )
    except Exception as exc:
        return A2AResponse.err(request.id, -32000, str(exc))


@app.get("/health", tags=["Health"])
async def health() -> dict:
    return {"status": "ok", "agent": "workflow"}
