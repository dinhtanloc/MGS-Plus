"""
wiki_server.py — Local MCP server exposing a Wikipedia search tool.

Implements the JSON-RPC 2.0 MCP HTTP transport so that MCPClient can
discover and call it exactly like any other MCP server.

Usage (standalone):
    uv run python -m src.agents.core.mcp.wiki_server

Docker:
    CMD ["python", "-m", "src.agents.core.mcp.wiki_server"]
"""
from __future__ import annotations

import json
import os
from typing import Any, Dict

import wikipediaapi
from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse

WIKI_LANG  = os.getenv("WIKI_LANG", "vi")       # Vietnamese by default; override with WIKI_LANG=en
PORT       = int(os.getenv("MCP_WIKI_PORT", "8020"))
USER_AGENT = "MGSPlus/1.0 (medical assistant; contact@mgsplus.vn)"

_wiki = wikipediaapi.Wikipedia(language=WIKI_LANG, user_agent=USER_AGENT)

app = FastAPI(title="MCP Wikipedia Server", docs_url=None, redoc_url=None)

# ── Tool definitions ──────────────────────────────────────────────────────────

_TOOLS = [
    {
        "name": "wiki_search",
        "description": (
            "Search Wikipedia for a topic and return the summary. "
            "Useful for general knowledge, medical concepts, drug information, etc."
        ),
        "inputSchema": {
            "type": "object",
            "properties": {
                "query": {
                    "type": "string",
                    "description": "Topic to search for",
                },
                "sentences": {
                    "type": "integer",
                    "description": "Maximum number of sentences to return (default: 5)",
                    "default": 5,
                },
            },
            "required": ["query"],
        },
    },
    {
        "name": "wiki_sections",
        "description": (
            "Return the section titles of a Wikipedia page. "
            "Useful for navigating long articles."
        ),
        "inputSchema": {
            "type": "object",
            "properties": {
                "title": {
                    "type": "string",
                    "description": "Exact Wikipedia page title",
                },
            },
            "required": ["title"],
        },
    },
]


# ── Tool implementations ──────────────────────────────────────────────────────

def _wiki_search(query: str, sentences: int = 5) -> str:
    page = _wiki.page(query)
    if not page.exists():
        # Try summary search by switching to English fallback
        en_wiki = wikipediaapi.Wikipedia(language="en", user_agent=USER_AGENT)
        page = en_wiki.page(query)
        if not page.exists():
            return f"No Wikipedia article found for '{query}'."

    # Split summary into sentences and return at most `sentences` of them
    summary = page.summary
    if sentences:
        parts = summary.split(". ")
        summary = ". ".join(parts[:sentences])
        if not summary.endswith("."):
            summary += "."

    return f"**{page.title}**\n\n{summary}\n\nURL: {page.fullurl}"


def _wiki_sections(title: str) -> str:
    page = _wiki.page(title)
    if not page.exists():
        return f"Page '{title}' not found."

    def _collect(sections, level=0) -> list[str]:
        lines = []
        for s in sections:
            lines.append("  " * level + f"- {s.title}")
            lines.extend(_collect(s.sections, level + 1))
        return lines

    lines = _collect(page.sections)
    return "\n".join(lines) if lines else "No sections found."


# ── JSON-RPC 2.0 dispatcher ───────────────────────────────────────────────────

def _dispatch(method: str, params: Dict[str, Any]) -> Any:
    if method == "ping":
        return "pong"

    if method == "tools/list":
        return {"tools": _TOOLS}

    if method == "tools/call":
        name      = params.get("name", "")
        arguments = params.get("arguments", {})

        if name == "wiki_search":
            text = _wiki_search(
                query=arguments.get("query", ""),
                sentences=int(arguments.get("sentences", 5)),
            )
            return {"content": [{"type": "text", "text": text}]}

        if name == "wiki_sections":
            text = _wiki_sections(title=arguments.get("title", ""))
            return {"content": [{"type": "text", "text": text}]}

        raise ValueError(f"Unknown tool '{name}'")

    raise ValueError(f"Unknown method '{method}'")


@app.post("/")
async def handle(request: Request) -> JSONResponse:
    body = await request.json()
    rpc_id = body.get("id", 1)
    method = body.get("method", "")
    params = body.get("params", {})

    try:
        result = _dispatch(method, params)
        return JSONResponse({"jsonrpc": "2.0", "id": rpc_id, "result": result})
    except Exception as exc:
        return JSONResponse(
            {"jsonrpc": "2.0", "id": rpc_id, "error": {"code": -32600, "message": str(exc)}},
            status_code=200,
        )


@app.get("/health")
async def health() -> Dict[str, str]:
    return {"status": "ok", "service": "mcp-wiki"}


if __name__ == "__main__":
    import uvicorn
    uvicorn.run("src.agents.core.mcp.wiki_server:app", host="0.0.0.0", port=PORT, reload=False)
