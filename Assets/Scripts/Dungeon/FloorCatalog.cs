using UnityEngine;

/// <summary>
/// Arsitektur denah lantai. Gagasannya dipinjam dari RogueElements: generasi
/// disusun sebagai langkah yang bisa ditukar, bukan satu algoritma tetap.
/// Library-nya sendiri tidak dipakai -- model datanya (satu peta tile menyambung)
/// tidak cocok dengan ruangan-pulau yang dihubungkan pintu di game ini.
/// </summary>
public enum FloorLayout
{
    /// <summary>Pertumbuhan frontier: pohon bercabang, banyak buntu. Mudah dibaca.</summary>
    Bercabang,
    /// <summary>Cincin tertutup dengan beberapa cabang keluar. Selalu ada dua arah.</summary>
    Melingkar,
    /// <summary>Gumpalan padat dengan banyak jalan pintas. Mudah tersesat.</summary>
    Gua,
    /// <summary>Satu lorong utama panjang dengan ruangan menempel di sisinya.</summary>
    Tulang,
    /// <summary>Petak rapat seperti denah bangunan. Rapi dan saling terhubung.</summary>
    Petak,
}

/// <summary>
/// Data satu lantai: tema visual, jumlah ruangan, dan daftar musuh/boss.
/// Dipisah dari generator supaya isi lantai bisa diubah tanpa menyentuh algoritma.
/// </summary>
[System.Serializable]
public class FloorProfile
{
    [Header("Identitas")]
    public string sceneName = "Floor1";
    public string title = "Lantai";
    [TextArea(2, 4)] public string lore = "";

    [Header("Tema visual")]
    [Tooltip("Lantai terbuka condong ke properti tanaman; tertutup ke tulang dan kristal")]
    public bool outdoor = false;
    [Tooltip("Ruangan mendapat kolam air di tengahnya")]
    public bool water = false;
    public Color floorTint = Color.white;
    public Color wallTint = Color.white;
    [Tooltip("Warna latar kamera di luar tembok; juga dipakai sebagai selubung gelap ruangan")]
    public Color skyTint = new Color(0.06f, 0.06f, 0.10f);

    [Header("Cahaya")]
    [Tooltip("Seberapa gelap selubung ruangan. Makin tinggi, makin kontras cahaya obornya.")]
    [Range(0f, 0.9f)] public float ambientStrength = 0.55f;
    [Tooltip("Warna cahaya obor. Alpha = terang dasarnya.")]
    public Color lightColor = new Color(1f, 0.75f, 0.41f, 0.30f);
    [Tooltip("Jangkauan cahaya dalam unit dunia")]
    public float lightRadius = 4.2f;

    [Header("Bentuk lantai")]
    [Tooltip("Arsitektur denah. Tiap lantai memakai algoritma berbeda supaya terasa sebagai tempat yang berbeda.")]
    public FloorLayout layout = FloorLayout.Bercabang;

    [Range(6, 30)] public int roomCount = 14;
    [Tooltip("Peluang menambah lorong pintas selain jalur utama")]
    [Range(0f, 1f)] public float loopChance = 0.25f;
    [Tooltip("Kepadatan properti hias per ruangan")]
    [Range(0f, 3f)] public float propDensity = 1f;

    [Header("Isi")]
    public GameObject[] enemies;
    public GameObject[] elites;
    public GameObject boss;
    [Range(1, 20)] public int enemiesPerRoom = 6;
}

/// <summary>
/// Aset tunggal di Resources berisi profil semua lantai. Diisi lewat
/// Tools > Pusaka > Rebuild Content, dibaca saat runtime oleh DungeonGenerator.
/// </summary>
public class FloorCatalog : ScriptableObject
{
    public FloorProfile[] floors;

    static FloorCatalog cached;
    public static FloorCatalog I
    {
        get
        {
            if (cached == null) cached = Resources.Load<FloorCatalog>("FloorCatalog");
            return cached;
        }
    }

    public static FloorProfile Get(int index)
    {
        var c = I;
        if (c == null || c.floors == null || c.floors.Length == 0) return null;
        return c.floors[Mathf.Clamp(index, 0, c.floors.Length - 1)];
    }

    public static int Count => (I != null && I.floors != null) ? I.floors.Length : 0;
}
