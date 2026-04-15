from __future__ import annotations

import asyncio
import concurrent.futures
from typing import Any, Optional, Type

from crewai.tools import BaseTool
from pydantic import BaseModel, Field

from src.agents.core.config import Settings, get_settings
from src.agents.core.internal_api_client import InternalApiClient


def _run_async(coro):
    """Run an async coroutine safely even when an event loop is already running."""
    with concurrent.futures.ThreadPoolExecutor(max_workers=1) as pool:
        future = pool.submit(asyncio.run, coro)
        return future.result()


# ── Schema helpers ────────────────────────────────────────────────────────────

class ChangePasswordInput(BaseModel):
    user_id: str = Field(description="ID of the user requesting password change")
    new_password_hint: str = Field(description="Description of desired password (never the real password)")


class AuthSettingsInput(BaseModel):
    user_id: str = Field(description="User ID")
    action: str = Field(
        description=(
            "Auth action to perform. Examples: "
            "'enable_2fa', 'disable_2fa', 'reset_session', 'revoke_tokens'"
        )
    )


class WebActionInput(BaseModel):
    action: str = Field(
        description=(
            "Web task to perform on the MGSPlus platform. Examples: "
            "'update_profile', 'configure_chatbot', 'view_notifications'"
        )
    )
    user_id: str = Field(description="User performing the action")
    payload: dict = Field(default_factory=dict, description="Action-specific parameters")


# ── Tools ─────────────────────────────────────────────────────────────────────

class ChangePasswordTool(BaseTool):
    name: str = "change_password"
    description: str = (
        "Initiate a password change flow for a user on the MGSPlus platform. "
        "This sends a secure reset link — never handles the raw password."
    )
    args_schema: Type[BaseModel] = ChangePasswordInput

    _settings: Settings = None  # type: ignore
    _api: InternalApiClient = None  # type: ignore

    def __init__(self, settings: Optional[Settings] = None, **kwargs: Any) -> None:
        super().__init__(**kwargs)
        self._settings = settings or get_settings()
        self._api = InternalApiClient(self._settings)

    def _run(self, user_id: str, new_password_hint: str) -> str:
        return _run_async(self._call_reset(user_id))

    async def _call_reset(self, user_id: str) -> str:
        try:
            resp = await self._api.post(
                "/api/auth/send-verification-email",
                json={"userId": user_id},
            )
            if resp.status_code in (200, 204):
                return (
                    f"Password reset email sent to user '{user_id}'. "
                    "They will receive a secure link to set a new password."
                )
            return (
                f"Backend returned {resp.status_code} when sending reset email for "
                f"user '{user_id}'. Please try again later."
            )
        except Exception as exc:
            return f"Could not reach backend to initiate password reset: {exc}"


class AuthSettingsTool(BaseTool):
    name: str = "manage_auth_settings"
    description: str = (
        "Manage authentication settings for a user: enable/disable 2FA, "
        "reset sessions, revoke tokens, etc."
    )
    args_schema: Type[BaseModel] = AuthSettingsInput

    _settings: Settings = None  # type: ignore
    _api: InternalApiClient = None  # type: ignore

    def __init__(self, settings: Optional[Settings] = None, **kwargs: Any) -> None:
        super().__init__(**kwargs)
        self._settings = settings or get_settings()
        self._api = InternalApiClient(self._settings)

    def _run(self, user_id: str, action: str) -> str:
        supported = {"enable_2fa", "disable_2fa", "reset_session", "revoke_tokens"}
        if action not in supported:
            return f"Unknown action '{action}'. Supported: {', '.join(sorted(supported))}."
        return _run_async(self._call_auth_action(user_id, action))

    async def _call_auth_action(self, user_id: str, action: str) -> str:
        try:
            resp = await self._api.post(
                f"/api/auth/{action.replace('_', '-')}",
                json={"userId": user_id},
            )
            if resp.status_code in (200, 204):
                return f"Auth action '{action}' successfully applied to user '{user_id}'."
            return (
                f"Backend returned {resp.status_code} for action '{action}' "
                f"on user '{user_id}'."
            )
        except Exception as exc:
            return f"Could not reach backend for auth action '{action}': {exc}"


class WebActionTool(BaseTool):
    name: str = "perform_web_action"
    description: str = (
        "Perform a task on the MGSPlus web platform on behalf of a user: "
        "update profile, configure chatbot settings, view notifications, etc."
    )
    args_schema: Type[BaseModel] = WebActionInput

    _settings: Settings = None  # type: ignore
    _api: InternalApiClient = None  # type: ignore

    def __init__(self, settings: Optional[Settings] = None, **kwargs: Any) -> None:
        super().__init__(**kwargs)
        self._settings = settings or get_settings()
        self._api = InternalApiClient(self._settings)

    def _run(self, action: str, user_id: str, payload: dict) -> str:
        return _run_async(self._call_web_action(action, user_id, payload))

    async def _call_web_action(self, action: str, user_id: str, payload: dict) -> str:
        endpoint_map = {
            "update_profile":     f"/api/users/{user_id}/profile",
            "view_notifications": f"/api/users/{user_id}/notifications",
            "configure_chatbot":  f"/api/users/{user_id}/chatbot-settings",
        }
        path = endpoint_map.get(action, f"/api/actions/{action}")
        try:
            resp = await self._api.patch(path, json={**payload, "userId": user_id})
            if resp.status_code in (200, 204):
                return (
                    f"Action '{action}' executed for user '{user_id}' "
                    f"with parameters {payload}. Operation completed successfully."
                )
            return (
                f"Backend returned {resp.status_code} for action '{action}' "
                f"on user '{user_id}'."
            )
        except Exception as exc:
            return f"Could not reach backend for action '{action}': {exc}"
