"""Execute an authoring script on the installed local Blender MCP add-on."""
import json
import socket
import sys
from pathlib import Path

script = Path(sys.argv[1]).resolve()
payload = {'type': 'execute_code', 'params': {'code': '__file__ = ' + repr(str(script)) + '\nexec(compile(open(__file__, encoding="utf-8-sig").read(), __file__, "exec"))'}}
with socket.create_connection(('127.0.0.1', 9876), timeout=10) as connection:
    connection.settimeout(300)
    connection.sendall(json.dumps(payload).encode())
    chunks = bytearray()
    while True:
        part = connection.recv(65536)
        if not part:
            raise RuntimeError('Blender closed the connection before returning a result')
        chunks.extend(part)
        try:
            result = json.loads(chunks)
            break
        except json.JSONDecodeError:
            pass
print(json.dumps(result, ensure_ascii=False))
if result.get('status') != 'success':
    raise SystemExit(1)
