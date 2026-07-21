using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pembuat elemen UI saat runtime. Dipakai layar yang dirakit lewat kode
/// (pilih hero, tas) supaya tidak ada lagi referensi yang harus dipasang manual
/// di scene -- sumber bug UI yang paling sering.
/// </summary>
public static class UIKit
{
    // palet dark heritage
    public static readonly Color Bg = new Color32(0x0F, 0x0F, 0x1A, 0xFF);
    public static readonly Color Panel = new Color32(0x18, 0x1A, 0x28, 0xF2);
    public static readonly Color PanelSoft = new Color32(0x22, 0x25, 0x33, 0xF2);
    public static readonly Color Gold = new Color32(0xE3, 0xB3, 0x41, 0xFF);
    public static readonly Color Teal = new Color32(0x2A, 0x9D, 0x8F, 0xFF);
    public static readonly Color Ember = new Color32(0xE7, 0x6F, 0x51, 0xFF);
    public static readonly Color Parchment = new Color32(0xE8, 0xE0, 0xCC, 0xFF);
    public static readonly Color Muted = new Color32(0x8A, 0x8F, 0xA3, 0xFF);

    public static Canvas MakeCanvas(string name = "Canvas", int sortOrder = 0)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));

        return canvas;
    }

    public static RectTransform Rect(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    /// <summary>Panel warna solid. raycast=false untuk hiasan, supaya tidak menelan klik tombol.</summary>
    public static Image MakePanel(Transform parent, Vector2 pos, Vector2 size, Color color, bool raycast = false)
    {
        var rt = Rect(parent, "Panel", pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    public static TextMeshProUGUI MakeText(Transform parent, string content, Vector2 pos, Vector2 size,
                                           float fontSize, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var rt = Rect(parent, "Text", pos, size);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = content;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = Parchment;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    /// <summary>Sprite dari tileset (potret, ikon item).</summary>
    public static Image MakeSprite(Transform parent, Sprite sprite, Vector2 pos, Vector2 size)
    {
        var rt = Rect(parent, "Icon", pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        img.preserveAspect = true;
        if (sprite == null) img.color = new Color(1, 1, 1, 0);
        return img;
    }

    public static Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size,
                                    Color? fill = null, float fontSize = 30f)
    {
        var rt = Rect(parent, label + " Button", pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = fill ?? PanelSoft;
        img.raycastTarget = true;

        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        // transisi warna instan bawaan diganti UIButtonFx: hover/tekan di-ease semua
        UIButtonFx.Attach(btn);

        var text = MakeText(rt, label, Vector2.zero, size, fontSize);
        text.color = Gold;
        text.fontStyle = FontStyles.Bold;

        return btn;
    }

    /// <summary>Garis aksen tipis; pemisah visual antar-bagian.</summary>
    public static Image MakeRule(Transform parent, Vector2 pos, float width, Color? color = null)
        => MakePanel(parent, pos, new Vector2(width, 4f), color ?? Gold);
}
