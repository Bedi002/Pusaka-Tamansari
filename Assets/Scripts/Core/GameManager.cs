using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Otak utama game yang hidup lintas-scene (DontDestroyOnLoad).
/// Menyimpan tingkat kesulitan, progres stage, dan skor; serta mengatur
/// perpindahan antar-scene (menu, stage, menang, kalah).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Kesulitan")]
    public Difficulty difficulty = Difficulty.Normal;

    [Header("Progres")]
    [Tooltip("Index stage saat ini (0 = stage pertama)")]
    public int currentStageIndex = 0;
    public int score = 0;

    [Header("Hero (pilih di CharacterSelect)")]
    [Tooltip("Urutan = Warrior, Archer, Mage (diisi otomatis oleh builder)")]
    public GameObject[] heroPrefabs;
    [Tooltip("Index hero terpilih (di-set layar CharacterSelect)")]
    public int selectedHero = 0;

    /// <summary>
    /// Prefab hero terpilih. HeroCatalog (Resources) diutamakan; array heroPrefabs
    /// hanya cadangan untuk scene lama yang masih menyimpan referensinya.
    /// </summary>
    public GameObject SelectedHeroPrefab
    {
        get
        {
            var hero = HeroCatalog.Get(selectedHero);
            if (hero != null && hero.prefab != null) return hero.prefab;
            return (heroPrefabs != null && selectedHero >= 0 && selectedHero < heroPrefabs.Length)
                ? heroPrefabs[selectedHero] : null;
        }
    }

    [Header("Isi tas run ini")]
    [Tooltip("Bertahan antar-lantai; player di-spawn ulang tiap lantai jadi tas tidak boleh ikut hilang")]
    public RunInventory run = new RunInventory();

    [Header("Scaling per Stage")]
    [Tooltip("Tambahan kekuatan musuh tiap naik stage. 0.25 = +25% per stage")]
    public float stageGrowth = 0.25f;

    [Header("Nama Scene (WAJIB ada di Build Settings)")]
    public string mainMenuScene = "MainMenu";
    public string difficultyScene = "DifficultySelect";
    public string victoryScene = "Victory";
    public string gameOverScene = "GameOver";
    public string[] stageScenes = { "Floor1", "Floor2", "Floor3", "Floor4", "Floor5" };

    // ---- Properti turunan ----
    public DifficultyProfile Profile => DifficultyTable.Get(difficulty);
    public int StageNumber => currentStageIndex + 1;
    public int TotalStages => stageScenes != null ? stageScenes.Length : 0;
    public bool IsFinalStage => currentStageIndex >= TotalStages - 1;
    /// <summary>Pengali kekuatan musuh berdasarkan stage saat ini.</summary>
    public float StageScale => 1f + currentStageIndex * stageGrowth;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Jaring pengaman: bila game dijalankan langsung dari scene stage (untuk testing)
    /// dan belum ada GameManager, buat satu pakai nilai default. Memakai AfterSceneLoad
    /// supaya GameManager yang sudah dipasang & diatur di Inspector tetap diutamakan.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        if (Instance == null && FindAnyObjectByType<GameManager>() == null)
        {
            var go = new GameObject("GameManager (auto)");
            go.AddComponent<GameManager>();
        }
    }

    public void SetDifficulty(Difficulty d) => difficulty = d;

    public void StartGame()
    {
        currentStageIndex = 0;
        score = 0;

        // run baru = tas kosong + bekal awal sesuai hero
        run.Clear();
        var hero = HeroCatalog.Get(selectedHero);
        if (hero != null && !string.IsNullOrEmpty(hero.startingItemId))
        {
            var def = ItemDatabase.Get(hero.startingItemId);
            if (def != null)
            {
                if (def.IsEquipment && def.category == ItemCategory.Weapon) run.weaponId = def.id;
                else run.slots.Add(new ItemStack(def.id, 1));
            }
        }

        LoadStage();
    }

    public void AdvanceStage()
    {
        if (IsFinalStage) { Victory(); return; }
        currentStageIndex++;
        LoadStage();
    }

    void LoadStage()
    {
        Time.timeScale = 1f;
        if (TotalStages > 0 && currentStageIndex >= 0 && currentStageIndex < TotalStages)
            SceneManager.LoadScene(stageScenes[currentStageIndex]);
        else
            Victory();
    }

    public void Victory()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(victoryScene);
    }

    public void GameOver()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameOverScene);
    }

    public void RetryFromStart() => StartGame();

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void AddScore(int amount)
    {
        // jimat penambah perolehan (blangkon/batik) menaikkan skor yang masuk
        var inv = Inventory.Instance;
        if (inv != null)
        {
            var stats = inv.GetComponent<PlayerStats>();
            if (stats != null && stats.XpGain != 0f)
                amount = Mathf.RoundToInt(amount * (1f + stats.XpGain));
        }

        score += amount;
        if (HUDController.Instance != null) HUDController.Instance.SetScore(score);
    }
}
