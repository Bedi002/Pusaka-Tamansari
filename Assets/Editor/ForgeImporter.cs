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
    const int PPU = 32;

    static readonly string[] ANIMS = { "idle", "walk", "attack", "hurt", "death" };
    // baris sheet CraftPix: 0=depan(bawah) 1=KIRI 2=KANAN 3=belakang(atas)
    static readonly string[] DIRNAME = { "down", "left", "right", "up" };

    enum Role { Hero, Enemy, Boss }

    class Def
    {
        public string folder; public Role role;
        public int hp = 60, dmg = 10, score = 8;
        public float speed = 2.4f, colliderR = 0.32f;
        public Def(string f, Role r) { folder = f; role = r; }
    }

    static List<Def> Roster() => new List<Def>
    {
        new Def("Warrior", Role.Hero) { colliderR = 0.35f },
        new Def("Archer",  Role.Hero) { colliderR = 0.32f },
        new Def("Mage",    Role.Hero) { colliderR = 0.32f },

        new Def("Slime_Fire",   Role.Enemy) { hp = 50,  dmg = 10, speed = 2.6f, score = 8,  colliderR = 0.30f },
        new Def("Slime_Ice",    Role.Enemy) { hp = 60,  dmg = 12, speed = 2.2f, score = 10, colliderR = 0.30f },
        new Def("Slime_Poison", Role.Enemy) { hp = 55,  dmg = 11, speed = 2.4f, score = 9,  colliderR = 0.30f },
        new Def("Orc",          Role.Enemy) { hp = 110, dmg = 18, speed = 2.2f, score = 16, colliderR = 0.34f },
        new Def("Vampire",      Role.Enemy) { hp = 80,  dmg = 16, speed = 2.6f, score = 14, colliderR = 0.32f },
        new Def("Plant",        Role.Enemy) { hp = 70,  dmg = 14, speed = 0.6f, score = 10, colliderR = 0.40f },
        new Def("Golem",        Role.Enemy) { hp = 200, dmg = 24, speed = 1.4f, score = 30, colliderR = 0.70f },

        new Def("DreadKnight",  Role.Boss)  { hp = 1400, dmg = 26, colliderR = 0.70f },
        new Def("Demon",        Role.Boss)  { hp = 1600, dmg = 28, colliderR = 0.65f },
    };

    [MenuItem("Tools/Pusaka/Import Forge Characters")]
    public static void Import()
    {
        Directory.CreateDirectory(PrefabDir);
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
                clipData.Add(new ForgeAnimator.Clip { key = $"{anim}_{DIRNAME[r]}", frames = frames, fps = fps, loop = loop });
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
        importer.SaveAndReimport();

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex == null) return null;
        int W = tex.width, H = tex.height, cell = H / 4, cols = Mathf.Max(1, W / cell);
        string baseName = Path.GetFileNameWithoutExtension(texPath);

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(importer);
        dp.InitSpriteEditorDataProvider();

        var rects = new List<SpriteRect>();
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < cols; c++)
                rects.Add(new SpriteRect
                {
                    name = $"{baseName}_d{r}_f{c}",
                    spriteID = GUID.Generate(),
                    rect = new Rect(c * cell, H - (r + 1) * cell, cell, cell),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = SpriteAlignment.Center,
                });
        dp.SetSpriteRects(rects.ToArray());
        dp.Apply();
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

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        switch (def.role)
        {
            case Role.Hero:
                go.tag = "Player";
                go.AddComponent<PlayerMovement>();
                go.AddComponent<PlayerHealth>();
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

    static string PrefabName(Def def) => def.role switch
    {
        Role.Hero => "Player_" + def.folder,
        Role.Boss => "Boss_" + def.folder,
        _ => "Enemy_" + def.folder,
    };
}
