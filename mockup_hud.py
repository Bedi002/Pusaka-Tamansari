"""Pemeriksa tata letak HUD: mendeteksi elemen yang saling menimpa pada kanvas
1920x1080. Dipakai untuk menyetel angka sebelum ditulis ke PusakaSceneBuilder,
karena tabrakan koordinat tidak terlihat sama sekali dari membaca kode."""
from PIL import Image, ImageDraw

W, H = 1920, 1080


def rect(name, cx, cy, w, h, always=True, container=False):
    """container=True: panel latar yang memang menaungi elemen lain,
    jadi tumpang-tindihnya bukan cacat."""
    return dict(name=name, cx=cx, cy=cy, w=w, h=h, always=always, container=container)


# ---- tata letak LAMA (seperti di screenshot) ----
OLD = [
    rect("Vitals",       0,  452, 1004, 100),
    rect("Stage",     -720,  480,  360,  60),
    rect("Ruangan",      0,  480,  360,  60),
    rect("Skor",       720,  480,  360,  60),
    rect("BossBar",      0,  400,  920,  70, always=False),
    rect("Minimap",    740,  280,  320, 320),
    rect("Kunci",        0, -510, 1500,  44),
]

# ---- tata letak BARU ----
NEW = [
    rect("Vitals",       0,  452, 1004,  96),
    rect("Stage",     -770,  474,  300,  52),
    rect("Ruangan",   -770,  420,  300,  46),
    rect("Skor",       770,  474,  300,  52),
    rect("BossBar",      0,  336,  920,  70, always=False),
    rect("Minimap",    762,  250,  300, 300),
    rect("Kunci",        0, -512, 1500,  40),
]


def bounds(r):
    return (r["cx"] - r["w"] / 2, r["cy"] - r["h"] / 2,
            r["cx"] + r["w"] / 2, r["cy"] + r["h"] / 2)


def overlaps(a, b):
    ax0, ay0, ax1, ay1 = bounds(a)
    bx0, by0, bx1, by1 = bounds(b)
    ox = min(ax1, bx1) - max(ax0, bx0)
    oy = min(ay1, by1) - max(ay0, by0)
    return (ox, oy) if ox > 0 and oy > 0 else None


def check(layout, label):
    print(f"--- {label} ---")
    bad = 0
    for i in range(len(layout)):
        for j in range(i + 1, len(layout)):
            a, b = layout[i], layout[j]
            if a.get("container") or b.get("container"):
                continue
            ov = overlaps(a, b)
            if ov:
                bad += 1
                print(f"  TIMPA {a['name']} x {b['name']}: {ov[0]:.0f}x{ov[1]:.0f} px")
    # keluar layar?
    for r in layout:
        x0, y0, x1, y1 = bounds(r)
        if x0 < -W / 2 or x1 > W / 2 or y0 < -H / 2 or y1 > H / 2:
            bad += 1
            print(f"  KELUAR LAYAR {r['name']}")
    print(f"  total masalah: {bad}\n")
    return bad


def draw(layout, path, label):
    img = Image.new("RGB", (W, H), (18, 18, 26))
    d = ImageDraw.Draw(img)
    d.rectangle([2, 2, W - 3, H - 3], outline=(70, 70, 90))
    for r in layout:
        x0, y0, x1, y1 = bounds(r)
        # kanvas Unity: +y ke atas, gambar: +y ke bawah
        px0, py0 = x0 + W / 2, H / 2 - y1
        px1, py1 = x1 + W / 2, H / 2 - y0
        col = (227, 179, 65) if r["always"] else (120, 60, 140)
        d.rectangle([px0, py0, px1, py1], outline=col, width=3)
        d.text((px0 + 6, py0 + 6), r["name"], fill=col)
    d.text((10, 10), label, fill=(230, 230, 230))
    img.save(path)


if __name__ == "__main__":
    check(OLD, "LAMA")
    check(NEW, "BARU")
    draw(OLD, "hud_lama.png", "TATA LETAK LAMA")
    draw(NEW, "hud_baru.png", "TATA LETAK BARU")
    print("gambar: hud_lama.png, hud_baru.png")
