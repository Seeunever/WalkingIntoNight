#!/usr/bin/env python3
"""
High-detail 128x128 pixel-art portrait engine (Stardew 64px+ fidelity).
Renders 1920s American pulp / subtle Lovecraft mood bust portraits.
Output: 256x256 PNG (2x integer upscale, crisp pixels).
"""

from __future__ import annotations

import random
from dataclasses import dataclass
from typing import Callable, List, Optional, Sequence, Tuple

import numpy as np
from PIL import Image

RGB = Tuple[int, int, int]
Pixel = Tuple[int, int, RGB]

# 160px canvas = 2.5x Stardew base (64px); output 320px via 2x nearest upscale
CANVAS_SIZE = 160
OUTPUT_SCALE = 2


def clamp(v: int) -> int:
    return max(0, min(255, v))


def shade(c: RGB, factor: float) -> RGB:
    return (clamp(int(c[0] * factor)), clamp(int(c[1] * factor)), clamp(int(c[2] * factor)))


def lerp_color(a: RGB, b: RGB, t: float) -> RGB:
    return (
        clamp(int(a[0] + (b[0] - a[0]) * t)),
        clamp(int(a[1] + (b[1] - a[1]) * t)),
        clamp(int(a[2] + (b[2] - a[2]) * t)),
    )


def ramp(base: RGB, shadows: float = 0.55, mid: float = 0.82, hi: float = 1.08) -> List[RGB]:
    return [
        shade(base, shadows * 0.75),
        shade(base, shadows),
        base,
        shade(base, mid),
        shade(base, hi),
        shade(base, hi * 1.06),
    ]


SKIN_TONES = {
    "white": [(228, 198, 178), (218, 188, 168), (238, 208, 188)],
    "black": [(98, 68, 52), (118, 82, 62), (88, 58, 44)],
    "latino": [(194, 148, 112), (184, 138, 104), (204, 158, 120)],
    "asian": [(232, 202, 178), (222, 192, 168), (242, 212, 188)],
    "native": [(182, 136, 100), (172, 126, 92), (192, 146, 108)],
}

HAIR_BASE = [
    (34, 26, 22), (52, 38, 28), (78, 56, 36), (116, 82, 48),
    (152, 112, 68), (188, 158, 108), (72, 72, 78), (128, 128, 136),
    (168, 72, 48), (48, 36, 32), (96, 68, 44), (196, 176, 140),
]

EYE_BASE = [
    (52, 88, 68), (72, 54, 38), (48, 68, 108), (88, 108, 88),
    (36, 38, 44), (96, 76, 56), (64, 92, 112), (58, 78, 58),
]

CLOTH_BASE = [
    (42, 48, 58), (56, 50, 44), (36, 42, 48), (62, 56, 50),
    (48, 58, 52), (68, 42, 36), (34, 38, 44), (58, 64, 72),
    (44, 52, 62), (72, 64, 54), (30, 34, 38), (78, 72, 62),
]

ACCENT = [(128, 108, 72), (96, 118, 96), (148, 64, 54), (88, 98, 128), (108, 92, 66)]


@dataclass
class PortraitParams:
    profession: str
    personality: str
    gender: str
    age: str
    ethnicity: str
    seed: int


class PixelCanvas:
    """160x160 RGBA pixel buffer with layer compositing."""

    def __init__(self, size: int = CANVAS_SIZE, bg: RGB = (28, 34, 44)):
        self.size = size
        self.rgb = np.full((size, size, 3), bg, dtype=np.uint8)
        self.a = np.full((size, size), 255, dtype=np.uint8)
        self.bg = bg

    def set(self, x: int, y: int, color: RGB, alpha: int = 255) -> None:
        x, y = int(x), int(y)
        if 0 <= x < self.size and 0 <= y < self.size:
            if alpha >= 255:
                self.rgb[y, x] = color
                self.a[y, x] = 255
            elif alpha > 0:
                t = alpha / 255.0
                cur = self.rgb[y, x].astype(float)
                self.rgb[y, x] = (cur * (1 - t) + np.array(color) * t).astype(np.uint8)
                self.a[y, x] = max(self.a[y, x], alpha)

    def fill_circle(self, cx: float, cy: float, r: float, color: RGB) -> None:
        s = self.size
        ys, xs = np.ogrid[:s, :s]
        mask = (xs - cx) ** 2 + (ys - cy) ** 2 <= r * r
        self.rgb[mask] = color

    def fill_ellipse(self, cx: float, cy: float, rx: float, ry: float, color: RGB) -> None:
        s = self.size
        ys, xs = np.ogrid[:s, :s]
        mask = ((xs - cx) / max(rx, 0.1)) ** 2 + ((ys - cy) / max(ry, 0.1)) ** 2 <= 1.0
        self.rgb[mask] = color
        self.a[mask] = 255

    def shaded_ellipse(
        self,
        cx: float,
        cy: float,
        rx: float,
        ry: float,
        palette: Sequence[RGB],
        light: Tuple[float, float] = (-0.35, -0.45),
    ) -> None:
        s = self.size
        for y in range(s):
            for x in range(s):
                nx = (x - cx) / max(rx, 0.1)
                ny = (y - cy) / max(ry, 0.1)
                d = nx * nx + ny * ny
                if d > 1.0:
                    continue
                depth = 1.0 - d
                lx = nx * light[0] + ny * light[1]
                t = 0.35 + depth * 0.35 + lx * 0.35
                t = max(0.0, min(1.0, t))
                idx = int(t * (len(palette) - 1))
                self.set(x, y, palette[idx])

    def draw_line(self, x0: int, y0: int, x1: int, y1: int, color: RGB, w: int = 1) -> None:
        dx, dy = abs(x1 - x0), abs(y1 - y0)
        sx = 1 if x0 < x1 else -1
        sy = 1 if y0 < y1 else -1
        err = dx - dy
        x, y = x0, y0
        while True:
            for ox in range(-w // 2, w // 2 + 1):
                for oy in range(-w // 2, w // 2 + 1):
                    self.set(x + ox, y + oy, color)
            if x == x1 and y == y1:
                break
            e2 = 2 * err
            if e2 > -dy:
                err -= dy
                x += sx
            if e2 < dx:
                err += dx
                y += sy

    def outline_ellipse(self, cx: float, cy: float, rx: float, ry: float, color: RGB, thickness: int = 1) -> None:
        s = self.size
        for y in range(s):
            for x in range(s):
                nx = (x - cx) / max(rx, 0.1)
                ny = (y - cy) / max(ry, 0.1)
                d = nx * nx + ny * ny
                if 0.92 <= d <= 1.05:
                    for t in range(thickness):
                        self.set(x, y, color)

    def blit_sprite(self, pixels: Sequence[Pixel], ox: int = 0, oy: int = 0) -> None:
        for x, y, c in pixels:
            self.set(x + ox, y + oy, c)

    def to_image(self, scale: int = OUTPUT_SCALE) -> Image.Image:
        self._apply_character_outline()
        rgba = np.dstack([self.rgb, self.a])
        img = Image.fromarray(rgba, "RGBA")
        if scale != 1:
            img = img.resize((self.size * scale, self.size * scale), Image.NEAREST)
        return img

    def _apply_character_outline(self) -> None:
        """1px dark outline around silhouette (Stardew-style readability)."""
        s = self.size
        bg = np.array(self.bg)
        outline = (18, 16, 22)
        copy = self.rgb.copy()
        for y in range(1, s - 1):
            for x in range(1, s - 1):
                if np.array_equal(copy[y, x], bg):
                    continue
                neighbors = [copy[y + dy, x + dx] for dy in (-1, 0, 1) for dx in (-1, 0, 1) if dy or dx]
                if any(np.array_equal(n, bg) for n in neighbors):
                    for dy in (-1, 0, 1):
                        for dx in (-1, 0, 1):
                            ny, nx = y + dy, x + dx
                            if 0 <= ny < s and 0 <= nx < s and np.array_equal(copy[ny, nx], bg):
                                self.rgb[ny, nx] = outline


# --- Eye templates (local coords, each eye ~14x10) ---
def eye_open(left: bool = True) -> List[Pixel]:
    px = []
    flip = -1 if left else 1
    base_x = 0
    white = (242, 238, 232)
    outline = (38, 32, 36)
    for dy in range(-4, 5):
        w = 6 - abs(dy) // 2
        for dx in range(-w, w + 1):
            x = base_x + dx * flip
            y = dy
            px.append((x, y, white))
    for dy in range(-2, 3):
        for dx in range(-3, 4):
            if dx * dx + dy * dy <= 8:
                px.append((base_x + dx * flip, dy, (68, 98, 72)))
    px.append((base_x + (2 if left else -2), -1, (22, 20, 26)))
    px.append((base_x + (1 if left else -1), -2, (250, 250, 255)))
    # upper lash line
    for dx in range(-5, 6):
        px.append((base_x + dx * flip, -5, outline))
    return px


def eye_narrow(left: bool = True) -> List[Pixel]:
    px = []
    flip = -1 if left else 1
    for dx in range(-5, 6):
        px.append((dx * flip, 0, (38, 32, 36)))
        px.append((dx * flip, 1, (242, 238, 232)))
        if abs(dx) < 4:
            px.append((dx * flip, 1, (68, 88, 62)))
    return px


def eye_wide(left: bool = True) -> List[Pixel]:
    px = eye_open(left)
    extra = []
    flip = -1 if left else 1
    for dx in range(-6, 7):
        extra.append((dx * flip, 5, (38, 32, 36)))
    return px + extra


def draw_eyes(canvas: PixelCanvas, cx: int, cy: int, iris: RGB, personality: str, rng: random.Random, sc: float = 1.0) -> None:
    if personality in ("nervous", "curious"):
        left, right = eye_wide(True), eye_wide(False)
    elif personality == "grim":
        left, right = eye_narrow(True), eye_narrow(False)
    else:
        left, right = eye_open(True), eye_open(False)

    # recolor iris
    def tint(pixels: List[Pixel]) -> List[Pixel]:
        out = []
        for x, y, c in pixels:
            if c == (68, 98, 72) or c == (68, 88, 62):
                out.append((x, y, iris))
            else:
                out.append((x, y, c))
        return out

    canvas.blit_sprite(tint(left), int(cx - 18 * sc), cy)
    canvas.blit_sprite(tint(right), int(cx + 18 * sc), cy)

    # brows
    brow = shade(iris, 0.35)
    bw = int(24 * sc)
    if personality == "angry" or personality == "grim":
        canvas.draw_line(cx - bw, cy - int(8 * sc), cx - int(10 * sc), cy - int(6 * sc), brow, 2)
        canvas.draw_line(cx + int(10 * sc), cy - int(6 * sc), cx + bw, cy - int(8 * sc), brow, 2)
    elif personality == "nervous":
        canvas.draw_line(cx - int(22 * sc), cy - int(9 * sc), cx - int(12 * sc), cy - int(7 * sc), brow, 1)
        canvas.draw_line(cx + int(12 * sc), cy - int(7 * sc), cx + int(22 * sc), cy - int(9 * sc), brow, 1)
    elif personality == "cheerful":
        for dx in range(int(-22 * sc), int(-8 * sc)):
            canvas.set(dx + cx, cy - int(8 * sc) + abs(dx + int(15 * sc)) // 4, brow)
        for dx in range(int(8 * sc), int(23 * sc)):
            canvas.set(dx + cx, cy - int(8 * sc) + abs(dx - int(15 * sc)) // 4, brow)
    else:
        canvas.draw_line(cx - int(22 * sc), cy - int(7 * sc), cx - int(10 * sc), cy - int(8 * sc), brow, 2)
        canvas.draw_line(cx + int(10 * sc), cy - int(8 * sc), cx + int(22 * sc), cy - int(7 * sc), brow, 2)


def draw_mouth(canvas: PixelCanvas, cx: int, cy: int, personality: str, lip: RGB) -> None:
    dark = shade(lip, 0.65)
    if personality == "cheerful":
        for dx in range(-8, 9):
            t = 1 - (dx / 8) ** 2
            canvas.set(cx + dx, cy + int(4 + t * 3), dark)
            canvas.set(cx + dx, cy + int(5 + t * 3), lip)
    elif personality == "grim":
        canvas.draw_line(cx - 9, cy + 5, cx + 9, cy + 4, dark, 2)
    elif personality == "nervous":
        canvas.set(cx, cy + 5, dark)
        for dx in range(-4, 5):
            canvas.set(cx + dx, cy + 6, lip)
    else:
        for dx in range(-6, 7):
            canvas.set(cx + dx, cy + 5, dark)
        canvas.set(cx, cy + 4, shade(lip, 0.9))


def draw_nose(canvas: PixelCanvas, cx: int, cy: int, skin: Sequence[RGB], width: float) -> None:
    shadow = skin[1]
    hi = skin[3] if len(skin) > 3 else skin[-1]
    w = int(width)
    canvas.set(cx, cy, hi)
    canvas.set(cx - w, cy + 2, shadow)
    canvas.set(cx + w, cy + 2, shadow)
    canvas.set(cx, cy + 3, shade(shadow, 0.85))
    canvas.set(cx - 1, cy + 4, shade(shadow, 0.75))
    canvas.set(cx + 1, cy + 4, shade(shadow, 0.75))


def draw_hair(
    canvas: PixelCanvas,
    cx: int,
    cy: int,
    rx: float,
    style: int,
    palette: Sequence[RGB],
    rng: random.Random,
    gender: str,
) -> None:
    """Layered hair: base volume + strand texture + highlight."""
    hi = palette[-1]
    mid = palette[2]
    dark = palette[0]

    if style == 0:  # short neat
        canvas.shaded_ellipse(cx, cy - 18, rx + 4, 22, palette)
        for _ in range(120):
            x = cx + rng.randint(int(-rx - 2), int(rx + 2))
            y = cy + rng.randint(-38, -12)
            canvas.set(x, y, rng.choice(palette))
    elif style == 1:  # side part
        canvas.shaded_ellipse(cx - 3, cy - 16, rx + 6, 24, palette)
        canvas.draw_line(cx - 8, cy - 36, cx + 4, cy - 10, shade(dark, 0.8), 1)
        for _ in range(90):
            x = cx + rng.randint(int(-rx), int(rx + 4))
            y = cy + rng.randint(-40, -8)
            canvas.set(x, y, mid if x > cx else dark)
    elif style == 2:  # bob (female)
        canvas.shaded_ellipse(cx, cy - 14, rx + 8, 26, palette)
        canvas.fill_ellipse(cx - rx - 4, cy - 2, 8, 18, mid)
        canvas.fill_ellipse(cx + rx - 2, cy - 2, 8, 18, mid)
    elif style == 3:  # long wavy
        canvas.shaded_ellipse(cx, cy - 16, rx + 5, 26, palette)
        for side in (-1, 1):
            for i in range(14):
                x = cx + side * (rx + 2 + i // 2)
                y = cy - 8 + i * 2
                canvas.fill_ellipse(x, y, 5, 7, rng.choice(palette[1:4]))
    elif style == 4:  # slick back
        canvas.shaded_ellipse(cx, cy - 20, rx + 2, 20, palette)
        for dx in range(-int(rx), int(rx) + 1):
            canvas.set(cx + dx, cy - 34, hi)
    elif style == 5:  # curly afro
        for _ in range(45):
            ox = cx + rng.randint(-int(rx + 6), int(rx + 6))
            oy = cy + rng.randint(-42, -8)
            canvas.fill_ellipse(ox, oy, rng.uniform(4, 8), rng.uniform(4, 8), rng.choice(palette))
    elif style == 6:  # bun
        canvas.shaded_ellipse(cx, cy - 16, rx + 3, 22, palette)
        canvas.fill_ellipse(cx + int(rx) - 2, cy - 32, 10, 10, mid)
        canvas.set(cx + int(rx), cy - 34, hi)
    elif style == 7:  # undercut
        canvas.shaded_ellipse(cx, cy - 18, rx + 5, 18, palette)
        for x in range(cx - int(rx) - 2, cx + int(rx) + 3):
            canvas.set(x, cy - 2, dark)
    elif style == 8:  # braids hint
        canvas.shaded_ellipse(cx, cy - 16, rx + 4, 24, palette)
        for side in (-1, 1):
            for i in range(8):
                x = cx + side * (rx + 1)
                y = cy - 6 + i * 3
                canvas.draw_line(x, y, x + side * 2, y + 2, dark, 1)
    elif style == 9:  # widow peak
        canvas.shaded_ellipse(cx, cy - 17, rx + 4, 23, palette)
        canvas.draw_line(cx, cy - 38, cx - 8, cy - 22, dark, 1)
        canvas.draw_line(cx, cy - 38, cx + 8, cy - 22, dark, 1)
    elif style == 10:  # messy
        canvas.shaded_ellipse(cx, cy - 15, rx + 7, 25, palette)
        for _ in range(160):
            x = cx + rng.randint(int(-rx - 6), int(rx + 6))
            y = cy + rng.randint(-42, -6)
            canvas.set(x, y, rng.choice(palette))
    else:  # fedora hair
        canvas.shaded_ellipse(cx, cy - 14, rx + 2, 18, palette)

    # specular streak
    for i in range(8):
        canvas.set(cx - 6 + i, cy - 30 + i // 2, hi)


def draw_coat_and_shirt(
    canvas: PixelCanvas,
    cx: int,
    cloth: Sequence[RGB],
    accent: RGB,
    rng: random.Random,
    sc: float = 1.0,
) -> None:
    s = canvas.size
    dark, base, light = cloth[0], cloth[2], cloth[4]
    y0 = int(72 * sc)
    for y in range(y0, s):
        for x in range(int(16 * sc), s - int(16 * sc)):
            t = abs(x - cx) / float(max(cx - int(16 * sc), 1))
            c = lerp_color(dark, base, 1 - t * 0.6)
            if y > int(100 * sc):
                c = lerp_color(c, dark, (y - int(100 * sc)) / (28 * sc))
            # fabric weave
            if (x + y) % 5 == 0:
                c = shade(c, 0.96)
            canvas.set(x, y, c)
    for i in range(int(28 * sc)):
        canvas.draw_line(cx - int(18 * sc) + i // 2, y0 + i, cx - int(4 * sc), int(88 * sc) + i, light, 1)
        canvas.draw_line(cx + int(18 * sc) - i // 2, y0 + i, cx + int(4 * sc), int(88 * sc) + i, light, 1)
    for y in range(int(88 * sc), int(118 * sc)):
        for x in range(cx - int(10 * sc), cx + int(11 * sc)):
            canvas.set(x, y, (220, 215, 205))
    for y in range(int(88 * sc), int(115 * sc)):
        canvas.set(cx, y, accent)
        canvas.set(cx - 1, y, shade(accent, 0.8))
        canvas.set(cx + 1, y, shade(accent, 0.8))
    for by in (int(96 * sc), int(106 * sc), int(116 * sc)):
        canvas.fill_circle(cx, by, 1.2 * sc, shade(base, 1.1))


def profession_layers(canvas: PixelCanvas, cx: int, prof: str, rng: random.Random, accent: RGB, sc: float = 1.0) -> None:
    """Profession-specific pixel accessories (1920s pulp)."""
    def R(v):
        return int(v * sc)

    if prof == "detective":
        for x in range(cx - R(34), cx + R(35)):
            canvas.set(x, R(42), (48, 44, 40))
            canvas.set(x, R(43), (58, 52, 46))
        canvas.fill_ellipse(cx, R(36), R(28), R(10), (52, 48, 42))
        canvas.fill_ellipse(cx, R(28), R(18), R(14), (54, 50, 44))
        for x in range(cx - R(14), cx + R(15)):
            canvas.set(x, R(32), (38, 34, 30))
    elif prof == "nurse":
        for x in range(cx - 12, cx + 13):
            canvas.set(x, 38, (240, 240, 248))
        canvas.fill_ellipse(cx, 34, 14, 8, (240, 240, 248))
        canvas.set(cx, 44, (200, 60, 60))
        canvas.draw_line(cx - 4, 44, cx + 4, 44, (200, 60, 60), 1)
        canvas.draw_line(cx, 40, cx, 48, (200, 60, 60), 1)
    elif prof == "police_officer":
        canvas.fill_ellipse(cx, 32, 20, 12, (42, 52, 68))
        for x in range(cx - 22, cx + 23):
            canvas.set(x, 40, (36, 44, 58))
        canvas.fill_circle(cx + 10, 36, 2, (200, 180, 60))
    elif prof == "priest":
        canvas.draw_line(cx - 6, 90, cx, 118, (240, 240, 245), 2)
        canvas.draw_line(cx + 6, 90, cx, 118, (240, 240, 245), 2)
    elif prof == "professor":
        for x in range(cx - 14, cx + 15):
            canvas.set(x, 46, (180, 150, 90))
    elif prof == "librarian":
        # glasses
        for eye_c in (cx - 18, cx + 18):
            for dy in range(-3, 4):
                for dx in range(-5, 6):
                    if abs(dx) + abs(dy) == 5:
                        canvas.set(eye_c + dx, 52 + dy, (40, 36, 38))
        canvas.draw_line(cx - 13, 52, cx + 13, 52, (40, 36, 38), 1)
    elif prof == "doctor":
        canvas.set(cx - 8, 92, (240, 240, 248))
        canvas.set(cx + 8, 92, (240, 240, 248))
        canvas.draw_line(cx - 6, 94, cx + 6, 94, (200, 60, 60), 1)
        canvas.draw_line(cx, 90, cx, 98, (200, 60, 60), 1)
    elif prof == "occultist":
        canvas.set(cx, 78, (120, 90, 140))
        for i in range(-2, 3):
            canvas.set(cx + i * 3, 82, (200, 180, 90))
    elif prof == "journalist":
        canvas.fill_rect_like(cx - 14, 100, cx + 14, 118, (210, 200, 185))
        canvas.draw_line(cx, 100, cx, 118, (180, 170, 155), 1)
    elif prof == "artist":
        canvas.fill_circle(cx + 26, 78, 4, (120, 80, 60))
        canvas.set(cx + 24, 74, (180, 200, 80))
    elif prof == "musician":
        canvas.fill_ellipse(cx + 30, 82, 6, 9, (160, 120, 70))
    elif prof == "photographer":
        canvas.fill_rect_like(cx + 24, 76, cx + 34, 86, (36, 36, 40))
        canvas.fill_circle(cx + 29, 81, 3, (100, 120, 140))
    elif prof == "veteran":
        for x in range(cx - 16, cx + 17):
            canvas.set(x, 44, (58, 62, 52))
    elif prof == "farmer":
        canvas.fill_ellipse(cx - 20, 36, 12, 6, (120, 90, 55))
    elif prof == "fisherman":
        canvas.fill_ellipse(cx - 22, 38, 10, 5, (70, 75, 65))
    elif prof == "socialite":
        canvas.fill_ellipse(cx, 96, 16, 6, (180, 140, 100))
        canvas.set(cx - 12, 92, (200, 180, 120))
        canvas.set(cx + 12, 92, (200, 180, 120))
    elif prof == "mechanic":
        canvas.fill_rect_like(cx - 18, 108, cx + 18, 114, (200, 180, 60))
    elif prof == "bootlegger":
        canvas.fill_rect_like(cx + 24, 88, cx + 30, 108, (60, 90, 50))


def fill_rect_helper(canvas: PixelCanvas, x0: int, y0: int, x1: int, y1: int, c: RGB) -> None:
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            canvas.set(x, y, c)


# monkey-patch helper onto canvas class
PixelCanvas.fill_rect_like = lambda self, x0, y0, x1, y1, c: fill_rect_helper(self, x0, y0, x1, y1, c)


def age_modifiers(age: str) -> Tuple[float, float, bool]:
    return {
        "young": (0.94, 0.02, False),
        "adult": (1.0, 0.06, False),
        "middle_aged": (1.02, 0.12, False),
        "senior": (0.98, 0.2, True),
        "elderly": (0.94, 0.28, True),
    }[age]


def render_portrait(params: PortraitParams) -> Image.Image:
    rng = random.Random(params.seed)
    S = CANVAS_SIZE / 128.0  # coordinate scale vs original 128 layout
    canvas = PixelCanvas(CANVAS_SIZE)

    skin_base = rng.choice(SKIN_TONES[params.ethnicity])
    skin = ramp(skin_base)
    hair_color = rng.choice(HAIR_BASE)
    if params.age in ("senior", "elderly") and rng.random() < 0.65:
        hair_color = rng.choice([(140, 138, 134), (160, 156, 150), (120, 118, 114)])
    hair = ramp(hair_color, shadows=0.5, mid=0.85, hi=1.1)
    iris = rng.choice(EYE_BASE)
    cloth = ramp(rng.choice(CLOTH_BASE))
    accent = rng.choice(ACCENT)

    face_scale, wrinkle, gray_hint = age_modifiers(params.age)
    if params.age == "middle_aged" and rng.random() < 0.35:
        gray_hint = True
    cx, cy = int(64 * S), int(58 * face_scale * S)
    jaw = 0.88 if params.gender == "female" else 1.0
    rx, ry = 26 * jaw * face_scale * S, 32 * face_scale * S

    # background gradient (subtle pulp mood)
    for y in range(CANVAS_SIZE):
        t = y / (CANVAS_SIZE - 1)
        c = lerp_color((22, 28, 36), (34, 42, 52), t)
        for x in range(CANVAS_SIZE):
            canvas.set(x, y, c)

    draw_coat_and_shirt(canvas, cx, cloth, accent, rng, S)

    # neck
    nw = int(10 * jaw)
    canvas.shaded_ellipse(cx, cy + 26, nw, 12, skin[1:5])

    # ears
    for ex in (cx - rx - 2, cx + rx - 2):
        canvas.shaded_ellipse(ex, cy + 2, 5, 8, skin[1:5])

    # face
    canvas.shaded_ellipse(cx, cy, rx, ry, skin)
    canvas.outline_ellipse(cx, cy, rx, ry, shade(skin[0], 0.7), 1)

    # cheek blush / cheekbone
    blush = lerp_color(skin[2], (180, 100, 90), 0.25)
    canvas.fill_ellipse(cx - 14, cy + 6, 5, 3, blush)
    canvas.fill_ellipse(cx + 14, cy + 6, 5, 3, blush)

    # wrinkles
    if wrinkle > 0.08:
        wcol = shade(skin[1], 0.82)
        for _ in range(int(wrinkle * 40)):
            x = cx + rng.randint(-18, 18)
            y = cy + rng.randint(-4, 18)
            canvas.set(x, y, wcol)
            canvas.set(x + 1, y, wcol)

    # hair back then front handled in draw_hair
    style = rng.randint(0, 11)
    if params.gender == "female" and style in (0, 4, 7):
        style = rng.choice([2, 3, 6, 8, 10])

    draw_hair(canvas, cx, cy, rx, style, hair, rng, params.gender)

    nose_w = 1.0 if params.ethnicity in ("white", "latino") else 1.3
    if params.ethnicity == "asian":
        nose_w = 0.8
    draw_nose(canvas, cx, cy + 4, skin, nose_w)

    draw_eyes(canvas, cx, cy - int(2 * S), iris, params.personality, rng, S)

    lip = lerp_color(skin[2], (150, 80, 80), 0.35)
    draw_mouth(canvas, cx, cy + int(10 * S), params.personality, lip)

    profession_layers(canvas, cx, params.profession, rng, accent, S)

    # chin shadow
    canvas.fill_ellipse(cx, cy + 18, rx * 0.7, 8, shade(skin[0], 0.75))

    # vignette (pixel dither)
    half = CANVAS_SIZE / 2.0
    for y in range(CANVAS_SIZE):
        for x in range(CANVAS_SIZE):
            dx, dy = (x - half) / half, (y - half) / half
            v = dx * dx + dy * dy
            if v > 0.75:
                t = min(1.0, (v - 0.75) * 3)
                r, g, b = canvas.rgb[y, x]
                canvas.rgb[y, x] = (
                    clamp(int(r * (1 - t * 0.5))),
                    clamp(int(g * (1 - t * 0.5))),
                    clamp(int(b * (1 - t * 0.5))),
                )

    return canvas.to_image(scale=OUTPUT_SCALE)


def build_name(params: PortraitParams) -> str:
    return f"{params.profession}-{params.personality}-{params.gender}-{params.age}-{params.ethnicity}"
