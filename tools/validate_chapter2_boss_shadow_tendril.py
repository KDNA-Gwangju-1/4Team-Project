from __future__ import annotations

import json
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / "output" / "chapter2-boss-shadow-tendril-v1"


def main() -> None:
    errors: list[str] = []
    results: list[dict[str, object]] = []
    for motion in ("attack_8f", "dissolve_8f"):
        motion_root = PACKAGE / motion
        for variant in ("transparent", "opaque_navy"):
            root = motion_root / variant
            frames = sorted((root / "frames").glob("*.png"))
            gifs = list(root.glob("*.gif"))
            sheets = list(root.glob("*-sheet.png"))
            if len(frames) != 8:
                errors.append(f"{motion}/{variant}: expected 8 PNG frames, got {len(frames)}")
            if len(gifs) != 1:
                errors.append(f"{motion}/{variant}: expected one GIF")
            if len(sheets) != 1:
                errors.append(f"{motion}/{variant}: expected one sprite sheet")
            for frame in frames:
                with Image.open(frame) as image:
                    if image.size != (512, 512):
                        errors.append(f"Wrong frame size: {frame} -> {image.size}")
                    if variant == "transparent" and "A" not in image.getbands():
                        errors.append(f"Missing alpha: {frame}")
                    if variant == "opaque_navy" and image.mode != "RGB":
                        errors.append(f"Opaque frame is not RGB: {frame}")
            if gifs:
                with Image.open(gifs[0]) as image:
                    if getattr(image, "n_frames", 1) != 8:
                        errors.append(f"Wrong GIF frame count: {gifs[0]}")
            if sheets:
                with Image.open(sheets[0]) as image:
                    if image.size != (2048, 1024):
                        errors.append(f"Wrong sheet size: {sheets[0]} -> {image.size}")
            results.append({"motion": motion, "variant": variant, "frames": len(frames)})

    last = sorted((PACKAGE / "dissolve_8f" / "transparent" / "frames").glob("*.png"))[-1]
    with Image.open(last).convert("RGBA") as image:
        if image.getchannel("A").getextrema()[1] != 0:
            errors.append("Final dissolve frame is not completely transparent")

    for path in PACKAGE.rglob("*.png"):
        try:
            with Image.open(path) as image:
                image.verify()
        except Exception as exc:
            errors.append(f"Corrupt PNG {path}: {exc}")

    report = {
        "status": "PASS" if not errors else "FAIL",
        "package": str(PACKAGE),
        "design": "boss-summoned shadow tendril",
        "animation_count": 2,
        "results": results,
        "errors": errors,
    }
    (PACKAGE / "00_docs" / "QA-REPORT.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    print(json.dumps(report, ensure_ascii=False, indent=2))
    raise SystemExit(0 if not errors else 1)


if __name__ == "__main__":
    main()
