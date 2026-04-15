from __future__ import annotations

from crewai import Agent, Task

# Category keywords used to infer the best Qdrant filter for a given question.
_CATEGORY_HINTS: dict[str, list[str]] = {
    "disease": [
        "disease", "symptom", "diagnosis", "infection", "fever", "pain",
        "bệnh", "triệu chứng", "chẩn đoán", "sốt",
    ],
    "drug": [
        "drug", "medication", "medicine", "dose", "dosage", "side effect",
        "thuốc", "liều", "tác dụng phụ",
    ],
    "insurance": [
        "insurance", "bhyt", "health card", "coverage", "reimbursement",
        "bảo hiểm", "thẻ bảo hiểm", "thanh toán",
    ],
    "nutrition": [
        "diet", "food", "nutrition", "eat", "vitamin", "calorie",
        "ăn", "dinh dưỡng", "thực phẩm",
    ],
    "guideline": [
        "guideline", "protocol", "procedure", "standard",
        "hướng dẫn", "quy trình",
    ],
}

_VI_CHARS = set("àáâãèéêìíòóôõùúýăđơưạảấầẩẫậắằẳẵặẹẻẽếềểễệỉịọỏốồổỗộớờởỡợụủứừửữựỳỷỹ")


def _detect_language(text: str) -> str:
    """Return 'vi' when the question contains Vietnamese characters, else 'en'."""
    lower = text.lower()
    vi_count = sum(1 for ch in lower if ch in _VI_CHARS)
    return "vi" if vi_count >= 2 else "en"


def _infer_category(question: str) -> str | None:
    """Return the best-matching Qdrant category, or None if unclear."""
    lower = question.lower()
    scores: dict[str, int] = {}
    for cat, keywords in _CATEGORY_HINTS.items():
        scores[cat] = sum(1 for kw in keywords if kw in lower)
    best_cat, best_score = max(scores.items(), key=lambda x: x[1])
    return best_cat if best_score >= 1 else None


def retrieve_knowledge_task(
    documents_agent: Agent,
    question: str,
    user_id: str = "anonymous",
) -> Task:
    """Build a RAG retrieval task for the Documents agent.

    The task description instructs the agent to:
    - Use inferred category/language filters to narrow the Qdrant search.
    - Cite every claim with [Source: <path>] taken from the search result header.
    - Fall back to MCP wiki search only when Qdrant finds nothing useful.
    - Never fabricate facts — if no evidence is found, say so explicitly.
    """
    lang     = _detect_language(question)
    category = _infer_category(question)

    category_hint = (
        f"Tip: this question looks like a '{category}' query — set category='{category}' "
        f"in your first qdrant_search call to get more focused results."
        if category
        else "No obvious category detected — omit the category filter on your first search."
    )

    lang_hint = (
        f"The question appears to be in '{lang}' — set language='{lang}' to prefer matching documents."
    )

    return Task(
        description=(
            f"Answer the following question using the MGSPlus medical knowledge base.\n\n"
            f"QUESTION: {question}\n"
            f"USER ID : {user_id}\n\n"

            "─── SEARCH STRATEGY ───────────────────────────────────────────────\n"
            "Step 1 — Primary search (shared knowledge base):\n"
            "  • Call qdrant_search with collection='knowledge_shared'.\n"
            f"  • {category_hint}\n"
            f"  • {lang_hint}\n"
            "  • Use score_threshold=0.30 for broad coverage; raise to 0.50 if too many\n"
            "    irrelevant results appear.\n\n"

            f"Step 2 — Personal documents (if question is user-specific):\n"
            f"  • If the question refers to the user's own health history or uploaded files,\n"
            f"    also call qdrant_search with collection='user_{user_id}_docs'.\n\n"

            "Step 3 — External fallback (only when Qdrant returns no useful results):\n"
            "  • Call mcp_search with source='wiki' for medical definitions or background.\n"
            "  • Do NOT call mcp_search if Qdrant already returned ≥ 1 relevant passage.\n\n"

            "─── CITATION RULES ─────────────────────────────────────────────────\n"
            "• Every factual claim MUST end with a citation in this exact format:\n"
            "    [Source: <value from the 'Source:' field in the search result>]\n"
            "• If multiple passages support the same claim, list all sources:\n"
            "    [Source: file_a.txt, file_b.jsonl]\n"
            "• If no relevant evidence was found, state clearly:\n"
            "    'I could not find reliable information about this topic in the knowledge base.'\n"
            "  Do NOT guess or fabricate medical information.\n\n"

            "─── ANSWER FORMAT ──────────────────────────────────────────────────\n"
            "Respond in the same language as the question.\n"
            "Structure your answer as follows:\n"
            "  1. Direct answer to the question (1–3 sentences).\n"
            "  2. Supporting details with inline citations.\n"
            "  3. (Optional) Important caveats or when to see a doctor.\n"
            "  4. Sources used (deduplicated list at the bottom).\n"
        ),
        expected_output=(
            "A medically accurate, well-structured answer written in the same language as the question. "
            "Every factual claim is followed by a [Source: ...] citation referencing the document "
            "path or URL returned by qdrant_search or mcp_search. "
            "If no evidence was found, the answer explicitly says so rather than guessing."
        ),
        agent=documents_agent,
    )
