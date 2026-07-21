using UnityEngine;

/// <summary>Satu hero yang bisa dipilih, lengkap dengan stat dasarnya.</summary>
[System.Serializable]
public class HeroProfile
{
    public string id = "warrior";
    public string displayName = "SENOPATI";
    public string role = "Petarung Jarak Dekat";
    [TextArea(2, 4)] public string blurb = "";

    public GameObject prefab;

    [Header("Stat dasar")]
    public int maxHp = 100;
    public int damage = 20;
    public float moveSpeed = 5f;
    public float attackRange = 0.9f;
    public float attackCooldown = 0.4f;

    [Header("Bekal awal")]
    public string startingItemId = "";

    [Header("Potret (index tile Tiny Dungeon)")]
    public int portraitTile = 96;
}

/// <summary>
/// Daftar hero sebagai satu aset di Resources.
///
/// Ini sekaligus perbaikan bug "cuma satu hero yang bisa dipilih": dulu ketiga
/// prefab hanya hidup sebagai referensi yang di-serialize di scene MainMenu, jadi
/// begitu game dimulai dari scene lain (atau referensi itu putus) GameManager
/// selalu jatuh ke hero pertama. Sekarang datanya dimuat dari Resources sehingga
/// tidak bergantung pada scene mana pun.
/// </summary>
public class HeroCatalog : ScriptableObject
{
    public HeroProfile[] heroes;

    static HeroCatalog cached;
    public static HeroCatalog I
    {
        get
        {
            if (cached == null) cached = Resources.Load<HeroCatalog>("HeroCatalog");
            return cached;
        }
    }

    public static int Count => (I != null && I.heroes != null) ? I.heroes.Length : 0;

    public static HeroProfile Get(int index)
    {
        if (Count == 0) return null;
        return I.heroes[Mathf.Clamp(index, 0, Count - 1)];
    }

    /// <summary>Terapkan stat hero ke instance yang baru di-spawn.</summary>
    public static void Apply(HeroProfile hero, GameObject instance)
    {
        if (hero == null || instance == null) return;

        // SetMaxHealth (bukan set field langsung): PlayerHealth.Awake sudah jalan
        // saat Instantiate, jadi HP sekarang perlu ikut disesuaikan.
        var hp = instance.GetComponent<PlayerHealth>();
        if (hp != null) hp.SetMaxHealth(hero.maxHp);

        var combat = instance.GetComponent<PlayerCombat>();
        if (combat != null)
        {
            combat.attackDamage = hero.damage;
            combat.attackRange = hero.attackRange;
            combat.comboCooldown = hero.attackCooldown;
        }

        var move = instance.GetComponent<PlayerMovement>();
        if (move != null) move.moveSpeed = hero.moveSpeed;
    }
}
