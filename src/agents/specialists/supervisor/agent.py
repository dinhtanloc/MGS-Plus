from __future__ import annotations

from typing import Optional

from crewai import Agent

from src.agents.core.config import Settings, build_llm, get_settings
from src.agents.specialists.documents.tools import MCPSearchTool, QdrantSearchTool
from src.agents.specialists.workflow.tools import AuthSettingsTool, ChangePasswordTool, WebActionTool
from src.agents.specialists.supervisor.prompts import BACKSTORY, GOAL, ROLE


def build_supervisor_agent(settings: Optional[Settings] = None) -> Agent:
    """Build the Supervisor Agent — single sequential agent with all tools.

    Uses Process.sequential so it handles document search and workflow actions
    directly without multi-agent delegation (which requires stronger models).
    """
    cfg = settings or get_settings()
    llm = build_llm(cfg)

    return Agent(
        role=ROLE,
        goal=GOAL,
        backstory=BACKSTORY,
        tools=[
            QdrantSearchTool(settings=cfg),
            MCPSearchTool(settings=cfg),
            ChangePasswordTool(settings=cfg),
            AuthSettingsTool(settings=cfg),
            WebActionTool(settings=cfg),
        ],
        llm=llm,
        verbose=True,
        allow_delegation=False,
        max_iter=8,
    )
