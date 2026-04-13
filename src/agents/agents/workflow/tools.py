from __future__ import annotations

from typing import Any, Optional, Type

from crewai.tools import BaseTool
from pydantic import BaseModel, Field

from src.agents.core.config import Settings, get_settings


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

    def __init__(self, settings: Optional[Settings] = None, **kwargs: Any) -> None:
        super().__init__(**kwargs)
        self._settings = settings or get_settings()

    def _run(self, user_id: str, new_password_hint: str) -> str:
        # In production: call the backend auth API to trigger a password reset email
        # For now: stub response
        return (
            f"Password reset initiated for user '{user_id}'. "
            "A secure reset link has been sent to their registered email."
        )


class AuthSettingsTool(BaseTool):
    name: str = "manage_auth_settings"
    description: str = (
        "Manage authentication settings for a user: enable/disable 2FA, "
        "reset sessions, revoke tokens, etc."
    )
    args_schema: Type[BaseModel] = AuthSettingsInput

    _settings: Settings = None  # type: ignore

    def __init__(self, settings: Optional[Settings] = None, **kwargs: Any) -> None:
        super().__init__(**kwargs)
        self._settings = settings or get_settings()

    def _run(self, user_id: str, action: str) -> str:
        # In production: call backend auth API
        supported = {"enable_2fa", "disable_2fa", "reset_session", "revoke_tokens"}
        if action not in supported:
            return f"Unknown action '{action}'. Supported: {', '.join(supported)}."
        return f"Auth action '{action}' successfully applied to user '{user_id}'."


class WebActionTool(BaseTool):
    name: str = "perform_web_action"
    description: str = (
        "Perform a task on the MGSPlus web platform on behalf of a user: "
        "update profile, configure chatbot settings, view notifications, etc."
    )
    args_schema: Type[BaseModel] = WebActionInput

    _settings: Settings = None  # type: ignore

    def __init__(self, settings: Optional[Settings] = None, **kwargs: Any) -> None:
        super().__init__(**kwargs)
        self._settings = settings or get_settings()

    def _run(self, action: str, user_id: str, payload: dict) -> str:
        # In production: call the MGSPlus backend REST API
        return (
            f"Action '{action}' executed for user '{user_id}' "
            f"with parameters {payload}. Operation completed successfully."
        )
