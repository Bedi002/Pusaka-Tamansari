using UnityEngine;

/// <summary>
/// Katalog sprite yang bisa dibaca saat runtime. AssetDatabase hanya ada di editor,
/// jadi generator dungeon prosedural memerlukan satu aset tunggal di Resources yang
/// menyimpan seluruh sprite tile. Diisi lewat Tools > Pusaka > Rebuild Content.
/// </summary>
public class ArtLibrary : ScriptableObject
{
    [Tooltip("Kenney Tiny Dungeon, index = nomor tile_XXXX")]
    public Sprite[] dungeon;
    [Tooltip("Kenney Tiny Town, index = nomor tile_XXXX")]
    public Sprite[] town;
    [Tooltip("Kenney Roguelike Caves & Dungeons -- tileset utama lingkungan")]
    public Sprite[] rogue;

    static ArtLibrary cached;
    public static ArtLibrary I
    {
        get
        {
            if (cached == null) cached = Resources.Load<ArtLibrary>("ArtLibrary");
            return cached;
        }
    }

    /// <summary>Sprite dari tileset dungeon. Null bila index di luar jangkauan.</summary>
    public static Sprite D(int i) => Pick(I != null ? I.dungeon : null, i);
    /// <summary>Sprite dari tileset town (pohon, semak, pagar, properti luar).</summary>
    public static Sprite T(int i) => Pick(I != null ? I.town : null, i);

    /// <summary>Sprite dari tileset roguelike -- lantai, tembok, air, pilar, obor.</summary>
    public static Sprite R(int i) => Pick(I != null ? I.rogue : null, i);

    static Sprite Pick(Sprite[] a, int i) => (a != null && i >= 0 && i < a.Length) ? a[i] : null;

    /// <summary>Ambil satu sprite acak dari daftar index (untuk variasi lantai/props).</summary>
    public static Sprite DAny(params int[] idx) => idx == null || idx.Length == 0 ? null : D(idx[Random.Range(0, idx.Length)]);
    public static Sprite TAny(params int[] idx) => idx == null || idx.Length == 0 ? null : T(idx[Random.Range(0, idx.Length)]);
}
