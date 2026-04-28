from __future__ import annotations

from typing import Optional

from crewai import Agent

from src.agents.core.config import Settings, build_llm, get_settings
from src.agents.specialists.workflow.tools import (
    AuthSettingsTool,
    ChangePasswordTool,
    MGSPlusPageFetchTool,
    SearchPlatformTool,
    ViewAppointmentsTool,
    ViewMedicalRecordsTool,
    WebActionTool,
)
from src.agents.specialists.workflow.prompts import BACKSTORY, GOAL, ROLE


def build_workflow_agent(settings: Optional[Settings] = None) -> Agent:
    """Build the Workflow Agent — helps users navigate and act on the MGSPlus platform."""
    cfg = settings or get_settings()

    llm = build_llm(cfg)

    return Agent(
        role=ROLE,
        goal=GOAL,
        backstory=BACKSTORY,
        tools=[
            ChangePasswordTool(settings=cfg),
            AuthSettingsTool(settings=cfg),
            WebActionTool(settings=cfg),
            ViewAppointmentsTool(settings=cfg),
            ViewMedicalRecordsTool(settings=cfg),
            SearchPlatformTool(settings=cfg),
            MGSPlusPageFetchTool(settings=cfg),
        ],
        llm=llm,
        verbose=True,
        allow_delegation=False,
        check_compatibility=False,
        max_iter=8,
    )
