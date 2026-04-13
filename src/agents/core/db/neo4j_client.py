from __future__ import annotations

from functools import lru_cache
from typing import Any, Dict, List, Optional

from src.agents.core.config import Settings


class Neo4jService:
    """Stub Neo4j client — wired up but not yet used (reserved for GraphRAG).

    Replace the stub methods with real neo4j driver calls when GraphRAG
    development begins.
    """

    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._driver = None
        self._connected = False

    def connect(self) -> None:
        try:
            from neo4j import GraphDatabase  # type: ignore

            self._driver = GraphDatabase.driver(
                self._settings.neo4j_uri,
                auth=(self._settings.neo4j_user, self._settings.neo4j_password),
            )
            self._connected = True
        except Exception as exc:
            # Non-fatal: Neo4j is optional until GraphRAG is active
            print(f"[Neo4j] Connection skipped: {exc}")

    def run_query(self, cypher: str, params: Optional[Dict[str, Any]] = None) -> List[Dict[str, Any]]:
        if not self._connected or self._driver is None:
            return []
        with self._driver.session() as session:
            result = session.run(cypher, params or {})
            return [dict(record) for record in result]

    def close(self) -> None:
        if self._driver:
            self._driver.close()


@lru_cache(maxsize=1)
def get_neo4j_service(settings: Optional[Settings] = None) -> Neo4jService:
    from src.agents.core.config import get_settings
    svc = Neo4jService(settings or get_settings())
    svc.connect()
    return svc
