using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Umpan balik untuk tombol tak terlihat di atas ilustrasi menu. Tombol yang
/// tergambar di latar tidak bisa dianimasikan, jadi kita menaruh sorotan emas
/// yang menyala + area klik yang membesar-menekan saat kursor berinteraksi.
/// Semua unscaled supaya tetap jalan di menu (Time.timeScale bebas).
/// </summary>
public class HitButtonFx : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    RectTransform rt;
    Image glow;
    Vector3 baseScale;
    bool hover, down;

    void Awake()
    {
        rt = (RectTransform)transform;
        baseScale = rt.localScale;

        // lapisan sorot emas, awalnya transparan; menyala saat hover
        var go = new GameObject("Glow", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        var grt = (RectTransform)go.transform;
        grt.anchorMin = Vector2.zero; grt.anchorMax = Vector2.one;
        grt.offsetMin = new Vector2(-10, -10); grt.offsetMax = new Vector2(10, 10);
        glow = go.GetComponent<Image>();
        glow.color = new Color(1f, 0.85f, 0.4f, 0f);
        glow.raycastTarget = false;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        hover = true;
        if (AudioManager.Instance != null) AudioManager.Instance.Play("ui_hover", 0.4f);
    }
    public void OnPointerExit(PointerEventData e) { hover = false; down = false; }
    public void OnPointerDown(PointerEventData e) { down = true; }
    public void OnPointerUp(PointerEventData e) { down = false; }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        float targetScale = down ? 0.96f : (hover ? 1.05f : 1f);
        rt.localScale = Vector3.Lerp(rt.localScale, baseScale * targetScale, dt * 14f);

        // sorot berdenyut halus saat hover supaya terasa hidup
        float targetA = hover ? (0.28f + Mathf.Sin(Time.unscaledTime * 6f) * 0.08f) : 0f;
        var c = glow.color;
        c.a = Mathf.Lerp(c.a, targetA, dt * 12f);
        glow.color = c;
    }
}
