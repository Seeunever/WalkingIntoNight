#!/usr/bin/env python3
"""
Batch-generate 500 high-quality 1920s pulp / CoC-style portrait sprites via Stable Diffusion.

Requirements (GPU strongly recommended):
  py -m pip install torch torchvision --index-url https://download.pytorch.org/whl/cu124
  py -m pip install diffusers transformers accelerate safetensors pillow

Usage:
  py tools/generate_portraits_ai.py              # full 500 (needs ~8GB VRAM)
  py tools/generate_portraits_ai.py --count 10   # smoke test
  py tools/generate_portraits_ai.py --device cpu --count 2  # slow fallback

Output: Assets/Resources/Portraits/Cthulhu1920s/*.png (320x320, game-ready)
"""

from __future__ import annotations

import argparse
import json
import random
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets" / "Resources" / "Portraits" / "Cthulhu1920s"
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
AGES = ["young adult", "adult", "middle-aged", "senior", "elderly"]
ETHNICITIES = ["Caucasian", "African American", "Latino", "East Asian", "Native American"]

PROF_VISUAL = {
    "detective": "fedora hat, trench coat collar",
    "librarian": "round glasses, cardigan",
    "professor": "tweed jacket, bow tie",
    "nurse": "nurse cap, white collar",
    "journalist": "press badge, suspenders",
    "occultist": "amulet pendant, dark hooded shawl",
    "smuggler": "leather jacket, scar",
    "fisherman": "weathered cap, wool sweater",
    "priest": "clerical collar, black shirt",
    "mechanic": "oil-stained coveralls collar",
    "accountant": "visored cap, vest and tie",
    "socialite": "pearl necklace, fur collar",
    "artist": "beret, paint smudge",
    "bootlegger": "pinstripe suit, slick hair",
    "veteran": "military cap, medal hint",
    "secretary": "hair bun, blouse with brooch",
    "doctor": "stethoscope, white coat collar",
    "shopkeeper": "apron collar, name tag",
    "farmer": "straw hat, plaid shirt",
    "musician": "trumpet mute pin, elegant shirt",
    "photographer": "vintage camera strap, vest",
    "antique_dealer": "monocle, velvet jacket",
    "dockworker": "cap, rough work shirt",
    "student": "letterman style collar, youthful",
    "police_officer": "police cap, uniform collar",
}

PERS_EXPRESSION = {
    "stoic": "neutral stern expression",
    "nervous": "anxious wide eyes, tense mouth",
    "cheerful": "warm slight smile",
    "grim": "somber frown, tired eyes",
    "curious": "raised eyebrow, intent gaze",
}

ETHNICITY_AGE_GENDER = {
    # maps to combo key suffix for naming (english slug)
}


def all_combos():
    combos = []
    for prof in PROFESSIONS:
        for pers in PERSONALITIES:
            for gender in GENDERS:
                for age in AGES:
                    for eth in ETHNICITIES:
                        combos.append((prof, pers, gender, age, eth))
    return combos


def slug_age(age: str) -> str:
    return age.replace("-", "_").replace(" ", "_").lower()


def slug_eth(eth: str) -> str:
    return {
        "Caucasian": "white",
        "African American": "black",
        "Latino": "latino",
        "East Asian": "asian",
        "Native American": "native",
    }[eth]


def build_name(prof, pers, gender, age, eth) -> str:
    age_slug = {"young adult": "young", "adult": "adult", "middle-aged": "middle_aged", "senior": "senior", "elderly": "elderly"}[age]
    return f"{prof}-{pers}-{gender}-{age_slug}-{slug_eth(eth)}"


def build_prompt(prof, pers, gender, age, eth) -> str:
    gender_word = "woman" if gender == "female" else "man"
    return (
        f"Detailed pixel art game portrait bust, 1920s American {eth} {age} {gender_word} {prof}, "
        f"{PROF_VISUAL.get(prof, '')}, {PERS_EXPRESSION.get(pers, '')}, "
        "Call of Cthulhu pulp horror mood, Lovecraftian subtle unease, muted palette, "
        "Stardew Valley portrait quality but sharper with richer shading, 5-level cel shading, "
        "crisp pixels, shoulders visible, facing camera, dark teal vignette background, "
        "visual novel character sprite, no text, no watermark, masterpiece game asset"
    )


NEGATIVE = (
    "blurry, soft, photorealistic, 3d render, anime, chibi, low detail, flat colors, "
    "extra fingers, deformed, watermark, text, logo, frame, multiple people, full body"
)


def load_pipeline(device: str):
    try:
        import torch
        from diffusers import StableDiffusionPipeline
    except ImportError:
        print("Missing deps. Install:\n  py -m pip install torch diffusers transformers accelerate safetensors pillow")
        sys.exit(1)

    dtype = torch.float16 if device == "cuda" else torch.float32
    model_id = "Lykon/DreamShaper"  # good for stylized portraits; ~2GB

    print(f"Loading {model_id} on {device}...")
    pipe = StableDiffusionPipeline.from_pretrained(
        model_id,
        torch_dtype=dtype,
        safety_checker=None,
    )
    if device == "cuda":
        pipe = pipe.to("cuda")
        try:
            pipe.enable_attention_slicing()
        except Exception:
            pass
    return pipe


def save_resized(img, path: Path, size: int = 320):
    from PIL import Image
    img = img.convert("RGBA")
    # center crop square
    w, h = img.size
    side = min(w, h)
    left = (w - side) // 2
    top = (h - side) // 2
    img = img.crop((left, top, left + side, top + side))
    img = img.resize((size, size), Image.LANCZOS)
    # slight posterize for pixel-crisp feel
    img = img.quantize(colors=64, method=2).convert("RGBA")
    img = img.resize((size, size), Image.NEAREST)
    img.save(path, "PNG", optimize=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--count", type=int, default=500)
    parser.add_argument("--device", default="auto", choices=["auto", "cuda", "cpu"])
    parser.add_argument("--steps", type=int, default=28)
    parser.add_argument("--start", type=int, default=0, help="Resume offset into shuffled combo list")
    args = parser.parse_args()

    device = args.device
    if device == "auto":
        try:
            import torch
            device = "cuda" if torch.cuda.is_available() else "cpu"
        except ImportError:
            device = "cpu"
    print(f"Device: {device}")

    combos = all_combos()
    rng = random.Random(SEED)
    rng.shuffle(combos)
    selected = combos[args.start : args.start + args.count]

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    pipe = load_pipeline(device)

    manifest = []
    if (OUT_DIR / "portraits_manifest.json").exists():
        with open(OUT_DIR / "portraits_manifest.json", encoding="utf-8") as f:
            manifest = json.load(f).get("portraits", [])

    for i, combo in enumerate(selected):
        prof, pers, gender, age, eth = combo
        name = build_name(prof, pers, gender, age, eth)
        out_path = OUT_DIR / f"{name}.png"
        if out_path.exists():
            print(f"Skip existing {name}")
            continue

        prompt = build_prompt(prof, pers, gender, age, eth)
        seed = SEED + (args.start + i) * 9973
        print(f"[{i+1}/{len(selected)}] {name}")

        result = pipe(
            prompt=prompt,
            negative_prompt=NEGATIVE,
            num_inference_steps=args.steps if device == "cuda" else min(args.steps, 12),
            guidance_scale=7.5,
            generator=__import__("torch").Generator(device=device).manual_seed(seed),
            width=384,
            height=384,
        )
        save_resized(result.images[0], out_path)

        entry = {
            "id": name,
            "file": f"Portraits/Cthulhu1920s/{name}",
            "profession": prof,
            "personality": pers,
            "gender": gender,
            "age": slug_age(age),
            "ethnicity": slug_eth(eth),
        }
        manifest = [m for m in manifest if m.get("id") != name] + [entry]

        if (i + 1) % 10 == 0:
            with open(OUT_DIR / "portraits_manifest.json", "w", encoding="utf-8") as f:
                json.dump({"count": len(manifest), "generator": "stable-diffusion", "portraits": manifest}, f, indent=2)

    with open(OUT_DIR / "portraits_manifest.json", "w", encoding="utf-8") as f:
        json.dump({"count": len(manifest), "generator": "stable-diffusion", "portraits": manifest}, f, indent=2)

    print(f"Done. Output: {OUT_DIR}")


if __name__ == "__main__":
    main()
