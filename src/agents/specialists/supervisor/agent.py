from __future__ import annotations

from typing import Optional

from crewai import Agent

from src.agents.core.config import Settings, build_llm, get_settings
from src.agents.agents.supervisor.tools import RouteToDocumentsTool, RouteToWorkflowTool


def build_supervisor_agent(settings: Optional[Settings] = None) -> Agent:
    """Build the Supervisor Agent — the entry point for all user queries.

    The supervisor:
    1. Analyses the user's intent.
    2. Routes to Documents agent for knowledge/retrieval tasks.
    3. Routes to Workflow agent for platform action tasks.
    4. Synthesises a final answer from sub-agent responses.

    In the hierarchical CrewAI process this agent also acts as *manager_agent*,
    so it can natively delegate CrewAI tasks to Documents/Workflow agents without
    going through the A2A HTTP layer (the A2A tools are used for cross-service calls).
    """
    cfg = settings or get_settings()

    llm = build_llm(cfg)

    return Agent(
        role="Supervisor & Intent Router",
        goal=(
            "Understand the user's question or request, classify its intent, "
            "and route it to the most appropriate specialist agent. "
            "Synthesise a clear, helpful final response from the sub-agent outputs."
        ),
        backstory=(
            "You are the intelligent orchestrator of the MGSPlus AI assistant system. "
            "You receive every user message first and decide: "
            "(1) Is this a knowledge/document question? → Documents agent. "
            "(2) Is this a platform task (account, settings, chatbot)? → Workflow agent. "
            "(3) Is it a general greeting or simple query you can answer directly? → Answer yourself. "
            "You always communicate in the user's language and ensure a seamless experience."
        ),
        tools=[
            RouteToDocumentsTool(settings=cfg),
            RouteToWorkflowTool(settings=cfg),
        ],
        llm=llm,
        verbose=True,
        allow_delegation=True,   # required for hierarchical manager role
        max_iter=10,
    )
