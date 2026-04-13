"""
MGSPlus Multi-Agent Server
--------------------------
Starts all three agent services concurrently:
  - Supervisor  → http://localhost:8010
  - Documents   → http://localhost:8011
  - Workflow    → http://localhost:8012

Usage:
    python -m src.agents.server          # run all three
    python -m src.agents.server supervisor  # run only supervisor
"""
from __future__ import annotations

import asyncio
import sys
from typing import List

import uvicorn

from src.agents.core.config import get_settings


def _build_configs() -> List[uvicorn.Config]:
    settings = get_settings()

    from src.agents.api.supervisor_app import app as supervisor_app
    from src.agents.api.documents_app import app as documents_app
    from src.agents.api.workflow_app import app as workflow_app

    return [
        uvicorn.Config(
            supervisor_app,
            host="0.0.0.0",
            port=settings.supervisor_port,
            log_level="info",
            reload=False,
        ),
        uvicorn.Config(
            documents_app,
            host="0.0.0.0",
            port=settings.documents_port,
            log_level="info",
            reload=False,
        ),
        uvicorn.Config(
            workflow_app,
            host="0.0.0.0",
            port=settings.workflow_port,
            log_level="info",
            reload=False,
        ),
    ]


async def _serve_all() -> None:
    configs = _build_configs()
    servers = [uvicorn.Server(cfg) for cfg in configs]
    await asyncio.gather(*[s.serve() for s in servers])


def _serve_single(name: str) -> None:
    settings = get_settings()
    app_map = {
        "supervisor": ("src.agents.api.supervisor_app:app", settings.supervisor_port),
        "documents": ("src.agents.api.documents_app:app", settings.documents_port),
        "workflow": ("src.agents.api.workflow_app:app", settings.workflow_port),
    }
    if name not in app_map:
        print(f"Unknown agent '{name}'. Choose from: {', '.join(app_map)}")
        sys.exit(1)
    app_str, port = app_map[name]
    uvicorn.run(app_str, host="0.0.0.0", port=port, reload=False)


def run() -> None:
    """Entrypoint registered in pyproject.toml scripts."""
    target = sys.argv[1] if len(sys.argv) > 1 else "all"
    if target == "all":
        asyncio.run(_serve_all())
    else:
        _serve_single(target)


if __name__ == "__main__":
    run()
