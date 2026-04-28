from __future__ import annotations

import asyncio
import concurrent.futures
from typing import Any, Dict, List, Optional, Type

import httpx
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


# ── View appointments tool ────────────────────────────────────────────────────

class ViewAppointmentsInput(BaseModel):
    user_id: str = Field(description="User ID to fetch appointments for")
    status: Optional[str] = Field(
        default=None,
        description="Filter by appointment status: 'upcoming', 'past', 'cancelled', or omit for all",
    )


class ViewAppointmentsTool(BaseTool):
    name: str = "view_appointments"
    description: str = (
        "View medical appointments for a user on the MGSPlus platform. "
        "Returns scheduled appointments with date, doctor name, specialty, and status. "
        "Use when the user asks about their upcoming or past appointments."
    )
    args_schema: Type[BaseModel] = ViewAppointmentsInput

    _settings: Settings = None  # type: ignore
    _api: InternalApiClient = None  # type: ignore

    def __init__(self, settings: Optional[Settings] = None, **kwargs: Any) -> None:
        super().__init__(**kwargs)
        self._settings = settings or get_settings()
        self._api = InternalApiClient(self._settings)

    def _run(self, user_id: str, status: Optional[str] = None) -> str:
        return _run_async(self._fetch_appointments(user_id, status))

    async def _fetch_appointments(self, user_id: str, status: Optional[str]) -> str:
        params: Dict[str, str] = {}
        if status:
            params["status"] = status
        try:
            resp = await self._api.get(f"/api/users/{user_id}/appointments", params=params)
            if resp.status_code == 200:
                data = resp.json()
                if not data:
                    return f"No appointments found for user '{user_id}'."
                lines = [f"Appointments for user '{user_id}':\n"]
                for apt in data:
                    lines.append(
                        f"  • [{apt.get('appointmentDate', 'N/A')}] "
                        f"Dr. {apt.get('doctorName', 'N/A')} — "
                        f"{apt.get('specialty', '')} | "
                        f"Status: {apt.get('status', 'N/A')} | "
                        f"Location: {apt.get('location', 'N/A')}"
                    )
                return "\n".join(lines)
            return f"Backend returned {resp.status_code} when fetching appointments for '{user_id}'."
        except Exception as exc:
            return f"Could not fetch appointments: {exc}"


# ── View medical records tool ─────────────────────────────────────────────────

class ViewMedicalRecordsInput(BaseModel):
    user_id: str = Field(description="User ID to fetch medical records for")
    record_type: Optional[str] = Field(
        default=None,
        description=(
            "Type of records to retrieve: "
            "'lab' (lab results), 'prescription' (medication history), "
            "'diagnosis' (diagnoses), 'imaging' (X-ray, MRI). "
            "Omit to retrieve all types."
        ),
    )
    limit: int = Field(default=10, ge=1, le=50, description="Max records to return")


class ViewMedicalRecordsTool(BaseTool):
    name: str = "view_medical_records"
    description: str = (
        "View medical records for a user on the MGSPlus platform. "
        "Returns lab results, prescriptions, diagnoses, and imaging records. "
        "Use when the user asks about their health history or test results."
    )
    args_schema: Type[BaseModel] = ViewMedicalRecordsInput

    _settings: Settings = None  # type: ignore
    _api: InternalApiClient = None  # type: ignore

    def __init__(self, settings: Optional[Settings] = None, **kwargs: Any) -> None:
        super().__init__(**kwargs)
        self._settings = settings or get_settings()
        self._api = InternalApiClient(self._settings)

    def _run(self, user_id: str, record_type: Optional[str] = None, limit: int = 10) -> str:
        return _run_async(self._fetch_records(user_id, record_type, limit))

    async def _fetch_records(
        self, user_id: str, record_type: Optional[str], limit: int
    ) -> str:
        params: Dict[str, Any] = {"limit": limit}
        if record_type:
            params["type"] = record_type
        try:
            resp = await self._api.get(f"/api/users/{user_id}/medical-records", params=params)
            if resp.status_code == 200:
                data = resp.json()
                if not data:
                    return f"No medical records found for user '{user_id}'."
                lines = [f"Medical records for user '{user_id}':\n"]
                for rec in data:
                    lines.append(
                        f"  • [{rec.get('date', 'N/A')}] "
                        f"Type: {rec.get('type', 'N/A')} | "
                        f"Title: {rec.get('title', 'N/A')} | "
                        f"Doctor: {rec.get('doctorName', 'N/A')} | "
                        f"Notes: {rec.get('notes', '')}"
                    )
                return "\n".join(lines)
            return f"Backend returned {resp.status_code} when fetching records for '{user_id}'."
        except Exception as exc:
            return f"Could not fetch medical records: {exc}"


# ── Search MGSPlus platform content ──────────────────────────────────────────

class SearchPlatformInput(BaseModel):
    query: str = Field(description="Search query to look up on the MGSPlus platform")
    content_type: str = Field(
        default="all",
        description=(
            "Type of content to search: "
            "'news' (health news), 'blog' (articles), "
            "'doctor' (doctor profiles), 'service' (hospital services), 'all'"
        ),
    )
    limit: int = Field(default=5, ge=1, le=20, description="Maximum results to return")


class SearchPlatformTool(BaseTool):
    name: str = "search_platform"
    description: str = (
        "Search the MGSPlus platform for content: news articles, blog posts, "
        "doctor profiles, and hospital services. "
        "Use when the user asks to find a doctor, read health news, or look up a service."
    )
    args_schema: Type[BaseModel] = SearchPlatformInput

    _settings: Settings = None  # type: ignore
    _api: InternalApiClient = None  # type: ignore

    def __init__(self, settings: Optional[Settings] = None, **kwargs: Any) -> None:
        super().__init__(**kwargs)
        self._settings = settings or get_settings()
        self._api = InternalApiClient(self._settings)

    def _run(self, query: str, content_type: str = "all", limit: int = 5) -> str:
        return _run_async(self._search(query, content_type, limit))

    async def _search(self, query: str, content_type: str, limit: int) -> str:
        params: Dict[str, Any] = {"q": query, "limit": limit}
        if content_type != "all":
            params["type"] = content_type
        try:
            resp = await self._api.get("/api/search", params=params)
            if resp.status_code == 200:
                data = resp.json()
                items: List[dict] = data if isinstance(data, list) else data.get("results", [])
                if not items:
                    return f"No results found on MGSPlus for: '{query}'."
                lines = [f"MGSPlus search results for '{query}':\n"]
                for item in items[:limit]:
                    lines.append(
                        f"  • [{item.get('type', 'content')}] "
                        f"{item.get('title', 'N/A')} "
                        f"— {item.get('summary', item.get('description', ''))[:120]}"
                    )
                return "\n".join(lines)
            return f"Backend returned {resp.status_code} for search query '{query}'."
        except Exception as exc:
            return f"Could not search MGSPlus platform: {exc}"


# ── Fetch MGSPlus page content ────────────────────────────────────────────────

class MGSPlusPageFetchInput(BaseModel):
    path: str = Field(
        description=(
            "API path or page path on the MGSPlus platform to fetch. "
            "Examples: '/api/news', '/api/blog', '/api/doctors', '/api/services'"
        )
    )
    params: Dict[str, Any] = Field(
        default_factory=dict,
        description="Optional query parameters, e.g. {'page': 1, 'limit': 10}",
    )


class MGSPlusPageFetchTool(BaseTool):
    name: str = "fetch_mgsplus_page"
    description: str = (
        "Fetch data from a specific MGSPlus platform page or API endpoint. "
        "Use this to browse lists of news articles, blog posts, doctors, or services "
        "when you need to retrieve the full listing rather than a specific search. "
        "Examples: path='/api/news' fetches the news feed, path='/api/doctors' fetches doctor list."
    )
    args_schema: Type[BaseModel] = MGSPlusPageFetchInput

    _settings: Settings = None  # type: ignore
    _api: InternalApiClient = None  # type: ignore

    def __init__(self, settings: Optional[Settings] = None, **kwargs: Any) -> None:
        super().__init__(**kwargs)
        self._settings = settings or get_settings()
        self._api = InternalApiClient(self._settings)

    def _run(self, path: str, params: Dict[str, Any] = None) -> str:
        return _run_async(self._fetch_page(path, params or {}))

    async def _fetch_page(self, path: str, params: Dict[str, Any]) -> str:
        try:
            resp = await self._api.get(path, params=params or {})
            if resp.status_code == 200:
                data = resp.json()
                if not data:
                    return f"No data returned from '{path}'."
                if isinstance(data, list):
                    lines = [f"Data from {path} ({len(data)} items):\n"]
                    for item in data[:20]:
                        title = (
                            item.get("title")
                            or item.get("name")
                            or item.get("fullName")
                            or str(item.get("id", ""))
                        )
                        desc  = item.get("summary") or item.get("description") or item.get("specialty") or ""
                        lines.append(f"  • {title} — {str(desc)[:100]}")
                    return "\n".join(lines)
                # Single object
                import json as _json
                return f"Data from {path}:\n{_json.dumps(data, ensure_ascii=False, indent=2)[:3000]}"
            return f"Backend returned {resp.status_code} for path '{path}'."
        except Exception as exc:
            return f"Could not fetch '{path}': {exc}"
