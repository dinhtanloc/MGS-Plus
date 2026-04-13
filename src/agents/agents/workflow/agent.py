from __future__ import annotations

from typing import Optional

from crewai import Agent, LLM

from src.agents.core.config import Settings, get_settings
from src.agents.agents.workflow.tools import AuthSettingsTool, ChangePasswordTool, WebActionTool


def build_workflow_agent(settings: Optional[Settings] = None) -> Agent:
    """Build the Workflow Agent — helps users navigate and act on the MGSPlus platform."""
    cfg = settings or get_settings()

    llm = LLM(model=cfg.llm_model, api_key=cfg.openai_api_key)

    return Agent(
        role="MGSPlus Platform Workflow Assistant",
        goal=(
            "Help users perform tasks on the MGSPlus web platform: "
            "account management (passwords, 2FA, sessions), "
            "profile updates, chatbot configuration, and other platform actions. "
            "Always confirm before taking irreversible actions."
        ),
        backstory=(
            "You are an expert at navigating the MGSPlus platform and assisting users "
            "with any workflow-related task. You know every feature of the platform and "
            "can guide users step-by-step or execute actions directly on their behalf."
        ),
        tools=[
            ChangePasswordTool(settings=cfg),
            AuthSettingsTool(settings=cfg),
            WebActionTool(settings=cfg),
        ],
        llm=llm,
        verbose=True,
        allow_delegation=False,
        max_iter=5,
    )
