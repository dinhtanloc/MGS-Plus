from __future__ import annotations

from crewai import Agent, Task

from src.agents.specialists.documents.prompts import (
    RETRIEVE_KNOWLEDGE_EXPECTED_OUTPUT,
    build_retrieve_knowledge_description,
)


def retrieve_knowledge_task(
    documents_agent: Agent,
    question: str,
    user_id: str = "anonymous",
) -> Task:
    """Build a RAG retrieval task for the Documents agent.

    The task description instructs the agent to:
    - Use inferred category/language filters to narrow the Qdrant search.
    - Cite every claim with [Source: <path>] taken from the search result header.
    - Fall back to MCP wiki search only when Qdrant finds nothing useful.
    - Never fabricate facts — if no evidence is found, say so explicitly.
    """
    return Task(
        description=build_retrieve_knowledge_description(question, user_id),
        expected_output=RETRIEVE_KNOWLEDGE_EXPECTED_OUTPUT,
        agent=documents_agent,
    )
