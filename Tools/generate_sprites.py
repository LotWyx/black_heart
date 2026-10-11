#!/usr/bin/env python3
"""
Генератор 16-битного пиксель-арта для Black Heart (16 px на юнит).

Стиль: тёмный контур в 1 пиксель, 4–5 плоских тонов (глубокая тень, тень, основа,
свет) и белые блики, без дизеринга и градиентного шума.
  - сердце, пули, осколки нарисованы вручную (карты пикселей ниже);
  - враги: силуэт задаётся геометрией, объём раскладывается на тона
    (cel shading), одиночные пиксели вычищаются, детали ставятся попиксельно.

Запуск:  python3 Tools/generate_sprites.py [папка_вывода]
По умолчанию пишет в Assets/Resources/Art.
"""
import math
import os
import struct
import sys
import zlib

import numpy as np

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Resources", "Art")
LIGHT = np.array([-0.55, -0.65, 0.52])     # свет сверху-слева (ось y вниз)
LIGHT /= np.linalg.norm(LIGHT)


def hexc(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))


# ---------------------------------------------------------------- PNG

def save_png(path, rgba):
    h, w, _ = rgba.shape
    data = (np.clip(rgba, 0, 1) * 255 + 0.5).astype(np.uint8)
    raw = b"".join(b"\x00" + data[y].tobytes() for y in range(h))

    def chunk(tag, payload):
        c = struct.pack(">I", len(payload)) + tag + payload
        return c + struct.pack(">I", zlib.crc32(tag + payload) & 0xFFFFFFFF)

    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as f:
        f.write(png)


# ---------------------------------------------------------------- ручные карты

def from_map(rows, palette):
    """Карта символов -> RGBA. '.' — прозрачный."""
    h, w = len(rows), len(rows[0])
    assert all(len(r) == w for r in rows), "строки разной длины"
    img = np.zeros((h, w, 4))
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch == ".":
                continue
            c = palette[ch]
            img[y, x, :3] = c[:3]
            img[y, x, 3] = c[3] if len(c) > 3 else 1.0
    return img


HEART_PAL = {
    "X": hexc("1c0a12"),   # контур
    "d": hexc("7a0c22"),   # глубокая тень
    "s": hexc("b8162f"),   # тень
    "R": hexc("e8283f"),   # основа
    "h": hexc("ff7584"),   # свет
    "W": hexc("ffffff"),   # блик
}

HEART = [
    "...XXXX...XXXX...",
    "..XhhRRX.XRRhRX..",
    ".XhWhRRRXRRRRRsX.",
    "XhWhRRRRRRRRRRRsX",
    "XhhRRRRRRRRRRRRsX",
    "XhRRRRRRRRRRRRsdX",
    "XRRRRRRRRRRRRssdX",
    ".XRRRRRRRRRRssdX.",
    "..XRRRRRRRRssdX..",
    "...XRRRRRRssdX...",
    "....XRRRRssdX....",
    ".....XRRssdX.....",
    "......XRsdX......",
    ".......XdX.......",
    "........X........",
]

HEART_CRACK = [(8, 1), (8, 2), (7, 3), (7, 4), (8, 5), (9, 6), (9, 7), (8, 8), (7, 9), (8, 10), (8, 11)]

SHARD = [
    ".XX.",
    "XhRX",
    "XRsX",
    ".XX.",
]

# Пуля в оттенках серого: цвет задаёт игра (белая / фиолетовая / синяя)
BULLET_PAL = {
    "X": hexc("16161e"),
    "D": hexc("62627a"),
    "M": hexc("9c9cae"),
    "L": hexc("d6d6e0"),
    "W": hexc("ffffff"),
}

BULLET = [
    "...XXXX...",
    ".XXWLLLXX.",
    ".XWWLLLMX.",
    "XLWLLLLLMX",
    "XLLLLLLLMX",
    "XLLLLLLMMX",
    "XMLLLLMMDX",
    ".XMMMMMDX.",
    ".XXDDDDXX.",
    "...XXXX...",
]

CRACK_PAL = {"X": hexc("0c0c12"), "W": hexc("ffffff") + (0.7,)}
BULLET_CRACK = [
    "..........",
    "....X.....",
    "....XW....",
    "...XW.....",
    "...XXW....",
    "..X..XW...",
    "....X.XW..",
    "....XW....",
    "..........",
    "..........",
]


def heart():
    return from_map(HEART, HEART_PAL)


def heart_broken():
    img = heart()
    for x, y in HEART_CRACK:
        img[y, x, :3] = HEART_PAL["X"]
        img[y, x, 3] = 1
        if img[y, x + 1, 3] > 0 and HEART[y][x + 1] in "Rh":
            img[y, x + 1, :3] = HEART_PAL["d"]   # тень по краю трещины
    return img


# ---------------------------------------------------------------- геометрия для врагов

def grid(w, h):
    y, x = np.mgrid[0:h, 0:w].astype(float)
    return x + 0.5, y + 0.5


def ellipse(x, y, cx, cy, rx, ry, angle=0.0):
    c, s = math.cos(angle), math.sin(angle)
    dx, dy = x - cx, y - cy
    u, v = dx * c + dy * s, -dx * s + dy * c
    return (u / rx) ** 2 + (v / ry) ** 2 <= 1.0


def polygon(x, y, pts):
    inside = np.zeros(x.shape, bool)
    n = len(pts)
    for i in range(n):
        x1, y1 = pts[i]
        x2, y2 = pts[(i + 1) % n]
        inside ^= ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-9) + x1)
    return inside


def line_pixels(points):
    out = []
    for (x0, y0), (x1, y1) in zip(points, points[1:]):
        x0, y0, x1, y1 = int(round(x0)), int(round(y0)), int(round(x1)), int(round(y1))
        dx, dy = abs(x1 - x0), -abs(y1 - y0)
        sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
        err = dx + dy
        while True:
            out.append((x0, y0))
            if x0 == x1 and y0 == y1:
                break
            e2 = 2 * err
            if e2 >= dy:
                err += dy
                x0 += sx
            if e2 <= dx:
                err += dx
                y0 += sy
    return out


def distance_inside(mask):
    h, w = mask.shape
    d = np.where(mask, 1e9, 0.0)
    r2 = math.sqrt(2)
    for passes in ((range(h), range(w), ((-1, 0, 1), (0, -1, 1), (-1, -1, r2), (1, -1, r2))),
                   (range(h - 1, -1, -1), range(w - 1, -1, -1), ((1, 0, 1), (0, 1, 1), (1, 1, r2), (-1, 1, r2)))):
        ys, xs, nb = passes
        for y in ys:
            for x in xs:
                if d[y, x] == 0:
                    continue
                best = d[y, x]
                for dx, dy, c in nb:
                    xx, yy = x + dx, y + dy
                    v = d[yy, xx] + c if 0 <= xx < w and 0 <= yy < h else c
                    if v < best:
                        best = v
                d[y, x] = best
    return d


def blur(a, passes):
    for _ in range(passes):
        p = np.pad(a, 1, mode="edge")
        a = sum(p[1 + dy:p.shape[0] - 1 + dy, 1 + dx:p.shape[1] - 1 + dx]
                for dy in (-1, 0, 1) for dx in (-1, 0, 1)) / 9.0
    return a


def pillow_light(mask, radius):
    """Освещённость силуэта, вспученного как подушка (0..1)."""
    t = np.clip(distance_inside(mask) / radius, 0, 1)
    height = blur(radius * np.sqrt(1 - (1 - t) ** 2), max(1, int(radius / 3)))
    gy, gx = np.gradient(height)
    n = np.dstack([-gx, -gy, np.ones_like(height)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return np.where(mask, np.clip((n * LIGHT).sum(axis=2), 0, 1), 0)


def sphere_light(x, y, cx, cy, r):
    dx, dy = (x - cx) / r, (y - cy) / r
    rr = dx * dx + dy * dy
    nz = np.sqrt(np.clip(1 - rr, 0, 1))
    n = np.dstack([dx, dy, nz])
    return np.clip((n * LIGHT).sum(axis=2), 0, 1), rr <= 1


def cel(light, mask, thresholds=(0.35, 0.58, 0.86)):
    """Свет -> номер тона: 0 глубокая тень, 1 тень, 2 основа, 3 свет."""
    tone = np.digitize(light, thresholds)
    return clean(np.where(mask, tone, -1), mask)


def clean(tone, mask, passes=2):
    """Убирает одиночные пиксели тона: заменяет на самый частый тон соседей."""
    h, w = tone.shape
    for _ in range(passes):
        out = tone.copy()
        for y in range(h):
            for x in range(w):
                if not mask[y, x]:
                    continue
                nb = [tone[yy, xx] for yy, xx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1))
                      if 0 <= yy < h and 0 <= xx < w and mask[yy, xx]]
                if nb and nb.count(tone[y, x]) == 0:
                    out[y, x] = max(set(nb), key=nb.count)
        tone = out
    return tone


class Canvas:
    def __init__(self, w, h):
        self.img = np.zeros((h, w, 4))
        self.w, self.h = w, h

    def fill_tones(self, tone, palette):
        for i, c in enumerate(palette):
            sel = tone == i
            self.img[sel, :3] = c
            self.img[sel, 3] = 1

    def outline(self, mask, color):
        p = np.pad(mask, 1)
        ring = (~mask) & (p[:-2, 1:-1] | p[2:, 1:-1] | p[1:-1, :-2] | p[1:-1, 2:])
        self.img[ring, :3] = color
        self.img[ring, 3] = 1

    def fill(self, mask, color):
        self.img[mask, :3] = color
        self.img[mask, 3] = 1

    def px(self, pts, color, clip=None):
        for x, y in pts:
            if 0 <= x < self.w and 0 <= y < self.h and (clip is None or clip[y, x]):
                self.img[y, x, :3] = color
                self.img[y, x, 3] = 1


# ---------------------------------------------------------------- враги

def ghost():
    w, h = 40, 40
    x, y = grid(w, h)
    body = ellipse(x, y, 20, 15, 12.5, 12.5) | ((x >= 7.5) & (x <= 32.5) & (y >= 15) & (y <= 31))
    wave = np.abs(np.sin(math.pi * (x - 7.5) / 6.25))
    body |= (x >= 7.5) & (x <= 32.5) & (y > 30) & (y <= 31 + 4 * wave)
    body |= ellipse(x, y, 6.5, 22, 4, 2.3, -0.6) | ellipse(x, y, 33.5, 22, 4, 2.3, 0.6)

    pal = [hexc("6f6fa8"), hexc("a5acd8"), hexc("dde6fb"), hexc("ffffff")]
    outline = hexc("1d1530")
    cv = Canvas(w, h)
    cv.fill_tones(cel(pillow_light(body, 6), body), pal)
    cv.outline(body, outline)

    dark = hexc("150c22")
    for cx in (15, 25):
        cv.fill(ellipse(x, y, cx, 15, 2.4, 3.4), dark)
        cv.px([(cx - 1, 13)], (1, 1, 1))
        cv.px([(cx - 4, 19), (cx - 3, 19)] if cx == 15 else [(cx + 2, 19), (cx + 3, 19)], hexc("f4a6c8"))
    cv.fill(ellipse(x, y, 20, 23.5, 2.6, 3.2), dark)
    cv.px([(19, 25), (20, 25), (21, 25), (20, 26)], hexc("e8577f"))
    return cv.img


def eye():
    w, h = 48, 36
    x, y = grid(w, h)
    cv = Canvas(w, h)

    # крылья
    left = [(15, 11), (9.5, 5.5), (4, 4.5), (0.6, 9), (1.4, 15.5), (4.2, 13.5), (5.6, 19), (8.4, 16.2), (10.5, 22), (14, 19)]
    wings = polygon(x, y, left) | polygon(x, y, [(w - px_, py) for px_, py in left])
    wing_pal = [hexc("2a1238"), hexc("44205a"), hexc("643484"), hexc("8a52ac")]
    cv.fill_tones(cel(pillow_light(wings, 2.5), wings), wing_pal)
    cv.outline(wings, hexc("12061a"))
    for bone in ([(14, 11), (9, 6), (4, 5)], [(9, 6), (2, 13)], [(9, 6), (6, 17)], [(9, 6), (10, 20)]):
        cv.px(line_pixels(bone), hexc("12061a"), clip=wings)
        cv.px(line_pixels([(w - 1 - px_, py) for px_, py in bone]), hexc("12061a"), clip=wings)

    # глазное яблоко
    light, ball = sphere_light(x, y, 24, 19, 11.5)
    sclera_pal = [hexc("9a7f88"), hexc("cdb9bc"), hexc("f4ece6"), hexc("ffffff")]
    tone = cel(light, ball, (0.3, 0.55, 0.9))
    cv.img[ball, 3] = 0
    cv.fill_tones(tone, sclera_pal)
    cv.outline(ball, hexc("240a14"))

    vein = hexc("c7283a")
    for pts in ([(13, 15), (15, 17), (16, 16)], [(14, 24), (16, 23), (17, 25)], [(33, 13), (31, 14), (30, 13)],
                [(34, 23), (32, 23), (31, 25)], [(20, 9), (21, 11)]):
        cv.px(line_pixels(pts), vein, clip=ball)

    # радужка, зрачок-щель, блик
    ix, iy = 25, 21
    ri = np.hypot(x - ix, y - iy)
    iris = ri <= 5.6
    cv.fill(iris, hexc("c42a46"))
    cv.fill(iris & (ri > 4.4), hexc("5a0a1e"))
    cv.fill(iris & (ri <= 4.4) & (x > ix) & (y > iy), hexc("ec6458"))
    cv.fill(ellipse(x, y, ix, iy, 1.2, 3.9), hexc("0a0408"))
    cv.px([(22, 18), (23, 18), (22, 19)], (1, 1, 1))
    cv.px([(14, 13), (15, 12)], (1, 1, 1))
    return cv.img


def skull():
    w, h = 36, 40
    x, y = grid(w, h)
    cranium = ellipse(x, y, 18, 15, 14, 13.5)
    face = ellipse(x, y, 18, 25, 10.5, 8)
    jaw = (x >= 9.5) & (x <= 26.5) & (y >= 26) & (y <= 35) & ellipse(x, y, 18, 29, 9, 7)
    shape = cranium | face | jaw

    pal = [hexc("7d6650"), hexc("b8a487"), hexc("ece3cc"), hexc("fffbee")]
    outline = hexc("21150f")
    cv = Canvas(w, h)
    cv.fill_tones(cel(pillow_light(shape, 6.5), shape), pal)
    cv.outline(shape, outline)

    # глазницы с бирюзовым огнём
    dark = hexc("120a0a")
    for cx, a in ((12, 0.25), (24, -0.25)):
        cv.fill(ellipse(x, y, cx, 19, 4.3, 4.7, a), dark)
        cv.px([(cx - 1, 19), (cx, 19), (cx - 1, 20), (cx, 20)], hexc("3fd8ff"))
        cv.px([(cx - 1, 19)], (1, 1, 1))
        cv.px([(cx - 2, 20), (cx + 1, 20), (cx - 1, 21), (cx, 21)], hexc("1a6a86"))

    # нос
    cv.px([(17, 23), (18, 23), (17, 24), (18, 24), (16, 25), (19, 25)], dark)

    # зубы
    teeth = (x >= 11) & (x <= 25) & (y >= 28) & (y <= 33) & shape
    cv.fill(teeth, pal[2])
    cv.fill(teeth & (y < 29), pal[3])
    for tx in range(13, 25, 3):
        cv.px([(tx, ty) for ty in range(28, 33)], pal[0])
    cv.px([(tx, 30) for tx in range(10, 26)], outline, clip=shape)

    # трещина
    crack = line_pixels([(23, 3), (22, 6), (24, 8), (22, 11)])
    cv.px([(px_ + 1, py) for px_, py in crack], pal[3], clip=shape)
    cv.px(crack, outline, clip=shape)
    return cv.img


def boom_circle():
    w = h = 32
    x, y = grid(w, h)
    r = np.hypot(x - 16, y - 16)
    img = np.zeros((h, w, 4))
    img[r <= 15.5] = (0.55, 0.55, 0.6, 1)
    img[(r > 9) & (r <= 10)] = (0.85, 0.85, 0.9, 1)
    img[(r > 14.2) & (r <= 15.5)] = (1, 1, 1, 1)
    return img


SPRITES = {
    "Player/Soul": heart,
    "Player/SoulBroken": heart_broken,
    "Player/SoulShard": lambda: from_map(SHARD, HEART_PAL),
    "Projectiles/Bullet": lambda: from_map(BULLET, BULLET_PAL),
    "Projectiles/BulletCrack": lambda: from_map(BULLET_CRACK, CRACK_PAL),
    "FX/BoomCircle": boom_circle,
    "Enemies/Ghost": ghost,
    "Enemies/Eye": eye,
    "Enemies/Skull": skull,
}


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else ROOT
    for sid, fn in SPRITES.items():
        img = fn()
        save_png(os.path.join(out, sid + ".png"), img)
        print(f"{sid:26s} {img.shape[1]}x{img.shape[0]}")


if __name__ == "__main__":
    main()
