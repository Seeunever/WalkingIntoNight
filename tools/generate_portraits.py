#!/usr/bin/env python3
"""Generate 500 high-detail 128px pixel-art portraits (Stardew 64px+ fidelity)."""

import json
import random
import shutil
from pathlib import Path

from pixel_portrait_engine import PortraitParams, build_name, render_portrait

OUT_DIR = Path(__file__).resolve().parents[1] / "Assets" / "Resources" / "Portraits" / "Cthulhu1920s"
COUNT = 500
SEED = 19260315

PROFESSIONS = [
    "detective", "librarian", "professor", "nurse", "journalist",
    "occultist", "smuggler", "fisherman", "priest", "mechanic",
    "accountant", "socialite", "artist", "bootlegger", "veteran",
    "secretary", "doctor", "shopkeeper", "farmer", "musician",
    "photographer", "antique_dealer", "dockworker", "student", "police_officer",
]

PERSONALITIES = ["stoic", "nervous", "cheerful", "grim", "curious"]
GENDERS = ["male", "female"]
AGES = ["young", "adult", "middle_aged", "senior", "elderly"]
ETHNICITIES = ["white", "black", "latino", "asian", "native"]


def all_combos():
    combos = []
    for prof in PROFESSIONS:
        for pers in PERSONALITIES:
            for gender in GENDERS:
                for age in AGES:
                    for eth in ETHNICITIES:
                        combos.append((prof, pers, gender, age, eth))
    return combos


def main():
    combos = all_combos()
    rng = random.Random(SEED)
    rng.shuffle(combos)
    selected = combos[:COUNT]

    if OUT_DIR.exists():
        for p in OUT_DIR.glob("*.png"):
            p.unlink()

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    manifest = []

    for i, (prof, pers, gender, age, eth) in enumerate(selected):
        params = PortraitParams(prof, pers, gender, age, eth, SEED + i * 9973)
        name = build_name(params)
        img = render_portrait(params)
        path = OUT_DIR / f"{name}.png"
        if path.exists():
            name = f"{name}-{i:03d}"
            path = OUT_DIR / f"{name}.png"
        img.save(path, "PNG", optimize=True)
        manifest.append({
            "id": name,
            "file": f"Portraits/Cthulhu1920s/{name}",
            "profession": prof,
            "personality": pers,
            "gender": gender,
            "age": age,
            "ethnicity": eth,
        })
        if (i + 1) % 50 == 0:
            print(f"Generated {i + 1}/{COUNT}")

    with open(OUT_DIR / "portraits_manifest.json", "w", encoding="utf-8") as f:
        json.dump({"count": len(manifest), "format": "128px pixel art @ 256 output", "portraits": manifest}, f, indent=2)

    total_kb = sum(p.stat().st_size for p in OUT_DIR.glob("*.png")) / 1024
    print(f"Done: {len(manifest)} portraits -> {OUT_DIR}")
    print(f"Total: {total_kb:.1f} KB ({total_kb/1024:.2f} MB)")


if __name__ == "__main__":
    main()
