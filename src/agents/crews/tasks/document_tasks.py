from __future__ import annotations

from crewai import Agent, Task


def retrieve_knowledge_task(documents_agent: Agent, question: str, user_id: str = "anonymous") -> Task:
    """Task for the Documents agent: retrieve relevant info for a question."""
    return Task(
        description=(
            f"Retrieve information to answer the following question:\n{question}\n\n"
            f"User ID: {user_id}\n\n"
            "Steps:\n"
            "1. Search the shared Qdrant knowledge base (knowledge_shared collection).\n"
            f"2. Also search the user's personal collection (user_{user_id}_docs) if relevant.\n"
            "3. If the local knowledge base doesn't have enough info, "
            "search external sources via MCP (wiki first, then Zalo/Messenger if applicable).\n"
            "4. Synthesise the retrieved content into a concise, accurate answer with source citations."
        ),
        expected_output=(
            "A well-structured answer with cited sources. "
            "Include document titles or URLs where available."
        ),
        agent=documents_agent,
    )
