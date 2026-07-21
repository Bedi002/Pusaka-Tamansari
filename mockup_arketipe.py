"""Mockup arketipe ruangan: tiap ruangan tempur punya karakter berbeda supaya
tidak semuanya terlihat sama. Dinilai dengan mata dulu, baru ditulis ke C#."""
import zipfile, io, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ART = r"C:\Users\LENOVO\AppData\Local\Temp\claude\C--Users-LENOVO-Documents-Claude\2793b5e4-51de-428f-9bfe-324cad925c4e\scratchpad\art"
z = zipfile.ZipFile(ART + r"\rl-caves.zip")
SHEET = Image.open(io.BytesIO(z.open("Spritesheet/roguelikeDungeon_transparent.png").read())).convert("RGBA")
COLS, T = 29, 16

def tile(i):
    r, c = divmod(i, COLS)
    return SHEET.crop((c * (T + 1), r * (T + 1), c * (T + 1) + T, r * (T + 1) + T))

FLOOR = [16, 17, 18, 19, 20, 37, 38]
CRACK = [96, 97, 98, 100, 101, 102]
WALL_D = [137, 142, 143, 144]
WALL_C = [282, 283, 284, 285, 286, 287, 288, 289]
PILLAR_T, PILLAR_B = 145, 174
ROCKS = [0, 1, 2, 3, 4, 5]
BONES = [58, 59, 60, 61]
SHROOM = [87, 88, 89, 90]
CRYSTAL = [358, 359, 361, 362]
BRAZIER = 506

def pick(rng, p): return p[rng.randrange(len(p))]

def base(W, H, rng, tint):
    ground = Image.new("RGBA", (W * T, H * T), (0, 0, 0, 255))
    for y in range(H):
        for x in range(W):
            t = pick(rng, FLOOR) if rng.random() < 0.86 else pick(rng, CRACK)
            ground.alpha_composite(tile(t), (x * T, y * T))
    g = np.asarray(ground.convert("RGB")).astype(np.float32) * (np.array(tint, np.float32) / 255.0)

    wall = Image.new("RGBA", ground.size, (0, 0, 0, 0))
    for x in range(W):
        for (y, p) in ((0, WALL_D), (1, WALL_C), (H - 1, WALL_D), (H - 2, WALL_C)):
            wall.alpha_composite(tile(pick(rng, p)), (x * T, y * T))
    for y in range(H):
        for (x, p) in ((0, WALL_D), (1, WALL_C), (W - 1, WALL_D), (W - 2, WALL_C)):
            wall.alpha_composite(tile(pick(rng, p)), (x * T, y * T))
    wa = np.asarray(wall).astype(np.float32)
    wl = wa[..., 3:4] / 255.0
    g = g * (1 - wl) + wa[..., :3] * (np.array(tint, np.float32) / 255.0) * 0.42 * wl
    return g

def apply_light(g, torches, W, H, tint):
    g = g * (1 - 0.58) + np.array((0x12, 0x10, 0x1E), np.float32) * 0.58
    mask = Image.new("L", (W * T, H * T), 0)
    md = ImageDraw.Draw(mask)
    rr = int(4.0 * T)
    for (tx, ty) in torches:
        cx, cy = tx * T + T // 2, ty * T + T // 2
        md.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(rr * 0.5))
    m = (np.asarray(mask).astype(np.float32) / 255.0)[..., None]
    g = np.clip(g + np.array((255, 190, 110), np.float32) * m * 0.30, 0, 255)
    return Image.fromarray(g.astype(np.uint8)).convert("RGBA")

def prop(img, x, y, idx, s=1.0):
    t = tile(idx)
    if s != 1.0:
        n = int(T * s)
        t = t.resize((n, n), Image.NEAREST)
    img.alpha_composite(t, (int(x * T - (t.width - T) / 2), int(y * T - (t.height - T))))

def cluster(img, rng, cx, cy, pool, n, W, H):
    for _ in range(n):
        x = min(W - 3, max(2, cx + rng.randrange(-2, 3)))
        y = min(H - 3, max(2, cy + rng.randrange(-2, 3)))
        prop(img, x, y, pick(rng, pool), rng.uniform(0.9, 1.3))

def edge_torches(W, H):
    t = []
    for i in range(max(2, W // 8)):
        x = int(3 + (W - 7) * (i / max(1, W // 8 - 1)))
        t += [(x, 2), (x, H - 3)]
    return t

def corner_torches(W, H):
    return [(3, 3), (W - 4, 3), (3, H - 4), (W - 4, H - 4)]

def archetype(name, W, H, seed, tint):
    rng = random.Random(seed)
    if name == "Aula":
        g = base(W, H, rng, tint)
        torches = edge_torches(W, H)
        img = apply_light(g, torches, W, H, tint)
        for cx in (5, W - 6):
            for row in range(3):
                cy = int(4 + row * (H - 8) / 2)
                prop(img, cx, cy, PILLAR_B, 1.0); prop(img, cx, cy - 1, PILLAR_T, 1.0)
    elif name == "Grotto":
        g = base(W, H, rng, tint)
        torches = corner_torches(W, H)
        img = apply_light(g, torches, W, H, tint)
        for _ in range(4): cluster(img, rng, rng.randrange(3, W - 3), rng.randrange(3, H - 3), SHROOM, 4, W, H)
        for _ in range(2): cluster(img, rng, rng.randrange(3, W - 3), rng.randrange(3, H - 3), CRYSTAL, 3, W, H)
    elif name == "Pekuburan":
        g = base(W, H, rng, tint)
        torches = corner_torches(W, H)
        img = apply_light(g, torches, W, H, tint)
        for _ in range(5): cluster(img, rng, rng.randrange(3, W - 3), rng.randrange(3, H - 3), BONES, 4, W, H)
    elif name == "Reruntuhan":
        g = base(W, H, rng, tint)
        torches = edge_torches(W, H)
        img = apply_light(g, torches, W, H, tint)
        for _ in range(4): cluster(img, rng, rng.randrange(3, W - 3), rng.randrange(3, H - 3), ROCKS, 3, W, H)
        prop(img, W // 2, H // 2, PILLAR_B, 1.0)  # pilar patah: cuma bagian bawah
    else:  # Kosong
        g = base(W, H, rng, tint)
        torches = edge_torches(W, H)
        img = apply_light(g, torches, W, H, tint)
        cluster(img, rng, 4, 4, CRYSTAL, 3, W, H)
        cluster(img, rng, W - 5, H - 5, SHROOM, 3, W, H)
    for (tx, ty) in torches:
        img.alpha_composite(tile(BRAZIER), (tx * T, ty * T))
    return img.resize((W * T * 2, H * T * 2), Image.NEAREST)

if __name__ == "__main__":
    tint = (150, 145, 170)
    names = ["Aula", "Grotto", "Pekuburan", "Reruntuhan", "Kosong"]
    imgs = [(n, archetype(n, 24, 15, 5 + i * 7, tint)) for i, n in enumerate(names)]
    w = max(im.width for _, im in imgs)
    h = sum(im.height + 26 for _, im in imgs)
    canvas = Image.new("RGBA", (w, h), (10, 10, 14, 255))
    d = ImageDraw.Draw(canvas)
    y = 0
    for n, im in imgs:
        d.text((6, y + 6), n, fill=(230, 200, 130, 255))
        canvas.alpha_composite(im, (0, y + 24)); y += im.height + 26
    canvas.save("mock_arch.png"); print("wrote mock_arch.png", canvas.size)
