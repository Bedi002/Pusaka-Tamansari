using UnityEngine;

/// <summary>
/// Kumpulan properti art AI (Assets/Art/Generated/Props) yang bisa dibaca saat
/// runtime oleh DungeonGenerator. Satu aset di Resources, diisi ContentBuilder.
///
/// Terpisah dari ArtLibrary (tileset 16px) karena prop ini ilustrasi resolusi
/// tinggi dengan pivot dan skala berbeda -- ukurannya diatur per-prop lewat
/// tinggi target dunia, bukan lewat PPU tile.
/// </summary>
public class PropLibrary : ScriptableObject
{
    public Sprite brazier;        // pengganti obor: sumber cahaya utama
    public Sprite statue;         // patung Ganesha -> shrine & boss
    public Sprite mask;           // topeng Barong -> boss & dinding
    public Sprite gamelan;        // -> shop & shrine
    public Sprite pillarBroken;   // pilar patah
    public Sprite vines;          // sulur gantung -> dinding
    public Sprite skeleton;       // -> pekuburan
    public Sprite cobweb;         // -> pekuburan
    public Sprite pots;           // -> ruangan umum
    public Sprite books;

    [Tooltip("Puing yang bisa ditabrak (batu, pecahan pot)")]
    public Sprite[] rubble;
    [Tooltip("Kristal bercahaya")]
    public Sprite[] gems;

    static PropLibrary cached;
    public static PropLibrary I
    {
        get
        {
            if (cached == null) cached = Resources.Load<PropLibrary>("PropLibrary");
            return cached;
        }
    }

    /// <summary>Tersedia? Generator jatuh ke prop Kenney bila belum dibangun.</summary>
    public static bool Ready => I != null && I.brazier != null;

    public static Sprite Rubble() => Pick(I != null ? I.rubble : null);
    public static Sprite Gem() => Pick(I != null ? I.gems : null);

    static Sprite Pick(Sprite[] a) => (a != null && a.Length > 0) ? a[Random.Range(0, a.Length)] : null;
}
