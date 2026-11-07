#!/usr/bin/env python3
"""
Конвертирует состояния BYOND DMI в RSI (Robust Toolbox).

Использование:
  python Tools/_Stories/dmi_to_rsi.py <file.dmi> <out.rsi> "<copyright>" <dmi_state>[=<rsi_state>] ...

Если RSI уже существует, состояния добавляются или заменяются (размер кадра должен совпадать).
Требуется Pillow.
"""
import json
import math
import re
import sys
from pathlib import Path

from PIL import Image


def parse_description(desc):
    width = int(re.search(r"width = (\d+)", desc).group(1))
    height = int(re.search(r"height = (\d+)", desc).group(1))
    states = []
    current = None
    for raw in desc.splitlines():
        line = raw.strip()
        match = re.match(r'state = "(.*)"', line)
        if match:
            current = {"name": match.group(1), "dirs": 1, "frames": 1, "delay": None}
            states.append(current)
            continue
        if current is None:
            continue
        match = re.match(r"(dirs|frames) = (\d+)", line)
        if match:
            current[match.group(1)] = int(match.group(2))
            continue
        match = re.match(r"delay = (.*)", line)
        if match:
            current["delay"] = [float(x) for x in match.group(1).split(",")]
    return width, height, states


def main():
    if len(sys.argv) < 5:
        sys.exit(__doc__)

    dmi_path, out_path, copyright_text, *specs = sys.argv[1:]
    sheet = Image.open(dmi_path)
    width, height, states = parse_description(sheet.info["Description"])
    sheet = sheet.convert("RGBA")
    sheet_columns = sheet.width // width

    offsets = {}
    index = 0
    for state in states:
        offsets.setdefault(state["name"], (index, state))
        index += state["dirs"] * state["frames"]

    out_dir = Path(out_path)
    out_dir.mkdir(parents=True, exist_ok=True)
    meta_path = out_dir / "meta.json"
    if meta_path.exists():
        meta = json.loads(meta_path.read_text(encoding="utf-8"))
        if meta["size"] != {"x": width, "y": height}:
            sys.exit(f"{out_path}: size {meta['size']} != {width}x{height}")
    else:
        meta = {"version": 1, "license": "CC-BY-SA-3.0", "copyright": copyright_text,
                "size": {"x": width, "y": height}, "states": []}

    for spec in specs:
        source, _, target = spec.partition("=")
        target = target or source
        if source not in offsets:
            sys.exit(f"{dmi_path}: no state '{source}'")

        start, state = offsets[source]
        dirs, frames = state["dirs"], state["frames"]
        total = dirs * frames
        columns = math.ceil(math.sqrt(total))
        rows = math.ceil(total / columns)
        image = Image.new("RGBA", (columns * width, rows * height))

        for direction in range(dirs):
            for frame in range(frames):
                source_index = start + frame * dirs + direction
                sx, sy = (source_index % sheet_columns) * width, (source_index // sheet_columns) * height
                tile = sheet.crop((sx, sy, sx + width, sy + height))
                target_index = direction * frames + frame
                image.paste(tile, ((target_index % columns) * width, (target_index // columns) * height))

        image.save(out_dir / f"{target}.png")

        entry = {"name": target}
        if dirs != 1:
            entry["directions"] = dirs
        if frames > 1:
            delay = state["delay"] or [1.0] * frames
            entry["delays"] = [[round(d / 10, 3) for d in delay] for _ in range(dirs)]

        meta["states"] = [s for s in meta["states"] if s["name"] != target] + [entry]

    meta_path.write_text(json.dumps(meta, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
