using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Membangun seluruh aset konten di Assets/Resources dari data DESIGN_BIBLE.md:
/// ArtLibrary, ItemDatabase (40 item), HeroCatalog (3 hero), FloorCatalog (5 lantai).
///
/// Aset-aset ini harus ada di Resources karena dibaca saat runtime oleh generator
/// dungeon dan sistem tas, di mana AssetDatabase tidak tersedia.
///
/// Jalankan: Tools > Pusaka > Rebuild Content
/// </summary>
public static class ContentBuilder
{
    const string ResDir = "Assets/Resources/";
    const string DunTiles = "Assets/Art/Kenney/TinyDungeon/Tiles/";
    const string TownTiles = "Assets/Art/Kenney/TinyTown/Tiles/";
    const string RogueTiles = "Assets/Art/Kenney/RoguelikeDungeon/Tiles/";
    const string PrefabDir = "Assets/Prefabs/Forge/";

    /// <summary>
    /// Satu tombol untuk seluruh pipeline, dengan urutan yang benar:
    /// prefab karakter -> aset konten -> scene lantai.
    /// Urutan ini wajib: FloorCatalog menunjuk prefab, dan FloorBuilder membaca FloorCatalog.
    /// </summary>
    [MenuItem("Tools/Pusaka/Rebuild EVERYTHING", false, -100)]
    public static void RebuildEverything()
    {
        ForgeImporter.Import();
        RebuildAll();

        // Scene menu ikut dibangun di sini. Sebelumnya hanya lantai yang dibangun,
        // sehingga perbaikan tata letak MainMenu / Game Over / Victory tidak pernah
        // terpakai kecuali pengguna kebetulan tahu harus menjalankan Build Menus.
        // Urutannya: menu dulu, lantai terakhir, supaya FloorBuilder yang menutup
        // Build Settings dengan daftar lengkapnya.
        PusakaSceneBuilder.BuildMenus();
        FloorBuilder.BuildFloors();

        Debug.Log("PUSAKA: seluruh pipeline selesai (prefab, konten, menu, lantai).");
    }

    [MenuItem("Tools/Pusaka/Rebuild Content")]
    public static void RebuildAll()
    {
        Directory.CreateDirectory(ResDir);
        BuildArtLibrary();
        BuildPropLibrary();
        BuildItemDatabase();
        BuildHeroCatalog();
        BuildFloorCatalog();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CONTENT: selesai. ArtLibrary + ItemDatabase + HeroCatalog + FloorCatalog dibangun.");
    }

    // ------------------------------------------------------------ ArtLibrary
    static void BuildArtLibrary()
    {
        var lib = LoadOrCreate<ArtLibrary>(ResDir + "ArtLibrary.asset");
        lib.dungeon = LoadTiles(DunTiles);
        lib.town = LoadTiles(TownTiles);
        lib.rogue = LoadTiles(RogueTiles);
        EditorUtility.SetDirty(lib);

        int dun = CountNonNull(lib.dungeon), town = CountNonNull(lib.town), rogue = CountNonNull(lib.rogue);
        Debug.Log($"CONTENT: ArtLibrary -> {rogue} tile roguelike, {dun} tile dungeon, {town} tile town.");
        if (rogue == 0)
            Debug.LogError("CONTENT: tileset roguelike kosong. Pastikan folder " + RogueTiles + " terimpor. " +
                           "Ini tileset lingkungan utama -- tanpa ini ruangan tidak punya lantai.");
    }

    static Sprite[] LoadTiles(string dir)
    {
        var list = new List<Sprite>();
        // batas 600: pack roguelike punya 522 tile, jauh di atas 132 milik pack Tiny
        for (int i = 0; i < 600; i++)
        {
            string path = $"{dir}tile_{i:0000}.png";
            if (!File.Exists(path)) break;
            list.Add(AssetDatabase.LoadAssetAtPath<Sprite>(path));
        }
        return list.ToArray();
    }

    static int CountNonNull(Sprite[] a)
    {
        if (a == null) return 0;
        int n = 0;
        foreach (var s in a) if (s != null) n++;
        return n;
    }

    // --------------------------------------------------------- ItemDatabase
    static ItemEffect E(EffectKind k, float v, float dur = 0f) => new ItemEffect(k, v, dur);

    static ItemDef Item(string id, string name, ItemCategory cat, Rarity rar,
                        int tile, int price, string flavor, params ItemEffect[] fx)
        => new ItemDef
        {
            id = id,
            displayName = name,
            category = cat,
            rarity = rar,
            tileIndex = tile,
            fromTown = false,
            price = price,
            flavor = flavor,
            effects = fx,
            stackMax = cat == ItemCategory.Consumable ? 5 : (cat == ItemCategory.Material ? 99 : 1),
        };

    static void BuildItemDatabase()
    {
        // Catatan konversi dari design bible:
        // - "add_armor N" (pengurangan flat) dipetakan ke Armor persen: N * 5%.
        // - Verb yang belum punya mesinnya (on_hit status, aura, reveal_map,
        //   cleanse, immune) diganti efek setara terdekat; ditandai di komentar.
        var items = new List<ItemDef>
        {
            // ---------------------------------------------------------- senjata
            Item("keris_lurus", "Keris Lurus", ItemCategory.Weapon, Rarity.Umum, 105, 15,
                 "Bilah lurus tanpa luk milik prajurit rendahan; tetap setia meski tuannya tiada.",
                 E(EffectKind.Damage, 4)),
            Item("busur_bambu", "Busur Bambu", ItemCategory.Weapon, Rarity.Umum, 131, 15,
                 "Busur latihan dari bambu apus. Ringan, jujur, dan tidak pernah mengeluh.",
                 E(EffectKind.Damage, 3), E(EffectKind.Range, 0.5f)),
            Item("tongkat_cendana", "Tongkat Cendana", ItemCategory.Weapon, Rarity.Umum, 130, 15,
                 "Kayu cendana wangi yang menyimpan sisa doa para resi terdahulu.",
                 E(EffectKind.Damage, 4)),
            Item("pedang_suduk", "Pedang Suduk", ItemCategory.Weapon, Rarity.Langka, 104, 30,
                 "Pedang pendek pengawal kedhaton, diasah untuk lorong sempit.",
                 E(EffectKind.Damage, 7)),
            Item("tombak_runcing", "Tombak Runcing", ItemCategory.Weapon, Rarity.Langka, 106, 30,
                 "Mata tombak bregada; jarak adalah zirah yang paling murah.",
                 E(EffectKind.Damage, 5), E(EffectKind.Range, 0.4f)),
            Item("gada_wesi", "Gada Wesi", ItemCategory.Weapon, Rarity.Langka, 107, 30,
                 "Berat di tangan, lebih berat lagi di kepala lawan.",
                 E(EffectKind.Damage, 8), E(EffectKind.AttackSpeed, -0.10f)),
            Item("keris_luk_sanga", "Keris Luk Sanga", ItemCategory.Weapon, Rarity.Pusaka, 105, 60,
                 "Sembilan lekuk berlumur warangan; lukanya kecil, tetapi tidak pernah sembuh sendiri.",
                 E(EffectKind.Damage, 9), E(EffectKind.DamagePct, 0.08f)),   // pengganti on_hit racun
            Item("panah_geni", "Panah Geni", ItemCategory.Weapon, Rarity.Pusaka, 131, 60,
                 "Anak panah bersumbu api dari upacara labuhan yang gagal.",
                 E(EffectKind.Damage, 6), E(EffectKind.DamagePct, 0.10f)),   // pengganti on_hit bakar
            Item("candrasa_kelam", "Candrasa Kelam", ItemCategory.Weapon, Rarity.Sakti, 103, 120,
                 "Bilah wayang yang jatuh ke dunia; meminum apa yang ia lukai.",
                 E(EffectKind.Damage, 10), E(EffectKind.LifeSteal, 0.08f)),
            Item("cakra_baskara", "Cakra Baskara", ItemCategory.Weapon, Rarity.Sakti, 118, 120,
                 "Roda matahari kecil; berputar paling tajam tepat sebelum senja.",
                 E(EffectKind.Damage, 12), E(EffectKind.CritChance, 0.10f)),

            // ----------------------------------------------------- zirah & jimat
            Item("baju_lurik", "Baju Lurik", ItemCategory.Armor, Rarity.Umum, 66, 15,
                 "Tenun lurik petani; garis-garisnya dianyam dengan sabar dan doa.",
                 E(EffectKind.MaxHp, 10)),
            Item("jarik_parang", "Jarik Parang", ItemCategory.Armor, Rarity.Langka, 66, 30,
                 "Motif parang hanya untuk kerabat raja. Kain ini tahu siapa yang pantas.",
                 E(EffectKind.MaxHp, 15)),
            Item("sabuk_epek_timang", "Sabuk Epek Timang", ItemCategory.Armor, Rarity.Langka, 65, 30,
                 "Sabuk upacara berkepala kuningan; menegakkan punggung dan nyali.",
                 E(EffectKind.Armor, 0.05f)),
            Item("gelang_akar_bahar", "Gelang Akar Bahar", ItemCategory.Charm, Rarity.Langka, 101, 30,
                 "Akar laut hitam penolak bala; yang menyentuh kasar akan tergores balik.",
                 E(EffectKind.Thorns, 3)),
            Item("blangkon_sukma", "Blangkon Sukma", ItemCategory.Charm, Rarity.Pusaka, 64, 60,
                 "Blangkon milik abdi dalem yang arif; lipatannya menyimpan ingatan.",
                 E(EffectKind.Armor, 0.05f), E(EffectKind.XpGain, 0.15f)),
            Item("selop_kilat", "Selop Kilat", ItemCategory.Charm, Rarity.Pusaka, 62, 60,
                 "Selop sutra penari srimpi; lantai seakan ikut melangkah.",
                 E(EffectKind.MoveSpeed, 0.4f)),
            Item("kalung_jimat_aksara", "Kalung Jimat Aksara", ItemCategory.Charm, Rarity.Pusaka, 64, 60,
                 "Rajah aksara Jawa dalam bungkus kain mori; membelokkan niat jahat.",
                 E(EffectKind.Dodge, 0.12f)),
            Item("zirah_bregada", "Zirah Bregada", ItemCategory.Armor, Rarity.Sakti, 66, 120,
                 "Zirah pasukan bregada keraton. Berat, tetapi begitu pula kesetiaan.",
                 E(EffectKind.Armor, 0.15f), E(EffectKind.MaxHp, 25), E(EffectKind.MoveSpeed, -0.2f)),
            Item("batik_sidomukti", "Batik Sidomukti", ItemCategory.Charm, Rarity.Sakti, 66, 120,
                 "Sidomukti: doa agar hidup mulia. Kain ini mendoakan pemakainya setiap langkah.",
                 E(EffectKind.MaxHp, 20), E(EffectKind.XpGain, 0.20f)),

            // ------------------------------------------------------- consumable
            Item("jamu_kunyit_asam", "Jamu Kunyit Asam", ItemCategory.Consumable, Rarity.Umum, 115, 15,
                 "Pahit di lidah, hangat di dada. Resep mbok jamu yang tak pernah salah.",
                 E(EffectKind.Heal, 30)),
            Item("jamu_beras_kencur", "Jamu Beras Kencur", ItemCategory.Consumable, Rarity.Umum, 113, 15,
                 "Putih susu, manis pedas; kaki terasa dua puluh tahun lebih muda.",
                 E(EffectKind.Heal, 15), E(EffectKind.MoveSpeed, 0.5f, 8f)),
            Item("tape_ketan", "Tape Ketan", ItemCategory.Consumable, Rarity.Umum, 114, 15,
                 "Ketan hijau fermentasi daun katuk; sedikit memabukkan, sangat memberanikan.",
                 E(EffectKind.Heal, 20), E(EffectKind.DamagePct, 0.10f, 8f)),
            Item("wedang_ronde", "Wedang Ronde", ItemCategory.Consumable, Rarity.Langka, 127, 30,
                 "Kuah jahe panas berisi ronde kenyal; malam paling dingin pun menyerah.",
                 E(EffectKind.Heal, 50)),
            Item("sekar_setaman", "Sekar Setaman", ItemCategory.Consumable, Rarity.Langka, 56, 30,
                 "Bokor kembang tujuh rupa; membasuh yang tidak tampak oleh mata.",
                 E(EffectKind.Heal, 20)),
            Item("lisah_telon", "Lisah Telon", ItemCategory.Consumable, Rarity.Langka, 126, 30,
                 "Minyak tiga rupa penjaga bayi; racun tua pun segan mendekat.",
                 E(EffectKind.Armor, 0.20f, 20f)),                            // pengganti immune racun
            Item("gudeg_komplit", "Gudeg Komplit", ItemCategory.Consumable, Rarity.Langka, 66, 30,
                 "Nangka muda dimasak semalaman, krecek, telur pindang. Alasan pulang ke Ngayogyakarta.",
                 E(EffectKind.Heal, 60)),
            Item("kopi_jos", "Kopi Jos", ItemCategory.Consumable, Rarity.Pusaka, 125, 60,
                 "Kopi ditanggap arang membara - jos! Jantung ikut menabuh kendhang.",
                 E(EffectKind.AttackSpeed, 0.30f, 12f)),
            Item("wedang_uwuh", "Wedang Uwuh", ItemCategory.Consumable, Rarity.Pusaka, 127, 60,
                 "Ramuan dedaunan Imogiri, merah secang; api luar dilawan api dalam.",
                 E(EffectKind.Heal, 35), E(EffectKind.Armor, 0.20f, 15f)),

            // ------------------------------------------------------------ pusaka
            Item("keris_kyai_sengkelat", "Keris Kyai Sengkelat", ItemCategory.Pusaka, Rarity.Legenda, 105, 250,
                 "Keris agung berpamor api. Ia memilih tangan yang menggenggamnya, bukan sebaliknya.",
                 E(EffectKind.Damage, 15), E(EffectKind.DamagePct, 0.12f)),
            Item("tombak_kyai_pleret", "Tombak Kyai Pleret", ItemCategory.Pusaka, Rarity.Legenda, 106, 250,
                 "Tombak pusaka Mataram; ujungnya pernah menentukan arah sejarah.",
                 E(EffectKind.Damage, 12), E(EffectKind.Range, 0.8f)),
            Item("songsong_agung", "Songsong Agung", ItemCategory.Pusaka, Rarity.Legenda, 129, 250,
                 "Payung kebesaran sultan. Yang bernaung di bawahnya berjalan di antara tetes takdir.",
                 E(EffectKind.Dodge, 0.20f), E(EffectKind.Armor, 0.10f)),
            Item("gong_kyai_sekati", "Gong Kyai Sekati", ItemCategory.Pusaka, Rarity.Legenda, 66, 250,
                 "Gong perayaan sekaten; gemanya membuat waktu ikut menunduk pelan.",
                 E(EffectKind.Armor, 0.10f), E(EffectKind.MaxHp, 15)),         // pengganti aura perlambat
            Item("kembang_wijayakusuma", "Kembang Wijayakusuma", ItemCategory.Pusaka, Rarity.Legenda, 130, 250,
                 "Kembang yang hanya mekar tengah malam dan hanya untuk yang belum selesai urusannya.",
                 E(EffectKind.Revive, 0.5f)),
            Item("cupu_manik_astagina", "Cupu Manik Astagina", ItemCategory.Pusaka, Rarity.Legenda, 56, 250,
                 "Cupu wasiat yang memperlihatkan apa yang seharusnya tidak terlihat.",
                 E(EffectKind.XpGain, 0.25f), E(EffectKind.Luck, 1f)),         // pengganti reveal_map
            Item("selendang_nawangwulan", "Selendang Nawangwulan", ItemCategory.Pusaka, Rarity.Legenda, 62, 250,
                 "Selendang bidadari yang dahulu dicuri Jaka Tarub; kini ia tahu rasanya dicuri.",
                 E(EffectKind.MoveSpeed, 0.5f), E(EffectKind.Dodge, 0.10f)),
            Item("batu_akik_geni", "Batu Akik Geni", ItemCategory.Charm, Rarity.Sakti, 102, 120,
                 "Akik merah delima yang menyimpan satu bara kecil dari gunung.",
                 E(EffectKind.CritChance, 0.10f), E(EffectKind.LifeSteal, 0.05f)),

            // -------------------------------------------------- bahan & mata uang
            Item("picis_kuno", "Picis Kuno", ItemCategory.Consumable, Rarity.Umum, 101, 1,
                 "Koin berlubang segi empat dari zaman yang lupa namanya sendiri.",
                 E(EffectKind.Gold, 1)),
            Item("kantong_picis", "Kantong Picis", ItemCategory.Consumable, Rarity.Langka, 66, 10,
                 "Kantong kain berisi picis; bunyi gemerincingnya menghibur di lorong gelap.",
                 E(EffectKind.Gold, 10)),
            Item("wesi_aji", "Wesi Aji", ItemCategory.Consumable, Rarity.Pusaka, 102, 60,
                 "Besi bintang jatuh, bahan pamor para empu. Menempa ulang senjata di tangan.",
                 E(EffectKind.Damage, 2)),                                     // permanen: duration 0
            Item("mote_sukma", "Mote Sukma", ItemCategory.Consumable, Rarity.Pusaka, 128, 60,
                 "Manik cahaya dari jiwa yang akhirnya lega. Hangat di genggaman.",
                 E(EffectKind.MaxHp, 2)),                                      // permanen: duration 0
        };

        // Ikon art AI (Assets/Art/Generated/Items) menggantikan tile untuk item
        // terpilih. Referensi Sprite di-serialize langsung ke aset ItemDatabase,
        // jadi tidak perlu ditaruh di Resources.
        ApplyIcons(items, new (string, string)[]
        {
            ("keris_kyai_sengkelat", "keris_celestial"),
            ("candrasa_kelam",       "sword_green"),
            ("tombak_kyai_pleret",   "spear_vitality"),
            ("gada_wesi",            "mace_barong"),
            ("busur_bambu",          "bow"),
            ("sabuk_epek_timang",    "shield_barong"),
            ("blangkon_sukma",       "helm_garuda"),
            ("jarik_parang",         "pauldron_ganesha"),
            ("selop_kilat",          "boot_wing"),
            ("zirah_bregada",        "greaves"),
            ("batik_sidomukti",      "robe_gamelan"),
        });

        var db = LoadOrCreate<ItemDatabase>(ResDir + "ItemDatabase.asset");
        db.items = items.ToArray();
        EditorUtility.SetDirty(db);
        Debug.Log($"CONTENT: ItemDatabase -> {items.Count} item.");
    }

    const string ItemArtDir = "Assets/Art/Generated/Items/";
    const string PropArtDir = "Assets/Art/Generated/Props/";

    static Sprite Prop(string name)
    {
        string path = PropArtDir + name + ".png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogWarning("CONTENT: prop '" + name + "' tak ditemukan");
        return s;
    }

    static void BuildPropLibrary()
    {
        var lib = LoadOrCreate<PropLibrary>(ResDir + "PropLibrary.asset");
        lib.brazier = Prop("brazier_fire");
        lib.statue = Prop("statue_ganesha");
        lib.mask = Prop("mask_barong");
        lib.gamelan = Prop("gamelan");
        lib.pillarBroken = Prop("pillar_broken");
        lib.vines = Prop("vines");
        lib.skeleton = Prop("skeleton");
        lib.cobweb = Prop("cobweb");
        lib.pots = Prop("pots");
        lib.books = Prop("books");
        lib.rubble = new[] { Prop("rubble_rock"), Prop("rubble_pot") };
        lib.gems = new[] { Prop("crystal") };
        EditorUtility.SetDirty(lib);

        Debug.Log($"CONTENT: PropLibrary -> brazier {(lib.brazier != null ? "ok" : "HILANG")}, " +
                  $"statue {(lib.statue != null ? "ok" : "-")}, {lib.rubble.Length} rubble.");
    }

    static void ApplyIcons(List<ItemDef> items, (string id, string art)[] map)
    {
        int ok = 0;
        foreach (var (id, art) in map)
        {
            string path = ItemArtDir + art + ".png";
            // Paksa impor dulu: di batchmode bersih, PNG mungkin belum diimpor
            // saat metode ini jalan, sehingga LoadAssetAtPath mengembalikan null.
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) { Debug.LogWarning($"CONTENT: ikon '{art}' tak ditemukan di {path}"); continue; }
            foreach (var it in items) if (it.id == id) { it.iconOverride = sprite; ok++; break; }
        }
        Debug.Log($"CONTENT: {ok}/{map.Length} item memakai ikon art AI.");
    }

    // ---------------------------------------------------------- HeroCatalog
    static void BuildHeroCatalog()
    {
        var cat = LoadOrCreate<HeroCatalog>(ResDir + "HeroCatalog.asset");
        // Satu hero saja, sesuai permintaan. Sprite Archer dan Mage tetap terpakai,
        // tetapi sebagai musuh (Enemy_Archer / Enemy_Mage).
        cat.heroes = new[]
        {
            new HeroProfile
            {
                id = "senopati", displayName = "SENOPATI", role = "Panglima Keraton",
                blurb = "Panglima terakhir yang menolak tunduk ketika Patih Kelam merebut keraton. " +
                        "Kerisnya sederhana, tetapi amarahnya adalah pusaka tersendiri.",
                prefab = Prefab("Player_Warrior"),
                maxHp = 120, damage = 24, moveSpeed = 2.9f, attackRange = 1.5f, attackCooldown = 0.7f,
                startingItemId = "keris_lurus", portraitTile = 96,
            },
        };

        foreach (var h in cat.heroes)
            if (h.prefab == null)
                Debug.LogError($"CONTENT: prefab hero '{h.displayName}' tidak ditemukan. " +
                               "Jalankan Tools > Pusaka > Import Forge Characters dulu.");

        EditorUtility.SetDirty(cat);
        Debug.Log($"CONTENT: HeroCatalog -> {cat.heroes.Length} hero.");
    }

    // --------------------------------------------------------- FloorCatalog
    static void BuildFloorCatalog()
    {
        var cat = LoadOrCreate<FloorCatalog>(ResDir + "FloorCatalog.asset");
        cat.floors = new[]
        {
            new FloorProfile
            {
                sceneName = "Floor1", layout = FloorLayout.Bercabang, title = "PELATARAN BERINGIN",
                lore = "Dulu pelataran ini riuh oleh gamelan dan pedagang sekar. Kini akar beringin " +
                       "mencekik gapura, dan prajurit yang telah gugur masih berbaris menjaga tuan yang lama pergi.",
                outdoor = true,
                floorTint = new Color(0.64f, 0.69f, 0.54f), wallTint = Hex("#75855F"), skyTint = Hex("#2B2138"),
                lightColor = new Color(1f, 0.75f, 0.41f, 0.30f), lightRadius = 4.2f, ambientStrength = 0.55f,
                roomCount = 13, loopChance = 0.20f, propDensity = 1.2f, enemiesPerRoom = 5,
                enemies = Prefabs("Enemy_Slime_Poison", "Enemy_Plant", "Enemy_Warrior"),
                elites = Prefabs("Enemy_Warrior"),
                boss = Prefab("Boss_Orc"),
            },
            new FloorProfile
            {
                sceneName = "Floor2", layout = FloorLayout.Melingkar, title = "UMBUL PASIRAMAN",
                lore = "Di umbul ini para putri dahulu membasuh diri di bawah songsong emas. Airnya tidak " +
                       "pernah surut - ia mengingat setiap wajah yang pernah tenggelam di dalamnya.",
                outdoor = false,
                floorTint = new Color(0.59f, 0.75f, 0.76f), wallTint = Hex("#5F8A88"), skyTint = Hex("#12283A"),
                lightColor = new Color(0.43f, 0.82f, 0.92f, 0.26f), lightRadius = 4.0f, ambientStrength = 0.60f, water = true,
                roomCount = 15, loopChance = 0.25f, propDensity = 1.0f, enemiesPerRoom = 6,
                enemies = Prefabs("Enemy_Slime_Ice", "Enemy_Archer", "Enemy_Warrior", "Enemy_Slime_Poison"),
                elites = Prefabs("Enemy_Orc"),
                boss = Prefab("Boss_Golem"),
            },
            new FloorProfile
            {
                sceneName = "Floor3", layout = FloorLayout.Gua, title = "SUMUR GUMULING",
                lore = "Sumur berpilin ini dahulu tempat doa dipanjatkan, tangganya melingkar seperti tasbih batu. " +
                       "Kini doa-doa itu berbalik arah, dan sesuatu yang haus menunggu di dasar lingkarannya.",
                outdoor = false,
                floorTint = new Color(0.59f, 0.57f, 0.67f), wallTint = Hex("#6A657F"), skyTint = Hex("#0F0F1A"),
                lightColor = new Color(1f, 0.65f, 0.31f, 0.38f), lightRadius = 3.4f, ambientStrength = 0.74f,
                roomCount = 17, loopChance = 0.30f, propDensity = 0.8f, enemiesPerRoom = 7,
                enemies = Prefabs("Enemy_Orc", "Enemy_Mage", "Enemy_Slime_Ice", "Enemy_Archer"),
                elites = Prefabs("Enemy_Golem"),
                boss = Prefab("Boss_Vampire"),
            },
            new FloorProfile
            {
                sceneName = "Floor4", layout = FloorLayout.Tulang, title = "PULO KENONGO",
                lore = "Pulo ini dahulu harum oleh kembang kenanga yang mekar untuk sultan. Patih Kelam membakarnya " +
                       "guna menutup jejak, tetapi abunya menolak menjadi dingin.",
                outdoor = true,
                floorTint = new Color(0.69f, 0.59f, 0.55f), wallTint = Hex("#7A6763"), skyTint = Hex("#3A1A14"),
                lightColor = new Color(1f, 0.55f, 0.27f, 0.36f), lightRadius = 4.0f, ambientStrength = 0.60f,
                roomCount = 18, loopChance = 0.30f, propDensity = 1.1f, enemiesPerRoom = 8,
                enemies = Prefabs("Enemy_Slime_Fire", "Enemy_Golem", "Enemy_Orc", "Enemy_Mage", "Enemy_Archer"),
                elites = Prefabs("Enemy_Vampire"),
                boss = Prefab("Boss_Demon"),
            },
            new FloorProfile
            {
                sceneName = "Floor5", layout = FloorLayout.Petak, title = "GEDHONG PUSAKA",
                lore = "Inilah gedhong tempat pusaka keraton disemayamkan, kini penuh oleh barang curian yang menolak " +
                       "tuan barunya. Di tengah ruangan, sang patih menunggu.",
                outdoor = false,
                floorTint = new Color(0.75f, 0.67f, 0.49f), wallTint = Hex("#6E6250"), skyTint = Hex("#1A1426"),
                lightColor = new Color(1f, 0.80f, 0.47f, 0.32f), lightRadius = 4.6f, ambientStrength = 0.58f,
                roomCount = 20, loopChance = 0.32f, propDensity = 1.0f, enemiesPerRoom = 9,
                enemies = Prefabs("Enemy_Vampire", "Enemy_Demon", "Enemy_Golem", "Enemy_Warrior",
                                  "Enemy_Mage", "Enemy_Slime_Fire"),
                elites = Prefabs("Enemy_Vampire", "Enemy_Demon"),
                boss = Prefab("Boss_DreadKnight"),
            },
        };

        foreach (var f in cat.floors)
        {
            if (f.boss == null) Debug.LogWarning($"CONTENT: boss lantai {f.title} tidak ditemukan.");
            int found = 0;
            if (f.enemies != null) foreach (var e in f.enemies) if (e != null) found++;
            if (found == 0) Debug.LogWarning($"CONTENT: lantai {f.title} tidak punya musuh.");
        }

        EditorUtility.SetDirty(cat);
        Debug.Log($"CONTENT: FloorCatalog -> {cat.floors.Length} lantai.");
    }

    // -------------------------------------------------------------- utility
    static GameObject Prefab(string name)
        => AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + name + ".prefab");

    static GameObject[] Prefabs(params string[] names)
    {
        var list = new List<GameObject>();
        foreach (var n in names)
        {
            var p = Prefab(n);
            if (p != null) list.Add(p);
            else Debug.LogWarning($"CONTENT: prefab hilang -> {n}");
        }
        return list.ToArray();
    }

    static Color Hex(string hex)
        => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }
}
