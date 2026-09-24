"""Run only after the user approves sending these five lines to Edge TTS."""
import asyncio
import json
from pathlib import Path
import edge_tts

ROOT = Path(__file__).resolve().parents[2]

async def main():
    out = ROOT / 'ArtSource/Chapter1ClueVoice/Preview'
    out.mkdir(parents=True, exist_ok=True)
    rows = json.loads((ROOT / 'ArtSource/Chapter1ClueVoice/dialogue.json').read_text(encoding='utf-8-sig'))
    manifest = []
    for row in rows:
        for i, text in enumerate(row['text'].split('\n\n')):
            dest = out / f"{row['id']}_{i+1:02}.mp3"
            if not dest.exists():
                await edge_tts.Communicate(text.strip().strip('()'), 'ko-KR-SunHiNeural', rate='-12%', pitch='-2Hz').save(str(dest))
            manifest.append(dict(id=row['id'], part=i+1, text=text, file=str(dest.relative_to(ROOT)), voice='ko-KR-SunHiNeural', rate='-12%', pitch='-2Hz'))
            print(dest.name, dest.stat().st_size, flush=True)
    (ROOT / 'ArtSource/Chapter1ClueVoice/manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')

asyncio.run(main())
