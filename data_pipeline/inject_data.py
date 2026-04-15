"""
inject_data.py — Full RAG ingest pipeline for MGSPlus.

Loads documents from multiple sources, chunks with RecursiveCharacterTextSplitter,
embeds via Ollama (nomic-embed-text), and upserts into Qdrant.

Usage:
    # Ingest everything under ./data/
    uv run python -m data_pipeline.inject_data

    # Specific directory
    uv run python -m data_pipeline.inject_data --data-dir ./data/medical

    # Reset collection first, then ingest
    uv run python -m data_pipeline.inject_data --reset

    # Dry-run (parse + chunk only, no Qdrant writes)
    uv run python -m data_pipeline.inject_data --dry-run

    # Custom collection
    uv run python -m data_pipeline.inject_data --collection my_collection
"""
from __future__ import annotations

import argparse
import asyncio
import csv
import hashlib
import json
import os
import re
import sys
import uuid
from dataclasses import dataclass, field
from pathlib import Path
from typing import Generator, List, Optional

import yaml
from qdrant_client import AsyncQdrantClient
from qdrant_client.models import Distance, Filter, FieldCondition, MatchValue, PointStruct, VectorParams

# ── Project root ──────────────────────────────────────────────────────────────
_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(_ROOT))

from data_pipeline.embed import OllamaEmbedder

# ── Config from YAML ──────────────────────────────────────────────────────────
with open(_ROOT / "configs" / "infra-config.yml") as _f:
    _infra = yaml.safe_load(_f)

_qdrant_cfg = _infra["databases"]["qdrant"]

QDRANT_HOST   = os.getenv("QDRANT_HOST",            _qdrant_cfg["host"])
QDRANT_PORT   = int(os.getenv("QDRANT_PORT",         str(_qdrant_cfg["port"])))
QDRANT_KEY    = os.getenv("QDRANT_API_KEY",          "")
VECTOR_SIZE   = int(os.getenv("VECTOR_SIZE",          str(_qdrant_cfg["vector_size"])))
COLLECTION    = os.getenv("QDRANT_COLLECTION",        _qdrant_cfg.get("shared_collection", "knowledge_shared"))

OLLAMA_URL    = os.getenv("OLLAMA_BASE_URL",          "http://localhost:11434")
EMBED_MODEL   = os.getenv("OLLAMA_EMBEDDING_MODEL",   "nomic-embed-text")
BATCH_SIZE    = int(os.getenv("INGEST_BATCH_SIZE",    "16"))
CONCURRENCY   = int(os.getenv("EMBED_CONCURRENCY",   "8"))


# ── Document model ────────────────────────────────────────────────────────────

@dataclass
class RawDocument:
    """A single unit of text before chunking."""
    doc_id:      str                    # stable ID based on source path + row
    text:        str                    # full raw text
    source:      str                    # relative file path or dataset name
    source_type: str = "local"          # "local_txt" | "local_csv" | "local_json" | "kaggle" | "seed"
    category:    str = "general"        # "medical_qa" | "disease" | "drug" | "guideline" | "general"
    language:    str = "vi"             # "vi" | "en" | "mixed"
    extra:       dict = field(default_factory=dict)   # any additional metadata


@dataclass
class DocumentChunk:
    """A chunk ready for embedding and upsert."""
    point_id:     str    # deterministic UUID
    text:         str
    doc_id:       str
    source:       str
    source_type:  str
    category:     str
    language:     str
    chunk_index:  int
    total_chunks: int
    char_count:   int
    extra:        dict = field(default_factory=dict)

    def to_payload(self) -> dict:
        return {
            "text":         self.text,
            "doc_id":       self.doc_id,
            "source":       self.source,
            "source_type":  self.source_type,
            "category":     self.category,
            "language":     self.language,
            "chunk_index":  self.chunk_index,
            "total_chunks": self.total_chunks,
            "char_count":   self.char_count,
            **self.extra,
        }


# ── Text splitter ─────────────────────────────────────────────────────────────

try:
    from langchain_text_splitters import RecursiveCharacterTextSplitter
    _USE_LANGCHAIN = True
except ImportError:
    _USE_LANGCHAIN = False


def split_text(text: str, chunk_size: int = 800, overlap: int = 100) -> List[str]:
    """
    Split text into overlapping chunks.
    Uses RecursiveCharacterTextSplitter when available (langchain-text-splitters),
    falls back to a simple paragraph-aware chunker.
    """
    text = text.strip()
    if not text:
        return []

    if _USE_LANGCHAIN:
        splitter = RecursiveCharacterTextSplitter(
            chunk_size=chunk_size,
            chunk_overlap=overlap,
            separators=["\n\n", "\n", "。", ".", "?", "!", " ", ""],
            keep_separator=False,
        )
        return [c for c in splitter.split_text(text) if c.strip()]

    # Fallback: split on blank lines, then merge until chunk_size
    paragraphs = [p.strip() for p in re.split(r"\n\s*\n", text) if p.strip()]
    chunks: List[str] = []
    current = ""
    for para in paragraphs:
        if len(current) + len(para) > chunk_size and current:
            chunks.append(current)
            # Keep overlap
            words = current.split()
            overlap_words = words[-max(1, overlap // 6):]
            current = " ".join(overlap_words) + " " + para
        else:
            current = (current + "\n\n" + para).strip() if current else para
    if current:
        chunks.append(current)
    return [c for c in chunks if len(c.strip()) >= 20]


# ── Document ID helpers ───────────────────────────────────────────────────────

def stable_doc_id(source: str, row: int = 0) -> str:
    """Stable SHA-256-based UUID for a document (idempotent re-runs)."""
    key = f"doc::{source}::{row}"
    return str(uuid.UUID(hashlib.sha256(key.encode()).hexdigest()[:32]))


def stable_chunk_id(doc_id: str, chunk_index: int) -> str:
    """Stable UUID for a specific chunk within a document."""
    key = f"chunk::{doc_id}::{chunk_index}"
    return str(uuid.UUID(hashlib.sha256(key.encode()).hexdigest()[:32]))


# ── Language detector (lightweight heuristic) ─────────────────────────────────

_VI_CHARS = re.compile(r"[àáâãèéêìíòóôõùúýăđơưạảấầẩẫậắằẳẵặẹẻẽếềểễệỉịọỏốồổỗộớờởỡợụủứừửữựỳỷỹỵ]")
_EN_CHARS = re.compile(r"[a-zA-Z]")


def detect_language(text: str) -> str:
    sample = text[:500]
    vi_count = len(_VI_CHARS.findall(sample))
    en_count = len(_EN_CHARS.findall(sample))
    if vi_count > 3:
        return "vi"
    if en_count > 10:
        return "en"
    return "mixed"


# ── Source loaders ────────────────────────────────────────────────────────────

# Priority order for text fields in CSV rows
_TEXT_FIELD_PRIORITY = (
    "text", "content", "answer", "description", "body",
    "report", "summary", "abstract", "paragraph", "passage",
    "question_answer", "qa"
)

_QUESTION_FIELDS = ("question", "query", "q", "title")
_ANSWER_FIELDS   = ("answer",   "content", "a", "text", "body")

_CATEGORY_MAP = {
    # keywords in filename/path → category
    "qa":          "medical_qa",
    "disease":     "disease",
    "symptom":     "disease",
    "drug":        "drug",
    "medicine":    "drug",
    "guideline":   "guideline",
    "protocol":    "guideline",
    "insurance":   "insurance",
    "bhyt":        "insurance",
    "nutrition":   "nutrition",
    "food":        "nutrition",
    "pharmacy":    "drug",
}


def _infer_category(path_str: str) -> str:
    lower = path_str.lower()
    for keyword, cat in _CATEGORY_MAP.items():
        if keyword in lower:
            return cat
    return "general"


def _best_text_from_row(row: dict) -> Optional[str]:
    """
    Intelligently extract text from a CSV/JSON row.
    Prefers Q+A concatenation when both question and answer fields exist.
    """
    # Try Q+A combination first
    q_val = next((str(row[k]).strip() for k in _QUESTION_FIELDS if k in row and row[k]), None)
    a_val = next((str(row[k]).strip() for k in _ANSWER_FIELDS   if k in row and row[k]), None)

    if q_val and a_val and q_val != a_val:
        return f"Q: {q_val}\nA: {a_val}"
    if q_val and not a_val:
        return q_val
    if a_val and not q_val:
        return a_val

    # Fall back to priority text fields
    for field_name in _TEXT_FIELD_PRIORITY:
        if field_name in row and str(row[field_name]).strip():
            return str(row[field_name]).strip()

    # Last resort: first non-empty column
    for v in row.values():
        s = str(v).strip()
        if len(s) > 20:
            return s

    return None


def iter_local_documents(
    data_dir: Path,
    category_override: Optional[str] = None,
) -> Generator[RawDocument, None, None]:
    """
    Walk data_dir and yield RawDocument from all supported file formats:
    .txt, .md, .csv, .tsv, .json, .jsonl
    """
    if not data_dir.exists():
        print(f"[WARN] Data directory not found: {data_dir}")
        return

    for path in sorted(data_dir.rglob("*")):
        if not path.is_file():
            continue

        suffix = path.suffix.lower()
        rel    = str(path.relative_to(data_dir))
        cat    = category_override or _infer_category(rel)

        # ── Plain text / Markdown ────────────────────────────────────────────
        if suffix in (".txt", ".md"):
            try:
                text = path.read_text(encoding="utf-8", errors="replace").strip()
                if len(text) < 20:
                    continue
                lang = detect_language(text)
                yield RawDocument(
                    doc_id=stable_doc_id(rel),
                    text=text,
                    source=rel,
                    source_type="local_txt",
                    category=cat,
                    language=lang,
                )
            except Exception as e:
                print(f"[SKIP] {path}: {e}")

        # ── CSV / TSV ────────────────────────────────────────────────────────
        elif suffix in (".csv", ".tsv"):
            delim = "\t" if suffix == ".tsv" else ","
            try:
                with open(path, encoding="utf-8", errors="replace", newline="") as fh:
                    reader = csv.DictReader(fh, delimiter=delim)
                    for i, row in enumerate(reader):
                        text = _best_text_from_row(row)
                        if not text or len(text) < 20:
                            continue
                        lang = detect_language(text)
                        # Extra metadata from the row (non-text columns, up to 10)
                        extra_keys = [
                            k for k in row
                            if k not in (*_QUESTION_FIELDS, *_ANSWER_FIELDS, *_TEXT_FIELD_PRIORITY)
                        ][:10]
                        extra = {k: row[k] for k in extra_keys if row[k]}

                        yield RawDocument(
                            doc_id=stable_doc_id(rel, i),
                            text=text,
                            source=rel,
                            source_type="local_csv",
                            category=cat,
                            language=lang,
                            extra=extra,
                        )
            except Exception as e:
                print(f"[SKIP] {path}: {e}")

        # ── JSON / JSONL ─────────────────────────────────────────────────────
        elif suffix in (".json", ".jsonl"):
            try:
                raw = path.read_text(encoding="utf-8", errors="replace")
                items: list = []
                # Try JSONL first
                for line in raw.splitlines():
                    line = line.strip()
                    if line:
                        try:
                            items.append(json.loads(line))
                        except json.JSONDecodeError:
                            break
                else:
                    pass  # all lines parsed

                # If JSONL parsing yielded nothing, try full JSON
                if not items:
                    parsed = json.loads(raw)
                    items = parsed if isinstance(parsed, list) else [parsed]

                for i, item in enumerate(items):
                    if not isinstance(item, dict):
                        continue
                    text = _best_text_from_row(item)
                    if not text or len(text) < 20:
                        continue
                    lang = detect_language(text)
                    yield RawDocument(
                        doc_id=stable_doc_id(rel, i),
                        text=text,
                        source=rel,
                        source_type="local_json",
                        category=cat,
                        language=lang,
                    )
            except Exception as e:
                print(f"[SKIP] {path}: {e}")


# ── Chunker ───────────────────────────────────────────────────────────────────

def chunk_document(
    doc: RawDocument,
    chunk_size: int = 800,
    overlap:    int = 100,
) -> List[DocumentChunk]:
    """Split a RawDocument into DocumentChunks with rich metadata."""
    raw_chunks = split_text(doc.text, chunk_size=chunk_size, overlap=overlap)
    total = len(raw_chunks)
    return [
        DocumentChunk(
            point_id    = stable_chunk_id(doc.doc_id, idx),
            text        = chunk,
            doc_id      = doc.doc_id,
            source      = doc.source,
            source_type = doc.source_type,
            category    = doc.category,
            language    = doc.language,
            chunk_index = idx,
            total_chunks= total,
            char_count  = len(chunk),
            extra       = doc.extra,
        )
        for idx, chunk in enumerate(raw_chunks)
    ]


# ── Qdrant helpers ────────────────────────────────────────────────────────────

async def ensure_collection(qc: AsyncQdrantClient, collection: str, vector_size: int) -> None:
    exists = await qc.collection_exists(collection)
    if not exists:
        await qc.create_collection(
            collection_name=collection,
            vectors_config=VectorParams(size=vector_size, distance=Distance.COSINE),
        )
        print(f"[QDRANT] Created collection '{collection}' (dim={vector_size})")
    else:
        info = await qc.get_collection(collection)
        existing_size = info.config.params.vectors.size  # type: ignore[union-attr]
        if existing_size != vector_size:
            raise ValueError(
                f"Collection '{collection}' already exists with dim={existing_size}, "
                f"but pipeline expects dim={vector_size}. "
                f"Run with --reset to recreate, or change VECTOR_SIZE."
            )
        print(f"[QDRANT] Using existing collection '{collection}' (dim={existing_size})")


async def reset_collection(qc: AsyncQdrantClient, collection: str, vector_size: int) -> None:
    exists = await qc.collection_exists(collection)
    if exists:
        await qc.delete_collection(collection)
        print(f"[QDRANT] Deleted collection '{collection}'")
    await ensure_collection(qc, collection, vector_size)


async def upsert_batch(
    qc:         AsyncQdrantClient,
    collection: str,
    chunks:     List[DocumentChunk],
    vectors:    List[List[float]],
) -> None:
    points = [
        PointStruct(
            id      = chunk.point_id,
            vector  = vector,
            payload = chunk.to_payload(),
        )
        for chunk, vector in zip(chunks, vectors)
    ]
    await qc.upsert(collection_name=collection, points=points)


# ── Main pipeline ─────────────────────────────────────────────────────────────

async def run_pipeline(
    data_dir:   Path,
    collection: str   = COLLECTION,
    reset:      bool  = False,
    dry_run:    bool  = False,
    chunk_size: int   = 800,
    overlap:    int   = 100,
    batch_size: int   = BATCH_SIZE,
) -> dict:
    """
    Full ingest pipeline: load → chunk → embed → upsert.
    Returns a stats dict: {docs, chunks, upserted, skipped}.
    """
    stats = {"docs": 0, "chunks": 0, "upserted": 0, "skipped": 0}

    # ── 1. Setup embedder & Qdrant ─────────────────────────────────────────
    embedder = OllamaEmbedder(
        base_url=OLLAMA_URL,
        model=EMBED_MODEL,
        vector_size=VECTOR_SIZE,
        concurrency=CONCURRENCY,
    )

    print(f"[EMBED] Checking Ollama at {OLLAMA_URL} (model={EMBED_MODEL})…")
    if not await embedder.health_check():
        print(f"[ERROR] Ollama not reachable or model '{EMBED_MODEL}' not pulled.")
        print(f"        Run:  ollama pull {EMBED_MODEL}")
        return stats

    if not dry_run:
        qc = AsyncQdrantClient(
            host=QDRANT_HOST,
            port=QDRANT_PORT,
            api_key=QDRANT_KEY or None,
        )
        if reset:
            await reset_collection(qc, collection, VECTOR_SIZE)
        else:
            await ensure_collection(qc, collection, VECTOR_SIZE)

    # ── 2. Load documents ─────────────────────────────────────────────────
    print(f"\n[LOAD] Scanning {data_dir}…")
    documents = list(iter_local_documents(data_dir))
    stats["docs"] = len(documents)
    print(f"       Found {len(documents)} documents")

    if not documents:
        print("[DONE] No documents to process.")
        if not dry_run:
            await qc.close()
        return stats

    # ── 3. Chunk all documents ────────────────────────────────────────────
    print(f"\n[CHUNK] chunk_size={chunk_size}, overlap={overlap}")
    all_chunks: List[DocumentChunk] = []
    for doc in documents:
        chunks = chunk_document(doc, chunk_size=chunk_size, overlap=overlap)
        all_chunks.extend(chunks)

    stats["chunks"] = len(all_chunks)
    print(f"        {len(documents)} docs → {len(all_chunks)} chunks")

    if dry_run:
        print("\n[DRY-RUN] Skipped embedding and upsert.")
        _print_stats(stats, dry_run=True)
        return stats

    # ── 4. Embed & upsert in batches ──────────────────────────────────────
    print(f"\n[INGEST] Batch size={batch_size}, concurrency={CONCURRENCY}")
    total_batches = (len(all_chunks) + batch_size - 1) // batch_size

    for batch_idx in range(total_batches):
        start = batch_idx * batch_size
        end   = min(start + batch_size, len(all_chunks))
        batch = all_chunks[start:end]

        texts   = [c.text for c in batch]
        vectors = await embedder.embed_batch(texts)

        # Filter out failed embeddings (zero vectors from unrecoverable errors)
        valid_chunks:  List[DocumentChunk]  = []
        valid_vectors: List[List[float]] = []
        for chunk, vec in zip(batch, vectors):
            if any(v != 0.0 for v in vec):
                valid_chunks.append(chunk)
                valid_vectors.append(vec)
            else:
                stats["skipped"] += 1

        if valid_chunks:
            await upsert_batch(qc, collection, valid_chunks, valid_vectors)
            stats["upserted"] += len(valid_chunks)

        pct = int((batch_idx + 1) / total_batches * 100)
        print(
            f"  [{pct:3d}%] batch {batch_idx+1}/{total_batches} — "
            f"{stats['upserted']} upserted, {stats['skipped']} skipped"
        )

    await qc.close()
    _print_stats(stats)
    return stats


def _print_stats(stats: dict, dry_run: bool = False) -> None:
    tag = "[DRY-RUN]" if dry_run else "[DONE]"
    print(
        f"\n{tag} docs={stats['docs']}  chunks={stats['chunks']}  "
        f"upserted={stats['upserted']}  skipped={stats['skipped']}"
    )


# ── CLI entry point ───────────────────────────────────────────────────────────

def _parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(
        description="Ingest medical documents into Qdrant for RAG",
        formatter_class=argparse.RawTextHelpFormatter,
    )
    p.add_argument(
        "--data-dir", default=str(_ROOT / "data"),
        help="Root directory to scan for documents (default: ./data)"
    )
    p.add_argument(
        "--collection", default=COLLECTION,
        help=f"Qdrant collection name (default: {COLLECTION})"
    )
    p.add_argument(
        "--reset", action="store_true",
        help="Delete and recreate the collection before ingesting"
    )
    p.add_argument(
        "--dry-run", action="store_true",
        help="Parse and chunk documents only — no embedding or Qdrant writes"
    )
    p.add_argument(
        "--chunk-size", type=int, default=800,
        help="Target chunk size in characters (default: 800)"
    )
    p.add_argument(
        "--overlap", type=int, default=100,
        help="Chunk overlap in characters (default: 100)"
    )
    p.add_argument(
        "--batch-size", type=int, default=BATCH_SIZE,
        help=f"Embedding batch size (default: {BATCH_SIZE})"
    )
    return p.parse_args()


if __name__ == "__main__":
    args = _parse_args()
    asyncio.run(run_pipeline(
        data_dir   = Path(args.data_dir),
        collection = args.collection,
        reset      = args.reset,
        dry_run    = args.dry_run,
        chunk_size = args.chunk_size,
        overlap    = args.overlap,
        batch_size = args.batch_size,
    ))
