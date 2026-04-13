from __future__ import annotations

from crewai import Agent, Task


def execute_platform_task(workflow_agent: Agent, request: str, user_id: str = "anonymous") -> Task:
    """Task for the Workflow agent: perform a platform action for the user."""
    return Task(
        description=(
            f"Platform task request: {request}\n\n"
            f"User ID: {user_id}\n\n"
            "Steps:\n"
            "1. Parse the user's intent (e.g. change password, enable 2FA, update profile).\n"
            "2. Choose the appropriate tool and execute the action.\n"
            "3. Confirm the action was successful and explain what was done.\n"
            "4. If the action requires user confirmation first, ask for it explicitly."
        ),
        expected_output=(
            "A confirmation message describing the action taken, "
            "or a request for additional information if needed before proceeding."
        ),
        agent=workflow_agent,
    )
