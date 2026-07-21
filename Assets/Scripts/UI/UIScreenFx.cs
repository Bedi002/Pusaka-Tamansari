using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Koreografi masuk generik untuk layar yang dirakit di scene (menu utama,
/// pilih kesulitan, layar akhir, pause). Anak kanvas diklasifikasi dari
/// bentuknya -- judul, garis aksen, tombol, panel -- lalu dianimasikan
/// bergiliran supaya layar terasa dirakit, bukan muncul mendadak.
/// Total koreografi dijaga di bawah ~0.8 detik.
/// </summary>
public static class UIScreenFx
{
    /// <summary>Ambil / pasang CanvasGroup; cara termurah mem-fade subtree utuh.</summary>
    public static CanvasGroup Group(RectTransform rt)
    {
        if (rt == null) return null;
        var cg = rt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }

    public static void PlayEntrance(RectTransform root)
    {
        if (root == null) return;

        int panelIdx = 0, textIdx = 0, btnIdx = 0;
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf) continue;

            var size = child.rect.size;
            var img = child.GetComponent<Image>();
            var text = child.GetComponent<TMP_Text>();
            bool isButton = child.GetComponent<Button>() != null;

            // latar layar penuh dibiarkan diam; meng-fade-nya hanya membuat kedip
            if (!isButton && img != null && size.x >= 1900f && size.y >= 1000f) continue;

            if (isButton)
            {
                float d = 0.36f + btnIdx * 0.07f;   // tombol selalu paling akhir
                UIMotion.FadeIn(Group(child), 0.3f, d);
                UIMotion.SlideFrom(child, Vector2.down, 22f, 0.34f, d);
                btnIdx++;
            }
            else if (img != null && size.y <= 10f && size.x >= 80f)
            {
                // garis aksen tipis: mengembang dari tengah
                UIMotion.WipeWidth(child, size.x, 0.4f, 0.1f);
            }
            else if (text != null && text.fontSize >= 56f)
            {
                // judul: turun dari atas lalu mengendap
                UIMotion.FadeIn(Group(child), 0.3f);
                UIMotion.SlideFrom(child, Vector2.up, 46f, 0.5f);
            }
            else if (text != null)
            {
                float d = 0.14f + textIdx * 0.05f;
                UIMotion.FadeIn(Group(child), 0.3f, d);
                UIMotion.SlideFrom(child, Vector2.up, 14f, 0.3f, d);
                textIdx++;
            }
            else
            {
                UIMotion.PopIn(child, 0.16f + panelIdx * 0.06f);
                panelIdx++;
            }
        }
    }

    /// <summary>Pasang UIButtonFx ke semua Button di bawah root (termasuk yang nonaktif).</summary>
    public static void AttachButtonFx(Transform root)
    {
        if (root == null) return;
        var buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++) UIButtonFx.Attach(buttons[i]);
    }
}
