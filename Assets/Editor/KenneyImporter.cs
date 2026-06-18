using UnityEngine;
using UnityEditor;

/// <summary>
/// Set import settings semua PNG di Assets/Art/Kenney supaya pixel-art tajam &
/// skala konsisten: Sprite (Single), filter Point, PPU 16, tanpa kompresi & mipmap.
/// Jalankan: Tools ▸ Pusaka ▸ Import Kenney Art  /  -executeMethod KenneyImporter.Run
/// </summary>
public static class KenneyImporter
{
    [MenuItem("Tools/Pusaka/Import Kenney Art")]
    public static void Run()
    {
        int n = 0;
        foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Kenney" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.filterMode = FilterMode.Point;
            ti.spritePixelsPerUnit = 16;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.Uncompressed;

            // FullRect wajib agar SpriteRenderer drawMode=Tiled (lantai/tembok) bekerja
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);

            ti.SaveAndReimport();
            n++;
        }
        Debug.Log($"KENNEY: import settings di-set untuk {n} tekstur.");
    }
}
