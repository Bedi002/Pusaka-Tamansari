using System.Collections;
using UnityEngine;

/// <summary>
/// Director satu stage. Membaca tingkat kesulitan & index stage dari
/// GameManager, lalu menjalankan beberapa wave musuh. Jumlah & kekuatan
/// musuh dikalikan oleh difficulty dan oleh nomor stage.
///
/// Musuh hidup dilacak lewat event Enemy.Died (bukan polling tag),
/// jadi deteksi "ruangan bersih" akurat & murah.
///
/// Di stage TERAKHIR: setelah semua wave habis, Boss dimunculkan; saat boss
/// kalah -> GameManager.Victory(). Di stage biasa: clear -> AdvanceStage().
/// </summary>
public class StageManager : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject enemyPrefab;
    [Tooltip("Hanya dipakai di stage terakhir")]
    public GameObject bossPrefab;

    [Header("Titik Spawn (kosong = pakai objek ber-komponen Spawner, atau posisi StageManager)")]
    public Transform[] spawnPoints;

    [Header("Pengaturan Wave (basis, sebelum dikali difficulty & stage)")]
    public int waveCount = 3;
    public int baseEnemiesPerWave = 4;
    public float timeBetweenSpawns = 0.6f;
    public float timeBetweenWaves = 2.5f;
    [Tooltip("Batas musuh hidup bersamaan di layar")]
    public int maxAliveAtOnce = 8;

    [Header("Opsional")]
    public GameObject exitDoor;     // diaktifkan saat stage bersih (visual)
    public float clearDelay = 2.5f; // jeda sebelum pindah stage / menang

    int aliveCount = 0;
    int currentWave = 0;
    bool stageEnding = false;

    DifficultyProfile profile;
    float stageScale = 1f;
    bool isFinalStage = false;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            profile = GameManager.Instance.Profile;
            stageScale = GameManager.Instance.StageScale;
            isFinalStage = GameManager.Instance.IsFinalStage;
            if (HUDController.Instance != null)
            {
                HUDController.Instance.SetStage(GameManager.Instance.StageNumber, GameManager.Instance.TotalStages);
                HUDController.Instance.SetScore(GameManager.Instance.score);
            }
        }
        else
        {
            // Mode tes mandiri (tanpa GameManager) -> anggap Normal & stage terakhir.
            profile = DifficultyTable.Get(Difficulty.Normal);
            isFinalStage = true;
        }

        CollectSpawnPointsIfEmpty();

        if (exitDoor != null) exitDoor.SetActive(false);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(AudioManager.Instance.battleMusic);

        StartCoroutine(RunStage());
    }

    void CollectSpawnPointsIfEmpty()
    {
        if (spawnPoints != null && spawnPoints.Length > 0) return;
        var found = FindObjectsByType<Spawner>(FindObjectsSortMode.None);
        if (found.Length > 0)
        {
            spawnPoints = new Transform[found.Length];
            for (int i = 0; i < found.Length; i++) spawnPoints[i] = found[i].transform;
        }
    }

    int EnemiesPerWave()
    {
        float n = baseEnemiesPerWave * profile.spawnCountMult * stageScale;
        return Mathf.Max(1, Mathf.RoundToInt(n));
    }

    float SpawnInterval() => Mathf.Max(0.05f, timeBetweenSpawns * profile.spawnIntervalMult);

    IEnumerator RunStage()
    {
        for (currentWave = 1; currentWave <= waveCount; currentWave++)
        {
            if (HUDController.Instance != null) HUDController.Instance.SetWave(currentWave, waveCount);

            int target = EnemiesPerWave();
            int spawned = 0;
            while (spawned < target)
            {
                while (aliveCount >= maxAliveAtOnce) yield return null;
                SpawnEnemy();
                spawned++;
                yield return new WaitForSeconds(SpawnInterval());
            }

            // tunggu wave ini bersih
            while (aliveCount > 0) yield return null;

            if (currentWave < waveCount)
                yield return new WaitForSeconds(timeBetweenWaves);
        }

        if (isFinalStage && bossPrefab != null)
        {
            yield return new WaitForSeconds(1f);
            SpawnBoss(); // boss yang akan memicu Victory saat kalah
        }
        else
        {
            StageClear();
        }
    }

    Vector3 PickSpawnPos()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            var t = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (t != null) return t.position;
        }
        return transform.position;
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null) return;
        var go = Instantiate(enemyPrefab, PickSpawnPos(), Quaternion.identity);
        var enemy = go.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Configure(profile, stageScale);
            enemy.Died += OnEnemyDied;
            aliveCount++;
        }
    }

    void OnEnemyDied(Enemy e)
    {
        e.Died -= OnEnemyDied;
        aliveCount = Mathf.Max(0, aliveCount - 1);
    }

    void SpawnBoss()
    {
        var go = Instantiate(bossPrefab, PickSpawnPos(), Quaternion.identity);
        var boss = go.GetComponent<Boss>();
        if (boss != null)
        {
            boss.Configure(profile, stageScale);
            boss.Defeated += OnBossDefeated;
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(AudioManager.Instance.bossMusic);
    }

    void OnBossDefeated(Boss b)
    {
        b.Defeated -= OnBossDefeated;
        if (HUDController.Instance != null)
        {
            HUDController.Instance.HideBossBar();
            HUDController.Instance.ShowMessage("MENANG!", clearDelay);
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.victory);
        StartCoroutine(EndAfter(() => { if (GameManager.Instance != null) GameManager.Instance.Victory(); }));
    }

    void StageClear()
    {
        if (stageEnding) return;
        stageEnding = true;

        if (exitDoor != null) exitDoor.SetActive(true);
        if (HUDController.Instance != null) HUDController.Instance.ShowMessage("STAGE CLEAR!", clearDelay);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.stageClear);

        StartCoroutine(EndAfter(() => { if (GameManager.Instance != null) GameManager.Instance.AdvanceStage(); }));
    }

    IEnumerator EndAfter(System.Action action)
    {
        yield return new WaitForSeconds(clearDelay);
        action?.Invoke();
    }
}
