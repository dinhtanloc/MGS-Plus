"""
internal_api_client.py — Async HTTP client for agent-to-backend REST API calls.

All requests include the X-Service-Key header for service-to-service auth.
"""
from __future__ import annotations

import os
from typing import Any, Dict, Optional

import httpx

from src.agents.core.config import Settings, get_settings

_SERVICE_KEY_HEADER = "X-Service-Key"


class InternalApiClient:
    """Thin async wrapper around httpx.AsyncClient for calling the MGSPlus backend."""

    def __init__(self, settings: Optional[Settings] = None) -> None:
        self._settings = settings or get_settings()
        self._base_url = self._settings.backend_url.rstrip("/")
        self._service_key = os.getenv("SERVICE_KEY", self._settings.agent_api_key)

    def _headers(self) -> Dict[str, str]:
        h: Dict[str, str] = {"Content-Type": "application/json"}
        if self._service_key:
            h[_SERVICE_KEY_HEADER] = self._service_key
        return h

    async def post(self, path: str, json: Any = None, **kwargs: Any) -> httpx.Response:
        async with httpx.AsyncClient() as client:
            return await client.post(
                f"{self._base_url}{path}",
                json=json,
                headers=self._headers(),
                timeout=15.0,
                **kwargs,
            )

    async def get(self, path: str, params: Any = None, **kwargs: Any) -> httpx.Response:
        async with httpx.AsyncClient() as client:
            return await client.get(
                f"{self._base_url}{path}",
                params=params,
                headers=self._headers(),
                timeout=15.0,
                **kwargs,
            )

    async def patch(self, path: str, json: Any = None, **kwargs: Any) -> httpx.Response:
        async with httpx.AsyncClient() as client:
            return await client.patch(
                f"{self._base_url}{path}",
                json=json,
                headers=self._headers(),
                timeout=15.0,
                **kwargs,
            )
