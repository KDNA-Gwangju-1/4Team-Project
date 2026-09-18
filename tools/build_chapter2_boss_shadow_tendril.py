from __future__ import annotations

import json
import shutil
from pathlib import Path

from PIL import Image

from build_chapter2_enemies_distinct_soft import export_animation, opaque, standardize


ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "output" / "chapter2-boss-shadow-tendril-v1"
GENERATED = Path(r"C:\Users\Note-038\.codex\generated_images\01a0acd2-304b-7e20-8907-152159c50899")
DESIGN = GENERATED / "exec-e52733f6-d174-4e25-9647-c0bdfcfdf519.png"
ATTACK = GENERATED / "exec-44135117-a620-4d6b-adae-15c7a0c0caf5.png"
DISSOLVE = GENERATED / "exec-549f3c38-bd0b-44a4-9871-ed4dc7f08dbe.png"
BOSS_REFERENCE = Path(r"C:\Users\Note-038\AppData\Local\Temp\codex-clipboard-e7b75c32-cab4-4593-8743-f3c0e0d36346.png")


def export_design() -> None:
    root = TARGET / "design"
    root.mkdir(parents=True, exist_ok=True)
    image = standardize(Image.open(DESIGN))
    image.save(root / "boss-shadow-tendril-transparent.png")
    opaque(image).save(root / "boss-shadow-tendril-opaque-navy.png")
    shutil.copy2(DESIGN, root / "boss-shadow-tendril-imagegen-original.png")


def write_docs() -> None:
    docs = TARGET / "00_docs"
    docs.mkdir(parents=True, exist_ok=True)
    (docs / "README.md").write_text("""# Chapter 2 Boss Shadow Tendril v1

- 보스의 검은 크레용 망토에서 소환되는 그림자 촉수
- `design`: 투명/네이비 불투명 디자인 PNG
- `attack_8f`: 생성 → 준비 → 오른쪽 내려찍기 → 복귀, 8F
- `dissolve_8f`: 위쪽부터 지워짐 → 그림자 웅덩이 소멸, 8F
- 각 모션: 투명/네이비 불투명, 512×512 프레임, 2048×1024 시트, GIF
- 소멸 8번째 프레임은 완전 투명
- 피벗: bottom-center
""", encoding="utf-8")
    (docs / "IMAGEGEN-PROMPTS.md").write_text("""# ImageGen prompts — Boss Shadow Tendril

Mode: built-in `imagegen`. Boss reference: user-supplied boss image.

## Design

Create one boss-summoned shadow tendril using the boss's dense black wax-crayon mass, rough warm-cream paper cutline, muted purple strokes and restrained red scratches. Thick tapering ribbon rising from a small shadow puddle, gentle S curve, simple three-lobed brush tip echoing the boss claw. Elegant shadow magic, not flesh. No face, suction cups, veins, anatomy or gore. Transparent background.

## Attack 8F

Exactly 8 isolated frames in 4x2 order: shadow puddle and nub; half rise; full upright; coil left; arc right; ground slam right with compact cream/red crayon impact; recoil; upright ready. Lock the base to bottom-center and keep every frame inside its cell.

## Dissolve 8F

Exactly 8 isolated frames in 4x2 order: intact; outline loosens; tip erases; upper half fragments; lower body shrinks; short stump; sparse flecks and fading puddle; completely empty transparent cell. Monotonic loss, like a shadow drawing being erased.
""", encoding="utf-8")
    if BOSS_REFERENCE.exists():
        refs = docs / "references"
        refs.mkdir(exist_ok=True)
        shutil.copy2(BOSS_REFERENCE, refs / "boss-shadow-tendril-reference.png")


def main() -> None:
    for path in (DESIGN, ATTACK, DISSOLVE):
        if not path.exists():
            raise FileNotFoundError(path)
    if TARGET.exists():
        raise RuntimeError(f"Target already exists: {TARGET}")
    TARGET.mkdir(parents=True)
    export_design()
    export_animation(ATTACK, TARGET / "attack_8f", "boss-shadow-tendril-attack-8f", 120)
    export_animation(DISSOLVE, TARGET / "dissolve_8f", "boss-shadow-tendril-dissolve-8f", 130, empty_last=True)
    write_docs()
    (TARGET / "role.json").write_text(json.dumps({
        "owner": "chapter2_boss",
        "type": "summoned_ground_hazard",
        "attack_direction": "right",
        "pivot": "bottom-center",
        "tone": "shadow magic, non-grotesque",
    }, ensure_ascii=False, indent=2), encoding="utf-8")
    print(TARGET)


if __name__ == "__main__":
    main()
