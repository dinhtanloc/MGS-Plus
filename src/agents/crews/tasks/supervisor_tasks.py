from __future__ import annotations

from crewai import Agent, Task


def route_and_respond_task(supervisor: Agent, question: str, context: str = "") -> Task:
    """Primary task: supervisor analyses, routes, and synthesises the final answer."""
    return Task(
        description=(
            f"User question: {question}\n\n"
            f"Additional context: {context}\n\n"
            "Steps:\n"
            "1. Classify the intent (document/knowledge retrieval OR platform workflow OR general).\n"
            "2. If retrieval: delegate to Documents agent via route_to_documents_agent tool.\n"
            "3. If platform task: delegate to Workflow agent via route_to_workflow_agent tool.\n"
            "4. Synthesise a final, clear, user-friendly answer."
        ),
        expected_output=(
            "A complete, helpful response to the user's question or request, "
            "written in the same language the user used."
        ),
        agent=supervisor,
    )
