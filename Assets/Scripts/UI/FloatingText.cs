using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Teks tempur melayang: angka damage, heal, kritis, dan "ELAK!". Satu kanvas
/// overlay + pool TMP, jadi sistem tempur boleh memanggil Spawn tiap frame
/// tanpa membuat sampah GC ataupun kanvas baru. Posisi dunia dikonversi tiap
/// frame supaya popup tetap menempel pada titik kejadian saat kamera bergerak.
/// </summary>
public class FloatingText : MonoBehaviour
{
    public static readonly Color DamageColor = new Color32(0xE7, 0x6F, 0x51, 0xFF);
    public static readonly Color CritColor = new Color32(0xE3, 0xB3, 0x41, 0xFF);
    public static readonly Color HealColor = new Color32(0x2A, 0x9D, 0x8F, 0xFF);
    public static readonly Color DodgeColor = new Color32(0x8A, 0x8F, 0xA3, 0xFF);

    const int MaxLive = 40;      // batas banjir: saat penuh, popup tertua didaur ulang
    const float Life = 0.85f;
    const float CritLife = 1.05f;
    const float RisePx = 64f;
    const float PopPortion = 0.18f;

    sealed class Popup
    {
        public RectTransform rt;
        public TextMeshProUGUI tmp;
        public Vector3 world;
        public Color color;
        public float t, dur, drift, baseScale;
    }

    static FloatingText instance;

    readonly List<Popup> live = new List<Popup>(MaxLive);
    readonly Stack<Popup> pool = new Stack<Popup>(MaxLive);
    RectTransform canvasRt;
    Camera cam;

    // ------------------------------------------------------------------- API

    public static void Spawn(Vector3 worldPos, string text, Color color, bool crit = false)
    {
        Ensure();
        instance.DoSpawn(worldPos, text, color, crit);
    }

    public static void SpawnDamage(Vector3 worldPos, int amount, bool crit = false)
        => Spawn(worldPos, amount.ToString(), crit ? CritColor : DamageColor, crit);

    public static void SpawnHeal(Vector3 worldPos, int amount)
        => Spawn(worldPos, "+" + amount, HealColor);

    public static void SpawnDodge(Vector3 worldPos)
        => Spawn(worldPos, "ELAK!", DodgeColor);

    // ------------------------------------------------------------------ inti

    static void Ensure()
    {
        if (instance != null) return;   // kanvas ikut mati saat ganti scene; dibuat ulang di sini
        var canvas = UIKit.MakeCanvas("FloatingText Canvas", 30);
        var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (raycaster != null) raycaster.enabled = false;   // teks tempur tidak boleh menelan klik
        instance = canvas.gameObject.AddComponent<FloatingText>();
        instance.canvasRt = (RectTransform)canvas.transform;
    }

    void DoSpawn(Vector3 worldPos, string text, Color color, bool crit)
    {
        Popup p;
        if (pool.Count > 0) p = pool.Pop();
        else if (live.Count >= MaxLive) { p = live[0]; live.RemoveAt(0); }
        else p = Create();

        p.tmp.text = text;
        p.tmp.fontSize = crit ? 52f : 34f;
        p.tmp.fontStyle = crit ? (FontStyles.Bold | FontStyles.Italic) : FontStyles.Bold;
        p.color = color;
        p.world = worldPos;
        p.t = 0f;
        p.dur = crit ? CritLife : Life;
        p.drift = Random.Range(-26f, 26f);
        p.baseScale = crit ? 1.25f : 1f;    // kritis lebih besar dan pop-nya lebih nendang
        p.rt.localScale = Vector3.zero;
        p.rt.gameObject.SetActive(true);
        live.Add(p);
    }

    Popup Create()
    {
        var tmp = UIKit.MakeText(canvasRt, "", Vector2.zero, new Vector2(260f, 80f), 34f);
        return new Popup { rt = (RectTransform)tmp.transform, tmp = tmp };
    }

    void LateUpdate()
    {
        if (live.Count == 0) return;
        if (cam == null) cam = Camera.main;

        float dt = Time.unscaledDeltaTime;
        for (int i = live.Count - 1; i >= 0; i--)
        {
            var p = live[i];
            p.t += dt;
            if (p.t >= p.dur || cam == null)
            {
                p.rt.gameObject.SetActive(false);
                live.RemoveAt(i);
                pool.Push(p);
                continue;
            }

            float u = p.t / p.dur;
            Vector3 sp = cam.WorldToScreenPoint(p.world);
            bool visible = sp.z > 0f;
            if (p.rt.gameObject.activeSelf != visible) p.rt.gameObject.SetActive(visible);
            if (!visible) continue;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, sp, null, out var lp);
            float rise = RisePx * (1f - (1f - u) * (1f - u));   // naik cepat lalu melambat
            p.rt.anchoredPosition = lp + new Vector2(p.drift * u, rise);

            // pop awal dengan overshoot, sisanya menyusut tipis
            float s = u < PopPortion
                ? OutBack(u / PopPortion)
                : 1f - 0.1f * ((u - PopPortion) / (1f - PopPortion));
            s *= p.baseScale;
            p.rt.localScale = new Vector3(s, s, 1f);

            var c = p.color;
            c.a = u < 0.55f ? 1f : 1f - (u - 0.55f) / 0.45f;
            p.tmp.color = c;
        }
    }

    static float OutBack(float p)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float q = p - 1f;
        return 1f + c3 * q * q * q + c1 * q * q;
    }
}
