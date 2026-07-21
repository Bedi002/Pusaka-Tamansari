using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Membangun scene lantai yang KOSONG dan ringan: kamera, spawner hero,
/// DungeonManager, DungeonGenerator, HUD, tas, dan menu jeda.
///
/// Denah ruangan TIDAK lagi dipahat di sini. Dulu tiap lantai berisi 7 ruangan
/// tetap hasil hardcode, sehingga tiap run identik dan ukuran petanya kecil.
/// Sekarang seluruh isi lantai dirakit DungeonGenerator saat game berjalan.
///
/// Jalankan: Tools > Pusaka > Build Dungeon Floors
/// </summary>
public static class FloorBuilder
{
    const string SceneDir = "Assets/Scenes/";

    [MenuItem("Tools/Pusaka/Build Dungeon Floors")]
    public static void BuildFloors()
    {
        int count = FloorCatalog.Count;
        if (count == 0)
        {
            Debug.LogError("FLOOR: FloorCatalog kosong. Jalankan Tools > Pusaka > Rebuild Content dulu.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            var profile = FloorCatalog.Get(i);
            BuildFloor(profile.sceneName, i, profile);
            Debug.Log($"FLOOR: {profile.sceneName} ({profile.title}) OK - {profile.roomCount} ruangan.");
        }

        RegisterBuildSettings(count);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"FLOOR: selesai, {count} lantai.");
    }

    static void BuildFloor(string sceneName, int index, FloorProfile profile)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // kamera
        var camGO = new GameObject("Main Camera", typeof(Camera));
        camGO.tag = "MainCamera";
        var cam = camGO.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 12f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = profile.skyTint;
        camGO.transform.position = new Vector3(0, 0, -10);
        // AudioListener sengaja TIDAK dipasang di kamera: AudioManager yang
        // bertahan lintas-scene memilikinya, supaya jumlahnya selalu tepat satu.

        // titik spawn hero (posisinya dipindahkan generator ke ruangan awal)
        var spawner = new GameObject("HeroSpawner").AddComponent<HeroSpawner>();
        var hero = HeroCatalog.Get(0);
        spawner.fallbackHero = hero != null ? hero.prefab : null;

        // manajer lantai
        var dm = new GameObject("DungeonManager").AddComponent<DungeonManager>();
        dm.cam = cam;

        var gen = new GameObject("DungeonGenerator").AddComponent<DungeonGenerator>();
        gen.floorIndex = index;
        gen.seed = 0;                       // 0 = denah acak tiap run

        // UI
        var canvas = PusakaSceneBuilder.BuildHud();
        PusakaSceneBuilder.BuildPauseMenu(canvas);
        new GameObject("InventoryUI").AddComponent<InventoryUI>();

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SceneDir + sceneName + ".unity");
    }

    /// <summary>Pastikan semua scene lantai terdaftar di Build Settings.</summary>
    static void RegisterBuildSettings(int floorCount)
    {
        var wanted = new System.Collections.Generic.List<string>
        {
            "MainMenu", "CharacterSelect", "DifficultySelect",
        };
        for (int i = 0; i < floorCount; i++) wanted.Add(FloorCatalog.Get(i).sceneName);
        wanted.Add("Victory");
        wanted.Add("GameOver");

        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        foreach (var name in wanted)
        {
            string path = SceneDir + name + ".unity";
            if (System.IO.File.Exists(path)) scenes.Add(new EditorBuildSettingsScene(path, true));
            else Debug.LogWarning($"FLOOR: scene belum ada, dilewati di Build Settings -> {name}");
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
