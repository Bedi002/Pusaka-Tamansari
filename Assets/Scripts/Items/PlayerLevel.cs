using UnityEngine;

/// <summary>
/// Naik level dari skor yang terkumpul. Tiap level memberi bonus permanen kecil
/// (HP maks, serangan) lewat PlayerStats dan memulihkan HP penuh -- momen "napas"
/// yang membuat kemajuan terasa, bukan sekadar angka naik.
///
/// XP = skor. GameManager.AddScore sudah dipanggil tiap musuh mati, jadi level
/// mengikuti skor tanpa jalur data kedua.
/// </summary>
public class PlayerLevel : MonoBehaviour
{
    public static PlayerLevel Instance { get; private set; }

    [Tooltip("XP untuk naik dari level 1 ke 2")]
    public int baseCost = 60;
    [Tooltip("Pengali biaya tiap level. 1.35 = tiap level 35% lebih mahal")]
    public float growth = 1.35f;

    public int Level { get; private set; } = 1;
    public int XpIntoLevel { get; private set; }
    public int XpForNext { get; private set; }

    PlayerStats stats;
    PlayerHealth health;
    int lastScore;

    void Awake()
    {
        Instance = this;
        stats = GetComponent<PlayerStats>();
        health = GetComponent<PlayerHealth>();
        XpForNext = baseCost;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        // mulai dari skor yang sudah ada (mis. lanjut lantai), jangan hitung ulang
        lastScore = GameManager.Instance != null ? GameManager.Instance.score : 0;
        PushHud();
    }

    void Update()
    {
        int score = GameManager.Instance != null ? GameManager.Instance.score : lastScore;
        if (score <= lastScore) return;

        XpIntoLevel += score - lastScore;
        lastScore = score;

        while (XpIntoLevel >= XpForNext) { XpIntoLevel -= XpForNext; LevelUp(); }
        PushHud();
    }

    void LevelUp()
    {
        Level++;
        XpForNext = Mathf.RoundToInt(baseCost * Mathf.Pow(growth, Level - 1));

        // bonus permanen run ini (bertahan antar-lantai lewat RunInventory)
        if (stats != null)
        {
            stats.AddPermanent(EffectKind.MaxHp, 8f);
            stats.AddPermanent(EffectKind.Damage, 2f);
            if (Level % 3 == 0) stats.AddPermanent(EffectKind.MoveSpeed, 0.1f);
        }
        if (health != null) health.Heal(9999);   // sembuh penuh; Heal sudah membatasi ke maks

        if (AudioManager.Instance != null) AudioManager.Instance.Play("ui_confirm", 1f);
        FloatingText.Spawn(transform.position, $"LEVEL {Level}", UIKit.Gold, true);
        if (HUDController.Instance != null) HUDController.Instance.ShowMessage($"Naik ke Level {Level}", 1.6f);
    }

    void PushHud()
    {
        if (HUDController.Instance != null)
            HUDController.Instance.SetLevel(Level, XpIntoLevel, XpForNext);
    }
}
