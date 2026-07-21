using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Warung Lelembut, ditunggui Mbah Warung. Menjual 3 perlengkapan dan 2 ramuan
/// yang diundi sekali saat ruangan dibangun; stok tidak pernah diisi ulang,
/// jadi keputusan membeli benar-benar punya bobot.
/// </summary>
public class ShopVendor : Interactable
{
    class Stock
    {
        public ItemDef def;
        public int price;
        public bool sold;
        public Image frame;
        public TextMeshProUGUI priceLabel;
    }

    static readonly string[] Greetings =
    {
        "Mampir dhisik, Ngger. Sing murah ora mesthi elek.",
        "Barang iki wis suwe ngenteni sing duwe.",
        "Aku ora nampa utang. Picis dhisik, lagi barang.",
        "Ati-ati ing ngisor. Sing mati ora kabeh gelem lunga.",
    };

    readonly List<Stock> stock = new List<Stock>();
    GameObject panel;
    CanvasGroup panelCg;
    RectTransform panelRoot;
    TextMeshProUGUI goldLabel, sayLabel;

    protected override string Prompt => "Warung";
    protected override float PromptHeight => 1.4f;

    protected override void Awake()
    {
        base.Awake();
        RollStock();
    }

    /// <summary>Tiga perlengkapan dan dua ramuan, diundi sekali seumur ruangan.</summary>
    void RollStock()
    {
        for (int i = 0; i < 3; i++) TryAdd(RandomEquipment());
        for (int i = 0; i < 2; i++) TryAdd(ItemDatabase.RandomOf(Rarity.Langka, ItemCategory.Consumable));
    }

    ItemDef RandomEquipment()
    {
        var cats = new[] { ItemCategory.Weapon, ItemCategory.Armor, ItemCategory.Charm };
        return ItemDatabase.RandomOf(RollRarity(), cats[Random.Range(0, cats.Length)]);
    }

    static Rarity RollRarity()
    {
        float r = Random.value;
        if (r < 0.06f) return Rarity.Legenda;
        if (r < 0.20f) return Rarity.Sakti;
        if (r < 0.45f) return Rarity.Pusaka;
        if (r < 0.75f) return Rarity.Langka;
        return Rarity.Umum;
    }

    void TryAdd(ItemDef def)
    {
        if (def == null) return;
        foreach (var s in stock) if (s.def.id == def.id) return;   // jangan jual barang kembar
        stock.Add(new Stock { def = def, price = InteractText.PriceOf(def.rarity) });
    }

    // ------------------------------------------------------------------ panel
    protected override void OnInteract()
    {
        if (stock.Count == 0) { Sfx("ui_error", 0.7f); return; }
        if (!OpenPanel()) return;

        Sfx("ui_confirm", 0.7f);
        if (panel == null) BuildPanel();
        panel.SetActive(true);

        if (sayLabel != null) sayLabel.text = Greetings[Random.Range(0, Greetings.Length)];
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
        var canvas = UIKit.MakeCanvas("Shop Canvas", 60);
        panel = canvas.gameObject;
        panelCg = panel.AddComponent<CanvasGroup>();

        UIKit.MakePanel(canvas.transform, Vector2.zero, new Vector2(4000, 4000),
                        new Color(0.02f, 0.02f, 0.05f, 0.86f), true);

        panelRoot = UIKit.Rect(canvas.transform, "PanelRoot", Vector2.zero, new Vector2(1500, 760));
        UIKit.MakePanel(panelRoot, Vector2.zero, new Vector2(1500, 760), UIKit.Panel, true);

        UIKit.MakeText(panelRoot, "WARUNG LELEMBUT", new Vector2(-540, 310), new Vector2(600, 70), 46,
                       TextAlignmentOptions.Left).color = UIKit.Gold;
        UIKit.MakeRule(panelRoot, new Vector2(0, 268), 1400);

        goldLabel = UIKit.MakeText(panelRoot, "", new Vector2(560, 310), new Vector2(400, 70), 34,
                                   TextAlignmentOptions.Right);
        goldLabel.color = UIKit.Gold;

        sayLabel = UIKit.MakeText(panelRoot, "", new Vector2(0, 214), new Vector2(1360, 50), 24);
        sayLabel.color = UIKit.Teal;
        sayLabel.fontStyle = FontStyles.Italic;

        // lima lapak berjajar
        float span = 280f;
        float originX = -(stock.Count - 1) * span * 0.5f;
        for (int i = 0; i < stock.Count; i++)
        {
            int index = i;
            BuildStall(panelRoot, stock[i], new Vector2(originX + i * span, -20f), index);
        }

        var close = UIKit.MakeButton(panelRoot, "TUTUP  (E)", new Vector2(0, -320), new Vector2(320, 64), null, 26);
        close.onClick.AddListener(Close);
    }

    void BuildStall(Transform parent, Stock s, Vector2 pos, int index)
    {
        var holder = UIKit.Rect(parent, "Stall" + index, pos, new Vector2(250, 420));

        s.frame = UIKit.MakePanel(holder, Vector2.zero, new Vector2(250, 420), UIKit.PanelSoft, true);

        UIKit.MakeSprite(holder, s.def.Icon, new Vector2(0, 132), new Vector2(112, 112));

        var name = UIKit.MakeText(holder, s.def.displayName, new Vector2(0, 46), new Vector2(230, 70), 24);
        name.color = ItemDef.RarityColor(s.def.rarity);
        name.fontStyle = FontStyles.Bold;

        UIKit.MakeText(holder, InteractText.Summary(s.def), new Vector2(0, -46), new Vector2(226, 120), 18,
                       TextAlignmentOptions.Top).color = UIKit.Parchment;

        s.priceLabel = UIKit.MakeText(holder, $"{s.price} picis", new Vector2(0, -140), new Vector2(230, 40), 24);
        s.priceLabel.color = UIKit.Gold;

        var buy = UIKit.MakeButton(holder, "BELI", new Vector2(0, -178), new Vector2(190, 56),
                                   new Color32(0x2A, 0x9D, 0x8F, 0xC0), 24);
        buy.onClick.AddListener(() => Buy(s, holder));
    }

    void Buy(Stock s, RectTransform holder)
    {
        var inv = Inventory.Instance;
        if (inv == null || s.sold) return;

        if (inv.Gold < s.price)
        {
            Sfx("ui_error", 0.8f);
            if (sayLabel != null) sayLabel.text = "Picismu kurang, Ngger.";
            UIMotion.Shake(holder, 8f, 0.25f);
            return;
        }

        inv.AddGold(-s.price);
        inv.Add(s.def);
        s.sold = true;

        Sfx("coin", 0.9f);
        UIMotion.Punch(holder, 0.14f, 0.3f);
        if (sayLabel != null) sayLabel.text = "Wis dadi duwekmu. Digunakake sing bener.";
        Refresh();
    }

    void Refresh()
    {
        var inv = Inventory.Instance;
        if (goldLabel != null) goldLabel.text = inv != null ? $"{inv.Gold} picis" : "";

        foreach (var s in stock)
        {
            if (s.frame == null) continue;
            if (s.sold)
            {
                s.frame.color = new Color(0.10f, 0.11f, 0.14f, 0.95f);
                if (s.priceLabel != null) { s.priceLabel.text = "LAKU"; s.priceLabel.color = UIKit.Muted; }
            }
            else
            {
                // lapak yang tak terbeli diredupkan supaya pilihan terbaca sekilas
                bool afford = inv != null && inv.Gold >= s.price;
                s.frame.color = afford ? UIKit.PanelSoft : new Color(0.13f, 0.13f, 0.17f, 0.95f);
                if (s.priceLabel != null) s.priceLabel.color = afford ? UIKit.Gold : UIKit.Ember;
            }
        }
    }
}
