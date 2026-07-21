"""Mockup ruangan dungeon: menyusun tile persis seperti rencana DungeonGenerator
dan meniru urutan render Unity (tint -> selubung gelap -> cahaya additive -> properti),
supaya komposisinya bisa dinilai dengan mata sebelum ditulis jadi C#."""
import zipfile, io, random, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ART = r"C:\Users\LENOVO\AppData\Local\Temp\claude\C--Users-LENOVO-Documents-Claude\2793b5e4-51de-428f-9bfe-324cad925c4e\scratchpad\art"
z = zipfile.ZipFile(ART + r"\rl-caves.zip")
SHEET = Image.open(io.BytesIO(z.open("Spritesheet/roguelikeDungeon_transparent.png").read())).convert("RGBA")
COLS, T, GAP = 29, 16, 1

def tile(i):
    r, c = divmod(i, COLS)
    x0, y0 = c * (T + GAP), r * (T + GAP)
    return SHEET.crop((x0, y0, x0 + T, y0 + T))

# --- palet tile terverifikasi ---
# 62-65 SENGAJA TIDAK dipakai: latarnya opak gelap, di lantai tampak seperti lubang
FLOOR_PLAIN = [16, 17, 18, 19, 20, 37, 38]
FLOOR_CRACK = [96, 97, 98, 100, 101, 102]
WALL_DARK   = [137, 142, 143, 144]                       # batu gelap pekat: batas ruangan harus jelas
WALL_CAP    = [282, 283, 284, 285, 286, 287, 288, 289]   # blok batu, sisi dalam tembok
WATER       = [320, 321, 322, 323]
WATER_EDGE  = [290, 291, 292, 296, 297, 319, 324]
PILLAR_TOP, PILLAR_BOT = 145, 174
ROCKS   = [0, 1, 2, 3, 4, 5]
BONES   = [58, 59, 60, 61]
SHROOMS = [87, 88]
BRAZIER = 506

def pick(rng, pool): return pool[rng.randrange(len(pool))]


def render_room(W, H, seed=7, scale=3, water=False,
                floor_tint=(255, 255, 255), ambient=(0x0F, 0x0F, 0x1A), amb_a=0.55,
                light=(255, 190, 110), light_a=0.85, light_r=6.0):
    rng = random.Random(seed)

    # ---------- lapisan lantai + tembok ----------
    ground = Image.new("RGBA", (W * T, H * T), (0, 0, 0, 255))
    for y in range(H):
        for x in range(W):
            t = pick(rng, FLOOR_PLAIN) if rng.random() < 0.86 else pick(rng, FLOOR_CRACK)
            ground.alpha_composite(tile(t), (x * T, y * T))

    if water:
        pw, ph = max(4, W // 3), max(3, H // 3)
        px, py = (W - pw) // 2, (H - ph) // 2
        for y in range(ph):
            for x in range(pw):
                edge = x in (0, pw - 1) or y in (0, ph - 1)
                t = pick(rng, WATER_EDGE) if edge else pick(rng, WATER)
                ground.alpha_composite(tile(t), ((px + x) * T, (py + y) * T))

    g = np.asarray(ground.convert("RGB")).astype(np.float32)

    # ---------- tint tema (multiply, seperti SpriteRenderer.color) ----------
    g *= np.array(floor_tint, np.float32) / 255.0

    # ---------- tembok: lapisan TERPISAH dengan tint jauh lebih gelap ----------
    # Kalau tembok ikut tint lantai, ia menyatu dan ruangan terlihat tak berbatas.
    wall_img = Image.new("RGBA", ground.size, (0, 0, 0, 0))
    for x in range(W):
        for (y, pool) in ((0, WALL_DARK), (1, WALL_CAP), (H - 1, WALL_DARK), (H - 2, WALL_CAP)):
            wall_img.alpha_composite(tile(pick(rng, pool)), (x * T, y * T))
    for y in range(H):
        for (x, pool) in ((0, WALL_DARK), (1, WALL_CAP), (W - 1, WALL_DARK), (W - 2, WALL_CAP)):
            wall_img.alpha_composite(tile(pick(rng, pool)), (x * T, y * T))

    wa = np.asarray(wall_img).astype(np.float32)
    wrgb = wa[..., :3] * (np.array(floor_tint, np.float32) / 255.0) * 0.42
    wal = (wa[..., 3:4] / 255.0)
    g = g * (1 - wal) + wrgb * wal

    # ---------- selubung gelap ----------
    g = g * (1.0 - amb_a) + np.array(ambient, np.float32) * amb_a

    # ---------- cahaya obor (additive di atas selubung) ----------
    torches = []
    n = max(2, W // 7)
    for i in range(n):
        x = int(3 + (W - 7) * (i / max(1, n - 1)))
        torches += [(x, 2), (x, H - 3)]

    mask = Image.new("L", (W * T, H * T), 0)
    md = ImageDraw.Draw(mask)
    rr = int(light_r * T)
    for (tx, ty) in torches:
        cx, cy = tx * T + T // 2, ty * T + T // 2
        md.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(rr * 0.5))
    m = (np.asarray(mask).astype(np.float32) / 255.0)[..., None]
    g = np.clip(g + np.array(light, np.float32) * m * light_a, 0, 255)

    img = Image.fromarray(g.astype(np.uint8)).convert("RGBA")

    # ---------- properti: digambar DI ATAS selubung, jadi tetap terbaca ----------
    occupied = set()
    for cx in (4, W - 5):
        for cy in (4, H - 6):
            img.alpha_composite(tile(PILLAR_TOP), (cx * T, cy * T))
            img.alpha_composite(tile(PILLAR_BOT), (cx * T, (cy + 1) * T))
            occupied |= {(cx, cy), (cx, cy + 1)}

    for _ in range(max(3, (W * H) // 80)):
        cxr, cyr = rng.randrange(3, W - 3), rng.randrange(3, H - 3)
        pool = pick(rng, [ROCKS, BONES, SHROOMS])
        for _ in range(rng.randrange(2, 5)):
            x = min(W - 3, max(2, cxr + rng.randrange(-2, 3)))
            y = min(H - 3, max(2, cyr + rng.randrange(-2, 3)))
            if (x, y) in occupied: continue
            occupied.add((x, y))
            img.alpha_composite(tile(pick(rng, pool)), (x * T, y * T))

    for (tx, ty) in torches:
        img.alpha_composite(tile(BRAZIER), (tx * T, ty * T))

    return img.resize((W * T * scale, H * T * scale), Image.NEAREST)


THEMES = {
    "F1 Pelataran Beringin": dict(floor_tint=(163, 177, 138), ambient=(0x2B, 0x21, 0x38),
                                  amb_a=0.55, light=(255, 190, 105), light_a=0.30, light_r=4.2),
    "F2 Umbul Pasiraman":    dict(floor_tint=(150, 190, 195), ambient=(0x12, 0x28, 0x3A),
                                  amb_a=0.60, light=(110, 210, 235), light_a=0.26, light_r=4.0, water=True),
    "F3 Sumur Gumuling":     dict(floor_tint=(150, 145, 170), ambient=(0x0F, 0x0F, 0x1A),
                                  amb_a=0.74, light=(255, 165, 80),  light_a=0.38, light_r=3.4),
    "F5 Gedhong Pusaka":     dict(floor_tint=(190, 172, 125), ambient=(0x1A, 0x14, 0x26),
                                  amb_a=0.58, light=(255, 205, 120), light_a=0.32, light_r=4.6),
}

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "mock_room.png"
    imgs = []
    for i, (name, kw) in enumerate(THEMES.items()):
        imgs.append((name, render_room(26, 17, seed=3 + i * 5, scale=2, **kw)))
    w = max(im.width for _, im in imgs)
    h = sum(im.height + 26 for _, im in imgs)
    canvas = Image.new("RGBA", (w, h), (10, 10, 14, 255))
    d = ImageDraw.Draw(canvas)
    y = 0
    for name, im in imgs:
        d.text((6, y + 6), name, fill=(230, 200, 130, 255))
        canvas.alpha_composite(im, (0, y + 24))
        y += im.height + 26
    canvas.save(out)
    print("wrote", out, canvas.size)
