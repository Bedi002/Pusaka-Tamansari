using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Impor karakter Sprite Forge (64px, 4 arah, beranimasi) menjadi prefab Unity:
/// slice tiap sheet jadi sub-sprite, kumpulkan jadi data ForgeAnimator (animator
/// berbasis kode, anti-bug — TANPA blend tree), + controller minimal yang hanya
/// memegang parameter (MoveX/MoveY/Speed/Attack*/Die/Hurt) yang sudah dipakai script.
/// Jalankan: Tools ▸ Pusaka ▸ Import Forge Characters
/// </summary>
public static class ForgeImporter
{
    const string Root = "Assets/Sprites/Forge/";
    const string PrefabDir = "Assets/Prefabs/Forge/";
    const string ShadowPath = "Assets/Sprites/Forge/_shadow.png";
    const int PPU = 32;
    static int _cell = 64;            // ukuran sel sheet terakhir (64 atau 128 utk golem) -> normalisasi skala
    static Sprite _shadow;            // sprite bayangan bersama (di-tint hitam, dipasang per prefab)
    static float _feetOffset = -0.4f; // posisi kaki relatif pusat sprite (unit) dari frame idle
    static float _bodyW = 1f;         // lebar badan (unit) dari frame idle -> ukuran bayangan

    static readonly string[] ANIMS = { "idle", "walk", "attack", "hurt", "death" };
    // urutan baris BEDA per-pack (terverifikasi dari sheet):
    static readonly string[] HERO_DIRS = { "down", "left", "right", "up" };   // hero + boss (Demon/DreadKnight)
    static readonly string[] CREATURE_DIRS = { "down", "up", "left", "right" }; // slime/orc/vampire/plant/golem

    enum Role { Hero, Enemy, Boss }

    class Def
    {
        public string folder; public Role role;
        public int hp = 60, dmg = 10, score = 8;
        public float speed = 2.4f, colliderR = 0.32f;
        public string[] dirs;     // urutan baris->arah; null = HERO_DIRS
        public string hitSfx = "hit_flesh";     // nama bank di Resources/Audio
        public string deathSfx = "enemy_die";
        public Def(string f, Role r) { folder = f; role = r; }
    }

    // Angka mengikuti tabel roster di DESIGN_BIBLE.md (bagian 3.1 dan 3.3).
    // Sprite kelas Warrior/Archer/Mage dipakai dua kali: sebagai hero DAN sebagai
    // abdi keraton yang kerasukan, sehingga variasi musuh naik tanpa aset baru.
    static List<Def> Roster() => new List<Def>
    {
        // SATU hero saja: Senopati. Archer dan Mage tetap dibangun, tetapi hanya
        // sebagai musuh (abdi keraton kerasukan), bukan pilihan pemain.
        new Def("Warrior", Role.Hero) { colliderR = 0.35f },

        // musuh reguler
        new Def("Slime_Poison", Role.Enemy) { hp = 28,  dmg = 6,  speed = 2.0f, score = 5,  colliderR = 0.30f, dirs = CREATURE_DIRS, deathSfx = "slime_die" },
        new Def("Slime_Ice",    Role.Enemy) { hp = 34,  dmg = 8,  speed = 1.9f, score = 8,  colliderR = 0.30f, dirs = CREATURE_DIRS, deathSfx = "slime_die" },
        new Def("Slime_Fire",   Role.Enemy) { hp = 26,  dmg = 5,  speed = 2.5f, score = 10, colliderR = 0.30f, dirs = CREATURE_DIRS, deathSfx = "slime_die" },
        new Def("Warrior",      Role.Enemy) { hp = 55,  dmg = 10, speed = 2.6f, score = 12, colliderR = 0.35f, hitSfx = "hit_metal" },
        new Def("Archer",       Role.Enemy) { hp = 40,  dmg = 9,  speed = 2.4f, score = 14, colliderR = 0.32f },
        new Def("Mage",         Role.Enemy) { hp = 45,  dmg = 14, speed = 2.0f, score = 18, colliderR = 0.32f },
        new Def("Plant",        Role.Enemy) { hp = 60,  dmg = 11, speed = 0f,   score = 12, colliderR = 0.40f, dirs = CREATURE_DIRS },
        new Def("Orc",          Role.Enemy) { hp = 120, dmg = 18, speed = 2.1f, score = 25, colliderR = 0.34f, dirs = CREATURE_DIRS },
        new Def("Golem",        Role.Enemy) { hp = 160, dmg = 20, speed = 1.5f, score = 30, colliderR = 0.70f, dirs = CREATURE_DIRS, hitSfx = "hit_metal" },
        new Def("Vampire",      Role.Enemy) { hp = 90,  dmg = 15, speed = 3.2f, score = 40, colliderR = 0.32f, dirs = CREATURE_DIRS },
        new Def("Demon",        Role.Enemy) { hp = 140, dmg = 22, speed = 2.8f, score = 50, colliderR = 0.65f, hitSfx = "hit_metal" },

        // boss per lantai
        new Def("Orc",          Role.Boss)  { hp = 500,  dmg = 20, speed = 2.3f, score = 150, colliderR = 0.34f, dirs = CREATURE_DIRS },
        new Def("Golem",        Role.Boss)  { hp = 650,  dmg = 24, speed = 1.7f, score = 200, colliderR = 0.70f, dirs = CREATURE_DIRS, hitSfx = "hit_metal" },
        new Def("Vampire",      Role.Boss)  { hp = 800,  dmg = 22, speed = 3.4f, score = 300, colliderR = 0.32f, dirs = CREATURE_DIRS },
        new Def("Demon",        Role.Boss)  { hp = 1000, dmg = 28, speed = 2.9f, score = 400, colliderR = 0.65f, hitSfx = "hit_metal" },
        new Def("DreadKnight",  Role.Boss)  { hp = 1400, dmg = 30, speed = 2.7f, score = 600, colliderR = 0.70f, hitSfx = "hit_metal" },
    };

    [MenuItem("Tools/Pusaka/Import Forge Characters")]
    public static void Import()
    {
        Directory.CreateDirectory(PrefabDir);
        _shadow = EnsureShadowSprite();
        int ok = 0;
        foreach (var d in Roster())
        {
            if (!Directory.Exists(Root + d.folder)) { Debug.LogWarning($"FORGE: folder hilang {d.folder}"); continue; }
            try { BuildCharacter(d); ok++; }
            catch (System.Exception e) { Debug.LogError($"FORGE: gagal {d.folder}: {e}"); }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"FORGE: import selesai ({ok} karakter).");
    }

    // ---------------------------------------------------------------- per karakter
    static void BuildCharacter(Def def)
    {
        AssetDatabase.DeleteAsset($"{Root}{def.folder}/Anim");   // buang .anim lama (sistem blend-tree)

        var clipData = new List<ForgeAnimator.Clip>();
        Sprite rep = null;
        var dn = def.dirs ?? HERO_DIRS;     // urutan baris->arah sesuai pack

        foreach (var anim in ANIMS)
        {
            string tex = $"{Root}{def.folder}/{def.folder}_{anim}.png";
            if (!File.Exists(tex)) continue;
            var grid = SliceGrid(tex);
            if (grid == null) continue;

            bool loop = anim == "idle" || anim == "walk";
            float fps = anim == "attack" ? 14f : anim == "death" ? 8f : 10f;
            for (int r = 0; r < 4; r++)
            {
                var frames = grid[r].Where(s => s != null).ToArray();
                if (frames.Length == 0) continue;
                if (rep == null) rep = frames[0];
                clipData.Add(new ForgeAnimator.Clip { key = $"{anim}_{dn[r]}", frames = frames, fps = fps, loop = loop });
            }
        }
        if (clipData.Count == 0) { Debug.LogWarning($"FORGE: {def.folder} tak ada sheet"); return; }

        var ctrl = BuildMinimalController(def);
        BuildPrefab(def, ctrl, rep, clipData.ToArray());
        Debug.Log($"FORGE: {def.folder} -> {clipData.Count} klip arah, prefab dibuat.");
    }

    // ---------------------------------------------------------------- slicing
    static Sprite[][] SliceGrid(string texPath)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = PPU;
        importer.mipmapEnabled = false;
        importer.isReadable = true;               // butuh baca piksel untuk buang frame kosong
        importer.SaveAndReimport();

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex == null) return null;
        int W = tex.width, H = tex.height, cell = H / 4, cols = Mathf.Max(1, W / cell);
        _cell = cell;                             // simpan utk normalisasi skala prefab
        string baseName = Path.GetFileNameWithoutExtension(texPath);
        var px = tex.GetPixels32();               // origin kiri-bawah, baris-mayor

        // dari frame idle depan (baris0, kolom0): ukur kaki & lebar badan -> tempat & ukuran bayangan
        if (baseName.EndsWith("_idle"))
        {
            int y0 = H - cell, feetY = H, minX = cell, maxX = -1;
            for (int y = y0; y < y0 + cell; y++)
            {
                int row = y * W; bool any = false;
                for (int x = 0; x < cell && x < W; x++)
                    if (px[row + x].a > 16) { any = true; if (x < minX) minX = x; if (x > maxX) maxX = x; }
                if (any && y < feetY) feetY = y;
            }
            if (maxX >= minX)
            {
                _feetOffset = (feetY - (y0 + cell / 2f)) / PPU;   // negatif = di bawah pusat
                _bodyW = (maxX - minX + 1) / (float)PPU;
            }
        }

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(importer);
        dp.InitSpriteEditorDataProvider();

        var rects = new List<SpriteRect>();
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < cols; c++)
            {
                int x0 = c * cell, y0 = H - (r + 1) * cell;
                if (!CellHasContent(px, W, x0, y0, cell)) continue;   // lewati frame transparan -> tak ada kedip
                rects.Add(new SpriteRect
                {
                    name = $"{baseName}_d{r}_f{c}",
                    spriteID = GUID.Generate(),
                    rect = new Rect(x0, y0, cell, cell),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = SpriteAlignment.Center,
                });
            }
        dp.SetSpriteRects(rects.ToArray());
        dp.Apply();
        importer.isReadable = false;              // kembalikan supaya build tak boros memori
        importer.SaveAndReimport();

        var all = AssetDatabase.LoadAllAssetsAtPath(texPath).OfType<Sprite>().ToList();
        var grid = new Sprite[4][];
        for (int r = 0; r < 4; r++)
        {
            grid[r] = new Sprite[cols];
            for (int c = 0; c < cols; c++)
                grid[r][c] = all.FirstOrDefault(s => s.name == $"{baseName}_d{r}_f{c}");
        }
        return grid;
    }

    // true bila cell punya piksel tak-transparan (px: Color32 origin kiri-bawah, baris-mayor)
    static bool CellHasContent(Color32[] px, int W, int x0, int y0, int cell, byte thr = 16)
    {
        for (int y = y0; y < y0 + cell; y++)
        {
            int row = y * W;
            for (int x = x0; x < x0 + cell; x++)
                if (px[row + x].a > thr) return true;
        }
        return false;
    }

    // ---------------------------------------------------------------- controller minimal (papan tulis parameter)
    static AnimatorController BuildMinimalController(Def def)
    {
        string path = $"{Root}{def.folder}/{def.folder}.controller";
        AssetDatabase.DeleteAsset(path);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        ctrl.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        foreach (var p in new[] { "Attack", "Attack1", "Attack2", "Attack3", "Die", "Hurt" })
            ctrl.AddParameter(p, AnimatorControllerParameterType.Trigger);
        ctrl.layers[0].stateMachine.AddState("Idle");   // state kosong; visual digerakkan ForgeAnimator
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    // ---------------------------------------------------------------- prefab
    static void BuildPrefab(Def def, AnimatorController ctrl, Sprite rep, ForgeAnimator.Clip[] clipData)
    {
        var go = new GameObject(PrefabName(def));
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = rep; sr.sortingOrder = 10;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f; rb.freezeRotation = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = def.colliderR;

        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = ctrl;

        var fa = go.AddComponent<ForgeAnimator>();
        fa.clips = clipData; fa.sr = sr; fa.animator = animator;

        // ukuran dunia konsisten lewat skala uniform (sprite+collider+attackPoint+bayangan ikut,
        // tanpa distorsi piksel). Sheet 128px (golem) digambar 2x -> dikompensasi /cell.
        float baseUnits = def.role == Role.Boss ? 2.0f : def.role == Role.Hero ? 1.35f : 1.3f;
        go.transform.localScale = Vector3.one * (baseUnits * 64f / Mathf.Max(64, _cell));

        // bayangan = objek anak di kaki (ikut skala, tak pernah nge-crop sprite / lompat)
        if (_shadow != null)
        {
            var sh = new GameObject("Shadow");
            sh.transform.SetParent(go.transform, false);
            var ssr = sh.AddComponent<SpriteRenderer>();
            ssr.sprite = _shadow;
            ssr.color = new Color(0f, 0f, 0f, 0.40f);
            ssr.sortingOrder = sr.sortingOrder - 1;
            float w = Mathf.Max(0.4f, _bodyW * 0.95f);
            sh.transform.localPosition = new Vector3(0f, _feetOffset, 0f);
            sh.transform.localScale = new Vector3(w, 0.7f * w, 1f);   // bayangan pipih di kaki
        }

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        switch (def.role)
        {
            case Role.Hero:
                go.tag = "Player";
                go.AddComponent<PlayerMovement>();
                go.AddComponent<PlayerHealth>();
                go.AddComponent<PlayerResources>();      // Tenaga & Aji
                var combat = go.AddComponent<PlayerCombat>();
                if (enemyLayer >= 0) combat.enemyLayers = 1 << enemyLayer;
                combat.attackRange = 0.7f; combat.attackOffset = 0.7f; combat.attackDamage = 40;
                var ap = new GameObject("AttackPoint");
                ap.transform.SetParent(go.transform, false);
                ap.transform.localPosition = new Vector3(0, -0.7f, 0);
                combat.attackPoint = ap.transform;
                break;

            case Role.Enemy:
                go.tag = "Enemy";
                if (enemyLayer >= 0) go.layer = enemyLayer;
                var e = go.AddComponent<Enemy>();
                e.maxHealth = def.hp; e.attackDamage = def.dmg; e.scoreReward = def.score;
                e.hitSoundKey = def.hitSfx; e.deathSoundKey = def.deathSfx;
                var m = go.AddComponent<EnemyMovement>();
                m.speed = def.speed; m.attackRange = 0.9f; m.stopDistance = 0.7f;
                if (enemyLayer >= 0) m.enemyMask = 1 << enemyLayer;
                break;

            case Role.Boss:
                go.tag = "Enemy";
                if (enemyLayer >= 0) go.layer = enemyLayer;
                var b = go.AddComponent<Boss>();
                b.bossName = def.folder == "Demon" ? "Iblis Pusaka" : "Ksatria Maut";
                b.maxHealth = def.hp; b.meleeDamage = def.dmg; b.attackRange = 1.4f;
                break;
        }

        PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + PrefabName(def) + ".prefab");
        Object.DestroyImmediate(go);
    }

    // pastikan _shadow.png ke-import sebagai Sprite (PPU 64 -> 64px = 1 unit)
    static Sprite EnsureShadowSprite()
    {
        var imp = AssetImporter.GetAtPath(ShadowPath) as TextureImporter;
        if (imp == null) { Debug.LogWarning($"FORGE: {ShadowPath} tak ada — prefab tanpa bayangan."); return null; }
        if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single || imp.spritePixelsPerUnit != 64f)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 64f;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ShadowPath);
    }

    static string PrefabName(Def def) => def.role switch
    {
        Role.Hero => "Player_" + def.folder,
        Role.Boss => "Boss_" + def.folder,
        _ => "Enemy_" + def.folder,
    };
}
