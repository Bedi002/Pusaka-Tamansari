using UnityEngine;

/// <summary>
/// Memanggang lantai satu ruangan menjadi SATU tekstur.
///
/// Alasannya: variasi per-tile itu yang membuat lantai terlihat seperti lantai batu
/// dan bukan bidang warna datar. Tetapi memasang satu GameObject per tile berarti
/// ruangan 38x25 = 950 objek, dikali 20 ruangan jadi 19.000 objek per lantai.
/// Dipanggang jadi tekstur: variasi penuh, satu SpriteRenderer per ruangan.
///
/// Tile sumber wajib Read/Write enabled -- diatur PixelArtImporter untuk folder
/// RoguelikeDungeon.
/// </summary>
public static class RoomFloorBaker
{
    const int TilePx = 16;

    /// <summary>
    /// Susun lantai berukuran wTiles x hTiles. Mengembalikan null bila tileset
    /// belum siap, sehingga pemanggil bisa jatuh ke cara lama.
    /// </summary>
    public static Sprite Bake(int wTiles, int hTiles, int[] plain, int[] crack, float crackChance,
                              int[] waterFill = null, int[] waterEdge = null, RectInt pool = default)
    {
        if (plain == null || plain.Length == 0) return null;

        var probe = ArtLibrary.R(plain[0]);
        if (probe == null || probe.texture == null) return null;

        int W = wTiles * TilePx, H = hTiles * TilePx;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = $"FloorBaked_{wTiles}x{hTiles}",
        };

        var buffer = new Color32[W * H];
        bool hasPool = waterFill != null && waterFill.Length > 0 && pool.width > 0 && pool.height > 0;

        for (int ty = 0; ty < hTiles; ty++)
            for (int tx = 0; tx < wTiles; tx++)
            {
                int index;
                if (hasPool && pool.Contains(new Vector2Int(tx, ty)))
                {
                    bool edge = tx == pool.xMin || tx == pool.xMax - 1
                             || ty == pool.yMin || ty == pool.yMax - 1;
                    var set = (edge && waterEdge != null && waterEdge.Length > 0) ? waterEdge : waterFill;
                    index = set[Random.Range(0, set.Length)];
                }
                else
                {
                    bool cracked = crack != null && crack.Length > 0 && Random.value < crackChance;
                    var set = cracked ? crack : plain;
                    index = set[Random.Range(0, set.Length)];
                }

                Blit(buffer, W, tx * TilePx, ty * TilePx, ArtLibrary.R(index));
            }

        tex.SetPixels32(buffer);
        tex.Apply(false, true);      // makeNoLongerReadable: lepas salinan CPU-nya

        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), TilePx);
    }

    // Satu ruangan memanggil Blit ratusan kali; tanpa cache tiap panggilan akan
    // menyalin ulang seluruh tekstur sumber dari GPU.
    static readonly System.Collections.Generic.Dictionary<Texture2D, Color32[]> pixelCache
        = new System.Collections.Generic.Dictionary<Texture2D, Color32[]>();

    static Color32[] Pixels(Texture2D tex)
    {
        if (tex == null) return null;
        if (pixelCache.TryGetValue(tex, out var cached)) return cached;

        Color32[] px = null;
        try { px = tex.GetPixels32(); }
        catch (UnityException)
        {
            // tekstur tidak Read/Write enabled -- lewati, jangan sampai crash
            Debug.LogWarning($"RoomFloorBaker: tekstur '{tex.name}' tidak readable.");
        }
        pixelCache[tex] = px;
        return px;
    }

    /// <summary>Salin satu tile 16x16 ke posisi (dstX, dstY) di buffer.</summary>
    static void Blit(Color32[] buffer, int bufferWidth, int dstX, int dstY, Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return;

        var r = sprite.textureRect;
        int sx = Mathf.RoundToInt(r.x), sy = Mathf.RoundToInt(r.y);
        int w = Mathf.Min(TilePx, Mathf.RoundToInt(r.width));
        int h = Mathf.Min(TilePx, Mathf.RoundToInt(r.height));

        var src = Pixels(sprite.texture);
        if (src == null) return;

        int texW = sprite.texture.width;
        for (int y = 0; y < h; y++)
        {
            int srcRow = (sy + y) * texW + sx;
            int dstRow = (dstY + y) * bufferWidth + dstX;
            for (int x = 0; x < w; x++)
            {
                var c = src[srcRow + x];
                if (c.a == 0) continue;                 // jaga piksel transparan
                buffer[dstRow + x] = c;
            }
        }
    }
}
