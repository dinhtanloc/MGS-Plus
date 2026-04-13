from __future__ import annotations

import asyncio
from typing import Any, Optional, Type

from crewai.tools import BaseTool
from pydantic import BaseModel, Field

from src.agents.core.a2a.client import A2AClient
from src.agents.core.config import Settings, get_settings


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
        result = asyncio.get_event_loop().run_until_complete(
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
        result = asyncio.get_event_loop().run_until_complete(
            self._a2a.send_task(
                base_url=self._settings.workflow_base_url,
                question=question,
                thread_id=thread_id,
                user_id=user_id,
            )
        )
        return result.answer
