using TMPro;
using UnityEngine;

/// <summary>
/// Sanggar Sesaji. Dua fungsi:
///
/// 1. Berkah -- bayar 15 HP atau 25 picis untuk satu berkah acak. Membayar
///    dengan HP membuat sanggar tetap berguna saat kantong kosong, dan menjadi
///    taruhan sungguhan menjelang ruangan boss.
/// 2. Tempa -- korbankan satu Wesi Aji untuk menaikkan tingkat senjata.
///
/// Tempaan sengaja disimpan sebagai TINGKAT senjata (RunInventory.weaponLevel),
/// bukan sebagai bonus damage lepas. Alasannya: tingkat itu terbaca sebagai
/// kemajuan yang dimiliki pemain ("kerisku sudah +3"), ikut berpindah kalau ia
/// mengganti senjata, dan angkanya bisa ditampilkan di satu tempat tanpa
/// menjejalkan efek tambahan ke tiap ItemDef.
/// </summary>
public class Shrine : Interactable
{
    struct Blessing
    {
        public string name, desc;
        public EffectKind kind;
        public float value;
    }

    static readonly Blessing[] Blessings =
    {
        new Blessing { name = "Berkah Bumi",  desc = "HP maksimum +10", kind = EffectKind.MaxHp,     value = 10f },
        new Blessing { name = "Berkah Geni",  desc = "Serangan +3",     kind = EffectKind.Damage,    value = 3f  },
        new Blessing { name = "Berkah Bayu",  desc = "Kecepatan +0.2",  kind = EffectKind.MoveSpeed, value = 0.2f },
        new Blessing { name = "Berkah Tirta", desc = "Pulih 50 HP",     kind = EffectKind.Heal,      value = 50f },
    };

    const int HpCost = 15, GoldCost = 25;

    GameObject panel;
    CanvasGroup panelCg;
    RectTransform panelRoot;
    TextMeshProUGUI statusLabel, forgeLabel;

    protected override string Prompt => "Sesaji";
    protected override float PromptHeight => 1.5f;

    protected override void OnInteract()
    {
        if (!OpenPanel()) return;
        Sfx("ui_confirm", 0.7f);

        if (panel == null) BuildPanel();
        panel.SetActive(true);
        Refresh();

        UIMotion.FadeIn(panelCg, 0.16f);
        UIMotion.PopIn(panelRoot, 0f, 0.26f);
    }

    protected override void OnPanelUpdate()
    {
        if (CloseKeyPressed() || Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    void Close()
    {
        Sfx("ui_back", 0.7f);
        if (panelCg != null)
            UIMotion.FadeOut(panelCg, 0.12f, 0f, () => { if (panel != null) panel.SetActive(false); });
        ClosePanel();
    }

    void BuildPanel()
    {
        var canvas = UIKit.MakeCanvas("Shrine Canvas", 60);
        panel = canvas.gameObject;
        panelCg = panel.AddComponent<CanvasGroup>();

        UIKit.MakePanel(canvas.transform, Vector2.zero, new Vector2(4000, 4000),
                        new Color(0.02f, 0.03f, 0.05f, 0.86f), true);

        panelRoot = UIKit.Rect(canvas.transform, "PanelRoot", Vector2.zero, new Vector2(1080, 620));
        UIKit.MakePanel(panelRoot, Vector2.zero, new Vector2(1080, 620), UIKit.Panel, true);

        UIKit.MakeText(panelRoot, "SANGGAR SESAJI", new Vector2(0, 234), new Vector2(900, 70), 46)
             .color = UIKit.Gold;
        UIKit.MakeRule(panelRoot, new Vector2(0, 194), 820);
        UIKit.MakeText(panelRoot, "Yang diberi, dibalas. Yang dipaksa, ditagih.",
                       new Vector2(0, 152), new Vector2(900, 40), 22).color = UIKit.Teal;

        var byHp = UIKit.MakeButton(panelRoot, $"SESAJI DARAH  ({HpCost} HP)", new Vector2(-250, 40),
                                    new Vector2(440, 84), new Color32(0xE7, 0x6F, 0x51, 0xB0), 26);
        byHp.onClick.AddListener(() => Offer(true));

        var byGold = UIKit.MakeButton(panelRoot, $"SESAJI PICIS  ({GoldCost} picis)", new Vector2(250, 40),
                                      new Vector2(440, 84), new Color32(0x2A, 0x9D, 0x8F, 0xC0), 26);
        byGold.onClick.AddListener(() => Offer(false));

        statusLabel = UIKit.MakeText(panelRoot, "", new Vector2(0, -46), new Vector2(940, 60), 26);
        statusLabel.color = UIKit.Parchment;

        UIKit.MakeRule(panelRoot, new Vector2(0, -92), 820, UIKit.Muted);

        forgeLabel = UIKit.MakeText(panelRoot, "", new Vector2(0, -134), new Vector2(940, 50), 22);
        forgeLabel.color = UIKit.Muted;

        var forge = UIKit.MakeButton(panelRoot, "TEMPA SENJATA", new Vector2(0, -196),
                                     new Vector2(400, 74), new Color32(0xE3, 0xB3, 0x41, 0x90), 26);
        forge.onClick.AddListener(Forge);

        var close = UIKit.MakeButton(panelRoot, "TUTUP  (E)", new Vector2(0, -268), new Vector2(300, 56), null, 22);
        close.onClick.AddListener(Close);
    }

    // ---------------------------------------------------------------- berkah
    void Offer(bool payWithHp)
    {
        var inv = Inventory.Instance;
        var hp = PlayerComponent<PlayerHealth>();
        var stats = PlayerComponent<PlayerStats>();
        if (inv == null || stats == null) return;

        if (payWithHp)
        {
            // sisakan minimal 1 HP: sanggar memeras, bukan membunuh
            if (hp == null || hp.Current <= HpCost)
            {
                Deny("Darahmu tidak cukup untuk dipersembahkan.");
                return;
            }
            hp.TakeDamage(HpCost, transform.position);
        }
        else
        {
            if (inv.Gold < GoldCost) { Deny("Picismu tidak cukup."); return; }
            inv.AddGold(-GoldCost);
        }

        var b = Blessings[Random.Range(0, Blessings.Length)];
        if (b.kind == EffectKind.Heal)
        {
            if (hp != null) hp.Heal(Mathf.RoundToInt(b.value));
        }
        else
        {
            stats.AddPermanent(b.kind, b.value);
        }

        Sfx("ui_confirm", 1f);
        FloatingText.Spawn(transform.position, b.name, UIKit.Teal, true);
        if (statusLabel != null)
        {
            statusLabel.text = $"{b.name} turun kepadamu. {b.desc}.";
            statusLabel.color = UIKit.Teal;
        }
        UIMotion.Punch(panelRoot, 0.08f, 0.3f);
        Refresh();
    }

    void Deny(string reason)
    {
        Sfx("ui_error", 0.8f);
        if (statusLabel != null) { statusLabel.text = reason; statusLabel.color = UIKit.Ember; }
        UIMotion.Shake(panelRoot, 10f, 0.25f);
    }

    // ---------------------------------------------------------------- tempaan
    void Forge()
    {
        var inv = Inventory.Instance;
        var stats = PlayerComponent<PlayerStats>();
        if (inv == null || inv.Run == null) return;

        if (string.IsNullOrEmpty(inv.Run.weaponId)) { Deny("Tidak ada senjata yang dipakai."); return; }

        int slot = FindWesiAji(inv);
        if (slot < 0) { Deny("Kau tidak membawa Wesi Aji."); return; }

        inv.Drop(slot);                       // pakai satu; jatuhnya langsung dihapus di bawah
        CleanupDroppedWesi();

        inv.Run.weaponLevel++;
        if (stats != null) stats.Recalculate();

        Sfx("crit", 0.9f);
        FloatingText.Spawn(transform.position, $"+{inv.Run.weaponLevel}", UIKit.Gold, true);
        if (statusLabel != null)
        {
            var w = ItemDatabase.Get(inv.Run.weaponId);
            statusLabel.text = $"{(w != null ? w.displayName : "Senjatamu")} kini +{inv.Run.weaponLevel}.";
            statusLabel.color = UIKit.Gold;
        }
        Refresh();
    }

    static int FindWesiAji(Inventory inv)
    {
        var slots = inv.Slots;
        for (int i = 0; i < slots.Count; i++)
            if (slots[i] != null && slots[i].id == "wesi_aji") return i;
        return -1;
    }

    /// <summary>
    /// Inventory.Drop menjatuhkan barangnya ke lantai. Untuk tempaan bahannya
    /// habis terpakai, jadi pickup yang baru muncul di dekat altar dibuang.
    /// </summary>
    void CleanupDroppedWesi()
    {
        var found = Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Exclude);
        foreach (var p in found)
        {
            if (p == null || p.itemId != "wesi_aji") continue;
            if (Vector2.Distance(p.transform.position, transform.position) > 4f) continue;
            Destroy(p.gameObject);
            return;
        }
    }

    void Refresh()
    {
        var inv = Inventory.Instance;
        if (forgeLabel == null || inv == null || inv.Run == null) return;

        var w = ItemDatabase.Get(inv.Run.weaponId);
        string weapon = w != null ? w.displayName : "tanpa senjata";
        int level = inv.Run.weaponLevel;
        bool hasMaterial = FindWesiAji(inv) >= 0;

        forgeLabel.text = $"Senjata: {weapon} +{level}   |   Wesi Aji: {(hasMaterial ? "ada" : "tidak ada")}" +
                          "   |   tiap tempaan menambah serangan +2";
        forgeLabel.color = hasMaterial ? UIKit.Parchment : UIKit.Muted;
    }
}
