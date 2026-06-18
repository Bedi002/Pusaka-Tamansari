using UnityEngine;
using UnityEditor;

/// <summary>
/// Bangun prefab karakter baru dari sprite Kenney Tiny Dungeon (16px, PPU16):
/// Player_Hero (knight), musuh (slime/skeleton/spider), Boss_Wizard.
/// Tanpa Animator — animasi via kode (facing/flip), combat & AI dari script lama.
/// Jalankan: Tools ▸ Pusaka ▸ Build Kenney Prefabs
/// </summary>
public static class PrefabBuilder
{
    const string Tiles = "Assets/Art/Kenney/TinyDungeon/Tiles/";
    const string PrefabDir = "Assets/Prefabs/";

    public static Sprite Tile(int i) => AssetDatabase.LoadAssetAtPath<Sprite>(Tiles + $"tile_{i:0000}.png");

    [MenuItem("Tools/Pusaka/Build Kenney Prefabs")]
    public static void Build()
    {
        BuildPlayer();
        BuildEnemy("Enemy_Slime", 108, 50, 10, 2.6f, 8);
        BuildEnemy("Enemy_Skeleton", 86, 90, 16, 2.0f, 14);
        BuildEnemy("Enemy_Spider", 122, 45, 9, 3.6f, 6);
        BuildBoss("Boss_Wizard", 84);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PREFAB: selesai bangun prefab Kenney.");
    }

    static GameObject NewChar(string name, int tile, float radius)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Tile(tile);
        sr.sortingOrder = 10;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = radius;
        return go;
    }

    static void Save(GameObject go, string name)
    {
        PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + name + ".prefab");
        Object.DestroyImmediate(go);
    }

    static void BuildPlayer()
    {
        var go = NewChar("Player_Hero", 96, 0.34f);
        go.tag = "Player";
        go.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;

        go.AddComponent<PlayerMovement>();
        go.AddComponent<PlayerHealth>();
        var combat = go.AddComponent<PlayerCombat>();
        int el = LayerMask.NameToLayer("Enemy");
        if (el >= 0) combat.enemyLayers = 1 << el;
        combat.attackRange = 0.7f;
        combat.attackOffset = 0.7f;
        combat.attackDamage = 40;

        var ap = new GameObject("AttackPoint");
        ap.transform.SetParent(go.transform, false);
        ap.transform.localPosition = new Vector3(0, -0.7f, 0);
        combat.attackPoint = ap.transform;

        Save(go, "Player_Hero");
    }

    static void BuildEnemy(string name, int tile, int hp, int dmg, float speed, int score)
    {
        var go = NewChar(name, tile, 0.32f);
        go.tag = "Enemy";
        int el = LayerMask.NameToLayer("Enemy");
        if (el >= 0) go.layer = el;
        go.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;

        var e = go.AddComponent<Enemy>();
        e.maxHealth = hp; e.attackDamage = dmg; e.scoreReward = score;

        var m = go.AddComponent<EnemyMovement>();
        m.speed = speed;
        m.attackRange = 0.9f; m.stopDistance = 0.7f;
        if (el >= 0) m.enemyMask = 1 << el;

        Save(go, name);
    }

    static void BuildBoss(string name, int tile)
    {
        var go = NewChar(name, tile, 0.5f);
        go.tag = "Enemy";
        int el = LayerMask.NameToLayer("Enemy");
        if (el >= 0) go.layer = el;
        go.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
        go.transform.localScale = Vector3.one * 2.2f;

        var b = go.AddComponent<Boss>();
        b.bossName = "Penyihir Pusaka";
        b.maxHealth = 1200;
        b.meleeDamage = 22;
        b.attackRange = 1.4f;

        Save(go, name);
    }
}
