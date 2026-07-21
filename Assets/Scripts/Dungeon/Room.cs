using System.Collections.Generic;
using UnityEngine;

// Nilai baru DITAMBAH DI BELAKANG supaya angka yang sudah tersimpan di scene lama
// tidak bergeser artinya.
public enum RoomType { Start, Combat, Boss, Treasure, Hidden, Elite, Shop, Shrine }
public enum RoomState { Unvisited, Active, Cleared }

/// <summary>
/// Satu ruangan dungeon (roguelike). transform.position = pusat ruangan.
/// Ruangan Combat: saat player masuk & belum clear -> kunci pintu + spawn musuh;
/// semua musuh mati -> clear + buka pintu. Boss: spawn boss; boss kalah -> floor selesai.
/// Start/Treasure/Hidden dianggap langsung clear (tidak ada tempur).
/// </summary>
public class Room : MonoBehaviour
{
    public RoomType type = RoomType.Combat;
    [Tooltip("Koordinat grid untuk minimap")]
    public Vector2Int gridPos;
    [Tooltip("Ukuran area ruangan (untuk framing kamera)")]
    public Vector2 size = new Vector2(16, 10);

    [Header("Tempur")]
    [Tooltip("Dipakai bila enemyPrefabs kosong (denah lama)")]
    public GameObject enemyPrefab;
    [Tooltip("Campuran tipe musuh; tiap spawn mengambil satu secara acak")]
    public GameObject[] enemyPrefabs;
    public GameObject bossPrefab;
    public int enemyCount = 4;
    public Transform[] spawnPoints;

    [Header("Pintu (auto-isi dari child bila kosong)")]
    public List<Door> doors = new List<Door>();

    public RoomState State { get; private set; } = RoomState.Unvisited;
    public bool IsCleared => State == RoomState.Cleared;
    public Vector3 Center => transform.position;

    int aliveCount = 0;

    void Awake()
    {
        if (doors == null || doors.Count == 0)
            doors = new List<Door>(GetComponentsInChildren<Door>(true));

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            var pts = new List<Transform>();
            foreach (var sp in GetComponentsInChildren<Spawner>(true)) pts.Add(sp.transform);
            spawnPoints = pts.ToArray();
        }

        if (type == RoomType.Start || type == RoomType.Treasure || type == RoomType.Hidden
            || type == RoomType.Shop || type == RoomType.Shrine)
            State = RoomState.Cleared;
    }

    public void OnPlayerEnter()
    {
        if (IsCleared) { OpenDoors(); return; }
        if (type == RoomType.Combat || type == RoomType.Elite) StartCombat();
        else if (type == RoomType.Boss) StartBoss();
        else { State = RoomState.Cleared; OpenDoors(); }
    }

    /// <summary>Satu musuh acak dari campuran; jatuh ke enemyPrefab bila daftar kosong.</summary>
    GameObject PickEnemy()
    {
        if (enemyPrefabs != null && enemyPrefabs.Length > 0)
        {
            // coba beberapa kali kalau ada slot null di daftar
            for (int k = 0; k < 6; k++)
            {
                var p = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                if (p != null) return p;
            }
        }
        return enemyPrefab;
    }

    void StartCombat()
    {
        State = RoomState.Active;
        LockDoors();

        var profile = GameManager.Instance != null ? GameManager.Instance.Profile : DifficultyTable.Get(Difficulty.Normal);
        float scale = GameManager.Instance != null ? GameManager.Instance.StageScale : 1f;
        if (type == RoomType.Elite) scale *= 1.35f;
        int n = Mathf.Max(1, Mathf.RoundToInt(enemyCount * profile.spawnCountMult * scale));

        aliveCount = 0;
        for (int i = 0; i < n; i++)
        {
            var prefab = PickEnemy();
            if (prefab == null) break;
            var go = Instantiate(prefab, SpawnPos(i), Quaternion.identity);
            if (type == RoomType.Elite) go.transform.localScale *= 1.25f;
            if (go.GetComponent<YSort>() == null) go.AddComponent<YSort>();
            var e = go.GetComponent<Enemy>();
            if (e != null) { e.Configure(profile, scale); e.Died += OnEnemyDied; aliveCount++; }
        }
        if (aliveCount == 0) Clear();
    }

    void OnEnemyDied(Enemy e)
    {
        e.Died -= OnEnemyDied;
        aliveCount = Mathf.Max(0, aliveCount - 1);
        if (aliveCount == 0) Clear();
    }

    void StartBoss()
    {
        State = RoomState.Active;
        LockDoors();

        var profile = GameManager.Instance != null ? GameManager.Instance.Profile : DifficultyTable.Get(Difficulty.Normal);
        float scale = GameManager.Instance != null ? GameManager.Instance.StageScale : 1f;

        var prefab = bossPrefab != null ? bossPrefab : enemyPrefab;
        if (prefab == null) { Clear(); return; }
        var go = Instantiate(prefab, SpawnPos(0), Quaternion.identity);
        if (go.GetComponent<YSort>() == null) go.AddComponent<YSort>();

        var b = go.GetComponent<Boss>();
        if (b != null) { b.Configure(profile, scale); b.Defeated += OnBossDefeated; }
        else
        {
            var e = go.GetComponent<Enemy>();
            if (e != null) { e.Configure(profile, scale); e.Died += OnEnemyDied; aliveCount = 1; }
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(AudioManager.Instance.bossMusic);
    }

    void OnBossDefeated(Boss b)
    {
        b.Defeated -= OnBossDefeated;
        Clear();
        if (DungeonManager.Instance != null) DungeonManager.Instance.OnBossDefeated();
    }

    void Clear()
    {
        State = RoomState.Cleared;
        OpenDoors();
        if ((type == RoomType.Combat || type == RoomType.Elite) && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.stageClear);
        LootTable.DropRoomReward(transform.position, type);
        if (DungeonManager.Instance != null) DungeonManager.Instance.OnRoomCleared(this);
    }

    Vector3 SpawnPos(int i)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            var t = spawnPoints[i % spawnPoints.Length];
            if (t != null) return t.position;
        }
        Vector2 off = Random.insideUnitCircle * Mathf.Min(size.x, size.y) * 0.3f;
        return transform.position + (Vector3)off;
    }

    public void LockDoors() { foreach (var d in doors) if (d != null) d.SetLocked(true); }
    public void OpenDoors() { foreach (var d in doors) if (d != null) d.SetLocked(false); }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.75f, 0.3f, 0.5f);
        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0));
    }
}
