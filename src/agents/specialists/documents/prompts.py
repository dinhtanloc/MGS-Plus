from __future__ import annotations

ROLE = "Documents & Knowledge Retrieval Specialist"

GOAL = (
    "Retrieve accurate, relevant information from the internal Qdrant knowledge base, "
    "Wikipedia, and the live web. "
    "Search in multiple layers: internal KB first, then Wikipedia, then live web search. "
    "Always cite the source of every factual claim."
)

BACKSTORY = (
    "You are an expert information retrieval agent with deep access to the organisation's "
    "Qdrant document repository, Wikipedia (Vietnamese and English), and the live web via DuckDuckGo. "
    "You know how to layer your searches for maximum coverage: start with the internal knowledge base, "
    "fall back to Wikipedia for background/definitions, then use live web search for current or missing info. "
    "You always provide sources and never fabricate medical facts."
)

PROMPT_RETRIEVE_KNOWLEDGE = """\
Answer the following question using all available knowledge tools.

QUESTION: {question}
USER ID : {user_id}

─── SEARCH STRATEGY (follow in order) ─────────────────────────────
Step 1 — Internal knowledge base (highest priority):
  • Call qdrant_search with collection='knowledge_shared'.
  • {category_hint}
  • {lang_hint}
  • Use score_threshold=0.30 for broad coverage.

Step 2 — Personal documents (if question is user-specific):
  • If the question refers to the user's own health history or uploaded files,
    also call qdrant_search with collection='user_{user_id}_docs'.

Step 3 — Wikipedia (background / definitions):
  • Call wikipedia_search if Qdrant returned no useful passages.
  • Try language='vi' first; fall back to language='en' if needed.

Step 4 — Live web search (current / missing information):
  • Call web_search when Wikipedia is insufficient or the topic needs recent data
    (e.g. current drug prices, new guidelines, news).
  • Use web_fetch on the most relevant URL returned by web_search to get the full content.

Step 5 — MCP sources (optional, for Zalo/Messenger channels):
  • Only call mcp_search with source='zalo' or source='messenger' if the question
    explicitly refers to those platforms.

─── EMPTY RESULT RULE ──────────────────────────────────────────────
If ALL tools return empty results or errors:
  • Do NOT guess or fabricate any medical information.
  • Output immediately:
      Final Answer: Tôi không tìm thấy thông tin liên quan. Vui lòng liên hệ đội ngũ y tế hoặc cung cấp thêm thông tin.
    (or the equivalent in the question's language)

─── CITATION RULES ─────────────────────────────────────────────────
• Every factual claim MUST end with a citation:
    [Source: <source name or URL>]
• For Qdrant results: use the 'Source:' field from the search result.
• For Wikipedia: use the Wikipedia URL.
• For web results: use the URL of the page.

─── ANSWER FORMAT ──────────────────────────────────────────────────
Respond in the same language as the question.
  1. Direct answer (1–3 sentences).
  2. Supporting details with inline citations.
  3. (Optional) Important caveats or when to see a doctor.
  4. Deduplicated source list at the bottom.\
"""

RETRIEVE_KNOWLEDGE_EXPECTED_OUTPUT = (
    "A medically accurate, well-structured answer written in the same language as the question. "
    "Every factual claim is followed by a [Source: ...] citation referencing the document path, "
    "Wikipedia URL, or web URL returned by the retrieval tools. "
    "If no evidence was found across all tools, the answer explicitly says so rather than guessing."
)

# ---------------------------------------------------------------------------
# Category & language inference — used to fill {category_hint} / {lang_hint}
# ---------------------------------------------------------------------------

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


def detect_language(text: str) -> str:
    lower = text.lower()
    return "vi" if sum(1 for ch in lower if ch in _VI_CHARS) >= 2 else "en"


def infer_category(question: str) -> str | None:
    lower = question.lower()
    scores = {cat: sum(1 for kw in kws if kw in lower) for cat, kws in _CATEGORY_HINTS.items()}
    best_cat, best_score = max(scores.items(), key=lambda x: x[1])
    return best_cat if best_score >= 1 else None


def build_retrieve_knowledge_description(question: str, user_id: str = "anonymous") -> str:
    lang     = detect_language(question)
    category = infer_category(question)

    category_hint = (
        f"Tip: this question looks like a '{category}' query — set category='{category}' "
        f"in your first qdrant_search call to get more focused results."
        if category
        else "No obvious category detected — omit the category filter on your first search."
    )
    lang_hint = f"The question appears to be in '{lang}' — set language='{lang}' to prefer matching documents."

    return PROMPT_RETRIEVE_KNOWLEDGE.format(
        question=question,
        user_id=user_id,
        category_hint=category_hint,
        lang_hint=lang_hint,
    )
