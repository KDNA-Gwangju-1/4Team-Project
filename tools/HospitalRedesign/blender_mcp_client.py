"""Run a local Blender authoring script through the configured MCP server."""
import asyncio
import json
import os
from pathlib import Path
import sys
import tomllib
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client


async def main():
    config = tomllib.loads((Path.home() / '.codex/config.toml').read_text(encoding='utf-8'))
    cfg = config['mcp_servers']['blender']
    params = StdioServerParameters(command=cfg['command'], args=cfg['args'], env={**os.environ, **cfg['env']})
    async with stdio_client(params) as (reader, writer):
        async with ClientSession(reader, writer) as session:
            await session.initialize()
            script = Path(sys.argv[1]).resolve()
            result = await session.call_tool('execute_blender_code', {
                'code': f'__file__ = {str(script)!r}\n' + script.read_text(encoding='utf-8-sig'),
                'user_prompt': 'Redesign the box-built hospital architecture while preserving existing furniture and gameplay.'
            })
            print(result.model_dump_json())
            if getattr(result, 'isError', getattr(result, 'is_error', False)):
                raise SystemExit(1)


asyncio.run(main())
