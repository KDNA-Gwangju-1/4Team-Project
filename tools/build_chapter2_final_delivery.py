from __future__ import annotations

import csv
import hashlib
import json
import shutil
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "output"
TARGET = OUT / "chapter2-final-delivery-v1"
INTEGRATION = OUT / "chapter2-final-integration-v1"
SEPARATED = OUT / "chapter2-game-assets-separated-v3"

ATTACK_SHEET = Path(
    r"C:\Users\Note-038\.codex\generated_images\01a0acd2-304b-7e20-8907-152159c50899\exec-9b9c1a55-e09f-412c-a6f6-5958f609d486.png"
)
DISSOLVE_SHEET = Path(
    r"C:\Users\Note-038\.codex\generated_images\01a0acd2-304b-7e20-8907-152159c50899\exec-2fd580f4-ef96-4f71-8a19-08e3bbb6b565.png"
)

NAVY = (12, 10, 35, 255)
CANVAS = (512, 512)


def ensure_clean_target() -> None:
    if TARGET.exists():
        raise RuntimeError(f"Target already exists: {TARGET}")
    TARGET.mkdir(parents=True)


def copy_files(paths: list[Path], destination: Path) -> list[Path]:
    destination.mkdir(parents=True, exist_ok=True)
    copied: list[Path] = []
    for path in paths:
        result = destination / path.name
        shutil.copy2(path, result)
        copied.append(result)
    return copied


def standardize(image: Image.Image, size: tuple[int, int] = CANVAS) -> Image.Image:
    source = image.convert("RGBA")
    max_w, max_h = size
    if source.width > max_w or source.height > max_h:
        scale = min(max_w / source.width, max_h / source.height)
        source = source.resize(
            (max(1, round(source.width * scale)), max(1, round(source.height * scale))),
            Image.Resampling.LANCZOS,
        )
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    x = (max_w - source.width) // 2
    y = max_h - source.height
    canvas.alpha_composite(source, (x, y))
    return canvas


def opaque(image: Image.Image) -> Image.Image:
    base = Image.new("RGBA", image.size, NAVY)
    base.alpha_composite(image.convert("RGBA"))
    return base.convert("RGB")


def save_sheet(frames: list[Image.Image], destination: Path, columns: int) -> None:
    rows = (len(frames) + columns - 1) // columns
    width, height = frames[0].size
    sheet = Image.new("RGBA", (columns * width, rows * height), (0, 0, 0, 0))
    for index, frame in enumerate(frames):
        sheet.alpha_composite(frame.convert("RGBA"), ((index % columns) * width, (index // columns) * height))
    destination.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(destination)


def save_opaque_sheet(frames: list[Image.Image], destination: Path, columns: int) -> None:
    rows = (len(frames) + columns - 1) // columns
    width, height = frames[0].size
    sheet = Image.new("RGB", (columns * width, rows * height), NAVY[:3])
    for index, frame in enumerate(frames):
        sheet.paste(opaque(frame), ((index % columns) * width, (index // columns) * height))
    destination.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(destination)


def save_gif(frames: list[Image.Image], destination: Path, duration: int, loop: bool, opaque_mode: bool) -> None:
    destination.parent.mkdir(parents=True, exist_ok=True)
    prepared = [opaque(frame) if opaque_mode else frame.convert("RGBA") for frame in frames]
    kwargs = dict(save_all=True, append_images=prepared[1:], duration=duration, disposal=2, optimize=False)
    if loop:
        kwargs["loop"] = 0
    prepared[0].save(destination, **kwargs)


def export_animation(
    source_paths: list[Path],
    destination: Path,
    prefix: str,
    duration: int,
    loop: bool,
    columns: int = 4,
) -> list[Image.Image]:
    frames = [standardize(Image.open(path)) for path in source_paths]
    transparent_dir = destination / "transparent"
    opaque_dir = destination / "opaque_navy"
    (transparent_dir / "frames").mkdir(parents=True, exist_ok=True)
    (opaque_dir / "frames").mkdir(parents=True, exist_ok=True)

    for index, frame in enumerate(frames, 1):
        filename = f"{prefix}-frame-{index:02d}.png"
        frame.save(transparent_dir / "frames" / filename)
        opaque(frame).save(opaque_dir / "frames" / filename)

    save_sheet(frames, transparent_dir / f"{prefix}-sheet.png", columns)
    save_opaque_sheet(frames, opaque_dir / f"{prefix}-sheet.png", columns)
    save_gif(frames, transparent_dir / f"{prefix}.gif", duration, loop, False)
    save_gif(frames, opaque_dir / f"{prefix}.gif", duration, loop, True)

    metadata = {
        "name": prefix,
        "frames": len(frames),
        "canvas": {"width": CANVAS[0], "height": CANVAS[1]},
        "duration_ms": duration,
        "loop": loop,
        "pivot": "bottom-center",
        "variants": ["transparent", "opaque_navy"],
    }
    (destination / "metadata.json").write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8")
    return frames


def split_generated_sheet(path: Path) -> list[Image.Image]:
    sheet = Image.open(path).convert("RGBA")
    x_bounds = [round(index * sheet.width / 4) for index in range(5)]
    y_bounds = [round(index * sheet.height / 2) for index in range(3)]
    frames: list[Image.Image] = []
    for row in range(2):
        for column in range(4):
            crop = sheet.crop((x_bounds[column], y_bounds[row], x_bounds[column + 1], y_bounds[row + 1]))
            frames.append(standardize(crop))
    return frames


def export_generated_animation(
    sheet_path: Path,
    destination: Path,
    prefix: str,
    duration: int,
    loop: bool,
) -> None:
    source_dir = destination / "source"
    source_dir.mkdir(parents=True, exist_ok=True)
    shutil.copy2(sheet_path, source_dir / f"{prefix}-imagegen-original-sheet.png")
    frames = split_generated_sheet(sheet_path)
    transparent_dir = destination / "transparent"
    opaque_dir = destination / "opaque_navy"
    (transparent_dir / "frames").mkdir(parents=True, exist_ok=True)
    (opaque_dir / "frames").mkdir(parents=True, exist_ok=True)
    for index, frame in enumerate(frames, 1):
        filename = f"{prefix}-frame-{index:02d}.png"
        frame.save(transparent_dir / "frames" / filename)
        opaque(frame).save(opaque_dir / "frames" / filename)
    save_sheet(frames, transparent_dir / f"{prefix}-sheet.png", 4)
    save_opaque_sheet(frames, opaque_dir / f"{prefix}-sheet.png", 4)
    save_gif(frames, transparent_dir / f"{prefix}.gif", duration, loop, False)
    save_gif(frames, opaque_dir / f"{prefix}.gif", duration, loop, True)
    metadata = {
        "name": prefix,
        "frames": 8,
        "canvas": {"width": 512, "height": 512},
        "duration_ms": duration,
        "loop": loop,
        "pivot": "bottom-center",
        "variants": ["transparent", "opaque_navy"],
        "source": "built-in imagegen sprite sheet",
    }
    (destination / "metadata.json").write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8")


def export_player() -> None:
    source = SEPARATED / "01_final" / "player"
    player_root = TARGET / "01_player"
    animations = [
        ("idle", sorted((source / "idle").glob("*.png")), "player-idle", 160, True),
        ("jump", sorted((source / "jump").glob("*.png")), "player-jump", 120, False),
        (
            "walk",
            sorted((OUT / "chapter2-art" / "player-concepts" / "animation-v7-walk-arm-arc" / "walk").glob("walk-frame-*.png")),
            "player-walk-v7",
            110,
            True,
        ),
        (
            "walk_flashlight_outlined",
            sorted((OUT / "chapter2-art" / "player-concepts" / "animation-v9-walk-flashlight-left-locked" / "walk").glob("*-export*.png")),
            "player-walk-flashlight-outlined-v9",
            110,
            True,
        ),
    ]
    for folder, paths, prefix, duration, loop in animations:
        export_animation(paths, player_root / folder, prefix, duration, loop)
    copy_files(sorted((source / "concept").glob("*.png")), player_root / "concept")


def export_enemies() -> None:
    enemy_root = TARGET / "02_enemies" / "core_scribble_enemy"
    copy_files(sorted((SEPARATED / "01_final" / "enemies").glob("*.png")), enemy_root / "design_states")
    export_generated_animation(ATTACK_SHEET, enemy_root / "attack_8f", "core-enemy-attack-8f", 90, False)
    export_generated_animation(DISSOLVE_SHEET, enemy_root / "dissolve_8f", "core-enemy-dissolve-8f", 120, False)


def export_boss() -> None:
    boss_root = TARGET / "03_boss" / "dissolve_20f"
    source_root = SEPARATED / "05_custom_export20"
    paths = sorted((source_root / "frames-transparent").glob("*.png"))
    export_animation(paths, boss_root, "boss-dissolve-20f", 200, False, columns=5)
    copy_files(
        [
            source_root / "boss-dissolve-custom-20f-transparent-sheet.png",
            source_root / "boss-dissolve-custom-20f-transparent-200ms.gif",
            source_root / "late-frames-checker-preview.png",
        ],
        boss_root / "source",
    )
    copy_files(sorted((SEPARATED / "01_final" / "boss_concepts").glob("*.png")), TARGET / "03_boss" / "concepts")


def export_backgrounds_and_supporting() -> None:
    shutil.copytree(INTEGRATION / "02_backgrounds", TARGET / "04_backgrounds")
    copy_files(sorted((SEPARATED / "01_final" / "memory_frames").glob("*.png")), TARGET / "05_supporting_assets" / "memory_frames")
    copy_files(sorted((SEPARATED / "01_final" / "boss_concepts").glob("*.png")), TARGET / "05_supporting_assets" / "boss_concepts")
    copy_files(sorted((SEPARATED / "01_final" / "player" / "concept").glob("*.png")), TARGET / "05_supporting_assets" / "player_concepts")
    shutil.copytree(INTEGRATION / "04_supporting" / "scene_mockups_reference", TARGET / "05_supporting_assets" / "scene_mockups_reference")


def export_source_archive() -> None:
    shutil.copytree(SEPARATED, TARGET / "99_source_archive" / "chapter2-game-assets-separated-v3")
    shutil.copy2(INTEGRATION / "README.md", TARGET / "99_source_archive" / "previous-integration-README.md")
    shutil.copy2(INTEGRATION / "integration-manifest.csv", TARGET / "99_source_archive" / "previous-integration-manifest.csv")


def alpha_range(path: Path) -> tuple[int, int]:
    try:
        with Image.open(path) as image:
            if "A" not in image.getbands():
                return 255, 255
            alpha = image.getchannel("A")
            return alpha.getextrema()
    except Exception:
        return -1, -1


def write_inventory() -> None:
    rows = []
    for path in sorted(item for item in TARGET.rglob("*") if item.is_file()):
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        width = height = ""
        alpha_min = alpha_max = ""
        if path.suffix.lower() in {".png", ".gif"}:
            try:
                with Image.open(path) as image:
                    width, height = image.size
                alpha_min, alpha_max = alpha_range(path)
            except Exception:
                pass
        rows.append(
            {
                "path": path.relative_to(TARGET).as_posix(),
                "extension": path.suffix.lower(),
                "bytes": path.stat().st_size,
                "width": width,
                "height": height,
                "alpha_min": alpha_min,
                "alpha_max": alpha_max,
                "sha256": digest,
            }
        )
    docs = TARGET / "00_docs"
    docs.mkdir(exist_ok=True)
    with (docs / "asset-inventory.csv").open("w", encoding="utf-8-sig", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=rows[0].keys())
        writer.writeheader()
        writer.writerows(rows)


def write_docs() -> None:
    docs = TARGET / "00_docs"
    docs.mkdir(exist_ok=True)
    readme = """# Chapter 2 Final Delivery v1

챕터 2의 플레이어, 핵심 일반 몹, 보스, 배경, 기억 이미지와 전체 원본 보관본을 한 번에 전달하는 최종 패키지입니다.

## 실행용 핵심 에셋

- `01_player`: Idle 6F, Jump 6F, Walk v7 8F, 칼선 손전등 Walk v9 8F
- `02_enemies/core_scribble_enemy`: 확정 디자인 상태, Attack 8F, Dissolve 8F
- `03_boss`: 수정된 Boss Dissolve 20F와 보스 콘셉트
- `04_backgrounds`: 맵·충돌 지형이 없는 원경 배경 5종
- `05_supporting_assets`: 액자·기억, 콘셉트, 참고 목업
- `99_source_archive`: 분리 전후의 챕터 2 원본 자산 전체 보관본

## 투명·불투명 규칙

애니메이션 폴더마다 다음 두 변형을 함께 제공합니다.

- `transparent`: 실제 알파 채널이 있는 PNG 프레임, GIF, 스프라이트 시트
- `opaque_navy`: RGB 짙은 남색 배경으로 합성한 PNG 프레임, GIF, 스프라이트 시트

배경 5종은 원래부터 불투명 RGB 이미지입니다. `00_docs/asset-inventory.csv`의 `alpha_min`, `alpha_max`로 전체 파일의 알파 상태를 확인할 수 있습니다.

## 재생 권장값

| Animation | Frames | Duration | Loop |
|---|---:|---:|---|
| Player Idle | 6 | 160 ms | Yes |
| Player Jump | 6 | 120 ms | No |
| Player Walk v7 | 8 | 110 ms | Yes |
| Player Walk Flashlight v9 | 8 | 110 ms | Yes |
| Core Enemy Attack | 8 | 90 ms | No |
| Core Enemy Dissolve | 8 | 120 ms | No |
| Boss Dissolve | 20 | 200 ms | No |

모든 실행용 프레임은 512×512, bottom-center 피벗 기준입니다. 손전등 걷기는 칼선 버전만 포함했습니다.
"""
    (docs / "README.md").write_text(readme, encoding="utf-8")

    generation = {
        "tool": "built-in imagegen",
        "skill": r"C:\Users\Note-038\.codex\skills\.system\imagegen\SKILL.md",
        "enemy_attack_sheet": ATTACK_SHEET.name,
        "enemy_dissolve_sheet": DISSOLVE_SHEET.name,
        "notes": "Exact final prompts are stored in 00_docs/IMAGEGEN-PROMPTS.md",
    }
    (docs / "generation-metadata.json").write_text(json.dumps(generation, ensure_ascii=False, indent=2), encoding="utf-8")


def main() -> None:
    for required in [INTEGRATION, SEPARATED, ATTACK_SHEET, DISSOLVE_SHEET]:
        if not required.exists():
            raise FileNotFoundError(required)
    ensure_clean_target()
    export_player()
    export_enemies()
    export_boss()
    export_backgrounds_and_supporting()
    export_source_archive()
    write_docs()
    write_inventory()
    print(TARGET)


if __name__ == "__main__":
    main()
