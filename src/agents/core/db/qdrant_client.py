from __future__ import annotations

from functools import lru_cache
from typing import Any, Dict, List, Optional

from qdrant_client import AsyncQdrantClient
from qdrant_client.models import (
    Distance,
    PointStruct,
    VectorParams,
    Filter,
    FieldCondition,
    MatchValue,
)

from src.agents.core.config import Settings


class QdrantService:
    """Thin async wrapper around qdrant-client with collection lifecycle helpers."""

    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._client: AsyncQdrantClient = AsyncQdrantClient(
            host=settings.qdrant_host,
            port=settings.qdrant_port,
            api_key=settings.qdrant_api_key or None,
            grpc_port=settings.qdrant_grpc_port,
            prefer_grpc=False,
        )

    # ── Collection helpers ────────────────────────────────────────────────────

    async def get_or_create_collection(
        self,
        name: str,
        vector_size: Optional[int] = None,
        distance: Distance = Distance.COSINE,
    ) -> None:
        size = vector_size or self._settings.qdrant_vector_size
        existing = await self._client.collection_exists(name)
        if not existing:
            await self._client.create_collection(
                collection_name=name,
                vectors_config=VectorParams(size=size, distance=distance),
            )

    # Convenience collection-name builders
    @staticmethod
    def user_collection(user_id: str) -> str:
        return f"user_{user_id}_docs"

    @staticmethod
    def thread_collection(thread_id: str) -> str:
        return f"thread_{thread_id}_images"

    # ── CRUD ──────────────────────────────────────────────────────────────────

    async def upsert(
        self,
        collection_name: str,
        points: List[PointStruct],
    ) -> None:
        await self._client.upsert(
            collection_name=collection_name,
            points=points,
        )

    async def search(
        self,
        collection_name: str,
        query_vector: List[float],
        limit: int = 5,
        payload_filter: Optional[Filter] = None,
    ) -> List[Dict[str, Any]]:
        results = await self._client.search(
            collection_name=collection_name,
            query_vector=query_vector,
            limit=limit,
            query_filter=payload_filter,
            with_payload=True,
        )
        return [
            {"id": str(r.id), "score": r.score, "payload": r.payload}
            for r in results
        ]

    async def delete_by_payload(
        self,
        collection_name: str,
        field: str,
        value: Any,
    ) -> None:
        await self._client.delete(
            collection_name=collection_name,
            points_selector=Filter(
                must=[FieldCondition(key=field, match=MatchValue(value=value))]
            ),
        )

    async def close(self) -> None:
        await self._client.close()


_qdrant_service: Optional[QdrantService] = None


def get_qdrant_service(settings: Optional[Settings] = None) -> QdrantService:
    """Return a module-level singleton QdrantService.

    ``@lru_cache`` cannot be used here because ``Settings`` is not hashable.
    A module-level singleton is equivalent since ``Settings`` itself is a singleton.
    """
    global _qdrant_service
    if _qdrant_service is None:
        from src.agents.core.config import get_settings
        _qdrant_service = QdrantService(settings or get_settings())
    return _qdrant_service
