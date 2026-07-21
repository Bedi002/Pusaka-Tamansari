using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pustaka tween ringan untuk seluruh UI. Sengaja tanpa paket eksternal:
/// satu runner Update terpusat + pool objek tween, sehingga loop animasi
/// bebas alokasi per-frame. Semua tween memakai unscaled time karena layar
/// tas dan pause men-set Time.timeScale = 0 -- tween berbasis waktu biasa
/// akan beku di tengah jalan.
///
/// Konvensi anti-konflik: tween baru pada target + kanal yang sama otomatis
/// mematikan tween lama, jadi pemanggil cukup memanggil ulang tanpa perlu
/// menyimpan handle. Tween posisi mewariskan titik "seharusnya" ke penerusnya
/// supaya pemanggilan beruntun (spam shake, buka-tutup cepat) tidak membuat
/// elemen bergeser permanen dari posisi aslinya.
/// </summary>
public static class UIMotion
{
    public enum Ease { Linear, OutQuad, OutCubic, InOutCubic, OutBack }

    enum Kind { Scale, Move, Size, FadeGraphic, FadeGroup, ColorTo, SliderTo, Count, Typewriter, Pulse, Shake }
    enum Channel { Scale, Position, Size, Alpha, Color, Slider, Text }

    sealed class Tween
    {
        public Kind kind;
        public Channel channel;
        public GameObject owner;               // untuk pencocokan Kill; komponen utama untuk cek hidup
        public UnityEngine.Object primary;
        public RectTransform rt;
        public Graphic gfx;
        public CanvasGroup cg;
        public TMP_Text tmp;
        public Slider slider;
        public Vector3 fromV3, toV3;
        public Vector2 fromV2, toV2;
        public Color fromC, toC;
        public float fromF, toF;
        public float delay, dur, t, cps, amp, period;
        public int fromI, toI, lastShown;
        public string fmt;
        public Ease ease;
        public bool loop, alive;
        public Action onComplete;

        public bool Dead => primary == null;

        public void Apply(float p, float u)
        {
            float e = Evaluate(ease, p);
            switch (kind)
            {
                case Kind.Scale:
                    rt.localScale = Vector3.LerpUnclamped(fromV3, toV3, e);
                    break;
                case Kind.Move:
                    rt.anchoredPosition = Vector2.LerpUnclamped(fromV2, toV2, e);
                    break;
                case Kind.Size:
                    rt.sizeDelta = Vector2.LerpUnclamped(fromV2, toV2, e);
                    break;
                case Kind.FadeGraphic:
                {
                    var c = fromC;
                    c.a = Mathf.LerpUnclamped(fromF, toF, e);
                    gfx.color = c;
                    break;
                }
                case Kind.FadeGroup:
                    cg.alpha = Mathf.LerpUnclamped(fromF, toF, e);
                    break;
                case Kind.ColorTo:
                    gfx.color = Color.LerpUnclamped(fromC, toC, e);
                    break;
                case Kind.SliderTo:
                    slider.value = Mathf.LerpUnclamped(fromF, toF, e);
                    break;
                case Kind.Count:
                {
                    int v = Mathf.RoundToInt(Mathf.Lerp(fromI, toI, e));
                    // SetText hanya saat angka berubah: TMP memformat angka tanpa sampah string
                    if (v != lastShown) { lastShown = v; tmp.SetText(fmt, v); }
                    break;
                }
                case Kind.Typewriter:
                {
                    int vis = p >= 1f ? int.MaxValue : Mathf.FloorToInt(u * cps);
                    if (vis != lastShown) { lastShown = vis; tmp.maxVisibleCharacters = vis; }
                    break;
                }
                case Kind.Pulse:
                {
                    // berdenyut di sekitar skala 1; kosinus mulai dari 1 agar masuknya mulus
                    float s = 1f + amp * 0.5f * (1f - Mathf.Cos(u * 2f * Mathf.PI / period));
                    rt.localScale = new Vector3(s, s, 1f);
                    break;
                }
                case Kind.Shake:
                {
                    // dua sinus beda frekuensi cukup terasa acak tanpa Random per-frame
                    float fall = 1f - p;
                    var off = new Vector2(Mathf.Sin(u * 71f), Mathf.Cos(u * 47f)) * (amp * fall);
                    rt.anchoredPosition = p >= 1f ? fromV2 : fromV2 + off;
                    break;
                }
            }
        }
    }

    sealed class Runner : MonoBehaviour
    {
        void Update() => Tick(Time.unscaledDeltaTime);
    }

    static Runner runner;
    static readonly List<Tween> active = new List<Tween>(64);
    static readonly Stack<Tween> pool = new Stack<Tween>(64);
    static bool hasInheritedPos;
    static Vector2 inheritedPos;

    // jaga-jaga bila domain reload dimatikan di editor: state statik harus bersih
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        active.Clear();
        pool.Clear();
        runner = null;
    }

    static void Ensure()
    {
        if (runner != null) return;
        var go = new GameObject("UIMotion Runner");
        go.hideFlags = HideFlags.DontSave;
        UnityEngine.Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<Runner>();
    }

    static void Tick(float dt)
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var tw = active[i];
            if (!tw.alive || tw.Dead) { Recycle(i); continue; }

            tw.t += dt;
            float u = tw.t - tw.delay;
            if (u < 0f) continue;                        // state awal sudah dipasang saat spawn

            float p = tw.dur <= 0f ? 1f : Mathf.Clamp01(u / tw.dur);
            tw.Apply(p, u);

            if (!tw.loop && u >= tw.dur)
            {
                var cb = tw.onComplete;
                Recycle(i);
                cb?.Invoke();
            }
        }
    }

    static void Recycle(int i)
    {
        var tw = active[i];
        int last = active.Count - 1;
        active[i] = active[last];
        active.RemoveAt(last);

        tw.alive = false;
        tw.onComplete = null;
        tw.owner = null;
        tw.primary = null;
        tw.rt = null; tw.gfx = null; tw.cg = null; tw.tmp = null; tw.slider = null;
        tw.fmt = null;
        pool.Push(tw);
    }

    static Tween Spawn(Kind kind, Channel channel, UnityEngine.Object primary, GameObject owner)
    {
        Ensure();
        hasInheritedPos = false;

        // bunuh tween lama pada kanal yang sama; tween posisi mewariskan titik tujuannya
        for (int i = 0; i < active.Count; i++)
        {
            var a = active[i];
            if (!a.alive || a.owner != owner || a.channel != channel) continue;
            if (a.kind == Kind.Move) { inheritedPos = a.toV2; hasInheritedPos = true; }
            else if (a.kind == Kind.Shake) { inheritedPos = a.fromV2; hasInheritedPos = true; }
            a.alive = false;
            a.onComplete = null;
        }

        var tw = pool.Count > 0 ? pool.Pop() : new Tween();
        tw.kind = kind;
        tw.channel = channel;
        tw.primary = primary;
        tw.owner = owner;
        tw.t = 0f;
        tw.delay = 0f;
        tw.dur = 0.25f;
        tw.ease = Ease.OutCubic;
        tw.loop = false;
        tw.alive = true;
        tw.onComplete = null;
        active.Add(tw);
        return tw;
    }

    static float Evaluate(Ease ease, float p)
    {
        switch (ease)
        {
            case Ease.OutQuad: return 1f - (1f - p) * (1f - p);
            case Ease.OutCubic: return 1f - (1f - p) * (1f - p) * (1f - p);
            case Ease.InOutCubic:
                return p < 0.5f ? 4f * p * p * p : 1f - Mathf.Pow(-2f * p + 2f, 3f) / 2f;
            case Ease.OutBack:
            {
                const float c1 = 1.70158f, c3 = c1 + 1f;
                float q = p - 1f;
                return 1f + c3 * q * q * q + c1 * q * q;
            }
            default: return p;
        }
    }

    // ------------------------------------------------------------------ fade

    public static void FadeIn(Graphic g, float dur = 0.25f, float delay = 0f)
    {
        if (g == null) return;
        var tw = Spawn(Kind.FadeGraphic, Channel.Alpha, g, g.gameObject);
        tw.gfx = g;
        tw.fromC = g.color;
        tw.fromF = 0f; tw.toF = 1f;
        tw.dur = dur; tw.delay = delay; tw.ease = Ease.OutQuad;
        var c = tw.fromC; c.a = 0f; g.color = c;
    }

    public static void FadeOut(Graphic g, float dur = 0.2f, float delay = 0f, Action onComplete = null)
    {
        if (g == null) return;
        var tw = Spawn(Kind.FadeGraphic, Channel.Alpha, g, g.gameObject);
        tw.gfx = g;
        tw.fromC = g.color;
        tw.fromF = g.color.a; tw.toF = 0f;
        tw.dur = dur; tw.delay = delay; tw.ease = Ease.OutQuad;
        tw.onComplete = onComplete;
    }

    public static void FadeIn(CanvasGroup cg, float dur = 0.25f, float delay = 0f)
    {
        if (cg == null) return;
        var tw = Spawn(Kind.FadeGroup, Channel.Alpha, cg, cg.gameObject);
        tw.cg = cg;
        tw.fromF = 0f; tw.toF = 1f;
        tw.dur = dur; tw.delay = delay; tw.ease = Ease.OutQuad;
        cg.alpha = 0f;
    }

    public static void FadeOut(CanvasGroup cg, float dur = 0.2f, float delay = 0f, Action onComplete = null)
    {
        if (cg == null) return;
        var tw = Spawn(Kind.FadeGroup, Channel.Alpha, cg, cg.gameObject);
        tw.cg = cg;
        tw.fromF = cg.alpha; tw.toF = 0f;
        tw.dur = dur; tw.delay = delay; tw.ease = Ease.OutQuad;
        tw.onComplete = onComplete;
    }

    // ---------------------------------------------------------- skala & posisi

    /// <summary>Muncul dengan pop: skala 0.75 ke 1 (overshoot) + fade lewat CanvasGroup.</summary>
    public static void PopIn(RectTransform rt, float delay = 0f, float dur = 0.3f)
    {
        if (rt == null) return;
        var tw = Spawn(Kind.Scale, Channel.Scale, rt, rt.gameObject);
        tw.rt = rt;
        tw.fromV3 = new Vector3(0.75f, 0.75f, 1f);
        tw.toV3 = Vector3.one;
        tw.dur = dur; tw.delay = delay; tw.ease = Ease.OutBack;
        rt.localScale = tw.fromV3;

        var cg = rt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
        FadeIn(cg, dur * 0.6f, delay);
    }

    public static void ScaleTo(RectTransform rt, float scale, float dur = 0.15f,
                               Ease ease = Ease.OutCubic, float delay = 0f)
    {
        if (rt == null) return;
        var tw = Spawn(Kind.Scale, Channel.Scale, rt, rt.gameObject);
        tw.rt = rt;
        tw.fromV3 = rt.localScale;
        tw.toV3 = new Vector3(scale, scale, 1f);
        tw.dur = dur; tw.delay = delay; tw.ease = ease;
    }

    /// <summary>Sentakan skala sekali: membesar sesaat lalu kembali ke 1.</summary>
    public static void Punch(RectTransform rt, float amp = 0.12f, float dur = 0.25f)
    {
        if (rt == null) return;
        var tw = Spawn(Kind.Scale, Channel.Scale, rt, rt.gameObject);
        tw.rt = rt;
        tw.fromV3 = new Vector3(1f + amp, 1f + amp, 1f);
        tw.toV3 = Vector3.one;
        tw.dur = dur; tw.ease = Ease.OutCubic;
        rt.localScale = tw.fromV3;
    }

    /// <summary>Meluncur masuk dari arah dir sejauh dist, menetap di posisi sekarang.</summary>
    public static void SlideFrom(RectTransform rt, Vector2 dir, float dist, float dur = 0.4f,
                                 float delay = 0f, Ease ease = Ease.OutCubic)
    {
        if (rt == null) return;
        var tw = Spawn(Kind.Move, Channel.Position, rt, rt.gameObject);
        tw.rt = rt;
        // bila memotong tween posisi lain, pakai titik tujuannya agar tidak bergeser
        tw.toV2 = hasInheritedPos ? inheritedPos : rt.anchoredPosition;
        tw.fromV2 = tw.toV2 + dir.normalized * dist;
        tw.dur = dur; tw.delay = delay; tw.ease = ease;
        rt.anchoredPosition = tw.fromV2;
    }

    public static void MoveTo(RectTransform rt, Vector2 pos, float dur = 0.2f,
                              Ease ease = Ease.OutCubic, float delay = 0f)
    {
        if (rt == null) return;
        var tw = Spawn(Kind.Move, Channel.Position, rt, rt.gameObject);
        tw.rt = rt;
        tw.fromV2 = rt.anchoredPosition;
        tw.toV2 = pos;
        tw.dur = dur; tw.delay = delay; tw.ease = ease;
    }

    /// <summary>Garis aksen mengembang dari tengah: lebar 0 ke target (pivot tengah).</summary>
    public static void WipeWidth(RectTransform rt, float width, float dur = 0.35f, float delay = 0f)
    {
        if (rt == null) return;
        var tw = Spawn(Kind.Size, Channel.Size, rt, rt.gameObject);
        tw.rt = rt;
        tw.fromV2 = new Vector2(0f, rt.sizeDelta.y);
        tw.toV2 = new Vector2(width, rt.sizeDelta.y);
        tw.dur = dur; tw.delay = delay; tw.ease = Ease.OutCubic;
        rt.sizeDelta = tw.fromV2;
    }

    // -------------------------------------------------------------- loop & fx

    /// <summary>Denyut lembut terus-menerus di sekitar skala 1. Hentikan dengan StopPulse.</summary>
    public static void Pulse(RectTransform rt, float amp = 0.05f, float period = 1.1f)
    {
        if (rt == null) return;
        var tw = Spawn(Kind.Pulse, Channel.Scale, rt, rt.gameObject);
        tw.rt = rt;
        tw.amp = amp; tw.period = Mathf.Max(0.05f, period);
        tw.loop = true;
        rt.localScale = Vector3.one;
    }

    public static void StopPulse(RectTransform rt)
    {
        if (rt == null) return;
        KillChannel(rt.gameObject, Channel.Scale);
        rt.localScale = Vector3.one;
    }

    public static void Shake(RectTransform rt, float amp = 6f, float dur = 0.3f)
    {
        if (rt == null) return;
        var tw = Spawn(Kind.Shake, Channel.Position, rt, rt.gameObject);
        tw.rt = rt;
        tw.fromV2 = hasInheritedPos ? inheritedPos : rt.anchoredPosition;
        tw.amp = amp; tw.dur = dur; tw.ease = Ease.Linear;
    }

    /// <summary>Kilat warna: mulai dari flash lalu pulih ke warna sekarang.</summary>
    public static void Flash(Graphic g, Color flash, float dur = 0.3f)
    {
        if (g == null) return;
        Flash(g, flash, g.color, dur);
    }

    /// <summary>Kilat warna dengan warna dasar eksplisit; pakai ini bila target sering di-flash beruntun.</summary>
    public static void Flash(Graphic g, Color flash, Color baseColor, float dur = 0.3f)
    {
        if (g == null) return;
        var tw = Spawn(Kind.ColorTo, Channel.Color, g, g.gameObject);
        tw.gfx = g;
        tw.fromC = flash; tw.toC = baseColor;
        tw.dur = dur; tw.ease = Ease.OutQuad;
        g.color = flash;
    }

    public static void ColorTo(Graphic g, Color to, float dur = 0.2f, float delay = 0f)
    {
        if (g == null) return;
        var tw = Spawn(Kind.ColorTo, Channel.Color, g, g.gameObject);
        tw.gfx = g;
        tw.fromC = g.color; tw.toC = to;
        tw.dur = dur; tw.delay = delay; tw.ease = Ease.OutQuad;
    }

    // ----------------------------------------------------------- nilai & teks

    public static void SliderTo(Slider s, float to, float dur = 0.3f)
    {
        if (s == null) return;
        var tw = Spawn(Kind.SliderTo, Channel.Slider, s, s.gameObject);
        tw.slider = s;
        tw.fromF = s.value; tw.toF = to;
        tw.dur = dur; tw.ease = Ease.OutCubic;
    }

    /// <summary>Angka berjalan dari from ke to; format prefix{0}suffix tanpa alokasi per-frame.</summary>
    public static void CountTo(TMP_Text label, int from, int to, float dur = 0.4f,
                               string prefix = "", string suffix = "", float delay = 0f)
    {
        if (label == null) return;
        var tw = Spawn(Kind.Count, Channel.Text, label, label.gameObject);
        tw.tmp = label;
        tw.fromI = from; tw.toI = to;
        tw.lastShown = from;
        tw.fmt = prefix + "{0}" + suffix;
        tw.dur = dur; tw.delay = delay; tw.ease = Ease.OutCubic;
        label.SetText(tw.fmt, from);
    }

    /// <summary>Teks diketik per huruf lewat maxVisibleCharacters (tanpa realokasi mesh per huruf).</summary>
    public static void Typewriter(TMP_Text label, string content, float cps = 40f, float delay = 0f)
    {
        if (label == null) return;
        content = content ?? "";
        var tw = Spawn(Kind.Typewriter, Channel.Text, label, label.gameObject);
        tw.tmp = label;
        tw.cps = Mathf.Max(1f, cps);
        // Length menghitung tag rich text juga; efeknya cuma durasi sedikit lebih panjang
        tw.dur = Mathf.Max(0.01f, content.Length / tw.cps);
        tw.delay = delay; tw.lastShown = 0; tw.ease = Ease.Linear;
        label.text = content;
        label.maxVisibleCharacters = 0;
    }

    // ------------------------------------------------------------------- kill

    /// <summary>Hentikan semua tween milik satu GameObject. onComplete tidak dipanggil.</summary>
    public static void Kill(GameObject go)
    {
        if (go == null) return;
        for (int i = 0; i < active.Count; i++)
        {
            var a = active[i];
            if (a.alive && a.owner == go) { a.alive = false; a.onComplete = null; }
        }
    }

    public static void Kill(Component c)
    {
        if (c != null) Kill(c.gameObject);
    }

    static void KillChannel(GameObject go, Channel channel)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var a = active[i];
            if (a.alive && a.owner == go && a.channel == channel) { a.alive = false; a.onComplete = null; }
        }
    }
}
