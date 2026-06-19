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

    /// <summary>Prefab hero yang sedang dipilih (null bila belum di-set).</summary>
    public GameObject SelectedHeroPrefab =>
        (heroPrefabs != null && selectedHero >= 0 && selectedHero < heroPrefabs.Length)
            ? heroPrefabs[selectedHero] : null;

    [Header("Scaling per Stage")]
    [Tooltip("Tambahan kekuatan musuh tiap naik stage. 0.25 = +25% per stage")]
    public float stageGrowth = 0.25f;

    [Header("Nama Scene (WAJIB ada di Build Settings)")]
    public string mainMenuScene = "MainMenu";
    public string difficultyScene = "DifficultySelect";
    public string victoryScene = "Victory";
    public string gameOverScene = "GameOver";
    public string[] stageScenes = { "Floor1", "Floor2", "Floor3" };

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
        if (Instance == null && FindFirstObjectByType<GameManager>() == null)
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
        score += amount;
        if (HUDController.Instance != null) HUDController.Instance.SetScore(score);
    }
}
