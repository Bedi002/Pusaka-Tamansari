using UnityEngine;

/// <summary>
/// Alat penghias ruangan. Masalah "peta terasa mati" sebagian besar bukan soal
/// tile-nya jelek, melainkan cara memakainya: satu sprite lantai polos di-tint
/// rata, tanpa cahaya, tanpa kedalaman, dan properti ditabur seragam.
/// Berkas ini menyediakan lapisan yang memperbaiki hal itu.
/// </summary>
public static class RoomDressing
{
    static Sprite glowSprite;

    /// <summary>
    /// Lingkaran cahaya lembut, dibuat sekali saat runtime lalu dipakai ulang.
    /// Dibuat dari kode supaya tidak menambah file art dan selalu mulus di
    /// resolusi berapa pun (tidak ikut Point filter seperti tile pixel art).
    /// </summary>
    public static Sprite Glow()
    {
        if (glowSprite != null) return glowSprite;

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = "GlowRuntime",
        };

        float half = size * 0.5f;
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half;
                // kuadratik terbalik: terang di pusat, meluruh halus ke tepi
                float a = Mathf.Clamp01(1f - d);
                a = a * a;
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px);
        tex.Apply();

        glowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        return glowSprite;
    }

    static Sprite solidSprite;

    /// <summary>Ukuran sisi sprite Solid() dalam unit dunia, untuk menghitung skala.</summary>
    public const float SolidUnits = 1f;

    /// <summary>Kotak putih polos; diwarnai dan diskalakan jadi selubung gelap ruangan.</summary>
    public static Sprite Solid()
    {
        if (solidSprite != null) return solidSprite;

        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "SolidRuntime" };
        var px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();

        // pixelsPerUnit = lebar tekstur -> sprite tepat SolidUnits x SolidUnits unit
        solidSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f / SolidUnits);
        return solidSprite;
    }

    /// <summary>Pasang bola cahaya di sebuah titik (dipakai obor, lentera, kolam).</summary>
    public static GameObject AddLight(Transform parent, Vector2 localPos, Color color, float radius, int order = 7)
    {
        var go = new GameObject("Light");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one * radius;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Glow();
        sr.color = color;
        sr.sortingOrder = order;
        // Additive lewat warna: sprite default tidak menjumlah, tetapi alpha
        // gradien di atas lantai gelap sudah cukup meyakinkan sebagai cahaya.
        return go;
    }
}

/// <summary>
/// Membuat cahaya obor berkedip pelan dan tidak seragam antar-obor.
/// Tanpa ini seluruh obor berdenyut serempak dan justru terlihat palsu.
/// </summary>
public class TorchFlicker : MonoBehaviour
{
    public float baseAlpha = 0.5f;
    public float amplitude = 0.12f;
    public float speed = 4.5f;

    SpriteRenderer sr;
    float seed;
    Vector3 baseScale;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        seed = Random.value * 100f;
        baseScale = transform.localScale;
    }

    void Update()
    {
        if (sr == null) return;
        // dua gelombang beda frekuensi -> kedipan terasa acak, bukan sinus rapi
        float n = Mathf.Sin(Time.time * speed + seed) * 0.6f
                + Mathf.Sin(Time.time * speed * 2.7f + seed * 1.7f) * 0.4f;

        var c = sr.color;
        c.a = Mathf.Clamp01(baseAlpha + n * amplitude);
        sr.color = c;

        transform.localScale = baseScale * (1f + n * 0.05f);
    }
}

/// <summary>
/// Membuat brazier bergoyang tipis + berkedip warna hangat, supaya apinya terasa
/// menyala hidup, bukan gambar diam. Dua gelombang beda frekuensi -> tidak
/// terlihat berdenyut mekanis.
/// </summary>
public class FlameSway : MonoBehaviour
{
    public float amount = 0.05f;   // seberapa besar goyangan skala
    public float speed = 5.5f;

    SpriteRenderer sr;
    Vector3 baseScale;
    float seed;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        seed = Random.value * 100f;
    }

    void Update()
    {
        float n = Mathf.Sin(Time.time * speed + seed) * 0.6f
                + Mathf.Sin(Time.time * speed * 2.3f + seed * 1.7f) * 0.4f;

        // nyala naik-turun sedikit (skala Y) dan mengecil-melebar sangat halus
        transform.localScale = new Vector3(
            baseScale.x * (1f + n * amount * 0.4f),
            baseScale.y * (1f + n * amount),
            1f);

        if (sr != null)
        {
            // sorot hangat berdenyut di puncak nyala
            float k = 1f + Mathf.Max(0f, n) * 0.12f;
            sr.color = new Color(k, k * 0.98f, k * 0.94f, 1f);
        }
    }
}

/// <summary>
/// Mengurutkan sprite berdasarkan posisi Y supaya karakter bisa lewat DI BELAKANG
/// pohon dan pilar, bukan selalu menembusnya. Dipasang pada objek yang bergerak;
/// properti statis cukup dihitung sekali saat dibangun.
/// </summary>
[DefaultExecutionOrder(100)]
public class YSort : MonoBehaviour
{
    public int offset = 0;
    [Tooltip("Titik kaki relatif pusat sprite; sorting memakai titik ini")]
    public float footOffset = -0.5f;

    SpriteRenderer[] renderers;
    int[] relative;          // urutan asli tiap renderer relatif terhadap root
    float lastY = float.NaN;

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        relative = new int[renderers.Length];

        // Simpan selisih urutan bawaan (mis. bayangan di bawah badan) supaya
        // penataan internal prefab tidak rusak saat seluruhnya digeser.
        int root = renderers.Length > 0 && renderers[0] != null ? renderers[0].sortingOrder : 0;
        for (int i = 0; i < renderers.Length; i++)
            relative[i] = renderers[i] != null ? renderers[i].sortingOrder - root : 0;
    }

    void LateUpdate()
    {
        if (renderers == null || renderers.Length == 0) return;

        float y = transform.position.y + footOffset;
        if (!float.IsNaN(lastY) && Mathf.Abs(y - lastY) < 0.02f) return;   // hemat: hanya saat cukup bergerak
        lastY = y;

        int order = Order(y) + offset;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].sortingOrder = order + relative[i];
        }
    }

    /// <summary>Y makin kecil (makin ke bawah layar) = makin depan.</summary>
    public static int Order(float worldY) => Mathf.Clamp(2000 - Mathf.RoundToInt(worldY * 10f), 10, 30000);
}
