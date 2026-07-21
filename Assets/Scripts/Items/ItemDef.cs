using UnityEngine;

/// <summary>Kategori item -- menentukan slot dan cara pakai.</summary>
public enum ItemCategory { Weapon, Armor, Charm, Consumable, Pusaka, Material }

/// <summary>Tingkat kelangkaan; dipakai untuk warna bingkai dan bobot loot.</summary>
public enum Rarity { Umum, Langka, Pusaka, Sakti, Legenda }

/// <summary>
/// Kosakata efek. Sengaja dibuat pendek: tiap efek harus bisa dieksekusi
/// PlayerStats/Inventory tanpa cabang khusus per-item.
/// </summary>
public enum EffectKind
{
    MaxHp,        // + nilai (flat)
    Damage,       // + nilai (flat)
    DamagePct,    // + nilai persen, 0.25 = +25%
    MoveSpeed,    // + nilai (unit/detik)
    AttackSpeed,  // + nilai persen (memperpendek cooldown)
    Range,        // + nilai (unit)
    Armor,        // pengurangan damage persen, 0.1 = -10% damage masuk
    CritChance,   // peluang 0..1 untuk damage x2
    LifeSteal,    // persen damage yang jadi HP
    Thorns,       // damage balik flat ke penyerang
    Heal,         // sekali pakai: pulihkan HP
    Gold,         // sekali pakai: tambah uang
    Luck,         // menaikkan kualitas loot
    Dodge,        // peluang 0..1 menghindar total dari serangan
    XpGain,       // + nilai persen skor yang didapat
    Revive        // sekali per run: batalkan kematian, bangkit dengan value% HP
}

[System.Serializable]
public class ItemEffect
{
    public EffectKind kind;
    public float value;
    [Tooltip("0 = permanen selama item dipakai/dipegang; >0 = efek sementara (detik)")]
    public float duration;

    public ItemEffect() { }
    public ItemEffect(EffectKind k, float v, float d = 0f) { kind = k; value = v; duration = d; }
}

[System.Serializable]
public class ItemDef
{
    public string id = "item";
    public string displayName = "Barang";
    [TextArea(1, 3)] public string flavor = "";
    public ItemCategory category = ItemCategory.Material;
    public Rarity rarity = Rarity.Umum;

    [Header("Sprite")]
    [Tooltip("Index tile pada tileset")]
    public int tileIndex = -1;
    [Tooltip("true = Tiny Town, false = Tiny Dungeon")]
    public bool fromTown = false;
    [Tooltip("Ikon khusus (art AI); bila diisi, dipakai menggantikan tile")]
    public Sprite iconOverride;

    [Header("Aturan")]
    public ItemEffect[] effects;
    public int price = 10;
    [Min(1)] public int stackMax = 1;

    public bool IsEquipment => category == ItemCategory.Weapon
                            || category == ItemCategory.Armor
                            || category == ItemCategory.Charm;
    public bool IsConsumable => category == ItemCategory.Consumable;

    public Sprite Icon => iconOverride != null
        ? iconOverride
        : (fromTown ? ArtLibrary.T(tileIndex) : ArtLibrary.D(tileIndex));

    public static Color RarityColor(Rarity r)
    {
        switch (r)
        {
            case Rarity.Langka: return new Color(0.36f, 0.72f, 0.94f);   // biru
            case Rarity.Pusaka: return new Color(0.62f, 0.44f, 0.88f);   // ungu
            case Rarity.Sakti: return new Color(0.89f, 0.70f, 0.25f);    // emas
            case Rarity.Legenda: return new Color(0.91f, 0.44f, 0.32f);  // bara
            default: return new Color(0.75f, 0.76f, 0.80f);              // abu
        }
    }
}

/// <summary>
/// Katalog seluruh item. Satu aset di Resources supaya bisa dibaca saat runtime
/// tanpa AssetDatabase. Diisi lewat Tools > Pusaka > Rebuild Content.
/// </summary>
public class ItemDatabase : ScriptableObject
{
    public ItemDef[] items;

    static ItemDatabase cached;
    public static ItemDatabase I
    {
        get
        {
            if (cached == null) cached = Resources.Load<ItemDatabase>("ItemDatabase");
            return cached;
        }
    }

    public static ItemDef Get(string id)
    {
        var db = I;
        if (db == null || db.items == null) return null;
        foreach (var it in db.items) if (it != null && it.id == id) return it;
        return null;
    }

    /// <summary>Item acak yang cocok untuk sebuah tingkat kelangkaan (fallback ke bawah).</summary>
    public static ItemDef RandomOf(Rarity rarity, ItemCategory? category = null)
    {
        var db = I;
        if (db == null || db.items == null || db.items.Length == 0) return null;

        var pool = new System.Collections.Generic.List<ItemDef>();
        for (int step = 0; step <= (int)rarity && pool.Count == 0; step++)
        {
            var want = (Rarity)((int)rarity - step);
            foreach (var it in db.items)
            {
                if (it == null || it.rarity != want) continue;
                if (it.category == ItemCategory.Material) continue;
                if (category.HasValue && it.category != category.Value) continue;
                pool.Add(it);
            }
        }
        return pool.Count == 0 ? null : pool[Random.Range(0, pool.Count)];
    }
}
