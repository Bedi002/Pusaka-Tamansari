using UnityEditor;
using UnityEngine;

/// <summary>
/// Menyeragamkan import setting semua pixel art proyek ini secara otomatis.
/// Tanpa ini setiap pack baru masuk dengan filter Bilinear + kompresi + PPU 100,
/// sehingga sprite buram dan skala antar-pack tidak konsisten.
///
/// Aturan:
///   Semua        -> Sprite, Point filter, tanpa kompresi, tanpa mipmap.
///   Tile Kenney  -> PPU 16 (tile 16px = 1 unit dunia), sprite tunggal.
///   RoguelikeDungeon -> tambah Read/Write: RoomFloorBaker membaca pikselnya
///                      untuk memanggang lantai ruangan jadi satu tekstur.
///   UI_RPG       -> PPU 100 (elemen UI, bukan tile dunia).
///   Sprites/Forge -> hanya filter/kompresi. Sheet-nya sudah di-slice manual,
///                    spriteImportMode dan PPU-nya JANGAN disentuh.
/// </summary>
public class PixelArtImporter : AssetPostprocessor
{
    // Menaikkan angka ini memaksa Unity mengimpor ulang semua aset yang lewat
    // sini. Naikkan bila aturan di bawah berubah.
    public override uint GetVersion() => 3;

    void OnPreprocessTexture()
    {
        string p = assetPath.Replace('\\', '/');
        if (!p.StartsWith("Assets/Art/") && !p.StartsWith("Assets/Sprites/")) return;

        var ti = (TextureImporter)assetImporter;

        ti.textureType = TextureImporterType.Sprite;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.npotScale = TextureImporterNPOTScale.None;

        // Art AI hasil generate (Art/Generated): ilustrasi resolusi tinggi, bukan
        // pixel-art 16px. Bilinear + PPU 100 supaya mulus saat diskalakan sebagai
        // latar layar penuh; sisanya Point supaya tile pixel tetap tajam.
        if (p.StartsWith("Assets/Art/Generated/"))
        {
            ti.filterMode = FilterMode.Bilinear;
            ti.spritePixelsPerUnit = 100f;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.maxTextureSize = 4096;
            return;
        }

        ti.filterMode = FilterMode.Point;

        // Versi sebelumnya menebak "impor pertama" dari ada/tidaknya file .meta.
        // Tebakan itu salah: Unity sudah menulis .meta sebelum OnPreprocessTexture
        // dipanggil, jadi PPU dan Read/Write tidak pernah benar-benar diterapkan --
        // tile masuk dengan PPU 100 dan isReadable 0, dan pemanggang lantai gagal.
        // Aturan di bawah deterministik per folder, jadi aman diterapkan selalu.

        if (p.Contains("/Sprites/Forge/")) return;      // sheet ter-slice: jangan diubah

        if (p.Contains("/Art/Kenney/UI_RPG/"))
        {
            ti.spritePixelsPerUnit = 100f;
            ti.spriteImportMode = SpriteImportMode.Single;
            return;
        }

        if (p.Contains("/Art/Kenney/"))
        {
            ti.spritePixelsPerUnit = 16f;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.isReadable = p.Contains("/RoguelikeDungeon/");
        }
    }
}
