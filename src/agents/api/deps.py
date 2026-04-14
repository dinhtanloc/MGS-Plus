from __future__ import annotations

from functools import lru_cache

from src.agents.core.config import get_settings
from src.agents.crews.main_crew import MainCrew


@lru_cache(maxsize=1)
def get_main_crew() -> MainCrew:
    """Singleton MainCrew instance shared across requests.

    No parameters here: if FastAPI sees a BaseModel parameter on a dependency
    it tries to parse it from the request body, conflicting with the endpoint's
    own body schema and causing 422 errors.  Settings are resolved internally
    via get_settings() which is already an lru_cache singleton.
    """
    return MainCrew(get_settings())
