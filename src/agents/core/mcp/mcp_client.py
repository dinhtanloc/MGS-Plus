from __future__ import annotations

from typing import Any, Dict, List, Optional

import httpx
from tenacity import retry, stop_after_attempt, wait_exponential, retry_if_exception_type


class MCPTool(dict):
    """Lightweight descriptor for a tool exposed by an MCP server."""

    @property
    def name(self) -> str:
        return self["name"]

    @property
    def description(self) -> str:
        return self.get("description", "")

    @property
    def input_schema(self) -> dict:
        return self.get("inputSchema", {})


class MCPClient:
    """Minimal async MCP client that speaks the MCP HTTP (SSE) transport.

    For each external data source (Zalo, Messenger, wiki, …), instantiate
    one client pointing at the MCP server URL for that source.

    Usage:
        client = MCPClient("http://localhost:3001")
        tools  = await client.list_tools()
        result = await client.call_tool("search", {"query": "dengue fever"})
    """

    def __init__(self, server_url: str, timeout: float = 30.0) -> None:
        self.server_url = server_url.rstrip("/")
        self._timeout = timeout

    @retry(
        stop=stop_after_attempt(3),
        wait=wait_exponential(multiplier=1, min=1, max=4),
        retry=retry_if_exception_type((httpx.TransportError, httpx.TimeoutException)),
        reraise=True,
    )
    async def list_tools(self) -> List[MCPTool]:
        async with httpx.AsyncClient(timeout=self._timeout) as client:
            resp = await client.post(
                f"{self.server_url}/",
                json={"jsonrpc": "2.0", "id": 1, "method": "tools/list", "params": {}},
            )
            resp.raise_for_status()
            data = resp.json()
            return [MCPTool(t) for t in data.get("result", {}).get("tools", [])]

    @retry(
        stop=stop_after_attempt(3),
        wait=wait_exponential(multiplier=1, min=1, max=4),
        retry=retry_if_exception_type((httpx.TransportError, httpx.TimeoutException)),
        reraise=True,
    )
    async def call_tool(
        self,
        tool_name: str,
        arguments: Optional[Dict[str, Any]] = None,
    ) -> Any:
        payload = {
            "jsonrpc": "2.0",
            "id": 1,
            "method": "tools/call",
            "params": {"name": tool_name, "arguments": arguments or {}},
        }
        async with httpx.AsyncClient(timeout=self._timeout) as client:
            resp = await client.post(f"{self.server_url}/", json=payload)
            resp.raise_for_status()
            data = resp.json()
            if "error" in data:
                raise ValueError(f"MCP error: {data['error']}")
            return data.get("result", {}).get("content")

    async def ping(self) -> bool:
        """Return True if the MCP server is reachable."""
        try:
            async with httpx.AsyncClient(timeout=5.0) as client:
                resp = await client.post(
                    f"{self.server_url}/",
                    json={"jsonrpc": "2.0", "id": 0, "method": "ping", "params": {}},
                )
                return resp.status_code < 500
        except Exception:
            return False
