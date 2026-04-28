from __future__ import annotations

from typing import Optional

from crewai import Agent

from src.agents.core.config import Settings, build_llm, get_settings
from src.agents.agents.documents.tools import MCPSearchTool, QdrantSearchTool


def build_documents_agent(settings: Optional[Settings] = None) -> Agent:
    """Build the Documents Agent — retrieves information from vector DB and external sources."""
    cfg = settings or get_settings()

    llm = build_llm(cfg)

    return Agent(
        role="Documents & Knowledge Retrieval Specialist",
        goal=(
            "Retrieve accurate, relevant information from the internal Qdrant knowledge base "
            "and external sources (Zalo, Messenger, Wikipedia, medical websites) via MCP. "
            "Always cite the source of retrieved information."
        ),
        backstory=(
            "You are an expert information retrieval agent with deep access to the organisation's "
            "document repository and external medical/health information platforms. "
            "You understand how to find the most relevant content and summarise it clearly."
        ),
        tools=[
            QdrantSearchTool(settings=cfg),
            MCPSearchTool(settings=cfg),
        ],
        llm=llm,
        verbose=True,
        allow_delegation=False,
        max_iter=5,
    )
