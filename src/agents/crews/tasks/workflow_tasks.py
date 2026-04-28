from __future__ import annotations

from crewai import Agent, Task

from src.agents.specialists.workflow.prompts import (
    EXECUTE_PLATFORM_EXPECTED_OUTPUT,
    build_execute_platform_description,
)


def execute_platform_task(workflow_agent: Agent, request: str, user_id: str = "anonymous") -> Task:
    """Task for the Workflow agent: perform a platform action for the user."""
    return Task(
        description=build_execute_platform_description(request, user_id),
        expected_output=EXECUTE_PLATFORM_EXPECTED_OUTPUT,
        agent=workflow_agent,
    )
