from __future__ import annotations

from typing import Optional
from uuid import uuid4

import httpx
from tenacity import retry, stop_after_attempt, wait_exponential, retry_if_exception_type

from src.agents.core.a2a.schemas import (
    A2ARequest,
    A2AResponse,
    AgentCard,
    TaskInput,
    TaskOutput,
)


class A2AClient:
    """HTTP client for sending tasks to other A2A-compatible agent services."""

    def __init__(self, timeout: float = 60.0) -> None:
        self._timeout = timeout

    @retry(
        stop=stop_after_attempt(3),
        wait=wait_exponential(multiplier=1, min=1, max=4),
        retry=retry_if_exception_type((httpx.TransportError, httpx.TimeoutException)),
        reraise=True,
    )
    async def get_agent_card(self, base_url: str) -> AgentCard:
        async with httpx.AsyncClient(timeout=self._timeout) as client:
            resp = await client.get(f"{base_url}/.well-known/agent.json")
            resp.raise_for_status()
            return AgentCard.model_validate(resp.json())

    @retry(
        stop=stop_after_attempt(3),
        wait=wait_exponential(multiplier=1, min=1, max=4),
        retry=retry_if_exception_type((httpx.TransportError, httpx.TimeoutException)),
        reraise=True,
    )
    async def send_task(
        self,
        base_url: str,
        question: str,
        thread_id: Optional[str] = None,
        user_id: str = "anonymous",
        context: Optional[dict] = None,
    ) -> TaskOutput:
        """Send a task to an agent and return its output.

        Raises:
            httpx.HTTPError: on network failure.
            ValueError: if the agent returns a JSON-RPC error.
        """
        request = A2ARequest(
            id=str(uuid4()),
            method="tasks/send",
            params=TaskInput(
                question=question,
                thread_id=thread_id or str(uuid4()),
                user_id=user_id,
                context=context or {},
            ),
        )
        async with httpx.AsyncClient(timeout=self._timeout) as client:
            resp = await client.post(
                f"{base_url}/a2a",
                json=request.model_dump(),
                headers={"Content-Type": "application/json"},
            )
            resp.raise_for_status()
            response = A2AResponse.model_validate(resp.json())

        if response.error:
            raise ValueError(
                f"A2A error {response.error.code}: {response.error.message}"
            )
        if response.result is None:
            raise ValueError("A2A response has neither result nor error")
        return response.result
