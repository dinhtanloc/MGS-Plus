from __future__ import annotations

from crewai import Agent, Task

from src.agents.specialists.supervisor.prompts import (
    ROUTE_AND_RESPOND_EXPECTED_OUTPUT,
    build_route_and_respond_description,
)


def route_and_respond_task(supervisor: Agent, question: str, context: str = "") -> Task:
    """Primary task: assistant answers the user question using available tools."""
    return Task(
        description=build_route_and_respond_description(question, context),
        expected_output=ROUTE_AND_RESPOND_EXPECTED_OUTPUT,
        agent=supervisor,
    )
