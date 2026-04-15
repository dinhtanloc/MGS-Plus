"""
embed.py — Async Ollama embedder with concurrency control and retry.

Usage (standalone):
    from data_pipeline.embed import OllamaEmbedder
    embedder = OllamaEmbedder()
    vector = await embedder.embed("dengue fever symptoms")
    vectors = await embedder.embed_batch(["text 1", "text 2"])
"""
from __future__ import annotations

import asyncio
import os
from typing import List

import httpx
from tenacity import (
    retry,
    retry_if_exception_type,
    stop_after_attempt,
    wait_exponential,
)

OLLAMA_URL    = os.getenv("OLLAMA_BASE_URL",       "http://localhost:11434")
EMBED_MODEL   = os.getenv("OLLAMA_EMBEDDING_MODEL", "nomic-embed-text")
VECTOR_SIZE   = int(os.getenv("VECTOR_SIZE",         "768"))
MAX_RETRIES   = 3
# Max concurrent embedding requests to Ollama (CPU-bound on the Ollama side)
DEFAULT_CONCURRENCY = int(os.getenv("EMBED_CONCURRENCY", "8"))


class OllamaEmbedder:
    """Async Ollama embedder with concurrency throttling and automatic retry."""

    def __init__(
        self,
        base_url:    str = OLLAMA_URL,
        model:       str = EMBED_MODEL,
        vector_size: int = VECTOR_SIZE,
        concurrency: int = DEFAULT_CONCURRENCY,
        timeout:     float = 60.0,
    ) -> None:
        self.base_url    = base_url.rstrip("/")
        self.model       = model
        self.vector_size = vector_size
        self._sem        = asyncio.Semaphore(concurrency)
        self._timeout    = timeout

    # ── Single embed ──────────────────────────────────────────────────────────

    async def embed(self, text: str) -> List[float]:
        """Embed a single text. Returns zero-vector on unrecoverable failure."""
        try:
            return await self._embed_with_retry(text)
        except Exception as exc:
            print(f"[EMBED] Unrecoverable error: {exc}")
            return [0.0] * self.vector_size

    @retry(
        stop=stop_after_attempt(MAX_RETRIES),
        wait=wait_exponential(multiplier=1, min=1, max=8),
        retry=retry_if_exception_type((httpx.TransportError, httpx.TimeoutException)),
        reraise=True,
    )
    async def _embed_with_retry(self, text: str) -> List[float]:
        async with self._sem:
            async with httpx.AsyncClient(timeout=self._timeout) as client:
                resp = await client.post(
                    f"{self.base_url}/api/embeddings",
                    json={"model": self.model, "prompt": text},
                )
                resp.raise_for_status()
                data = resp.json()

        vec = data.get("embedding", [])
        if len(vec) != self.vector_size:
            raise ValueError(
                f"Expected vector of size {self.vector_size}, got {len(vec)}. "
                f"Did you pull the correct model? Run: ollama pull {self.model}"
            )
        return vec

    # ── Batch embed ───────────────────────────────────────────────────────────

    async def embed_batch(self, texts: List[str]) -> List[List[float]]:
        """Embed a list of texts concurrently, respecting the semaphore limit."""
        tasks = [asyncio.create_task(self.embed(t)) for t in texts]
        return list(await asyncio.gather(*tasks))

    # ── Health check ──────────────────────────────────────────────────────────

    async def health_check(self) -> bool:
        """Return True if Ollama is reachable and the model is available."""
        try:
            async with httpx.AsyncClient(timeout=5.0) as client:
                resp = await client.get(f"{self.base_url}/api/tags")
                resp.raise_for_status()
                tags = resp.json()
                models = [m["name"] for m in tags.get("models", [])]
                # Accept both "nomic-embed-text" and "nomic-embed-text:latest"
                return any(m.startswith(self.model.split(":")[0]) for m in models)
        except Exception:
            return False
