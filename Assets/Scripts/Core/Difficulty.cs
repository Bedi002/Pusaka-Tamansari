using UnityEngine;

/// <summary>Tiga tingkat kesulitan game.</summary>
public enum Difficulty { Easy, Normal, Hard }

/// <summary>
/// Kumpulan pengali yang dipakai untuk menyesuaikan musuh & player
/// berdasarkan tingkat kesulitan. Semua nilai relatif terhadap Normal (1.0).
/// </summary>
[System.Serializable]
public class DifficultyProfile
{
    public string label = "Normal";

    [Header("Pengali Musuh")]
    public float enemyHealthMult = 1f;
    public float enemyDamageMult = 1f;
    public float enemySpeedMult = 1f;

    [Header("Pengali Spawn")]
    [Tooltip("Pengali jumlah musuh per wave")]
    public float spawnCountMult = 1f;
    [Tooltip("Pengali jeda antar spawn (lebih kecil = musuh datang lebih cepat)")]
    public float spawnIntervalMult = 1f;

    [Header("Pemain")]
    [Tooltip("Pengali besar damage yang DITERIMA player")]
    public float playerDamageTakenMult = 1f;
}

/// <summary>Preset bawaan untuk tiap tingkat kesulitan. Tweak di sini bila perlu.</summary>
public static class DifficultyTable
{
    public static DifficultyProfile Get(Difficulty d)
    {
        switch (d)
        {
            case Difficulty.Easy:
                return new DifficultyProfile
                {
                    label = "Easy",
                    enemyHealthMult = 0.7f,
                    enemyDamageMult = 0.6f,
                    enemySpeedMult = 0.85f,
                    spawnCountMult = 0.7f,
                    spawnIntervalMult = 1.4f,
                    playerDamageTakenMult = 0.6f
                };
            case Difficulty.Hard:
                return new DifficultyProfile
                {
                    label = "Hard",
                    enemyHealthMult = 1.5f,
                    enemyDamageMult = 1.5f,
                    enemySpeedMult = 1.2f,
                    spawnCountMult = 1.4f,
                    spawnIntervalMult = 0.65f,
                    playerDamageTakenMult = 1.5f
                };
            default:
                return new DifficultyProfile { label = "Normal" };
        }
    }
}
