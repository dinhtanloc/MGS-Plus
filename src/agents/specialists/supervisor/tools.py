from __future__ import annotations

import asyncio
import concurrent.futures
from typing import Any, Optional, Type

from crewai.tools import BaseTool
from pydantic import BaseModel, Field

from src.agents.core.a2a.client import A2AClient
from src.agents.core.config import Settings, get_settings


def _run_async(coro):
    """Run an async coroutine safely even when an event loop is already running.

    CrewAI tool._run() is synchronous, but uvicorn's event loop is already
    active.  Calling asyncio.get_event_loop().run_until_complete() on a
    running loop raises RuntimeError.  Instead, we spin up a fresh thread
    (with its own event loop) via asyncio.run().
    """
    with concurrent.futures.ThreadPoolExecutor(max_workers=1) as pool:
        future = pool.submit(asyncio.run, coro)
        return future.result()


class RouteToDocumentsInput(BaseModel):
    question: str = Field(description="The user question to forward to the Documents agent")
    thread_id: str = Field(description="Current conversation thread ID")
    user_id: str = Field(default="anonymous", description="User ID")


class RouteToWorkflowInput(BaseModel):
    question: str = Field(description="The platform task request to forward to the Workflow agent")
    thread_id: str = Field(description="Current conversation thread ID")
    user_id: str = Field(default="anonymous", description="User ID")


class RouteToDocumentsTool(BaseTool):
    name: str = "route_to_documents_agent"
    description: str = (
        "Delegate a question that requires document retrieval, medical knowledge, "
        "or external source lookup to the Documents agent. "
        "Use this when the user asks about health information, documents, images, "
        "or content from Zalo/Messenger/Wikipedia."
    )
    args_schema: Type[BaseModel] = RouteToDocumentsInput

    _settings: Settings = None  # type: ignore
    _a2a: A2AClient = None  # type: ignore

    def __init__(self, settings: Optional[Settings] = None, **kwargs: Any) -> None:
        super().__init__(**kwargs)
        self._settings = settings or get_settings()
        self._a2a = A2AClient()

    def _run(self, question: str, thread_id: str, user_id: str = "anonymous") -> str:
        result = _run_async(
            self._a2a.send_task(
                base_url=self._settings.documents_base_url,
                question=question,
                thread_id=thread_id,
                user_id=user_id,
            )
        )
        return result.answer


class RouteToWorkflowTool(BaseTool):
    name: str = "route_to_workflow_agent"
    description: str = (
        "Delegate a platform task (account management, chatbot configuration, "
        "profile updates, etc.) to the Workflow agent. "
        "Use this when the user wants to perform an action on the MGSPlus website."
    )
    args_schema: Type[BaseModel] = RouteToWorkflowInput

    _settings: Settings = None  # type: ignore
    _a2a: A2AClient = None  # type: ignore

    def __init__(self, settings: Optional[Settings] = None, **kwargs: Any) -> None:
        super().__init__(**kwargs)
        self._settings = settings or get_settings()
        self._a2a = A2AClient()

    def _run(self, question: str, thread_id: str, user_id: str = "anonymous") -> str:
        result = _run_async(
            self._a2a.send_task(
                base_url=self._settings.workflow_base_url,
                question=question,
                thread_id=thread_id,
                user_id=user_id,
            )
        )
        return result.answer
