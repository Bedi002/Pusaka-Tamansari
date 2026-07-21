using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Nyawa tombol: terangkat + membesar saat hover, penyet saat ditekan lalu
/// snap-back dengan pegas underdamped, dan aksen emas meluncur masuk dari
/// bawah. Semua easing dihitung sendiri di Update (unscaled, tanpa alokasi)
/// alih-alih memakai transisi warna bawaan Button yang terasa instan.
/// </summary>
[DisallowMultipleComponent]
public class UIButtonFx : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    const float HoverScale = 1.045f;
    const float PressScale = 0.955f;
    const float LiftPx = 4f;
    const float HoverInTime = 0.11f;
    const float SpringK = 340f;    // kekakuan pegas skala
    const float SpringDamp = 13f;  // sengaja underdamped supaya snap-back terasa hidup

    RectTransform rt;
    Graphic bg;
    Color baseColor, hoverColor;
    RectTransform accent;
    float accentW;
    float hoverT, pressT, scaleMul = 1f, scaleVel;
    Vector2 basePos;
    bool hovered, pressed, resting = true;

    public static UIButtonFx Attach(Button b)
    {
        if (b == null) return null;
        var fx = b.GetComponent<UIButtonFx>();
        if (fx == null) fx = b.gameObject.AddComponent<UIButtonFx>();
        // transisi bawaan dimatikan: seluruh visual state diambil alih agar bisa di-ease
        b.transition = Selectable.Transition.None;
        return fx;
    }

    void Awake()
    {
        rt = (RectTransform)transform;
        bg = GetComponent<Graphic>();
        if (bg != null)
        {
            baseColor = bg.color;
            hoverColor = Color.Lerp(baseColor, Color.white, 0.22f);
        }
        basePos = rt.anchoredPosition;
    }

    void Start()
    {
        // aksen dibuat di Start supaya lebar rect final sudah diketahui
        accentW = Mathf.Max(40f, rt.rect.width * 0.55f);
        var go = new GameObject("Accent", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        accent = (RectTransform)go.transform;
        accent.anchorMin = accent.anchorMax = new Vector2(0.5f, 0f);
        accent.pivot = new Vector2(0.5f, 0f);
        accent.anchoredPosition = new Vector2(0f, 5f);
        accent.sizeDelta = new Vector2(0f, 3f);
        var img = go.AddComponent<Image>();
        img.color = UIKit.Gold;
        img.raycastTarget = false;
    }

    void OnDisable()
    {
        // panel bisa disembunyikan saat kursor masih di atas tombol; state harus pulih
        hovered = pressed = false;
        hoverT = pressT = 0f;
        scaleMul = 1f; scaleVel = 0f;
        resting = true;
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            rt.anchoredPosition = basePos;
        }
        if (bg != null) bg.color = baseColor;
        if (accent != null) accent.sizeDelta = new Vector2(0f, 3f);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f) return;

        bool wantRest = !hovered && !pressed;
        if (resting && wantRest)
        {
            // saat diam, ikuti posisi hasil animasi masuk layar agar lift tidak salah pijakan
            basePos = rt.anchoredPosition;
            return;
        }

        hoverT = Mathf.MoveTowards(hoverT, hovered ? 1f : 0f, dt / HoverInTime);
        pressT = Mathf.MoveTowards(pressT, pressed ? 1f : 0f, dt / 0.06f);
        float h = hoverT * hoverT * (3f - 2f * hoverT);

        float target = pressed ? PressScale : (hovered ? HoverScale : 1f);
        scaleVel += (target - scaleMul) * (SpringK * dt);
        scaleVel *= Mathf.Exp(-SpringDamp * dt);
        scaleMul += scaleVel * dt;
        rt.localScale = new Vector3(scaleMul, scaleMul, 1f);

        rt.anchoredPosition = basePos + new Vector2(0f, LiftPx * h);

        if (bg != null)
        {
            var c = Color.Lerp(baseColor, hoverColor, h);
            if (pressT > 0f)
            {
                float d = 1f - 0.10f * pressT;
                c.r *= d; c.g *= d; c.b *= d;
            }
            bg.color = c;
        }

        if (accent != null)
        {
            float e = 1f - (1f - h) * (1f - h) * (1f - h);
            accent.sizeDelta = new Vector2(accentW * e, 3f);
        }

        // kembali tidur bila semua nilai sudah menetap; Update jadi murah saat idle
        if (wantRest && hoverT <= 0f && pressT <= 0f &&
            Mathf.Abs(scaleMul - 1f) < 0.001f && Mathf.Abs(scaleVel) < 0.01f)
        {
            scaleMul = 1f; scaleVel = 0f;
            rt.localScale = Vector3.one;
            rt.anchoredPosition = basePos;
            if (bg != null) bg.color = baseColor;
            if (accent != null) accent.sizeDelta = new Vector2(0f, 3f);
            resting = true;
        }
        else resting = false;
    }

    public void OnPointerEnter(PointerEventData e) { hovered = true; resting = false; }
    public void OnPointerExit(PointerEventData e) { hovered = false; }

    public void OnPointerDown(PointerEventData e)
    {
        pressed = true;
        resting = false;
        scaleVel = -3.5f;   // sentakan awal supaya penyetnya terasa seketika
    }

    public void OnPointerUp(PointerEventData e)
    {
        pressed = false;
        scaleVel = 2.5f;    // dorongan balik: melewati target sedikit lalu mengendap
    }
}
