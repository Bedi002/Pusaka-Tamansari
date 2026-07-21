"""Port LayoutGrid ke Python untuk membedah kenapa jumlah ruangannya salah.
Logikanya graf murni, jadi bisa diuji ribuan kali dalam hitungan detik."""
import random, math
from collections import deque

DIRS = [(0, 1), (0, -1), (-1, 0), (1, 0)]


def bfs(cells, links, origin):
    seen = {origin}
    q = deque([origin])
    while q:
        cur = q.popleft()
        for n in links.get(cur, ()):
            if n in cells and n not in seen:
                seen.add(n)
                q.append(n)
    return seen


def layout_grid(count, rng, verbose=False):
    wanted = math.ceil(count * 1.18)
    w = max(3, round(math.sqrt(wanted)))
    h = max(3, math.ceil(wanted / w))

    cells = {(0, 0)}
    for y in range(h):
        for x in range(w):
            if (x, y) == (0, 0):
                continue
            if x + y != 1 and rng.random() < 0.15:
                continue
            cells.add((x, y))
    after_holes = len(cells)

    links = {c: set() for c in cells}

    def link(a, b):
        links[a].add(b)
        links[b].add(a)

    # pohon rentang acak
    seen = {(0, 0)}
    frontier = [(0, 0)]
    while frontier:
        i = rng.randrange(len(frontier))
        cur = frontier[i]
        opts = [(cur[0] + d[0], cur[1] + d[1]) for d in DIRS]
        opts = [n for n in opts if n in cells and n not in seen]
        if not opts:
            frontier.pop(i)
            continue
        nxt = opts[rng.randrange(len(opts))]
        link(cur, nxt)
        seen.add(nxt)
        frontier.append(nxt)

    for c in list(cells):
        if c not in seen:
            cells.discard(c)
            links.pop(c, None)
    after_span = len(cells)

    for c in list(cells):
        for d in DIRS:
            n = (c[0] + d[0], c[1] + d[1])
            if n in cells and rng.random() < 0.45:
                link(c, n)

    # GrowTo: tambah ruangan di pinggiran sampai target terpenuhi
    guard = 0
    while len(cells) < count and guard < 500:
        guard += 1
        opts = []
        for c in cells:
            for d in DIRS:
                n = (c[0] + d[0], c[1] + d[1])
                if n not in cells:
                    opts.append((c, n))
        if not opts:
            break
        src, n = opts[rng.randrange(len(opts))]
        cells.add(n)
        links.setdefault(n, set())
        link(src, n)
    after_grow = len(cells)

    # TrimTo
    guard = 0
    while len(cells) > count and guard < 200:
        guard += 1
        dist = {}
        seen2 = {(0, 0)}
        q = deque([((0, 0), 0)])
        while q:
            cur, dd = q.popleft()
            dist[cur] = dd
            for n in links[cur]:
                if n in cells and n not in seen2:
                    seen2.add(n)
                    q.append((n, dd + 1))

        order = sorted(cells, key=lambda c: -dist.get(c, 0))
        removed = False
        for victim in order:
            if victim == (0, 0):
                continue
            nb = list(links[victim])
            for n in nb:
                links[n].discard(victim)
            cells.discard(victim)
            if len(bfs(cells, links, (0, 0))) != len(cells):
                cells.add(victim)
                for n in nb:
                    links[n].add(victim)
                continue
            removed = True
            break
        if not removed:
            break

    if verbose:
        print(f"  target={count} grid={w}x{h}={w*h} setelah_lubang={after_holes} "
              f"setelah_pohon={after_span} setelah_tumbuh={after_grow} akhir={len(cells)}")
    return len(cells)


for target in (16, 20):
    rng = random.Random(1000)
    print(f"target {target}:")
    layout_grid(target, rng, verbose=True)

    counts = [layout_grid(target, random.Random(s)) for s in range(300)]
    lo, hi = min(counts), max(counts)
    bad = sum(1 for c in counts if c < target * 0.6 or c > target * 1.4)
    print(f"  300 percobaan -> {lo}-{hi}, di luar batas: {bad}")
