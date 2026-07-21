using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Layar tas. Dibuka dengan Tab atau I; game ikut berhenti selama tas terbuka.
/// Seluruh isinya dirakit lewat kode, jadi tidak ada yang perlu dipasang di scene.
///
/// Seluruh isi panel kini bernaung di satu holder (panelRoot) supaya bisa
/// dianimasikan sebagai satu kesatuan: panel pop, slot menyusul bergelombang
/// diagonal, kolom kanan meluncur masuk. Semua animasi unscaled karena
/// timeScale = 0 selama tas terbuka.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    const int Cols = 6, Rows = 4;
    const float SlotSize = 96f, SlotGap = 10f;
    const float RuleWidth = 1400f;

    GameObject root;
    CanvasGroup rootCg, detailCg;
    RectTransform panelRoot, titleRt, ruleRt, goldRt, equipCol, detailCol, closeRt;
    readonly List<Image> slotIcons = new List<Image>();
    readonly List<Image> slotFrames = new List<Image>();
    readonly List<TextMeshProUGUI> slotCounts = new List<TextMeshProUGUI>();

    Image equipWeapon, equipArmor, equipCharmA, equipCharmB;
    TextMeshProUGUI goldText, detailName, detailBody, pusakaText;
    Inventory inv;
    int selected = -1;
    int lastPulsed = -1, lastDetailSel = -2;
    bool open;

    void Awake()
    {
        Instance = this;
        Build();
        // jangan lewat SetOpen: menyentuh timeScale saat start bisa membatalkan pause
        open = false;
        if (root != null) root.SetActive(false);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I)) Toggle();
        if (open && Input.GetKeyDown(KeyCode.Escape)) SetOpen(false);

        if (inv == null)
        {
            inv = Inventory.Instance;
            if (inv != null) { inv.Changed += Refresh; Refresh(); }
        }
    }

    public void Toggle() => SetOpen(!open);

    void SetOpen(bool value)
    {
        open = value;
        Time.timeScale = value ? 0f : 1f;
        if (root == null) return;

        if (value)
        {
            UIMotion.Kill(root);
            root.SetActive(true);
            selected = -1;          // buka selalu mulai netral; juga menghindari pulse bentrok dgn pop masuk
            Refresh();
            PlayEntrance();
        }
        else
        {
            UIMotion.Kill(root);
            UIMotion.FadeOut(rootCg, 0.12f, 0f, () => { if (root != null) root.SetActive(false); });
            UIMotion.ScaleTo(panelRoot, 0.96f, 0.12f);
        }
    }

    void PlayEntrance()
    {
        UIMotion.FadeIn(rootCg, 0.15f);
        UIMotion.PopIn(panelRoot, 0f, 0.26f);
        UIMotion.SlideFrom(titleRt, Vector2.left, 26f, 0.3f, 0.06f);
        UIMotion.WipeWidth(ruleRt, RuleWidth, 0.35f, 0.08f);
        UIMotion.SlideFrom(goldRt, Vector2.right, 26f, 0.3f, 0.06f);

        // gelombang diagonal: slot kiri-atas dulu, kanan-bawah terakhir
        for (int i = 0; i < slotFrames.Count; i++)
        {
            int r = i / Cols, c = i % Cols;
            UIMotion.PopIn((RectTransform)slotFrames[i].transform, 0.1f + (r + c) * 0.02f, 0.22f);
        }

        UIMotion.FadeIn(UIScreenFx.Group(equipCol), 0.3f, 0.18f);
        UIMotion.SlideFrom(equipCol, Vector2.right, 30f, 0.35f, 0.18f);
        UIMotion.FadeIn(UIScreenFx.Group(detailCol), 0.3f, 0.24f);
        UIMotion.SlideFrom(detailCol, Vector2.right, 30f, 0.35f, 0.24f);
        UIMotion.FadeIn(UIScreenFx.Group(closeRt), 0.3f, 0.3f);
        UIMotion.SlideFrom(closeRt, Vector2.down, 18f, 0.3f, 0.3f);
    }

    // ------------------------------------------------------------------ rakit
    void Build()
    {
        var canvas = UIKit.MakeCanvas("Inventory Canvas", 50);
        root = canvas.gameObject;
        rootCg = root.AddComponent<CanvasGroup>();

        UIKit.MakePanel(canvas.transform, Vector2.zero, new Vector2(4000, 4000),
                        new Color(0.02f, 0.02f, 0.05f, 0.88f), true);

        panelRoot = UIKit.Rect(canvas.transform, "PanelRoot", Vector2.zero, new Vector2(1500, 860));

        UIKit.MakePanel(panelRoot, Vector2.zero, new Vector2(1500, 860), UIKit.Panel, true);
        var title = UIKit.MakeText(panelRoot, "TAS", new Vector2(-560, 360), new Vector2(400, 70), 52,
                                   TextAlignmentOptions.Left);
        title.color = UIKit.Gold;
        titleRt = title.rectTransform;
        ruleRt = UIKit.MakeRule(panelRoot, new Vector2(0, 322), RuleWidth).rectTransform;

        goldText = UIKit.MakeText(panelRoot, "0 kepeng", new Vector2(560, 360), new Vector2(400, 70), 34,
                                  TextAlignmentOptions.Right);
        goldText.color = UIKit.Gold;
        goldRt = goldText.rectTransform;

        BuildGrid(panelRoot);
        BuildEquipColumn(panelRoot);
        BuildDetail(panelRoot);

        var close = UIKit.MakeButton(panelRoot, "TUTUP  (Tab)", new Vector2(0, -390), new Vector2(340, 66), null, 26);
        close.onClick.AddListener(() => SetOpen(false));
        closeRt = (RectTransform)close.transform;
    }

    void BuildGrid(Transform parent)
    {
        float startX = -640f + SlotSize * 0.5f;
        float startY = 220f - SlotSize * 0.5f;

        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
            {
                int index = r * Cols + c;
                var pos = new Vector2(startX + c * (SlotSize + SlotGap), startY - r * (SlotSize + SlotGap));

                var frame = UIKit.MakePanel(parent, pos, new Vector2(SlotSize, SlotSize), UIKit.PanelSoft, true);
                frame.gameObject.name = $"Slot{index}";

                var btn = frame.gameObject.AddComponent<Button>();
                btn.targetGraphic = frame;
                btn.onClick.AddListener(() => OnSlotClicked(index));

                // slot punya bahasa geraknya sendiri (angkat + pulse), bukan UIButtonFx
                var frameRt = (RectTransform)frame.transform;
                var hover = frame.gameObject.AddComponent<HoverRelay>();
                hover.onEnter = () => { if (index != selected) UIMotion.ScaleTo(frameRt, 1.06f, 0.1f); };
                hover.onExit = () => { if (index != selected) UIMotion.ScaleTo(frameRt, 1f, 0.12f); };

                var icon = UIKit.MakeSprite(frame.transform, null, Vector2.zero, new Vector2(SlotSize - 18f, SlotSize - 18f));
                var count = UIKit.MakeText(frame.transform, "", new Vector2(30, -30), new Vector2(50, 30), 20);
                count.color = UIKit.Parchment;

                slotFrames.Add(frame);
                slotIcons.Add(icon);
                slotCounts.Add(count);
            }
    }

    void BuildEquipColumn(Transform parent)
    {
        // satu holder supaya seluruh kolom bisa meluncur masuk bersama
        equipCol = UIKit.Rect(parent, "EquipColumn", new Vector2(190, 0), new Vector2(340, 700));

        UIKit.MakeText(equipCol, "DIPAKAI", new Vector2(0, 246), new Vector2(300, 44), 28).color = UIKit.Teal;

        equipWeapon = EquipSlot(equipCol, "SENJATA", new Vector2(0, 160), ItemCategory.Weapon, false);
        equipArmor = EquipSlot(equipCol, "ZIRAH", new Vector2(0, 34), ItemCategory.Armor, false);
        equipCharmA = EquipSlot(equipCol, "JIMAT I", new Vector2(0, -92), ItemCategory.Charm, false);
        equipCharmB = EquipSlot(equipCol, "JIMAT II", new Vector2(0, -218), ItemCategory.Charm, true);

        pusakaText = UIKit.MakeText(equipCol, "", new Vector2(0, -320), new Vector2(320, 90), 20);
        pusakaText.color = UIKit.Ember;
    }

    Image EquipSlot(Transform parent, string label, Vector2 pos, ItemCategory cat, bool charmB)
    {
        UIKit.MakeText(parent, label, pos + new Vector2(-96, 0), new Vector2(150, 40), 22,
                       TextAlignmentOptions.Right).color = UIKit.Muted;

        var frame = UIKit.MakePanel(parent, pos + new Vector2(24, 0), new Vector2(88, 88), UIKit.PanelSoft, true);
        var btn = frame.gameObject.AddComponent<Button>();
        btn.targetGraphic = frame;
        btn.onClick.AddListener(() => { if (inv != null) { inv.Unequip(cat, charmB); Refresh(); } });
        UIButtonFx.Attach(btn);

        return UIKit.MakeSprite(frame.transform, null, Vector2.zero, new Vector2(70, 70));
    }

    void BuildDetail(Transform parent)
    {
        detailCol = UIKit.Rect(parent, "DetailColumn", new Vector2(520, 40), new Vector2(400, 560));

        UIKit.MakePanel(detailCol, Vector2.zero, new Vector2(400, 560), UIKit.PanelSoft, false);

        // teks rincian dipisah ke grup sendiri supaya bisa cross-fade saat pilihan berganti
        var detailTexts = UIKit.Rect(detailCol, "DetailTexts", Vector2.zero, new Vector2(400, 560));
        detailCg = detailTexts.gameObject.AddComponent<CanvasGroup>();

        detailName = UIKit.MakeText(detailTexts, "", new Vector2(0, 228), new Vector2(370, 60), 30);
        detailName.color = UIKit.Gold;

        detailBody = UIKit.MakeText(detailTexts, "Pilih barang untuk melihat rinciannya.",
                                    new Vector2(0, 20), new Vector2(360, 380), 22, TextAlignmentOptions.Top);
        detailBody.color = UIKit.Parchment;

        var drop = UIKit.MakeButton(detailCol, "BUANG", new Vector2(0, -236), new Vector2(240, 60),
                                    new Color32(0xE7, 0x6F, 0x51, 0xB0), 24);
        drop.onClick.AddListener(() =>
        {
            if (inv != null && selected >= 0) { inv.Drop(selected); selected = -1; Refresh(); }
        });
    }

    // --------------------------------------------------------------- interaksi
    void OnSlotClicked(int index)
    {
        if (inv == null) return;
        if (selected == index) { inv.UseSlot(index); selected = -1; }
        else
        {
            selected = index;
            UIMotion.Punch((RectTransform)slotFrames[index].transform, 0.1f, 0.2f);
        }
        Refresh();
    }

    void Refresh()
    {
        if (inv == null) return;

        goldText.text = $"{inv.Gold} kepeng";

        var slots = inv.Slots;
        for (int i = 0; i < slotIcons.Count; i++)
        {
            bool filled = i < slots.Count;
            var def = filled ? slots[i].Def : null;

            slotIcons[i].sprite = def != null ? def.Icon : null;
            slotIcons[i].color = def != null ? Color.white : new Color(1, 1, 1, 0);
            slotCounts[i].text = (filled && slots[i].count > 1) ? slots[i].count.ToString() : "";

            slotFrames[i].color = i == selected
                ? UIKit.Teal * 0.6f
                : (def != null ? ItemDef.RarityColor(def.rarity) * 0.35f : UIKit.PanelSoft);
        }

        SetIcon(equipWeapon, inv.Weapon);
        SetIcon(equipArmor, inv.Armor);
        SetIcon(equipCharmA, inv.CharmA);
        SetIcon(equipCharmB, inv.CharmB);

        var relics = inv.Run != null ? inv.Run.pusaka : null;
        if (relics != null && relics.Count > 0)
        {
            var names = new List<string>();
            foreach (var id in relics)
            {
                var d = ItemDatabase.Get(id);
                if (d != null) names.Add(d.displayName);
            }
            pusakaText.text = "PUSAKA: " + string.Join(", ", names);
        }
        else pusakaText.text = "";

        var sel = (selected >= 0 && selected < slots.Count) ? slots[selected].Def : null;
        if (sel == null)
        {
            detailName.text = "";
            detailBody.text = "Pilih barang untuk melihat rinciannya.\nKlik dua kali untuk memakai.";
        }
        else
        {
            detailName.text = sel.displayName;
            detailName.color = ItemDef.RarityColor(sel.rarity);
            detailBody.text = $"<i>{sel.flavor}</i>\n\n{Describe(sel)}\n\nKlik lagi untuk " +
                              (sel.IsConsumable ? "memakai." : "memasang.");
        }

        // animasi status hanya saat layar benar-benar tampil
        bool animate = open && root != null && root.activeSelf;

        if (selected != lastPulsed)
        {
            if (lastPulsed >= 0 && lastPulsed < slotFrames.Count)
                UIMotion.StopPulse((RectTransform)slotFrames[lastPulsed].transform);
            if (animate && selected >= 0 && selected < slotFrames.Count)
                UIMotion.Pulse((RectTransform)slotFrames[selected].transform, 0.05f, 1.0f);
            lastPulsed = animate ? selected : -1;
        }

        if (selected != lastDetailSel)
        {
            if (animate && detailCg != null) UIMotion.FadeIn(detailCg, 0.18f);
            lastDetailSel = selected;
        }
    }

    void SetIcon(Image img, ItemDef def)
    {
        if (img == null) return;
        img.sprite = def != null ? def.Icon : null;
        img.color = def != null ? Color.white : new Color(1, 1, 1, 0);
    }

    static string Describe(ItemDef def)
    {
        if (def.effects == null || def.effects.Length == 0) return "Tidak memberi efek.";
        var lines = new List<string>();
        foreach (var e in def.effects)
        {
            if (e == null) continue;
            string s = EffectText(e);
            if (e.duration > 0f) s += $" ({e.duration:0.#} detik)";
            lines.Add("- " + s);
        }
        return string.Join("\n", lines);
    }

    static string EffectText(ItemEffect e)
    {
        switch (e.kind)
        {
            case EffectKind.MaxHp: return $"HP maksimum +{e.value:0}";
            case EffectKind.Damage: return $"Serangan +{e.value:0}";
            case EffectKind.DamagePct: return $"Serangan +{e.value * 100f:0}%";
            case EffectKind.MoveSpeed: return $"Kecepatan +{e.value:0.#}";
            case EffectKind.AttackSpeed: return $"Kecepatan serang +{e.value * 100f:0}%";
            case EffectKind.Range: return $"Jangkauan +{e.value:0.#}";
            case EffectKind.Armor: return $"Tahan damage {e.value * 100f:0}%";
            case EffectKind.CritChance: return $"Peluang kritis {e.value * 100f:0}%";
            case EffectKind.LifeSteal: return $"Serap darah {e.value * 100f:0}%";
            case EffectKind.Thorns: return $"Duri balas {e.value:0}";
            case EffectKind.Heal: return $"Pulihkan {e.value:0} HP";
            case EffectKind.Gold: return $"Dapat {e.value:0} kepeng";
            case EffectKind.Luck: return $"Keberuntungan +{e.value:0.#}";
            default: return e.kind.ToString();
        }
    }
}
