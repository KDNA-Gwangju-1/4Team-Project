from __future__ import annotations

import csv
import hashlib
import json
import shutil
import sys
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "output"
TARGET = OUT / "chapter2-enemies-distinct-soft-v1"
GENERATED = Path(r"C:\Users\Note-038\.codex\generated_images\01a0acd2-304b-7e20-8907-152159c50899")
STYLE_REFERENCE = GENERATED / "exec-eefdea2b-ca25-429f-aaa4-5a1e74df630a.png"
NAVY = (12, 10, 35, 255)
CANVAS = (512, 512)

ASSETS = {
    "patch_rabbit": {
        "design": GENERATED / "exec-fa4228bd-ddee-4e89-8101-d63c0170e3ae.png",
        "attack": GENERATED / "exec-b68ab0bb-2af5-4552-a9bf-f909af36813a.png",
        "dissolve": GENERATED / "exec-4e0edfa8-a51d-4ccc-9ffa-6b1eb875990a.png",
        "role": "quick ground-hopping attacker",
    },
    "folded_crow": {
        "design": GENERATED / "exec-50ff6064-1f32-457b-b69b-e487e075793e.png",
        "attack": GENERATED / "exec-fb9075a8-38fe-4e60-8829-4fbcc4d74671.png",
        "dissolve": GENERATED / "exec-363a9daa-8c52-46e8-ab05-7925ec70f3b3.png",
        "role": "fast aerial swooping attacker",
    },
    "night_light": {
        "design": GENERATED / "exec-8b5ed7dc-bf32-41f6-9e3e-d442f1b066be.png",
        "attack": GENERATED / "exec-bd40bc01-9c73-4d69-b6ae-073b5b60a930.png",
        "dissolve": GENERATED / "exec-2a2c74c7-1d1a-4bef-b830-2efc2c1f6007.png",
        "role": "slow hovering ranged attacker",
    },
}


def clean_alpha(image: Image.Image, threshold: int = 64) -> Image.Image:
    rgba = image.convert("RGBA")
    alpha = rgba.getchannel("A").point(
        lambda value: 0 if value < threshold else min(255, round((value - threshold) * 255 / (255 - threshold)))
    )
    rgba.putalpha(alpha)
    return rgba


def remove_edge_specks(image: Image.Image, max_area_ratio: float = 0.12) -> Image.Image:
    rgba = image.convert("RGBA")
    alpha = rgba.getchannel("A")
    width, height = rgba.size
    pixels = alpha.load()
    visited = bytearray(width * height)
    remove: list[tuple[int, int]] = []
    max_area = int(width * height * max_area_ratio)

    for y in range(height):
        for x in range(width):
            key = y * width + x
            if visited[key] or pixels[x, y] == 0:
                continue
            stack = [(x, y)]
            visited[key] = 1
            component: list[tuple[int, int]] = []
            touches_edge = False
            while stack:
                px, py = stack.pop()
                component.append((px, py))
                if px <= 1 or py <= 1 or px >= width - 2 or py >= height - 2:
                    touches_edge = True
                for nx, ny in ((px - 1, py), (px + 1, py), (px, py - 1), (px, py + 1)):
                    if 0 <= nx < width and 0 <= ny < height:
                        nkey = ny * width + nx
                        if not visited[nkey] and pixels[nx, ny] != 0:
                            visited[nkey] = 1
                            stack.append((nx, ny))
            if touches_edge and len(component) <= max_area:
                remove.extend(component)

    if remove:
        for x, y in remove:
            pixels[x, y] = 0
        rgba.putalpha(alpha)
    return rgba


def standardize(image: Image.Image) -> Image.Image:
    source = remove_edge_specks(clean_alpha(image))
    if source.width > CANVAS[0] or source.height > CANVAS[1]:
        scale = min(CANVAS[0] / source.width, CANVAS[1] / source.height)
        source = source.resize((round(source.width * scale), round(source.height * scale)), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    canvas.alpha_composite(source, ((CANVAS[0] - source.width) // 2, CANVAS[1] - source.height))
    return canvas


def split_sheet(path: Path, empty_last: bool = False) -> list[Image.Image]:
    sheet = Image.open(path).convert("RGBA")
    xs = [round(index * sheet.width / 4) for index in range(5)]
    ys = [round(index * sheet.height / 2) for index in range(3)]
    frames = [
        standardize(sheet.crop((xs[column], ys[row], xs[column + 1], ys[row + 1])))
        for row in range(2)
        for column in range(4)
    ]
    if empty_last:
        frames[-1] = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    return frames


def opaque(image: Image.Image) -> Image.Image:
    background = Image.new("RGBA", image.size, NAVY)
    background.alpha_composite(image)
    return background.convert("RGB")


def export_animation(sheet_path: Path, destination: Path, prefix: str, duration: int, empty_last: bool = False) -> None:
    frames = split_sheet(sheet_path, empty_last=empty_last)
    for variant, opaque_mode in (("transparent", False), ("opaque_navy", True)):
        root = destination / variant
        frames_dir = root / "frames"
        frames_dir.mkdir(parents=True, exist_ok=True)
        prepared = []
        for index, frame in enumerate(frames, 1):
            output = opaque(frame) if opaque_mode else frame
            output.save(frames_dir / f"{prefix}-frame-{index:02d}.png")
            prepared.append(output)
        sheet = Image.new("RGB" if opaque_mode else "RGBA", (2048, 1024), NAVY[:3] if opaque_mode else (0, 0, 0, 0))
        for index, frame in enumerate(prepared):
            if opaque_mode:
                sheet.paste(frame, ((index % 4) * 512, (index // 4) * 512))
            else:
                sheet.alpha_composite(frame, ((index % 4) * 512, (index // 4) * 512))
        sheet.save(root / f"{prefix}-sheet.png")
        prepared[0].save(root / f"{prefix}.gif", save_all=True, append_images=prepared[1:], duration=duration, disposal=2, optimize=False)
    source = destination / "source"
    source.mkdir(parents=True, exist_ok=True)
    shutil.copy2(sheet_path, source / f"{prefix}-imagegen-original-sheet.png")
    (destination / "metadata.json").write_text(json.dumps({
        "name": prefix,
        "frames": 8,
        "canvas": {"width": 512, "height": 512},
        "duration_ms": duration,
        "pivot": "bottom-center",
        "alpha_cleanup_threshold": 64,
        "variants": ["transparent", "opaque_navy"],
    }, ensure_ascii=False, indent=2), encoding="utf-8")


def export_design_from_attack(sheet_path: Path, destination: Path, prefix: str) -> None:
    destination.mkdir(parents=True, exist_ok=True)
    design = split_sheet(sheet_path)[0]
    design.save(destination / f"{prefix}-transparent.png")
    opaque(design).save(destination / f"{prefix}-opaque-navy.png")
    shutil.copy2(sheet_path, destination / f"{prefix}-source-attack-sheet.png")


def write_prompts() -> None:
    text = """# ImageGen prompts — distinct soft enemy set

Built-in `imagegen` mode. The supplied rounded scribble image was used only as a style reference for wax-crayon texture, rough cream paper cutline, black/purple/red palette, and wistful child-memory mood. Its body and face were not reused.

## Patch Rabbit

Design: a small worn stuffed rabbit, compact cloth body, two mismatched floppy ears, short rounded paws, one cream stitched eye, purple patches, faded red ribbon. Attack 8F: neutral, crouch, squash, hop start, forward midair hop, landing, rebound, neutral. Dissolve 8F: soft edge fade, crayon and paper flecks, gentle shrink, sparse flecks, empty final frame.

## Folded Crow

Design: a small angular bird made from uneven folded-paper triangles, black crayon planes, cream cutline, purple fold lines, one cream eye, restrained red fold accent. Attack 8F: neutral hover, wings lift, press down, tilt down-right, swoop, tucked dive, flare brake, neutral. Dissolve 8F: folded tips and strokes erase into harmless paper flecks, then empty.

## Night-Light

Design: a floating child's bedside lantern shaped like a crooked house, peaked roof, cream square window, curved handle, black casing, purple panels, red switch, cream star motes. Attack 8F: hover, bob, charge glow, star grows, star launches right, recoil, settle, neutral. Dissolve 8F: window dims, stars drift, outline erases, house shrinks into star and paper flecks, then empty.

## Shared constraints

Every animation uses exactly 8 isolated frames in a strict 4x2 layout with generous transparent padding. No cell crossing. Genuine alpha background. No teeth, claws, wounds, exposed stuffing, gore, environment, labels, text, or watermark.
"""
    (TARGET / "00_docs" / "IMAGEGEN-PROMPTS-DISTINCT-SOFT-ENEMIES.md").write_text(text, encoding="utf-8")


def update_readme() -> None:
    path = TARGET / "00_docs" / "README.md"
    text = """# Chapter 2 — Distinct Soft Enemy Set v1

- `02_enemies/patch_rabbit`: 봉제 토끼형 지상 점프 몹, Attack 8F + Dissolve 8F
- `02_enemies/folded_crow`: 종이접기 새형 공중 돌진 몹, Attack 8F + Dissolve 8F
- `02_enemies/night_light`: 집 모양 야간등형 원거리 몹, Attack 8F + Dissolve 8F
- 기준 초안에서는 크레용 질감, 크림색 칼선, 색감만 반영하고 몹 형태는 전부 다르게 제작
- 각 모션은 투명/불투명 네이비, 512×512 개별 프레임, 4×2 시트, GIF를 포함
- 모든 소멸 모션의 8번째 프레임은 완전 투명
- 이후 기존 통합본에 `02_enemies` 아래로 병합하도록 구성
"""
    path.write_text(text, encoding="utf-8")


def write_inventory() -> None:
    rows = []
    inventory = TARGET / "00_docs" / "asset-inventory.csv"
    if inventory.exists():
        inventory.unlink()
    for path in sorted(item for item in TARGET.rglob("*") if item.is_file()):
        width = height = alpha_min = alpha_max = ""
        if path.suffix.lower() in {".png", ".gif"}:
            try:
                with Image.open(path) as image:
                    width, height = image.size
                    if "A" in image.getbands():
                        alpha_min, alpha_max = image.getchannel("A").getextrema()
                    else:
                        alpha_min = alpha_max = 255
            except Exception:
                pass
        rows.append({
            "path": path.relative_to(TARGET).as_posix(),
            "extension": path.suffix.lower(),
            "bytes": path.stat().st_size,
            "width": width,
            "height": height,
            "alpha_min": alpha_min,
            "alpha_max": alpha_max,
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        })
    with inventory.open("w", encoding="utf-8-sig", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=rows[0].keys())
        writer.writeheader()
        writer.writerows(rows)


def main() -> None:
    required = [STYLE_REFERENCE]
    for asset in ASSETS.values():
        required.extend([asset["design"], asset["attack"], asset["dissolve"]])
    for path in required:
        if not path.exists():
            raise FileNotFoundError(path)
    refresh = "--refresh" in sys.argv
    if TARGET.exists() and not refresh:
        raise RuntimeError(f"Target already exists: {TARGET}; use --refresh")

    (TARGET / "00_docs").mkdir(parents=True, exist_ok=True)
    (TARGET / "02_enemies").mkdir(parents=True, exist_ok=True)

    refs = TARGET / "00_docs" / "references"
    refs.mkdir(parents=True, exist_ok=True)
    shutil.copy2(STYLE_REFERENCE, refs / "enemy-style-reference.png")

    for name, asset in ASSETS.items():
        prefix = name.replace("_", "-")
        enemy = TARGET / "02_enemies" / name
        export_design_from_attack(asset["attack"], enemy / "design", prefix)
        export_animation(asset["attack"], enemy / "attack_8f", f"{prefix}-attack-8f", 110)
        export_animation(asset["dissolve"], enemy / "dissolve_8f", f"{prefix}-dissolve-8f", 130, empty_last=True)
        (enemy / "role.json").write_text(json.dumps({
            "role": asset["role"],
            "tone": "wistful, childlike, non-grotesque",
        }, ensure_ascii=False, indent=2), encoding="utf-8")

    write_prompts()
    update_readme()
    write_inventory()
    print(TARGET)


if __name__ == "__main__":
    main()
