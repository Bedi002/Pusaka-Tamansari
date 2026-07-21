using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Membangun seluruh lantai dungeon saat runtime, sehingga tiap kali main
/// denahnya berbeda (inti genre roguelike). Menggantikan denah 7-ruangan
/// yang dulu dipahat manual di editor.
///
/// Urutan eksekusi -100: ruangan harus sudah ada sebelum HeroSpawner (-50)
/// dan DungeonManager (0) mencarinya.
///
/// Algoritma: pertumbuhan frontier di grid -> klasifikasi tipe ruangan lewat BFS
/// -> pembangunan geometri (lantai, tembok, pintu, properti, titik spawn).
/// Ruangan tidak bersentuhan fisik; pintu adalah trigger teleport, jadi tiap
/// ruangan boleh berbeda ukuran tanpa risiko tumpang tindih.
/// </summary>
[DefaultExecutionOrder(-100)]
public class DungeonGenerator : MonoBehaviour
{
    [Header("Lantai")]
    [Tooltip("Index profil di FloorCatalog. Di-set builder per scene.")]
    public int floorIndex = 0;
    [Tooltip("0 = acak tiap run. Isi selain 0 untuk denah yang bisa diulang (debug).")]
    public int seed = 0;

    [Header("Jarak antar-ruangan di grid")]
    public float stepX = 64f;
    public float stepY = 50f;

    static readonly Vector2Int[] Dirs = {
        new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(-1, 0), new Vector2Int(1, 0)
    };

    class Cell
    {
        public Vector2Int g;
        public RoomType type = RoomType.Combat;
        public Vector2 size;
        public Room room;
        public readonly List<Vector2Int> links = new List<Vector2Int>();
    }

    Dictionary<Vector2Int, Cell> cells;
    FloorProfile profile;

    /// <summary>Ruangan awal hasil generate (dibaca DungeonManager).</summary>
    public Room StartRoom { get; private set; }

    void Awake()
    {
        profile = FloorCatalog.Get(floorIndex);
        if (profile == null)
        {
            Debug.LogError("DungeonGenerator: FloorCatalog belum dibuat. Jalankan Tools > Pusaka > Rebuild Content.");
            return;
        }

        Random.InitState(seed != 0 ? seed : System.Environment.TickCount);

        Layout(Mathf.Max(6, profile.roomCount));
        Classify();
        BuildAll();

        var dm = FindAnyObjectByType<DungeonManager>();
        if (dm != null && StartRoom != null) dm.startRoom = StartRoom;

        var cam = Camera.main;
        if (cam != null) cam.backgroundColor = profile.skyTint;
    }

    /// <summary>
    /// Bangun HANYA graf ruangannya, tanpa geometri. Dipakai validator editor
    /// untuk memastikan tiap algoritma denah menghasilkan lantai yang benar:
    /// semua ruangan terjangkau dari awal, dan ada tepat satu ruangan boss.
    /// </summary>
    public string ValidateGraph(FloorProfile p, int trials = 25)
    {
        profile = p;
        int minRooms = int.MaxValue, maxRooms = 0, worstDeadEnds = 0;
        float sumLinks = 0f;

        for (int t = 0; t < trials; t++)
        {
            Random.InitState(1000 + t);
            Layout(Mathf.Max(6, p.roomCount));
            Classify();

            var reach = Bfs(new Vector2Int(0, 0));
            if (reach.Count != cells.Count)
                return $"GAGAL {p.layout}: {cells.Count - reach.Count} ruangan terputus dari awal";

            int bosses = 0, deadEnds = 0, links = 0;
            foreach (var kv in cells)
            {
                if (kv.Value.type == RoomType.Boss) bosses++;
                if (kv.Value.links.Count == 1) deadEnds++;
                links += kv.Value.links.Count;
            }
            if (bosses != 1) return $"GAGAL {p.layout}: ruangan boss ada {bosses}, harus 1";

            // jumlah ruangan harus mendekati permintaan profil: lantai 1 ruangan
            // tidak punya perjalanan, lantai 29 ruangan melelahkan
            int want = Mathf.Max(6, p.roomCount);
            if (cells.Count < want * 0.6f || cells.Count > want * 1.4f)
                return $"GAGAL {p.layout}: {cells.Count} ruangan, diminta {want}";

            minRooms = Mathf.Min(minRooms, cells.Count);
            maxRooms = Mathf.Max(maxRooms, cells.Count);
            worstDeadEnds = Mathf.Max(worstDeadEnds, deadEnds);
            sumLinks += links / (float)cells.Count;
        }

        return $"OK {p.layout,-11} ruangan {minRooms}-{maxRooms}, " +
               $"rata-rata sambungan {sumLinks / trials:0.00}, buntu maks {worstDeadEnds}";
    }

    // ------------------------------------------------------------ 1. denah
    /// <summary>
    /// Memilih algoritma denah sesuai profil lantai. Tiap gaya menghasilkan
    /// arsitektur yang benar-benar berbeda, bukan sekadar acak ulang: pemain
    /// harus bisa merasakan bahwa Sumur Gumuling itu gua yang membingungkan
    /// sementara Gedhong Pusaka itu bangunan berpetak.
    /// </summary>
    void Layout(int count)
    {
        cells = new Dictionary<Vector2Int, Cell>();
        var origin = new Vector2Int(0, 0);
        cells[origin] = new Cell { g = origin };

        switch (profile.layout)
        {
            case FloorLayout.Melingkar: LayoutRing(count); break;
            case FloorLayout.Gua: LayoutCave(count); break;
            case FloorLayout.Tulang: LayoutSpine(count); break;
            case FloorLayout.Petak: LayoutGrid(count); break;
            default: LayoutBranching(count); break;
        }

        // Jaring pengaman untuk SEMUA gaya, urutannya penting:
        //   sambung yang terputus -> tumbuhkan bila kurang -> pangkas bila lebih.
        // Lantai dengan ruangan tak terjangkau tidak bisa ditamatkan, dan lantai
        // yang jumlah ruangannya meleset jauh merusak ritme permainan. Keduanya
        // tidak terlihat sampai seseorang kebetulan mendapat seed yang buruk --
        // validator menemukan keduanya pada gaya Petak.
        ConnectStragglers();
        GrowTo(count);
        TrimTo(count);
        AddShortcuts();
    }

    /// <summary>
    /// Tambahkan ruangan di pinggiran sampai jumlahnya mencapai target. Lubang
    /// acak dan pembuangan pulau bisa menyisakan lantai yang jauh lebih kecil
    /// dari yang diminta; tanpa ini gaya Petak pernah menghasilkan 3 ruangan.
    /// </summary>
    void GrowTo(int target)
    {
        int guard = 0;
        var options = new List<(Vector2Int from, Vector2Int to)>();

        while (cells.Count < target && guard++ < 500)
        {
            options.Clear();
            foreach (var kv in cells)
                foreach (var d in Dirs)
                {
                    var n = kv.Key + d;
                    if (!cells.ContainsKey(n)) options.Add((kv.Key, n));
                }
            if (options.Count == 0) break;

            var (from, to) = options[Random.Range(0, options.Count)];
            Ensure(to);
            Link(from, to);
        }
    }

    /// <summary>Lorong pintas: menyambung ruangan bertetangga yang belum terhubung.</summary>
    void AddShortcuts()
    {
        var keys = new List<Vector2Int>(cells.Keys);
        foreach (var k in keys)
            foreach (var d in Dirs)
            {
                var n = k + d;
                if (!cells.ContainsKey(n) || cells[k].links.Contains(n)) continue;
                if (Random.value < profile.loopChance * 0.5f) Link(k, n);
            }
    }

    Cell Ensure(Vector2Int g)
    {
        if (!cells.TryGetValue(g, out var c)) { c = new Cell { g = g }; cells[g] = c; }
        return c;
    }

    /// <summary>Cincin tertutup, lalu beberapa cabang menjulur keluar darinya.</summary>
    void LayoutRing(int count)
    {
        int w = Mathf.Max(3, Mathf.RoundToInt(Mathf.Sqrt(count) + 1));
        int h = Mathf.Max(3, count / 2 - w + 2);

        var ring = new List<Vector2Int>();
        for (int x = 0; x < w; x++) { ring.Add(new Vector2Int(x, 0)); }
        for (int y = 1; y < h; y++) { ring.Add(new Vector2Int(w - 1, y)); }
        for (int x = w - 2; x >= 0; x--) { ring.Add(new Vector2Int(x, h - 1)); }
        for (int y = h - 2; y >= 1; y--) { ring.Add(new Vector2Int(0, y)); }

        foreach (var g in ring) Ensure(g);
        for (int i = 0; i < ring.Count; i++) Link(ring[i], ring[(i + 1) % ring.Count]);

        // cabang keluar sampai jumlah ruangan terpenuhi
        int guard = 0;
        while (cells.Count < count && guard++ < count * 40)
        {
            var from = ring[Random.Range(0, ring.Count)];
            var to = from + Dirs[Random.Range(0, Dirs.Length)];
            if (cells.ContainsKey(to)) continue;
            Ensure(to);
            Link(from, to);
        }
    }

    /// <summary>Gumpalan padat: tetangga banyak dibiarkan, jalan pintas melimpah.</summary>
    void LayoutCave(int count)
    {
        var open = new List<Vector2Int> { new Vector2Int(0, 0) };
        int guard = 0;
        while (cells.Count < count && guard++ < count * 60)
        {
            var from = open[Random.Range(0, open.Count)];
            var to = from + Dirs[Random.Range(0, Dirs.Length)];
            if (cells.ContainsKey(to)) continue;

            Ensure(to);
            Link(from, to);
            open.Add(to);

            // sambungkan juga ke tetangga lain yang sudah ada -> banyak lingkaran
            foreach (var d in Dirs)
            {
                var n = to + d;
                if (cells.ContainsKey(n) && n != from && Random.value < 0.55f) Link(to, n);
            }
        }
    }

    /// <summary>Lorong utama panjang dengan ruangan menempel di kiri-kanannya.</summary>
    void LayoutSpine(int count)
    {
        int spine = Mathf.Max(4, count * 2 / 3);
        var cur = new Vector2Int(0, 0);
        var trunk = new List<Vector2Int> { cur };

        for (int i = 1; i < spine; i++)
        {
            // sesekali membelok supaya tidak jadi garis lurus membosankan
            var dir = Random.value < 0.75f ? new Vector2Int(1, 0)
                                           : new Vector2Int(0, Random.value < 0.5f ? 1 : -1);
            var next = cur + dir;
            if (cells.ContainsKey(next)) { next = cur + new Vector2Int(1, 0); if (cells.ContainsKey(next)) break; }
            Ensure(next);
            Link(cur, next);
            trunk.Add(next);
            cur = next;
        }

        int guard = 0;
        while (cells.Count < count && guard++ < count * 40)
        {
            var from = trunk[Random.Range(0, trunk.Count)];
            var to = from + Dirs[Random.Range(0, Dirs.Length)];
            if (cells.ContainsKey(to)) continue;
            Ensure(to);
            Link(from, to);
        }
    }

    /// <summary>Petak rapat seperti denah bangunan, sebagian sel dilubangi.</summary>
    void LayoutGrid(int count)
    {
        // Ukuran petak dihitung mundur dari target: 15% sel jadi lubang, jadi
        // isi kotaknya sekitar count/0.85. Versi sebelumnya mengisi jauh lebih
        // besar lalu berharap dipangkas, dan menghasilkan 29 ruangan untuk 20.
        int wanted = Mathf.CeilToInt(count * 1.18f);
        int w = Mathf.Max(3, Mathf.RoundToInt(Mathf.Sqrt(wanted)));
        int h = Mathf.Max(3, Mathf.CeilToInt(wanted / (float)w));

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (x == 0 && y == 0) continue;              // origin sudah ada

                // Sel yang bersentuhan dengan asal TIDAK boleh dilubangi. Kalau
                // keempatnya jadi lubang, ruangan awal terkurung dan sisa lantai
                // ikut terbuang -- validator sempat menemukan lantai bersisa
                // 1 ruangan gara-gara ini.
                bool touchesOrigin = x + y == 1;
                if (!touchesOrigin && Random.value < 0.15f) continue;   // lubang: halaman dalam

                Ensure(new Vector2Int(x, y));
            }

        // Sambungan dibangun sebagai pohon rentang acak dari ruangan awal, BUKAN
        // dengan mengundi tiap tetangga lalu menambal yang terputus. Menambal
        // setelahnya rapuh: validator sempat mendapat 24 ruangan, lalu 8, karena
        // penambalan dan pemangkasan saling meniadakan. Pohon rentang menjamin
        // tersambung secara konstruksi; sisanya tinggal ditambah jalan pintas.
        SpanningLink();
        foreach (var k in new List<Vector2Int>(cells.Keys))
            foreach (var d in Dirs)
                if (cells.ContainsKey(k + d) && Random.value < 0.45f) Link(k, k + d);
    }

    /// <summary>
    /// Pohon rentang acak: telusuri sel dari asal, tiap sel baru disambungkan ke
    /// sel yang menemukannya. Sel yang tak tersentuh berarti pulau terpisah dan
    /// dibuang, karena tidak bersentuhan dengan apa pun.
    /// </summary>
    void SpanningLink()
    {
        var origin = new Vector2Int(0, 0);
        var seen = new HashSet<Vector2Int> { origin };
        var frontier = new List<Vector2Int> { origin };

        while (frontier.Count > 0)
        {
            int pick = Random.Range(0, frontier.Count);
            var cur = frontier[pick];

            var options = new List<Vector2Int>();
            foreach (var d in Dirs)
            {
                var n = cur + d;
                if (cells.ContainsKey(n) && !seen.Contains(n)) options.Add(n);
            }

            if (options.Count == 0) { frontier.RemoveAt(pick); continue; }

            var next = options[Random.Range(0, options.Count)];
            Link(cur, next);
            seen.Add(next);
            frontier.Add(next);
        }

        foreach (var k in new List<Vector2Int>(cells.Keys))
            if (!seen.Contains(k)) cells.Remove(k);
    }

    /// <summary>
    /// Pangkas kelebihan ruangan dengan membuang ujung buntu, supaya jumlahnya
    /// tetap dekat dengan yang diminta profil lantai. Petak yang diisi penuh
    /// bisa meleset jauh dari target -- validator mencatat 29 ruangan untuk
    /// permintaan 20.
    /// </summary>
    void TrimTo(int target)
    {
        var origin = new Vector2Int(0, 0);
        int guard = 0;

        while (cells.Count > target && guard++ < 200)
        {
            var dist = Bfs(origin);

            // urut dari yang terjauh: yang dibuang sebaiknya pinggiran, bukan
            // ruangan di tengah yang jadi simpul lalu lintas
            var order = new List<Vector2Int>(cells.Keys);
            order.Sort((a, b) =>
            {
                int da = dist.TryGetValue(a, out var x) ? x : 0;
                int db = dist.TryGetValue(b, out var y) ? y : 0;
                return db.CompareTo(da);
            });

            bool removed = false;
            foreach (var victim in order)
            {
                if (victim == origin) continue;

                var cut = cells[victim];
                var neighbours = new List<Vector2Int>(cut.links);

                foreach (var n in neighbours)
                    if (cells.ContainsKey(n)) cells[n].links.Remove(victim);
                cells.Remove(victim);

                // batal kalau membuangnya memutus lantai jadi dua
                if (Bfs(origin).Count != cells.Count)
                {
                    cells[victim] = cut;
                    foreach (var n in neighbours)
                        if (cells.ContainsKey(n) && !cells[n].links.Contains(victim)) cells[n].links.Add(victim);
                    continue;
                }

                removed = true;
                break;
            }
            if (!removed) break;    // tak ada yang aman dibuang
        }
    }

    /// <summary>
    /// Menyambungkan ruangan yang terputus dari ruangan awal.
    ///
    /// Versi pertama hanya satu lintasan dan menyambung ke tetangga mana pun --
    /// tetangganya sendiri bisa ikut terputus, jadi masalahnya tidak selesai.
    /// Validator menangkapnya: 6 dari 16 ruangan tak terjangkau, artinya ruangan
    /// boss bisa berada di balik dinding dan lantai tidak bisa ditamatkan.
    /// Sekarang: berulang, dan HANYA menyambung ke tetangga yang sudah terjangkau.
    /// </summary>
    void ConnectStragglers()
    {
        var origin = new Vector2Int(0, 0);

        for (int pass = 0; pass < 12; pass++)
        {
            var reach = Bfs(origin);
            if (reach.Count == cells.Count) return;

            bool progressed = false;
            foreach (var k in new List<Vector2Int>(cells.Keys))
            {
                if (reach.ContainsKey(k)) continue;
                foreach (var d in Dirs)
                {
                    var n = k + d;
                    if (!cells.ContainsKey(n) || !reach.ContainsKey(n)) continue;
                    Link(k, n);
                    progressed = true;
                    break;
                }
            }
            if (!progressed) break;   // sisanya benar-benar terpencil
        }

        // Pulau yang tidak bersentuhan dengan apa pun tidak bisa disambung;
        // membuangnya lebih baik daripada meninggalkan ruangan yang mustahil dicapai.
        var final = Bfs(origin);
        foreach (var k in new List<Vector2Int>(cells.Keys))
        {
            if (final.ContainsKey(k)) continue;
            foreach (var n in cells[k].links)
                if (cells.ContainsKey(n)) cells[n].links.Remove(k);
            cells.Remove(k);
        }
    }

    void LayoutBranching(int count)
    {
        var origin = new Vector2Int(0, 0);
        var open = new List<Vector2Int> { origin };

        int guard = 0;
        while (cells.Count < count && guard++ < count * 60)
        {
            var from = open[Random.Range(0, open.Count)];
            var dir = Dirs[Random.Range(0, Dirs.Length)];
            var to = from + dir;
            if (cells.ContainsKey(to)) continue;

            // hindari ruangan yang menempel ke banyak tetangga -> denah tetap
            // berbentuk cabang, bukan gumpalan padat yang membingungkan
            if (NeighborCount(to) > 1 && Random.value > 0.25f) continue;

            var cell = new Cell { g = to };
            cells[to] = cell;
            Link(from, to);
            open.Add(to);

            // ruangan yang sudah punya 3 sambungan tidak lagi jadi kandidat tumbuh
            if (cells[from].links.Count >= 3) open.Remove(from);
        }
    }

    int NeighborCount(Vector2Int g)
    {
        int n = 0;
        foreach (var d in Dirs) if (cells.ContainsKey(g + d)) n++;
        return n;
    }

    void Link(Vector2Int a, Vector2Int b)
    {
        if (!cells[a].links.Contains(b)) cells[a].links.Add(b);
        if (!cells[b].links.Contains(a)) cells[b].links.Add(a);
    }

    // ------------------------------------------------- 2. tipe tiap ruangan
    void Classify()
    {
        var origin = new Vector2Int(0, 0);
        var dist = Bfs(origin);

        // boss = buntu terjauh dari start; kalau tak ada buntu, ambil yang terjauh
        Vector2Int boss = origin;
        int bestScore = -1;
        foreach (var kv in cells)
        {
            if (kv.Key == origin) continue;
            int d = dist.TryGetValue(kv.Key, out var v) ? v : 0;
            int score = d * 2 + (kv.Value.links.Count == 1 ? 3 : 0);
            if (score > bestScore) { bestScore = score; boss = kv.Key; }
        }

        cells[origin].type = RoomType.Start;
        cells[boss].type = RoomType.Boss;

        // ruangan buntu lain jadi ruangan hadiah -- imbalan karena mau menjelajah
        var deadEnds = new List<Vector2Int>();
        foreach (var kv in cells)
            if (kv.Key != origin && kv.Key != boss && kv.Value.links.Count == 1)
                deadEnds.Add(kv.Key);
        Shuffle(deadEnds);

        var special = new[] { RoomType.Treasure, RoomType.Shop, RoomType.Shrine, RoomType.Treasure };
        for (int i = 0; i < deadEnds.Count; i++)
            cells[deadEnds[i]].type = i < special.Length ? special[i] : RoomType.Elite;

        // sisanya tempur; sebagian kecil jadi elite
        foreach (var kv in cells)
        {
            if (kv.Value.type != RoomType.Combat) continue;
            if (kv.Key == origin || kv.Key == boss) continue;
            int d = dist.TryGetValue(kv.Key, out var v) ? v : 0;
            if (d >= 3 && Random.value < 0.2f) kv.Value.type = RoomType.Elite;
        }

        foreach (var kv in cells) kv.Value.size = SizeFor(kv.Value.type);
    }

    Dictionary<Vector2Int, int> Bfs(Vector2Int from)
    {
        var dist = new Dictionary<Vector2Int, int> { { from, 0 } };
        var q = new Queue<Vector2Int>();
        q.Enqueue(from);
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            foreach (var n in cells[cur].links)
            {
                if (dist.ContainsKey(n)) continue;
                dist[n] = dist[cur] + 1;
                q.Enqueue(n);
            }
        }
        return dist;
    }

    Vector2 SizeFor(RoomType t)
    {
        switch (t)
        {
            case RoomType.Start: return new Vector2(26, 18);
            case RoomType.Boss: return new Vector2(42, 28);
            case RoomType.Elite: return new Vector2(36, 24);
            case RoomType.Treasure:
            case RoomType.Shop:
            case RoomType.Shrine: return new Vector2(22, 16);
            default: return new Vector2(Random.Range(28, 39), Random.Range(19, 26));
        }
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ------------------------------------------------------- 3. bangun scene
    void BuildAll()
    {
        var root = new GameObject("Dungeon").transform;

        foreach (var kv in cells) kv.Value.room = BuildRoom(root, kv.Value);

        foreach (var kv in cells)
            foreach (var to in kv.Value.links)
            {
                if (!cells.ContainsKey(to)) continue;
                AddDoor(kv.Value, cells[to]);
            }

        StartRoom = cells[new Vector2Int(0, 0)].room;

        var spawner = FindAnyObjectByType<HeroSpawner>();
        if (spawner != null && StartRoom != null) spawner.transform.position = StartRoom.Center;
    }

    Room BuildRoom(Transform parent, Cell c)
    {
        var go = new GameObject($"Room_{c.type}_{c.g.x}_{c.g.y}");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(c.g.x * stepX, c.g.y * stepY, 0f);

        var room = go.AddComponent<Room>();
        room.type = c.type;
        room.gridPos = c.g;
        room.size = c.size;
        room.enemyPrefabs = EnemiesFor(c.type);
        room.bossPrefab = c.type == RoomType.Boss ? profile.boss : null;
        room.enemyCount = EnemyCountFor(c.type);

        float hx = c.size.x * 0.5f, hy = c.size.y * 0.5f;

        BuildFloor(go.transform, c);
        BuildWalls(go.transform, c);
        Decorate(go.transform, c, hx, hy);
        AddSpawnPoints(go.transform, room, c);
        AddAmbient(go.transform, c);

        return room;
    }

    /// <summary>
    /// Lantai dipanggang jadi satu tekstur dengan variasi per-tile. Cara lama
    /// (satu sprite ditarik melintasi seluruh ruangan) menghasilkan bidang warna
    /// datar -- itulah yang membuat peta terlihat mati.
    /// </summary>
    void BuildFloor(Transform room, Cell c)
    {
        int wt = Mathf.Max(4, Mathf.RoundToInt(c.size.x));
        int ht = Mathf.Max(4, Mathf.RoundToInt(c.size.y));

        // kolam di tengah ruangan untuk lantai bertema air
        var pool = default(RectInt);
        if (profile.water && c.type != RoomType.Boss)
        {
            int pw = Mathf.Max(4, wt / 3), ph = Mathf.Max(3, ht / 3);
            pool = new RectInt((wt - pw) / 2, (ht - ph) / 2, pw, ph);
        }

        var baked = RoomFloorBaker.Bake(wt, ht,
                                        Tiles.RL.FloorPlain, Tiles.RL.FloorCrack, 0.14f,
                                        Tiles.RL.Water, Tiles.RL.WaterEdge, pool);

        var go = new GameObject("Floor");
        go.transform.SetParent(room, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = -10;
        sr.color = profile.floorTint * FloorAccent(c.type);

        if (baked != null)
        {
            sr.sprite = baked;
        }
        else
        {
            // cadangan bila tileset belum siap: minimal ruangan tetap punya lantai
            sr.sprite = ArtLibrary.R(Tiles.RL.FloorPlain[0]);
            if (sr.sprite != null) { sr.drawMode = SpriteDrawMode.Tiled; sr.size = c.size; }
        }
    }

    /// <summary>
    /// Tembok dua lapis: lapis luar pekat, lapis dalam blok batu. Tint-nya
    /// jauh lebih gelap dari lantai -- kalau disamakan, tembok menyatu dengan
    /// lantai dan ruangan terlihat tak berbatas.
    /// </summary>
    void BuildWalls(Transform room, Cell c)
    {
        float hx = c.size.x * 0.5f, hy = c.size.y * 0.5f;
        var outer = profile.floorTint * 0.42f; outer.a = 1f;
        var inner = profile.floorTint * 0.60f; inner.a = 1f;

        // lapis luar (tepat di batas) + lapis dalam (satu tile ke dalam)
        Band(room, "WallTop", new Vector2(0, hy - 0.5f), new Vector2(c.size.x, 1f), Tiles.RL.WallOuter, outer);
        Band(room, "WallTopIn", new Vector2(0, hy - 1.5f), new Vector2(c.size.x, 1f), Tiles.RL.WallInner, inner);
        Band(room, "WallBottom", new Vector2(0, -hy + 0.5f), new Vector2(c.size.x, 1f), Tiles.RL.WallOuter, outer);
        Band(room, "WallBottomIn", new Vector2(0, -hy + 1.5f), new Vector2(c.size.x, 1f), Tiles.RL.WallInner, inner);
        Band(room, "WallLeft", new Vector2(-hx + 0.5f, 0), new Vector2(1f, c.size.y), Tiles.RL.WallOuter, outer);
        Band(room, "WallLeftIn", new Vector2(-hx + 1.5f, 0), new Vector2(1f, c.size.y), Tiles.RL.WallInner, inner);
        Band(room, "WallRight", new Vector2(hx - 0.5f, 0), new Vector2(1f, c.size.y), Tiles.RL.WallOuter, outer);
        Band(room, "WallRightIn", new Vector2(hx - 1.5f, 0), new Vector2(1f, c.size.y), Tiles.RL.WallInner, inner);

        // Collider di tepi DALAM pita tembok, jadi pemain berhenti di kaki tembok
        // dan tidak pernah berdiri di atasnya.
        Barrier(room, new Vector2(0, hy - 1f), new Vector2(c.size.x, 2f));
        Barrier(room, new Vector2(0, -hy + 1f), new Vector2(c.size.x, 2f));
        Barrier(room, new Vector2(-hx + 1f, 0), new Vector2(2f, c.size.y));
        Barrier(room, new Vector2(hx - 1f, 0), new Vector2(2f, c.size.y));
    }

    void Band(Transform room, string name, Vector2 pos, Vector2 size, int[] set, Color color)
    {
        var sprite = ArtLibrary.R(set[Random.Range(0, set.Length)]);
        if (sprite == null) return;

        var go = new GameObject(name);
        go.transform.SetParent(room, false);
        go.transform.localPosition = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        sr.sortingOrder = 5;
        sr.color = color;
    }

    void Barrier(Transform room, Vector2 pos, Vector2 size)
    {
        var go = new GameObject("Barrier");
        go.transform.SetParent(room, false);
        go.transform.localPosition = pos;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
    }

    /// <summary>
    /// Selubung gelap seukuran ruangan. Inilah yang membuat cahaya obor terbaca
    /// sebagai cahaya, bukan sekadar noda terang di atas lantai yang sudah terang.
    /// </summary>
    void AddAmbient(Transform room, Cell c)
    {
        var go = new GameObject("Ambient");
        go.transform.SetParent(room, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = RoomDressing.Solid();

        // Skala transform, bukan drawMode Sliced: sprite putih polos tidak punya
        // border sehingga Sliced akan jatuh ke Simple sambil melempar warning.
        var span = c.size + Vector2.one * 2f;
        go.transform.localScale = new Vector3(span.x / RoomDressing.SolidUnits,
                                              span.y / RoomDressing.SolidUnits, 1f);
        // Order 4: di atas lantai (-10) dan bercak (-9), tetapi di bawah cahaya (6),
        // properti, dan karakter. Yang digelapkan hanya lantai -- karakter tetap
        // terbaca jelas, sementara cahaya obor punya latar gelap untuk dibaca.
        sr.sortingOrder = 4;

        var tint = profile.skyTint;
        // arena boss sedikit lebih terang: pemain harus bisa membaca telegraf serangan
        tint.a = profile.ambientStrength * (c.type == RoomType.Boss ? 0.85f : 1f);
        sr.color = tint;
    }

    Color FloorAccent(RoomType t)
    {
        switch (t)
        {
            case RoomType.Boss: return new Color(1.05f, 0.85f, 0.85f);
            case RoomType.Treasure: return new Color(1.05f, 1.0f, 0.82f);
            case RoomType.Shrine: return new Color(0.88f, 0.98f, 1.05f);
            case RoomType.Shop: return new Color(1.0f, 0.96f, 0.88f);
            case RoomType.Elite: return new Color(0.95f, 0.88f, 1.0f);
            default: return Color.white;
        }
    }

    GameObject[] EnemiesFor(RoomType t)
    {
        if (t == RoomType.Combat) return profile.enemies;
        if (t == RoomType.Elite)
            return (profile.elites != null && profile.elites.Length > 0) ? profile.elites : profile.enemies;
        return null;
    }

    int EnemyCountFor(RoomType t)
    {
        if (t == RoomType.Combat) return Mathf.Max(1, profile.enemiesPerRoom);
        if (t == RoomType.Elite) return Mathf.Max(1, Mathf.RoundToInt(profile.enemiesPerRoom * 0.6f));
        return 0;
    }

    // ------------------------------------------------------------ properti
    void Decorate(Transform room, Cell c, float hx, float hy)
    {
        var keepOut = new List<Vector2> { Vector2.zero };
        foreach (var to in c.links)
            keepOut.Add(DoorOffset(c, to - c.g, hx, hy));

        int budget = Mathf.RoundToInt(profile.propDensity * (c.size.x * c.size.y) / 42f);

        // Pola obor ikut berganti per ruangan supaya tepi ruangan juga terlihat
        // berbeda, bukan cuma isinya. Ruangan bukan-tempur tetap deret dinding.
        bool combat = c.type == RoomType.Combat || c.type == RoomType.Elite;
        int h = c.g.x * 73856093 ^ c.g.y * 19349663;
        bool corners = combat && (Mathf.Abs(h) % 5) >= 3;   // sebagian ruangan: obor di sudut saja

        // Posisi obor dicatat ke keepOut supaya properti tidak menimpanya
        // (tumpang-tindih brazier + prop yang terlihat di screenshot).
        void Torch(Vector2 p) { AddTorch(room, p); keepOut.Add(p); }

        if (corners)
        {
            Torch(new Vector2(-hx + 3f, hy - 3f));
            Torch(new Vector2(hx - 3f, hy - 3f));
            Torch(new Vector2(-hx + 3f, -hy + 3f));
            Torch(new Vector2(hx - 3f, -hy + 3f));
        }
        else
        {
            int lamps = Mathf.Max(2, Mathf.RoundToInt(c.size.x / 7f));
            for (int i = 0; i < lamps; i++)
            {
                float x = Mathf.Lerp(-hx + 2f, hx - 2f, lamps == 1 ? 0.5f : i / (float)(lamps - 1));
                Torch(new Vector2(x, hy - 2.5f));
                Torch(new Vector2(x, -hy + 2.5f));
            }
        }

        switch (c.type)
        {
            case RoomType.Treasure:
                MakeChest(room, new Vector2(0, 1.5f), 2);
                ScatterFrom(room, c, keepOut, 4, DecorSet(), false);
                break;

            case RoomType.Shop:
                // Warung Lelembut: Mbah Warung berdiri di balik meja dagangannya
                Prop(room, new Vector2(0, 0.2f), ArtLibrary.R(Tiles.RL.PillarBottom), 3, true);
                MakeVendor(room, new Vector2(0, 2.2f));
                Prop(room, new Vector2(-3.5f, 0.2f), ArtLibrary.R(Tiles.RL.Chest), 3, false);
                Prop(room, new Vector2(3.5f, 0.2f), ArtLibrary.R(Tiles.RL.Chest), 3, false);
                AddTorch(room, new Vector2(-5f, 2.2f));
                AddTorch(room, new Vector2(5f, 2.2f));
                break;

            case RoomType.Shrine:
                // Sanggar Sesaji: altar batu diapit dua pedupaan
                MakeShrine(room, new Vector2(0, 1.2f));
                AddTorch(room, new Vector2(-3f, 1.2f));
                AddTorch(room, new Vector2(3f, 1.2f));
                if (PropLibrary.Ready)
                {
                    PropArt(room, new Vector2(-hx + 3.5f, -hy + 3f), PropLibrary.I.gamelan, 1.6f, 2, false);
                    PropArt(room, new Vector2(hx - 3.5f, -hy + 3f), PropLibrary.I.statue, 2.4f, 3, true);
                }
                break;

            case RoomType.Boss:
                if (PropLibrary.Ready)
                {
                    // topeng Barong raksasa di dinding belakang + patung penjaga di sudut
                    PropArt(room, new Vector2(0, hy - 3.5f), PropLibrary.I.mask, 3.2f, 4, false);
                    foreach (var s in new[] { -1f, 1f })
                        PropArt(room, new Vector2(s * (hx - 4f), -hy + 4f), PropLibrary.I.statue, 2.8f, 3, true);
                }
                else
                {
                    foreach (var s in new[] { -1f, 1f })
                        foreach (var v in new[] { -1f, 1f })
                            Prop(room, new Vector2(s * (hx - 4f), v * (hy - 4f)),
                                 ArtLibrary.R(Tiles.RL.PillarTop), 3, true);
                }
                break;

            case RoomType.Start:
                Prop(room, new Vector2(-hx + 3f, -hy + 3f), ArtLibrary.R(Tiles.RL.Rocks[0]), 3, true);
                Prop(room, new Vector2(hx - 3f, -hy + 3f), ArtLibrary.R(Tiles.RL.PillarBottom), 3, true);
                break;

            case RoomType.Combat:
            case RoomType.Elite:
                DecorateArchetype(room, c, keepOut, hx, hy);
                break;

            default:
                ScatterFrom(room, c, keepOut, budget, SolidSet(), true);
                ScatterFrom(room, c, keepOut, budget, DecorSet(), false);
                break;
        }
    }

    /// <summary>
    /// Lima wajah ruangan tempur. Sebelumnya SEMUA ruangan tempur memakai pola
    /// hias yang sama persis -- obor di dua sisi lalu taburan seragam -- sehingga
    /// tiap ruangan terlihat identik. Sekarang tiap ruangan memilih satu arketipe
    /// secara deterministik dari posisi gridnya: denah yang sama selalu
    /// menghasilkan ruangan yang sama, tetapi ruangan yang berbeda benar-benar
    /// berbeda. Prop-nya hanya memakai tile yang sudah diverifikasi transparan
    /// (bukan 358-362 yang berlatar kotak pucat).
    /// </summary>
    enum Archetype { Aula, Grotto, Pekuburan, Reruntuhan, Kosong }

    void DecorateArchetype(Transform room, Cell c, List<Vector2> keepOut, float hx, float hy)
    {
        // hash posisi -> arketipe; deterministik tapi tersebar
        int h = c.g.x * 73856093 ^ c.g.y * 19349663 ^ (int)profile.layout * 83492791;
        var arch = (Archetype)(Mathf.Abs(h) % 5);

        // Lapisan prop art AI di atas prop tile: tiap ruangan mendapat beberapa
        // benda detail-tinggi supaya lingkungan mendekati mockup, bukan cuma tile.
        if (PropLibrary.Ready)
        {
            var lib = PropLibrary.I;
            switch (arch)
            {
                // hanya prop yang jelas terbaca; vines & pillar patah (grey pucat,
                // washout di lantai) tidak dipakai sebagai fitur lantai
                case Archetype.Grotto:
                    ArtFeature(room, c, keepOut, PropLibrary.Gem(), 1.4f, false);
                    ArtFeature(room, c, keepOut, lib.pots, 1.3f, false);
                    break;
                case Archetype.Pekuburan:
                    ArtFeature(room, c, keepOut, lib.skeleton, 1.8f, false);
                    ArtFeature(room, c, keepOut, PropLibrary.Gem(), 1.3f, false);
                    break;
                case Archetype.Reruntuhan:
                    ArtFeature(room, c, keepOut, PropLibrary.Rubble(), 1.6f, true);
                    ArtFeature(room, c, keepOut, lib.pots, 1.3f, false);
                    break;
                case Archetype.Aula:
                    ArtFeature(room, c, keepOut, lib.statue, 2.6f, true);
                    break;
                default:
                    ArtFeature(room, c, keepOut, lib.skeleton, 1.8f, false);
                    break;
            }
        }

        // Prop utama tiap ruangan adalah prop art AI (blok di atas). Tile Kenney
        // hanya taburan kecil pelengkap -- SEDIKIT saja. Versi lama menabur banyak
        // batu coklat + pilar putih yang terbaca sebagai noise ("berantakan,
        // nyangkut"), jadi rock & pilar Kenney tidak lagi ditabur bila prop AI ada.
        if (PropLibrary.Ready)
        {
            switch (arch)
            {
                case Archetype.Grotto:
                    ClusterScatter(room, c, keepOut, 2, Tiles.RL.Mushrooms, false);
                    break;
                case Archetype.Pekuburan:
                    ClusterScatter(room, c, keepOut, 2, Tiles.RL.Bones, false);
                    break;
                // Aula/Reruntuhan/Kosong: cukup prop AI, biar lapang & tak berantakan
            }
            return;
        }

        // ---- fallback tanpa prop AI: pakai tile Kenney seperti dulu ----
        switch (arch)
        {
            case Archetype.Aula:
                for (float sx = -1; sx <= 1; sx += 2)
                    for (int r = 0; r < 3; r++)
                    {
                        float px = sx * (hx - 5f);
                        float py = Mathf.Lerp(-hy + 4f, hy - 4f, r / 2f);
                        Prop(room, new Vector2(px, py + 0.5f), ArtLibrary.R(Tiles.RL.PillarTop), 3, true);
                        Prop(room, new Vector2(px, py - 0.5f), ArtLibrary.R(Tiles.RL.PillarBottom), 3, false);
                        keepOut.Add(new Vector2(px, py));
                    }
                break;
            case Archetype.Grotto:
                ClusterScatter(room, c, keepOut, 4, Tiles.RL.Mushrooms, false);
                ClusterScatter(room, c, keepOut, 3, Tiles.RL.Rocks, true);
                break;
            case Archetype.Pekuburan:
                ClusterScatter(room, c, keepOut, 5, Tiles.RL.Bones, false);
                break;
            case Archetype.Reruntuhan:
                ClusterScatter(room, c, keepOut, 4, Tiles.RL.Rocks, true);
                break;
            default:
                ClusterScatter(room, c, keepOut, 2, Tiles.RL.Mushrooms, false);
                break;
        }
    }

    /// <summary>Taruh satu prop art AI di titik kosong acak; catat keep-out-nya.</summary>
    void ArtFeature(Transform room, Cell c, List<Vector2> keepOut, Sprite sprite, float height, bool solid)
    {
        if (sprite == null) return;
        float hx = c.size.x * 0.5f - 3f, hy = c.size.y * 0.5f - 3f;
        for (int a = 0; a < 20; a++)
        {
            var p = new Vector2(Random.Range(-hx, hx), Random.Range(-hy, hy));
            bool ok = true;
            foreach (var k in keepOut) if (Vector2.Distance(p, k) < 4f) { ok = false; break; }
            if (!ok) continue;
            keepOut.Add(p);
            PropArt(room, p, sprite, height, solid ? 3 : 2, solid);
            return;
        }
    }

    /// <summary>Menabur satu rumpun prop dari satu jenis di titik acak.</summary>
    void ClusterScatter(Transform room, Cell c, List<Vector2> keepOut, int clusters, int[] set, bool solid)
    {
        if (set == null || set.Length == 0) return;
        float hx = c.size.x * 0.5f - 2.5f, hy = c.size.y * 0.5f - 2.5f;

        for (int ci = 0; ci < clusters; ci++)
        {
            Vector2 center = Vector2.zero;
            bool ok = false;
            for (int a = 0; a < 20 && !ok; a++)
            {
                center = new Vector2(Random.Range(-hx, hx), Random.Range(-hy, hy));
                ok = true;
                foreach (var k in keepOut) if (Vector2.Distance(center, k) < 4.5f) { ok = false; break; }
            }
            if (!ok) continue;

            int members = Random.Range(2, 5);
            int primary = set[Random.Range(0, set.Length)];
            for (int m = 0; m < members; m++)
            {
                Vector2 p = m == 0 ? center : center + Random.insideUnitCircle * Random.Range(1f, 2.4f);
                if (Mathf.Abs(p.x) > hx || Mathf.Abs(p.y) > hy) continue;
                keepOut.Add(p);
                int idx = Random.value < 0.7f ? primary : set[Random.Range(0, set.Length)];
                var go = Prop(room, p, ArtLibrary.R(idx), solid ? 3 : 2, solid);
                if (go != null) go.transform.localScale *= Random.Range(0.85f, 1.2f);
            }
        }
    }

    /// <summary>Properti padat (bertabrakan) -- membuat ruangan punya navigasi.</summary>
    int[] SolidSet() => Tiles.RL.Rocks;

    /// <summary>Properti hias (tanpa tabrakan) -- rumput, jamur, tumpahan koin.</summary>
    int[] DecorSet()
    {
        // lantai terbuka condong ke jamur dan tanaman, lantai dalam ke tulang dan kristal
        if (profile.outdoor) return Concat(Tiles.RL.Mushrooms, Tiles.RL.Rocks);
        return Concat(Tiles.RL.Bones, Tiles.RL.Crystals);
    }

    static int[] Concat(int[] a, int[] b)
    {
        var r = new int[a.Length + b.Length];
        a.CopyTo(r, 0); b.CopyTo(r, a.Length);
        return r;
    }

    /// <summary>
    /// Menabur properti secara BERUMPUN, bukan merata. Taburan seragam membuat
    /// ruangan terlihat seperti kisi acak; rumpun menghasilkan bentuk yang terbaca
    /// sebagai rumpun pohon, tumpukan puing, atau sudut yang ditinggalkan.
    /// </summary>
    void ScatterFrom(Transform room, Cell c, List<Vector2> keepOut, int count, int[] set, bool solid)
    {
        if (set == null || set.Length == 0 || count <= 0) return;
        float hx = c.size.x * 0.5f - 2.5f, hy = c.size.y * 0.5f - 2.5f;

        int clusters = Mathf.Max(1, Mathf.RoundToInt(count / 2.5f));
        int placed = 0;

        for (int ci = 0; ci < clusters && placed < count; ci++)
        {
            // cari pusat rumpun yang tidak menabrak pintu / tengah ruangan
            Vector2 center = Vector2.zero;
            bool ok = false;
            for (int attempt = 0; attempt < 24 && !ok; attempt++)
            {
                center = new Vector2(Random.Range(-hx, hx), Random.Range(-hy, hy));
                ok = true;
                foreach (var k in keepOut)
                    if (Vector2.Distance(center, k) < 5.0f) { ok = false; break; }
            }
            if (!ok) continue;

            // satu rumpun memakai jenis yang senada supaya terbaca menyatu
            int primary = set[Random.Range(0, set.Length)];
            int members = Random.Range(2, 5);

            for (int m = 0; m < members && placed < count; m++)
            {
                Vector2 p = m == 0 ? center : center + Random.insideUnitCircle * Random.Range(1.2f, 2.8f);
                if (Mathf.Abs(p.x) > hx || Mathf.Abs(p.y) > hy) continue;

                bool clear = true;
                foreach (var k in keepOut)
                    if (Vector2.Distance(p, k) < 1.3f) { clear = false; break; }
                if (!clear) continue;

                keepOut.Add(p);

                // sebagian besar anggota memakai jenis utama, sisanya variasi
                int idx = (Random.value < 0.7f) ? primary : set[Random.Range(0, set.Length)];
                var sprite = ArtLibrary.R(idx);
                var go = Prop(room, p, sprite, solid ? 3 : 2, solid);

                // ukuran sedikit berbeda-beda -> rumpun tidak terlihat seperti stempel
                if (go != null) go.transform.localScale *= Random.Range(0.85f, 1.2f);
                placed++;
            }
        }
    }

    /// <summary>Obor: sprite api + bola cahaya berkedip di atas selubung gelap.</summary>
    void AddTorch(Transform room, Vector2 localPos)
    {
        GameObject brazier;
        float flameY;   // tinggi nyala di atas titik tanam, dalam unit dunia

        // brazier art AI bila tersedia; jatuh ke tile Kenney bila belum dibangun
        if (PropLibrary.Ready && PropLibrary.I.brazier != null)
        {
            brazier = PropArt(room, localPos, PropLibrary.I.brazier, 1.9f, 6, false);
            flameY = 1.2f;   // nyala di bagian atas brazier
        }
        else
        {
            brazier = Prop(room, localPos, ArtLibrary.R(Tiles.RL.Brazier), 6, false);
            flameY = 0.4f;
        }

        // brazier bergoyang halus supaya apinya terasa hidup, bukan gambar diam
        if (brazier != null)
        {
            var sway = brazier.AddComponent<FlameSway>();
            sway.amount = 0.05f; sway.speed = 5.5f;
        }

        // SATU cahaya lembut per brazier, alpha DITURUNKAN: banyak brazier per
        // ruangan menumpuk secara aditif; versi sebelumnya (cahaya + inti terang
        // 0.85) membuat seluruh lantai membanjir oranye. Animasi api ada di
        // FlameSway pada sprite, bukan dari cahaya terang.
        var c = profile.lightColor;
        c.a *= 0.55f;                              // redam supaya tumpukan tidak menyilaukan
        var glow = RoomDressing.AddLight(room, localPos + new Vector2(0f, flameY * 0.4f),
                                         c, profile.lightRadius * 0.85f, 6);
        var gf = glow.AddComponent<TorchFlicker>();
        gf.baseAlpha = c.a; gf.amplitude = 0.10f; gf.speed = 5f;
    }

    /// <summary>
    /// Menaruh prop art AI dengan tinggi dunia tertentu. Prop ini ilustrasi
    /// resolusi tinggi (PPU 100), jadi ukurannya diatur lewat tinggi target,
    /// bukan skala tetap seperti tile 16px. Diurutkan menurut Y agar pemain
    /// bisa lewat di belakangnya.
    /// </summary>
    GameObject PropArt(Transform parent, Vector2 localPos, Sprite sprite, float targetHeight, int order, bool solid)
    {
        if (sprite == null) return null;

        var go = new GameObject(solid ? "PropArtSolid" : "PropArt");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        float h = sprite.bounds.size.y;
        float s = h > 0.01f ? targetHeight / h : 1f;
        go.transform.localScale = new Vector3(s, s, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = solid ? YSort.Order(parent.position.y + localPos.y) : order;

        // Bayangan lembut di kaki prop: memisahkan prop dari lantai supaya tidak
        // menyatu/washout (keluhan "prop terlihat trash / low contrast"). Skala
        // bayangan dalam ruang LOKAL prop (yang sudah diperkecil), jadi dibagi s.
        var sh = new GameObject("Shadow");
        sh.transform.SetParent(go.transform, false);
        float shW = sprite.bounds.size.x * 0.6f;
        sh.transform.localScale = new Vector3(shW, shW * 0.42f, 1f);
        sh.transform.localPosition = new Vector3(0f, -sprite.bounds.size.y * 0.46f, 0f);
        var shr = sh.AddComponent<SpriteRenderer>();
        shr.sprite = RoomDressing.Glow();
        shr.color = new Color(0f, 0f, 0f, 0.32f);
        shr.sortingOrder = (solid ? YSort.Order(parent.position.y + localPos.y) : order) - 1;

        if (solid)
        {
            // collider pendek di kaki prop: yang menabrak alasnya, bukan pucuknya
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(sprite.bounds.size.x * 0.55f, sprite.bounds.size.y * 0.28f);
            col.offset = new Vector2(0f, -sprite.bounds.size.y * 0.34f);
        }
        return go;
    }

    GameObject Prop(Transform parent, Vector2 localPos, Sprite sprite, int order, bool solid)
    {
        if (sprite == null) return null;
        var go = new GameObject(solid ? "Obstacle" : "Prop");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one * (solid ? 1.6f : 1.2f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        // Properti padat diurutkan menurut Y sekali di sini (posisinya tak pernah
        // berubah), supaya karakter bisa lewat di belakang pohon dan pilar.
        sr.sortingOrder = solid ? YSort.Order(parent.position.y + localPos.y) : order;

        if (solid)
        {
            var col = go.AddComponent<BoxCollider2D>();
            // collider lebih pendek dari sprite: kaki pohon yang menabrak, bukan pucuknya
            col.size = new Vector2(0.85f, 0.55f);
            col.offset = new Vector2(0f, -0.2f);
        }
        return go;
    }

    /// <summary>Pedagang hantu di ruangan Shop. Logikanya ada di ShopVendor.</summary>
    void MakeVendor(Transform parent, Vector2 pos)
    {
        var go = new GameObject("MbahWarung");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * 2.6f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ArtLibrary.D(100);                       // sosok tua Tiny Dungeon
        sr.sortingOrder = YSort.Order(parent.position.y + pos.y);
        sr.color = new Color(0.72f, 0.95f, 0.92f, 0.88f);    // pucat kehijauan: ia lelembut

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 1.1f;

        RoomDressing.AddLight(parent, pos, new Color(0.35f, 0.85f, 0.80f, 0.45f), 4f, 6);
        go.AddComponent<ShopVendor>();
    }

    /// <summary>Altar sesaji. Logikanya ada di Shrine.</summary>
    void MakeShrine(Transform parent, Vector2 pos)
    {
        var go = new GameObject("SanggarSesaji");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * 2.2f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ArtLibrary.R(Tiles.RL.PillarTop);
        sr.sortingOrder = YSort.Order(parent.position.y + pos.y);

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 1.3f;

        RoomDressing.AddLight(parent, pos, new Color(0.30f, 0.90f, 0.82f, 0.50f), 4.5f, 6);
        go.AddComponent<Shrine>();
    }

    void MakeChest(Transform parent, Vector2 pos, int rolls)
    {
        var go = Prop(parent, pos, ArtLibrary.R(Tiles.RL.Chest), 3, false);
        if (go == null) return;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.6f, 1.6f);
        var chest = go.AddComponent<Chest>();
        chest.rolls = Mathf.Max(1, rolls);
    }

    void AddSpawnPoints(Transform parent, Room room, Cell c)
    {
        int n = Mathf.Max(4, room.enemyCount);
        var pts = new Transform[n];
        float rx = c.size.x * 0.30f, ry = c.size.y * 0.30f;
        for (int i = 0; i < n; i++)
        {
            float a = (i / (float)n) * Mathf.PI * 2f;
            var go = new GameObject("Spawn");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry, 0f);
            pts[i] = go.transform;
        }
        room.spawnPoints = pts;
    }

    // ---------------------------------------------------------------- pintu
    Vector2 DoorOffset(Cell c, Vector2Int dir, float hx, float hy)
        => new Vector2(Mathf.Clamp(dir.x, -1, 1) * (hx - 1.4f), Mathf.Clamp(dir.y, -1, 1) * (hy - 1.4f));

    void AddDoor(Cell from, Cell to)
    {
        var dir = to.g - from.g;
        float hx = from.size.x * 0.5f, hy = from.size.y * 0.5f;
        var local = DoorOffset(from, dir, hx, hy);

        var doorGO = new GameObject($"Door_to_{to.g.x}_{to.g.y}");
        doorGO.transform.SetParent(from.room.transform, false);
        doorGO.transform.localPosition = local;

        var col = doorGO.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = Mathf.Abs(dir.x) > 0 ? new Vector2(1.8f, 4.5f) : new Vector2(4.5f, 1.8f);

        var door = doorGO.AddComponent<Door>();
        door.targetRoom = to.room;

        var vis = new GameObject("DoorVis");
        vis.transform.SetParent(doorGO.transform, false);
        vis.transform.localScale = Vector3.one * 1.6f;
        var sr = vis.AddComponent<SpriteRenderer>();
        sr.sprite = ArtLibrary.R(Tiles.RL.DoorWood);
        sr.sortingOrder = 6;
        door.visual = sr;

        // titik masuk di sisi seberang ruangan tujuan
        var entry = new GameObject($"Entry_from_{from.g.x}_{from.g.y}");
        entry.transform.SetParent(to.room.transform, false);
        entry.transform.localPosition = new Vector3(
            -Mathf.Clamp(dir.x, -1, 1) * (to.size.x * 0.5f - 3.2f),
            -Mathf.Clamp(dir.y, -1, 1) * (to.size.y * 0.5f - 3.2f), 0f);
        door.entryPoint = entry.transform;
    }

    // -------------------------------------------------------------- utility
    GameObject Tiled(Transform parent, string name, Vector2 localPos, Vector2 size, Sprite sprite, int order, bool collider)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        if (sprite != null) { sr.drawMode = SpriteDrawMode.Tiled; sr.size = size; }

        if (collider)
        {
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
        }
        return go;
    }

    void Tint(GameObject go, Color c)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = c;
    }
}
