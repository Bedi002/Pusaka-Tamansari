using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using TMPro;

/// <summary>
/// Generator scene otomatis untuk Pusaka Tamansari. Membuat seluruh alur main
/// (MainMenu, DifficultySelect, Stage1/2/3, Victory, GameOver) + prefab Boss,
/// lalu mendaftarkannya ke Build Settings — tanpa wiring manual.
///
/// Jalankan dari menu: Tools ▸ Pusaka ▸ Build All Scenes
/// atau headless: -executeMethod PusakaSceneBuilder.BuildAll
/// </summary>
public static class PusakaSceneBuilder
{
    const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
    const string BossPrefabPath = "Assets/Prefabs/Boss.prefab";
    const string SceneDir = "Assets/Scenes/";

    static GameObject enemyPrefab;
    static GameObject bossPrefab;
    static TMP_FontAsset cachedFont;

    [MenuItem("Tools/Pusaka/Build All Scenes")]
    public static void BuildAll()
    {
        Debug.Log("PUSAKA: mulai membangun scene...");

        BuildBossPrefab();           Debug.Log("PUSAKA: boss prefab OK");
        BuildStage("Stage1", false); Debug.Log("PUSAKA: Stage1 OK");
        BuildStage("Stage2", false); Debug.Log("PUSAKA: Stage2 OK");
        BuildStage("Stage3", true);  Debug.Log("PUSAKA: Stage3 OK");
        BuildMainMenu();             Debug.Log("PUSAKA: MainMenu OK");
        BuildDifficulty();           Debug.Log("PUSAKA: DifficultySelect OK");
        BuildEnd("Victory", "MENANG!", new Color(0.05f, 0.12f, 0.06f));
        BuildEnd("GameOver", "GAME OVER", new Color(0.12f, 0.05f, 0.05f));
        SetupBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PUSAKA: Build All Scenes SELESAI.");
    }

    // ---------------------------------------------------------------- Boss prefab
    static void BuildBossPrefab()
    {
        enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        if (enemyPrefab == null) { Debug.LogError("PUSAKA: Enemy.prefab tidak ditemukan!"); return; }

        ConfigureEnemyPrefab();

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab);
        PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        var em = inst.GetComponent<EnemyMovement>(); if (em != null) Object.DestroyImmediate(em);
        var en = inst.GetComponent<Enemy>(); if (en != null) Object.DestroyImmediate(en);

        var boss = inst.AddComponent<Boss>();
        boss.bossName = "Penjaga Pusaka";
        boss.maxHealth = 1500;
        boss.meleeDamage = 30;
        inst.name = "Boss";
        inst.transform.localScale = Vector3.one * 2.2f;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0) inst.layer = enemyLayer;

        bossPrefab = PrefabUtility.SaveAsPrefabAsset(inst, BossPrefabPath);
        Object.DestroyImmediate(inst);
    }

    static void ConfigureEnemyPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0) root.layer = enemyLayer;
        var em = root.GetComponent<EnemyMovement>();
        if (em != null && enemyLayer >= 0) em.enemyMask = 1 << enemyLayer;
        PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // ---------------------------------------------------------------- Stages
    static void BuildStage(string sceneName, bool isFinal)
    {
        var scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);

        var smGO = new GameObject("StageManager");
        var sm = smGO.AddComponent<StageManager>();
        sm.enemyPrefab = enemyPrefab;
        if (isFinal && bossPrefab != null) sm.bossPrefab = bossPrefab;

        // pastikan serangan player mengenai layer Enemy
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
        {
            var combat = playerGO.GetComponent<PlayerCombat>();
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (combat != null && enemyLayer >= 0) combat.enemyLayers = 1 << enemyLayer;
        }

        var canvasGO = BuildHud();
        BuildPauseMenu(canvasGO);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SceneDir + sceneName + ".unity");
    }

    public static GameObject BuildHud()
    {
        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null) canvasGO = MakeCanvas();
        EnsureEventSystem();

        var hud = canvasGO.GetComponent<HUDController>();
        if (hud == null) hud = canvasGO.AddComponent<HUDController>();

        var htGO = GameObject.Find("HealthText");
        if (htGO != null) { var tmp = htGO.GetComponent<TextMeshProUGUI>(); if (tmp != null) hud.healthText = tmp; }

        MakeBar(canvasGO.transform, "HealthBar", new Vector2(-650, -460), new Vector2(440, 36),
            new Color(0.85f, 0.2f, 0.2f), out var hpSlider);
        hud.healthSlider = hpSlider;

        hud.stageText = MakeText(canvasGO.transform, "Stage", new Vector2(-720, 480), new Vector2(360, 60), 34);
        hud.waveText = MakeText(canvasGO.transform, "Wave", new Vector2(0, 480), new Vector2(360, 60), 34);
        hud.scoreText = MakeText(canvasGO.transform, "Skor: 0", new Vector2(720, 480), new Vector2(360, 60), 34);
        hud.centerMessage = MakeText(canvasGO.transform, "", new Vector2(0, 120), new Vector2(1400, 160), 80);

        var bossRoot = new GameObject("BossBar", typeof(RectTransform));
        bossRoot.transform.SetParent(canvasGO.transform, false);
        var brRt = (RectTransform)bossRoot.transform;
        brRt.anchoredPosition = new Vector2(0, 400);
        brRt.sizeDelta = new Vector2(920, 70);
        hud.bossNameText = MakeText(bossRoot.transform, "BOSS", new Vector2(0, 32), new Vector2(920, 44), 32);
        MakeBar(bossRoot.transform, "BossHealth", new Vector2(0, -6), new Vector2(900, 28),
            new Color(0.75f, 0.12f, 0.6f), out var bossSlider);
        hud.bossSlider = bossSlider;
        hud.bossBarRoot = bossRoot;

        // Minimap (pojok kanan atas) + MinimapController
        var miniPanel = new GameObject("Minimap", typeof(RectTransform), typeof(Image));
        miniPanel.transform.SetParent(canvasGO.transform, false);
        var mpRt = (RectTransform)miniPanel.transform;
        mpRt.anchoredPosition = new Vector2(740, 280);
        mpRt.sizeDelta = new Vector2(320, 320);
        miniPanel.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.7f);
        var mapRoot = new GameObject("MapRoot", typeof(RectTransform));
        mapRoot.transform.SetParent(miniPanel.transform, false);
        var mc = canvasGO.GetComponent<MinimapController>();
        if (mc == null) mc = canvasGO.AddComponent<MinimapController>();
        mc.mapRoot = (RectTransform)mapRoot.transform;

        return canvasGO;
    }

    public static void BuildPauseMenu(GameObject canvasGO)
    {
        var pm = new GameObject("PauseMenu").AddComponent<PauseMenuController>();

        var panel = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGO.transform, false);
        var pRt = (RectTransform)panel.transform;
        pRt.anchorMin = Vector2.zero; pRt.anchorMax = Vector2.one;
        pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

        MakeText(panel.transform, "JEDA", new Vector2(0, 250), new Vector2(800, 150), 80);
        var resume = MakeButton(panel.transform, "LANJUT", new Vector2(0, 60), new Vector2(460, 92));
        var restart = MakeButton(panel.transform, "ULANGI STAGE", new Vector2(0, -60), new Vector2(460, 92));
        var quit = MakeButton(panel.transform, "MENU UTAMA", new Vector2(0, -180), new Vector2(460, 92));
        UnityEventTools.AddPersistentListener(resume.onClick, new UnityAction(pm.Resume));
        UnityEventTools.AddPersistentListener(restart.onClick, new UnityAction(pm.RestartStage));
        UnityEventTools.AddPersistentListener(quit.onClick, new UnityAction(pm.QuitToMenu));

        pm.pausePanel = panel;
    }

    // ---------------------------------------------------------------- Menus
    static void BuildMainMenu()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AddCameraAndEvent(new Color(0.06f, 0.07f, 0.10f));

        var gm = new GameObject("GameManager").AddComponent<GameManager>();
        gm.stageScenes = new[] { "Stage1", "Stage2", "Stage3" };
        new GameObject("AudioManager").AddComponent<AudioManager>();

        var canvas = MakeCanvas();
        MakeText(canvas.transform, "PUSAKA TAMANSARI", new Vector2(0, 300), new Vector2(1500, 200), 96);
        MakeText(canvas.transform, "Tekan PLAY untuk memulai petualangan", new Vector2(0, 170), new Vector2(1100, 70), 34);

        var menu = new GameObject("Menu").AddComponent<MainMenuController>();
        var play = MakeButton(canvas.transform, "PLAY", new Vector2(0, 0), new Vector2(440, 96));
        var quit = MakeButton(canvas.transform, "QUIT", new Vector2(0, -130), new Vector2(440, 96));
        UnityEventTools.AddPersistentListener(play.onClick, new UnityAction(menu.PlayGame));
        UnityEventTools.AddPersistentListener(quit.onClick, new UnityAction(menu.QuitGame));

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), SceneDir + "MainMenu.unity");
    }

    static void BuildDifficulty()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AddCameraAndEvent(new Color(0.08f, 0.07f, 0.12f));

        var canvas = MakeCanvas();
        MakeText(canvas.transform, "PILIH KESULITAN", new Vector2(0, 300), new Vector2(1400, 180), 84);

        var ctrl = new GameObject("DifficultyMenu").AddComponent<DifficultySelectController>();
        var easy = MakeButton(canvas.transform, "EASY", new Vector2(0, 90), new Vector2(460, 92));
        var normal = MakeButton(canvas.transform, "NORMAL", new Vector2(0, -30), new Vector2(460, 92));
        var hard = MakeButton(canvas.transform, "HARD", new Vector2(0, -150), new Vector2(460, 92));
        UnityEventTools.AddPersistentListener(easy.onClick, new UnityAction(ctrl.SelectEasy));
        UnityEventTools.AddPersistentListener(normal.onClick, new UnityAction(ctrl.SelectNormal));
        UnityEventTools.AddPersistentListener(hard.onClick, new UnityAction(ctrl.SelectHard));

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), SceneDir + "DifficultySelect.unity");
    }

    static void BuildEnd(string sceneName, string title, Color bg)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AddCameraAndEvent(bg);

        var canvas = MakeCanvas();
        MakeText(canvas.transform, title, new Vector2(0, 300), new Vector2(1400, 200), 96);

        var ctrl = new GameObject("EndScreen").AddComponent<EndScreenController>();
        ctrl.scoreText = MakeText(canvas.transform, "Skor Akhir: 0", new Vector2(0, 130), new Vector2(1000, 90), 48);

        var retry = MakeButton(canvas.transform, "ULANGI", new Vector2(0, -30), new Vector2(460, 92));
        var menu = MakeButton(canvas.transform, "MENU UTAMA", new Vector2(0, -150), new Vector2(460, 92));
        UnityEventTools.AddPersistentListener(retry.onClick, new UnityAction(ctrl.Retry));
        UnityEventTools.AddPersistentListener(menu.onClick, new UnityAction(ctrl.MainMenu));

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), SceneDir + sceneName + ".unity");
    }

    // ---------------------------------------------------------------- Build settings
    static void SetupBuildSettings()
    {
        string[] order = { "MainMenu", "DifficultySelect", "Stage1", "Stage2", "Stage3", "Victory", "GameOver" };
        var list = new List<EditorBuildSettingsScene>();
        foreach (var n in order)
            list.Add(new EditorBuildSettingsScene(SceneDir + n + ".unity", true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    // ---------------------------------------------------------------- Helpers UI
    static GameObject MakeCanvas(string name = "Canvas")
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        return go;
    }

    static void AddCameraAndEvent(Color bg)
    {
        var camGO = new GameObject("Main Camera", typeof(Camera));
        camGO.tag = "MainCamera";
        var cam = camGO.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = bg;
        cam.orthographic = true;
        EnsureEventSystem();
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    static TextMeshProUGUI MakeText(Transform parent, string text, Vector2 pos, Vector2 size, float fontSize)
    {
        var go = new GameObject("Text (" + text + ")", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        var f = GetFont();
        if (f != null) t.font = f;
        return t;
    }

    static Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(0.20f, 0.26f, 0.36f, 1f);
        var label2 = MakeText(go.transform, label, Vector2.zero, size, 38);
        label2.fontStyle = FontStyles.Bold;
        return go.GetComponent<Button>();
    }

    static void MakeBar(Transform parent, string name, Vector2 pos, Vector2 size, Color fillColor, out Slider slider)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Slider));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var faRt = (RectTransform)fillArea.transform;
        faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one;
        faRt.offsetMin = new Vector2(3, 3); faRt.offsetMax = new Vector2(-3, -3);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fRt = (RectTransform)fill.transform;
        fRt.anchorMin = Vector2.zero; fRt.anchorMax = Vector2.one;
        fRt.offsetMin = Vector2.zero; fRt.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = fillColor;

        slider = go.GetComponent<Slider>();
        slider.fillRect = fRt;
        slider.targetGraphic = fill.GetComponent<Image>();
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;
    }

    static TMP_FontAsset GetFont()
    {
        if (cachedFont != null) return cachedFont;
        if (TMP_Settings.defaultFontAsset != null) { cachedFont = TMP_Settings.defaultFontAsset; return cachedFont; }
        var guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        if (guids.Length > 0)
            cachedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        return cachedFont;
    }
}
