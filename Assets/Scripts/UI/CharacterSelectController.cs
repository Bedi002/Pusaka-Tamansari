using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Layar pilih hero. Kartu dirakit saat runtime dari HeroCatalog, bukan dari
/// objek yang dipahat di scene.
///
/// Bug lama "cuma satu hero yang bisa dipilih" berakar di sini: kartu dan tombol
/// dipasang manual, sehingga panel hiasan layar penuh menutupi raycast sebagian
/// tombol dan pilihan selalu jatuh ke hero pertama. Sekarang jumlah kartu selalu
/// mengikuti isi katalog dan tiap tombol membawa index-nya sendiri lewat closure.
///
/// Bingkai emas kini anak dari holder kartu (bukan saudara terpisah) supaya
/// seluruh kartu bisa diangkat dan dianimasikan sebagai satu benda.
/// </summary>
public class CharacterSelectController : MonoBehaviour
{
    public string nextScene = "DifficultySelect";
    public string backScene = "MainMenu";

    int hovered = -1;
    TextMeshProUGUI statLine;

    void Start()
    {
        var canvas = UIKit.MakeCanvas("CharacterSelect Canvas");

        UIKit.MakePanel(canvas.transform, Vector2.zero, new Vector2(4000, 4000), UIKit.Bg);

        var title = UIKit.MakeText(canvas.transform, "PILIH SATRIA", new Vector2(0, 430), new Vector2(1400, 110), 76);
        title.color = UIKit.Gold;
        var rule = UIKit.MakeRule(canvas.transform, new Vector2(0, 368), 760);
        var sub = UIKit.MakeText(canvas.transform, "Tiga jalan menuju pusaka. Pilih yang paling kau percayai.",
                                 new Vector2(0, 322), new Vector2(1200, 60), 30);
        sub.color = UIKit.Muted;

        // koreografi masuk: judul turun, garis mengembang dari tengah, kartu menyusul
        UIMotion.FadeIn(title, 0.3f);
        UIMotion.SlideFrom(title.rectTransform, Vector2.up, 50f, 0.5f);
        UIMotion.WipeWidth(rule.rectTransform, 760f, 0.4f, 0.1f);
        UIMotion.FadeIn(sub, 0.3f, 0.16f);
        UIMotion.SlideFrom(sub.rectTransform, Vector2.up, 14f, 0.3f, 0.16f);

        int count = HeroCatalog.Count;
        if (count == 0)
        {
            UIKit.MakeText(canvas.transform, "HeroCatalog kosong.\nJalankan Tools > Pusaka > Rebuild Content.",
                           Vector2.zero, new Vector2(1200, 200), 34).color = UIKit.Ember;
            return;
        }

        float spacing = 520f;
        float originX = -(count - 1) * spacing * 0.5f;

        for (int i = 0; i < count; i++)
        {
            int index = i;                                  // salin: closure harus memegang nilainya sendiri
            var hero = HeroCatalog.Get(index);
            BuildCard(canvas.transform, hero, index, new Vector2(originX + i * spacing, 10f),
                      0.18f + i * 0.06f);
        }

        statLine = UIKit.MakeText(canvas.transform, "", new Vector2(0, -400), new Vector2(1400, 60), 28);
        statLine.color = UIKit.Teal;

        var back = UIKit.MakeButton(canvas.transform, "KEMBALI", new Vector2(0, -480), new Vector2(300, 70), null, 26);
        back.onClick.AddListener(() => SceneManager.LoadScene(backScene));

        // baris info dan tombol kembali menutup koreografi, muncul paling akhir
        UIMotion.FadeIn(UIScreenFx.Group((RectTransform)back.transform), 0.3f, 0.45f);
        UIMotion.SlideFrom((RectTransform)back.transform, Vector2.down, 20f, 0.3f, 0.45f);
    }

    void BuildCard(Transform parent, HeroProfile hero, int index, Vector2 pos, float delay)
    {
        if (hero == null) return;

        var holder = UIKit.Rect(parent, hero.displayName + " Card", pos, new Vector2(472, 632));

        // bingkai emas tipis digambar dulu supaya berada di belakang badan kartu
        var frame = UIKit.MakePanel(holder, Vector2.zero, new Vector2(472, 632), UIKit.Gold * 0.5f);
        var bg = UIKit.MakePanel(holder, Vector2.zero, new Vector2(460, 620), UIKit.Panel, true);

        UIKit.MakeSprite(holder, ArtLibrary.D(hero.portraitTile), new Vector2(0, 168), new Vector2(200, 200));

        UIKit.MakeText(holder, hero.displayName, new Vector2(0, 34), new Vector2(420, 60), 44)
             .color = UIKit.Gold;
        UIKit.MakeText(holder, hero.role, new Vector2(0, -12), new Vector2(420, 40), 24)
             .color = UIKit.Teal;
        UIKit.MakeText(holder, hero.blurb, new Vector2(0, -100), new Vector2(400, 150), 22)
             .color = UIKit.Parchment;

        UIKit.MakeText(holder,
            $"HP {hero.maxHp}   SERANG {hero.damage}   LAJU {hero.moveSpeed:0.#}",
            new Vector2(0, -206), new Vector2(420, 40), 20).color = UIKit.Muted;

        var pick = UIKit.MakeButton(holder, "PILIH", new Vector2(0, -262), new Vector2(300, 74),
                                    new Color32(0x2A, 0x9D, 0x8F, 0xC0));
        pick.onClick.AddListener(() => Choose(index));

        UIMotion.PopIn(holder, delay, 0.3f);

        // kartu terangkat + bingkai menyala saat kursor di atasnya (event enter merambat dari anak)
        var hover = holder.gameObject.AddComponent<HoverRelay>();
        hover.onEnter = () =>
        {
            hovered = index;
            UIMotion.MoveTo(holder, pos + new Vector2(0f, 8f), 0.15f);
            UIMotion.ScaleTo(holder, 1.02f, 0.15f);
            UIMotion.ColorTo(bg, UIKit.PanelSoft, 0.15f);
            UIMotion.ColorTo(frame, UIKit.Gold * 0.85f, 0.15f);
            RefreshStats();
        };
        hover.onExit = () =>
        {
            if (hovered == index) hovered = -1;
            UIMotion.MoveTo(holder, pos, 0.18f);
            UIMotion.ScaleTo(holder, 1f, 0.18f);
            UIMotion.ColorTo(bg, UIKit.Panel, 0.18f);
            UIMotion.ColorTo(frame, UIKit.Gold * 0.5f, 0.18f);
        };
    }

    void RefreshStats()
    {
        var hero = HeroCatalog.Get(hovered);
        if (hero == null || statLine == null) return;
        var item = ItemDatabase.Get(hero.startingItemId);
        statLine.text = item != null ? $"Bekal awal: {item.displayName}" : "";
        UIMotion.FadeIn(statLine, 0.2f);
    }

    public void Choose(int index)
    {
        if (GameManager.Instance != null) GameManager.Instance.selectedHero = index;
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.uiClick);
        SceneManager.LoadScene(nextScene);
    }

    // dipertahankan supaya tombol lama yang masih terhubung lewat UnityEvent tetap jalan
    public void SelectWarrior() => Choose(0);
    public void SelectArcher() => Choose(1);
    public void SelectMage() => Choose(2);
}

/// <summary>Meneruskan event pointer ke delegate biasa.</summary>
public class HoverRelay : MonoBehaviour,
    UnityEngine.EventSystems.IPointerEnterHandler,
    UnityEngine.EventSystems.IPointerExitHandler
{
    public System.Action onEnter, onExit;
    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e) => onEnter?.Invoke();
    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e) => onExit?.Invoke();
}
