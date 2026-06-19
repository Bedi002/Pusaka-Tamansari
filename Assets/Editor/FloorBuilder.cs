using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Generator lantai dungeon roguelike memakai art Kenney Tiny Dungeon.
/// Lantai/tembok = sprite tiled (16px), pintu & props = sprite Kenney, player &
/// musuh = prefab Kenney (PrefabBuilder). Ruangan tertutup tembok; pintu trigger
/// teleport antar-ruangan; kamera snap via DungeonManager.
/// Jalankan: Tools ▸ Pusaka ▸ Build Dungeon Floors
/// </summary>
public static class FloorBuilder
{
    const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    const string SceneDir = "Assets/Scenes/";
    const string PrefabDir = "Assets/Prefabs/";

    static readonly Vector2 Interior = new Vector2(22f, 14f);
    static readonly Vector2 Step = new Vector2(52f, 40f);

    // tile index Kenney Tiny Dungeon
    const int T_FLOOR = 48, T_WALL = 57, T_DOOR = 45, T_BARREL = 65, T_CHEST = 89, T_CABINET = 63, T_BANNER = 29, T_TOMB = 62;

    // Karakter Forge beranimasi (hero + musuh), environment dari tile Kenney.
    static GameObject[] heroes;
    static GameObject[][] floorEnemies;   // tipe musuh per-floor
    static GameObject[] floorBoss;        // boss per-floor
    static int curFloor, erIdx;

    struct Conn { public Vector2Int a, b; public Conn(Vector2Int x, Vector2Int y) { a = x; b = y; } }

    [MenuItem("Tools/Pusaka/Build Dungeon Floors")]
    public static void BuildFloors()
    {
        heroes = new[] { Load("Forge/Player_Warrior"), Load("Forge/Player_Archer"), Load("Forge/Player_Mage") };
        floorEnemies = new[]
        {
            new[] { Load("Forge/Enemy_Slime_Fire"), Load("Forge/Enemy_Slime_Ice"), Load("Forge/Enemy_Slime_Poison") },
            new[] { Load("Forge/Enemy_Orc"), Load("Forge/Enemy_Vampire") },
            new[] { Load("Forge/Enemy_Plant"), Load("Forge/Enemy_Golem") },
        };
        floorBoss = new[] { Load("Forge/Boss_Demon"), Load("Forge/Boss_DreadKnight"), Load("Forge/Boss_DreadKnight") };
        if (heroes[0] == null) Debug.LogWarning("FLOOR: prefab Forge belum ada — jalankan 'Import Forge Characters' dulu.");

        BuildFloor("Floor1", 0); Debug.Log("FLOOR: Floor1 OK");
        BuildFloor("Floor2", 1); Debug.Log("FLOOR: Floor2 OK");
        BuildFloor("Floor3", 2); Debug.Log("FLOOR: Floor3 OK");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("FLOOR: SELESAI.");
    }

    static GameObject Load(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + name + ".prefab");
    static Sprite Tile(int i) => PrefabBuilder.Tile(i);

    static void BuildFloor(string sceneName, int floorIdx)
    {
        curFloor = floorIdx; erIdx = 0;
        EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        CleanOldGeometry();
        SetupCamera();

        var defs = new Dictionary<Vector2Int, RoomType>
        {
            { new Vector2Int(0, 0), RoomType.Start },
            { new Vector2Int(0, 1), RoomType.Combat },
            { new Vector2Int(-1, 1), RoomType.Treasure },
            { new Vector2Int(1, 1), RoomType.Combat },
            { new Vector2Int(1, 2), RoomType.Treasure },
            { new Vector2Int(0, 2), RoomType.Combat },
            { new Vector2Int(0, 3), RoomType.Boss },
        };
        var conns = new List<Conn>
        {
            new Conn(new Vector2Int(0,0), new Vector2Int(0,1)),
            new Conn(new Vector2Int(0,1), new Vector2Int(-1,1)),
            new Conn(new Vector2Int(0,1), new Vector2Int(1,1)),
            new Conn(new Vector2Int(1,1), new Vector2Int(1,2)),
            new Conn(new Vector2Int(0,1), new Vector2Int(0,2)),
            new Conn(new Vector2Int(0,2), new Vector2Int(0,3)),
        };

        var dungeon = new GameObject("Dungeon");
        var rooms = new Dictionary<Vector2Int, Room>();
        foreach (var kv in defs)
            rooms[kv.Key] = MakeRoom(dungeon.transform, kv.Key, kv.Value);

        foreach (var c in conns)
        {
            var ra = rooms[c.a]; var rb = rooms[c.b];
            Vector2 dir = new Vector2(c.b.x - c.a.x, c.b.y - c.a.y);
            AddDoor(ra, rb, dir);
            AddDoor(rb, ra, -dir);
        }

        var startRoom = rooms[new Vector2Int(0, 0)];

        // hapus player bawaan SampleScene (kalau ada) -> hero di-spawn runtime sesuai pilihan
        var baked = GameObject.FindWithTag("Player");
        if (baked != null) Object.DestroyImmediate(baked);

        var hs = new GameObject("HeroSpawner");
        hs.transform.position = startRoom.Center;
        var spawner = hs.AddComponent<HeroSpawner>();
        spawner.fallbackHero = (heroes != null && heroes.Length > 0) ? heroes[0] : null;

        // DungeonManager (player di-resolve saat runtime lewat tag)
        var dm = new GameObject("DungeonManager").AddComponent<DungeonManager>();
        dm.startRoom = startRoom;
        dm.cam = Camera.main;

        var canvas = PusakaSceneBuilder.BuildHud();
        PusakaSceneBuilder.BuildPauseMenu(canvas);

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SceneDir + sceneName + ".unity");
    }

    static void CleanOldGeometry()
    {
        // CATATAN: "Player" TIDAK dihapus — kita pakai ulang player LPC beranimasi.
        string[] kill = { "Tembok", "Square", "Pintu Keluar", "EnemySpawner", "Dungeon", "DungeonManager", "New Text", "Text (New Text)", "HeroSpawner" };
        foreach (var n in kill)
        {
            var go = GameObject.Find(n);
            int guard = 0;
            while (go != null && guard++ < 50) { Object.DestroyImmediate(go); go = GameObject.Find(n); }
        }
    }

    static void SetupCamera()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            foreach (var mb in cam.GetComponents<MonoBehaviour>())
                if (mb != null && mb.GetType().Name.Contains("Cinemachine")) Object.DestroyImmediate(mb);
        }
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (mb != null && mb.GetType().Name.Contains("Cinemachine")) mb.gameObject.SetActive(false);
    }

    static Room MakeRoom(Transform parent, Vector2Int grid, RoomType type)
    {
        var go = new GameObject($"Room_{type}_{grid.x}_{grid.y}");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(grid.x * Step.x, grid.y * Step.y, 0f);

        var room = go.AddComponent<Room>();
        room.type = type; room.gridPos = grid; room.size = Interior;
        room.enemyPrefab = EnemyForRoom(type, grid);
        if (type == RoomType.Boss) room.bossPrefab = (floorBoss != null && curFloor < floorBoss.Length) ? floorBoss[curFloor] : null;
        room.enemyCount = type == RoomType.Combat ? 5 : 0;

        float halfX = Interior.x / 2f, halfY = Interior.y / 2f, t = 1f;
        MakeTiled(go.transform, "Floor", Vector2.zero, Interior, Tile(T_FLOOR), -10, false);
        MakeTiled(go.transform, "WallTop", new Vector2(0, halfY), new Vector2(Interior.x + t, t), Tile(T_WALL), 5, true);
        MakeTiled(go.transform, "WallBottom", new Vector2(0, -halfY), new Vector2(Interior.x + t, t), Tile(T_WALL), 5, true);
        MakeTiled(go.transform, "WallLeft", new Vector2(-halfX, 0), new Vector2(t, Interior.y + t), Tile(T_WALL), 5, true);
        MakeTiled(go.transform, "WallRight", new Vector2(halfX, 0), new Vector2(t, Interior.y + t), Tile(T_WALL), 5, true);

        // obor/panji di sudut atas dinding (semua ruangan) + props per tipe
        MakeProp(go.transform, new Vector2(-halfX + 1.5f, halfY - 1.5f), Tile(T_BANNER), 6);
        MakeProp(go.transform, new Vector2(halfX - 1.5f, halfY - 1.5f), Tile(T_BANNER), 6);

        if (type == RoomType.Treasure)
        {
            MakeProp(go.transform, new Vector2(0, 0.5f), Tile(T_CHEST), 2);
            MakeProp(go.transform, new Vector2(-2.5f, 0.5f), Tile(T_BARREL), 2);
            MakeProp(go.transform, new Vector2(2.5f, 0.5f), Tile(T_BARREL), 2);
            MakeProp(go.transform, new Vector2(0, -2.5f), Tile(T_CABINET), 2);
        }
        else if (type == RoomType.Boss)
        {
            MakeProp(go.transform, new Vector2(-halfX + 2.5f, -halfY + 2f), Tile(T_TOMB), 2);
            MakeProp(go.transform, new Vector2(halfX - 2.5f, -halfY + 2f), Tile(T_TOMB), 2);
            MakeProp(go.transform, new Vector2(0, halfY - 2f), Tile(T_BANNER), 6);
        }
        else if (type == RoomType.Combat)
        {
            MakeProp(go.transform, new Vector2(-halfX + 2f, -halfY + 2f), Tile(T_BARREL), 2);
            MakeProp(go.transform, new Vector2(halfX - 2f, halfY - 2.5f), Tile(T_TOMB), 2);
            MakeProp(go.transform, new Vector2(halfX - 2.5f, -halfY + 2f), Tile(T_BARREL), 2);
        }
        else if (type == RoomType.Start)
        {
            MakeProp(go.transform, new Vector2(-halfX + 2f, -halfY + 2f), Tile(T_CABINET), 2);
            MakeProp(go.transform, new Vector2(halfX - 2f, -halfY + 2f), Tile(T_BARREL), 2);
        }
        return room;
    }

    static GameObject EnemyForRoom(RoomType type, Vector2Int grid)
    {
        var list = (floorEnemies != null && curFloor >= 0 && curFloor < floorEnemies.Length) ? floorEnemies[curFloor] : null;
        if (list == null) return null;
        for (int k = 0; k < list.Length; k++)              // rotasi tipe musuh antar-ruangan
        {
            var p = list[(erIdx + k) % list.Length];
            if (p != null) { erIdx += k + 1; return p; }
        }
        return null;
    }

    static void AddDoor(Room from, Room to, Vector2 dir)
    {
        dir = new Vector2(Mathf.Clamp(dir.x, -1, 1), Mathf.Clamp(dir.y, -1, 1));
        float halfX = Interior.x / 2f, halfY = Interior.y / 2f;

        var doorGO = new GameObject($"Door_to_{to.gridPos.x}_{to.gridPos.y}");
        doorGO.transform.SetParent(from.transform, false);
        doorGO.transform.localPosition = new Vector3(dir.x * (halfX - 1.1f), dir.y * (halfY - 1.1f), 0f);

        var col = doorGO.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = Mathf.Abs(dir.x) > 0 ? new Vector2(1.6f, 4f) : new Vector2(4f, 1.6f);

        var door = doorGO.AddComponent<Door>();
        door.targetRoom = to;

        var visGO = new GameObject("DoorVis");
        visGO.transform.SetParent(doorGO.transform, false);
        var sr = visGO.AddComponent<SpriteRenderer>();
        sr.sprite = Tile(T_DOOR);
        sr.sortingOrder = 4;
        visGO.transform.localScale = Vector3.one * 1.3f;
        door.visual = sr;

        var ep = new GameObject("Entry");
        ep.transform.SetParent(to.transform, false);
        ep.transform.localPosition = new Vector3(-dir.x * (halfX - 2.5f), -dir.y * (halfY - 2.5f), 0f);
        door.entryPoint = ep.transform;
    }

    static void MakeTiled(Transform parent, string name, Vector2 localPos, Vector2 size, Sprite sprite, int sortOrder, bool collider)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortOrder;
        if (sprite != null) { sr.drawMode = SpriteDrawMode.Tiled; sr.size = size; }
        if (collider) { var c = go.AddComponent<BoxCollider2D>(); c.size = size; }
    }

    static void MakeProp(Transform parent, Vector2 localPos, Sprite sprite, int sortOrder)
    {
        var go = new GameObject("Prop");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortOrder;
    }

}
