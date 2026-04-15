from __future__ import annotations

from functools import lru_cache

from fastapi import Depends, Header, HTTPException, status

from src.agents.core.config import get_settings
from src.agents.crews.main_crew import MainCrew


# ── Crew singleton ─────────────────────────────────────────────────────────────

@lru_cache(maxsize=1)
def get_main_crew() -> MainCrew:
    """Singleton MainCrew instance shared across requests."""
    return MainCrew(get_settings())


# ── API-key authentication ─────────────────────────────────────────────────────

def verify_api_key(x_api_key: str = Header(default="")) -> None:
    """Dependency that validates the X-Api-Key header against AGENT_API_KEY.

    If AGENT_API_KEY is not configured (empty string), authentication is
    skipped — allows local development without setting the key.
    To enforce in production, set AGENT_API_KEY in your .env file.
    """
    settings = get_settings()
    configured_key = settings.agent_api_key

    if not configured_key:
        # Key not configured — allow all (dev mode)
        return

    if x_api_key != configured_key:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Invalid or missing X-Api-Key header",
        )
