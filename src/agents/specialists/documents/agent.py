from __future__ import annotations

from typing import Optional

from crewai import Agent

from src.agents.core.config import Settings, build_llm, get_settings
from src.agents.specialists.documents.tools import (
    MCPSearchTool,
    QdrantSearchTool,
    WebFetchTool,
    WebSearchTool,
    WikipediaSearchTool,
)
from src.agents.specialists.documents.prompts import BACKSTORY, GOAL, ROLE


def build_documents_agent(settings: Optional[Settings] = None) -> Agent:
    """Build the Documents Agent — retrieves information from vector DB and external sources."""
    cfg = settings or get_settings()

    llm = build_llm(cfg)

    return Agent(
        role=ROLE,
        goal=GOAL,
        backstory=BACKSTORY,
        tools=[
            QdrantSearchTool(settings=cfg),
            MCPSearchTool(settings=cfg),
            WebSearchTool(),
            WebFetchTool(),
            WikipediaSearchTool(),
        ],
        llm=llm,
        verbose=True,
        allow_delegation=False,
        check_compatibility=False,
        max_iter=8,
    )
