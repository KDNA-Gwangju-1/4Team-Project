from __future__ import annotations

import json
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / "output" / "chapter2-enemies-distinct-soft-v1"
ENEMIES = ("patch_rabbit", "folded_crow", "night_light")


def main() -> None:
    errors: list[str] = []
    checked: list[dict[str, object]] = []

    for enemy in ENEMIES:
        enemy_root = PACKAGE / "02_enemies" / enemy
        for motion in ("attack_8f", "dissolve_8f"):
            motion_root = enemy_root / motion
            record: dict[str, object] = {"enemy": enemy, "motion": motion}
            for variant in ("transparent", "opaque_navy"):
                root = motion_root / variant
                frames = sorted((root / "frames").glob("*.png"))
                gifs = list(root.glob("*.gif"))
                sheets = list(root.glob("*-sheet.png"))
                if len(frames) != 8:
                    errors.append(f"{enemy}/{motion}/{variant}: expected 8 PNG frames, got {len(frames)}")
                if len(gifs) != 1:
                    errors.append(f"{enemy}/{motion}/{variant}: expected one GIF")
                if len(sheets) != 1:
                    errors.append(f"{enemy}/{motion}/{variant}: expected one sheet")
                for frame in frames:
                    with Image.open(frame) as image:
                        if image.size != (512, 512):
                            errors.append(f"Wrong frame size: {frame} -> {image.size}")
                        if variant == "transparent" and "A" not in image.getbands():
                            errors.append(f"Missing alpha: {frame}")
                        if variant == "opaque_navy" and image.mode != "RGB":
                            errors.append(f"Opaque frame not RGB: {frame}")
                if gifs:
                    with Image.open(gifs[0]) as image:
                        if getattr(image, "n_frames", 1) != 8:
                            errors.append(f"Wrong GIF frame count: {gifs[0]}")
                if sheets:
                    with Image.open(sheets[0]) as image:
                        if image.size != (2048, 1024):
                            errors.append(f"Wrong sheet size: {sheets[0]} -> {image.size}")
            if motion == "dissolve_8f":
                last = sorted((motion_root / "transparent" / "frames").glob("*.png"))[-1]
                with Image.open(last).convert("RGBA") as image:
                    if image.getchannel("A").getextrema()[1] != 0:
                        errors.append(f"Final dissolve frame is not empty: {last}")
            checked.append({"enemy": enemy, "motion": motion, "frames_per_variant": 8})

    all_pngs = list(PACKAGE.rglob("*.png"))
    for path in all_pngs:
        try:
            with Image.open(path) as image:
                image.verify()
        except Exception as exc:
            errors.append(f"Corrupt PNG {path}: {exc}")

    report = {
        "status": "PASS" if not errors else "FAIL",
        "package": str(PACKAGE),
        "enemy_count": len(ENEMIES),
        "animation_count": len(checked),
        "checked": checked,
        "total_png_count": len(all_pngs),
        "errors": errors,
    }
    report_path = PACKAGE / "00_docs" / "QA-REPORT.json"
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))
    raise SystemExit(0 if not errors else 1)


if __name__ == "__main__":
    main()
