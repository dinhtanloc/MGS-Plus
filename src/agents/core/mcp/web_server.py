"""
web_server.py — Local MCP server exposing web search and web fetch tools.

Implements the JSON-RPC 2.0 MCP HTTP transport so that MCPClient can
discover and call it exactly like any other MCP server.

Usage (standalone):
    uv run python -m src.agents.core.mcp.web_server

Docker:
    CMD ["python", "-m", "src.agents.core.mcp.web_server"]
"""
from __future__ import annotations

import os
from typing import Any, Dict

import httpx
from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse

PORT       = int(os.getenv("MCP_WEB_PORT", "8021"))
USER_AGENT = "MGSPlus/1.0 (medical assistant; contact@mgsplus.vn)"
MAX_SEARCH_RESULTS = 8
MAX_FETCH_CHARS    = 5000

app = FastAPI(title="MCP Web Server", docs_url=None, redoc_url=None)

# ── Tool definitions ──────────────────────────────────────────────────────────

_TOOLS = [
    {
        "name": "web_search",
        "description": (
            "Search the web using DuckDuckGo for up-to-date information. "
            "Returns titles, snippets, and URLs. "
            "Use for current events, recent medical guidelines, or any topic not in the knowledge base."
        ),
        "inputSchema": {
            "type": "object",
            "properties": {
                "query": {
                    "type": "string",
                    "description": "Search query",
                },
                "max_results": {
                    "type": "integer",
                    "description": "Maximum results to return (default: 5, max: 8)",
                    "default": 5,
                },
                "region": {
                    "type": "string",
                    "description": "Region/language: 'vn-vi' (Vietnamese), 'us-en' (English)",
                    "default": "vn-vi",
                },
            },
            "required": ["query"],
        },
    },
    {
        "name": "web_fetch",
        "description": (
            "Fetch and extract the text content from a specific URL. "
            "Strips scripts, styles, and navigation — returns the main readable text. "
            "Useful for reading full articles or MGSPlus platform pages."
        ),
        "inputSchema": {
            "type": "object",
            "properties": {
                "url": {
                    "type": "string",
                    "description": "URL to fetch",
                },
                "max_chars": {
                    "type": "integer",
                    "description": "Maximum characters to return (default: 3000)",
                    "default": 3000,
                },
            },
            "required": ["url"],
        },
    },
]


# ── Tool implementations ──────────────────────────────────────────────────────

def _web_search(query: str, max_results: int = 5, region: str = "vn-vi") -> str:
    try:
        from duckduckgo_search import DDGS

        max_results = min(max_results, MAX_SEARCH_RESULTS)
        results = []
        with DDGS() as ddgs:
            for r in ddgs.text(query, region=region, max_results=max_results):
                results.append(r)

        if not results:
            return f"No web results found for: '{query}'. Try rephrasing or using a broader query."

        lines = [f"Web search results for: '{query}' (region={region})\n"]
        for i, r in enumerate(results, 1):
            lines.append(
                f"[{i}] {r.get('title', 'No title')}\n"
                f"     URL: {r.get('href', '')}\n"
                f"     {r.get('body', '')}\n"
            )
        return "\n".join(lines)
    except Exception as exc:
        return f"Web search failed: {exc}"


def _web_fetch(url: str, max_chars: int = 3000) -> str:
    try:
        from bs4 import BeautifulSoup

        max_chars = min(max_chars, MAX_FETCH_CHARS)
        resp = httpx.get(
            url,
            follow_redirects=True,
            timeout=15.0,
            headers={"User-Agent": USER_AGENT},
        )
        resp.raise_for_status()
        soup = BeautifulSoup(resp.text, "lxml")

        for tag in soup(["script", "style", "nav", "footer", "header", "aside"]):
            tag.decompose()

        text = soup.get_text(separator="\n", strip=True)
        lines = [ln for ln in text.splitlines() if ln.strip()]
        text = "\n".join(lines)[:max_chars]

        return f"Content fetched from {url}:\n\n{text}"
    except Exception as exc:
        return f"Could not fetch '{url}': {exc}"


# ── JSON-RPC 2.0 dispatcher ───────────────────────────────────────────────────

def _dispatch(method: str, params: Dict[str, Any]) -> Any:
    if method == "ping":
        return "pong"

    if method == "tools/list":
        return {"tools": _TOOLS}

    if method == "tools/call":
        name      = params.get("name", "")
        arguments = params.get("arguments", {})

        if name == "web_search":
            text = _web_search(
                query=arguments.get("query", ""),
                max_results=int(arguments.get("max_results", 5)),
                region=arguments.get("region", "vn-vi"),
            )
            return {"content": [{"type": "text", "text": text}]}

        if name == "web_fetch":
            text = _web_fetch(
                url=arguments.get("url", ""),
                max_chars=int(arguments.get("max_chars", 3000)),
            )
            return {"content": [{"type": "text", "text": text}]}

        raise ValueError(f"Unknown tool '{name}'")

    raise ValueError(f"Unknown method '{method}'")


@app.post("/")
async def handle(request: Request) -> JSONResponse:
    body   = await request.json()
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
    return {"status": "ok", "service": "mcp-web"}


if __name__ == "__main__":
    import uvicorn
    uvicorn.run("src.agents.core.mcp.web_server:app", host="0.0.0.0", port=PORT, reload=False)
