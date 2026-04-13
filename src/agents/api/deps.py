from __future__ import annotations

from functools import lru_cache
from typing import Optional

from src.agents.core.config import Settings, get_settings
from src.agents.crews.main_crew import MainCrew


@lru_cache(maxsize=1)
def get_main_crew(settings: Optional[Settings] = None) -> MainCrew:
    """Singleton MainCrew instance shared across requests."""
    return MainCrew(settings or get_settings())
