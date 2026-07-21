using TMPro;
using UnityEngine;

/// <summary>
/// Dasar semua objek dunia yang merespons tombol E (warung, sanggar).
/// Prompt melayang dibangun sebagai kanvas world-space yang menempel pada
/// objek: ikut kamera dengan gratis, tanpa konversi posisi tiap frame
/// seperti FloatingText. Subclass cukup mengisi Prompt dan OnInteract.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public abstract class Interactable : MonoBehaviour
{
    // Satu panel untuk seluruh game: pemain tidak mungkin berdiri di dua
    // pemicu berbeda dengan dua panel terbuka; Interactable lain berhenti
    // membaca E selama ada panel milik siapa pun yang terbuka.
    static Interactable openOwner;
    public static bool PanelOpen => openOwner != null;

    // jaga-jaga bila domain reload dimatikan di editor: state statik harus bersih
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => openOwner = null;

    /// <summary>Transform pemain selama berada di dalam pemicu; null di luar.</summary>
    protected Transform Player { get; private set; }

    CanvasGroup promptCg;
    RectTransform promptHolder;
    int openedFrame = -1;   // GetKeyDown yang sama tidak boleh membuka lalu langsung menutup panel

    protected abstract string Prompt { get; }
    protected abstract void OnInteract();

    /// <summary>Tinggi prompt di atas pusat objek, dalam satuan dunia.</summary>
    protected virtual float PromptHeight => 1.7f;

    protected virtual void Awake() => BuildPrompt();

    void BuildPrompt()
    {
        var go = new GameObject("Prompt", typeof(RectTransform));
        go.transform.SetParent(transform, false);

        // generator men-scale objek dunia (vendor 2.6x, altar 2.2x); kanvas harus
        // menetralkan skala induk supaya ukuran prompt konsisten di semua objek
        float parentScale = Mathf.Max(0.0001f, transform.lossyScale.x);
        const float unitPerPx = 0.012f;
        go.transform.localScale = Vector3.one * (unitPerPx / parentScale);
        go.transform.localPosition = new Vector3(0f, PromptHeight / parentScale, 0f);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 60;   // di atas sprite dunia, di bawah kanvas layar (HUD 0+, tas 50)

        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(300f, 76f);

        promptCg = go.AddComponent<CanvasGroup>();
        promptCg.alpha = 0f;
        promptCg.blocksRaycasts = false;
        promptCg.interactable = false;

        // holder terpisah supaya animasi skala tidak merusak skala kompensasi kanvas
        promptHolder = UIKit.Rect(go.transform, "Holder", Vector2.zero, rt.sizeDelta);
        UIKit.MakePanel(promptHolder, Vector2.zero, rt.sizeDelta, new Color(0.06f, 0.06f, 0.10f, 0.85f));
        UIKit.MakeRule(promptHolder, new Vector2(0f, -34f), 220f);
        var label = UIKit.MakeText(promptHolder, Prompt, Vector2.zero, rt.sizeDelta, 34f);
        label.color = UIKit.Gold;
        label.fontStyle = FontStyles.Bold;
    }

    void Update()
    {
        if (openOwner == this) { OnPanelUpdate(); return; }
        if (openOwner != null || Player == null) return;
        // layar lain (tas, pause) sedang memegang game; E tidak boleh menembusnya
        if (Time.timeScale == 0f) return;
        if (Input.GetKeyDown(KeyCode.E)) OnInteract();
    }

    /// <summary>Berjalan tiap frame selama panel milik objek ini terbuka (tombol tutup dsb).</summary>
    protected virtual void OnPanelUpdate() { }

    /// <summary>true bila pemain menekan tombol tutup; frame pembuka dikecualikan.</summary>
    protected bool CloseKeyPressed()
        => Time.frameCount != openedFrame && Input.GetKeyDown(KeyCode.E);

    /// <summary>Klaim panel + bekukan game. false bila panel lain sedang terbuka.</summary>
    protected bool OpenPanel()
    {
        if (openOwner != null) return false;
        openOwner = this;
        openedFrame = Time.frameCount;
        Time.timeScale = 0f;   // karena itu seluruh gerak panel wajib unscaled
        HidePrompt();
        return true;
    }

    protected void ClosePanel()
    {
        if (openOwner != this) return;
        openOwner = null;
        Time.timeScale = 1f;
        if (Player != null) ShowPrompt();
    }

    protected virtual void OnDestroy()
    {
        // scene bisa berganti saat panel terbuka; timeScale tidak boleh tertinggal 0
        if (openOwner == this) { openOwner = null; Time.timeScale = 1f; }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Player = other.transform;
        if (openOwner != this) ShowPrompt();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Player = null;
        HidePrompt();
    }

    /// <summary>Komponen pemain; lewat parent karena collider bisa berupa anak rig.</summary>
    protected T PlayerComponent<T>() where T : Component
        => Player == null ? null : Player.GetComponentInParent<T>();

    void ShowPrompt()
    {
        if (promptCg == null) return;
        UIMotion.FadeIn(promptCg, 0.18f);
        UIMotion.Punch(promptHolder, 0.1f, 0.22f);
    }

    void HidePrompt()
    {
        if (promptCg == null) return;
        UIMotion.FadeOut(promptCg, 0.15f);
    }

    protected static void Sfx(string bank, float vol = 1f)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.Play(bank, vol);
    }
}

/// <summary>
/// Teks dan harga bersama untuk panel interaksi. Duplikat kecil dari
/// InventoryUI.EffectText karena versi itu private dan file UI bukan milik
/// modul ini.
/// </summary>
public static class InteractText
{
    /// <summary>
    /// Tangga harga design bible (Biasa 15 / Pilihan 30 / Langka 60 / Agung 120 /
    /// Pusaka 250) dipetakan berdasarkan urutan tingkat ke enum Rarity kode
    /// (Umum / Langka / Pusaka / Sakti / Legenda): posisi ordinal sama, hanya
    /// namanya yang berbeda.
    /// </summary>
    public static int PriceOf(Rarity r)
    {
        switch (r)
        {
            case Rarity.Langka: return 30;
            case Rarity.Pusaka: return 60;
            case Rarity.Sakti: return 120;
            case Rarity.Legenda: return 250;
            default: return 15;
        }
    }

    public static string Line(EffectKind kind, float value)
    {
        switch (kind)
        {
            case EffectKind.MaxHp: return $"HP maks +{value:0.#}";
            case EffectKind.Damage: return $"Serangan +{value:0.#}";
            case EffectKind.DamagePct: return $"Serangan +{value * 100f:0}%";
            case EffectKind.MoveSpeed: return $"Kecepatan +{value:0.##}";
            case EffectKind.AttackSpeed: return $"Kec. serang +{value * 100f:0}%";
            case EffectKind.Range: return $"Jangkauan +{value:0.#}";
            case EffectKind.Armor: return $"Tahan damage {value * 100f:0}%";
            case EffectKind.CritChance: return $"Kritis +{value * 100f:0}%";
            case EffectKind.LifeSteal: return $"Serap darah {value * 100f:0}%";
            case EffectKind.Thorns: return $"Duri balas {value:0.#}";
            case EffectKind.Heal: return $"Pulih {value:0} HP";
            case EffectKind.Gold: return $"+{value:0} picis";
            case EffectKind.Luck: return $"Keberuntungan +{value:0.#}";
            case EffectKind.Dodge: return $"Elak +{value * 100f:0}%";
            case EffectKind.XpGain: return $"Perolehan +{value * 100f:0}%";
            case EffectKind.Revive: return $"Bangkit {value * 100f:0}% HP";
            default: return kind.ToString();
        }
    }

    public static string Summary(ItemDef def)
    {
        if (def == null || def.effects == null || def.effects.Length == 0) return "Tanpa efek.";
        var sb = new System.Text.StringBuilder(96);
        foreach (var e in def.effects)
        {
            if (e == null) continue;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append("- ").Append(Line(e.kind, e.value));
            if (e.duration > 0f) sb.Append(" (").Append(e.duration.ToString("0.#")).Append(" dtk)");
        }
        return sb.Length == 0 ? "Tanpa efek." : sb.ToString();
    }
}
