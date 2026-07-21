using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Mengelola tampilan HUD dalam stage: HP player, info stage/wave, skor,
/// bar boss, dan pesan tengah layar. Semua referensi bersifat opsional
/// (null-safe), jadi HUD tetap jalan walau sebagian slot belum diisi.
///
/// Semua perubahan nilai dianimasikan lewat UIMotion: bar HP meluncur (bukan
/// lompat), kena pukul berkilat putih dan bergetar saat sekarat, skor berjalan
/// angka demi angka, dan kartu pembuka lantai mengetik lore-nya sendiri.
/// Signature publik dipertahankan karena dipanggil kode non-UI.
/// </summary>
public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("HP Pemain")]
    public Slider healthSlider;
    public TextMeshProUGUI healthText;

    [Header("Aji & Tenaga")]
    public Slider manaSlider;
    public TextMeshProUGUI manaText;
    public Slider staminaSlider;
    public TextMeshProUGUI staminaText;

    [Header("Info Stage")]
    public TextMeshProUGUI stageText;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI levelText;

    [Header("Boss (root disembunyikan saat tidak ada boss)")]
    public GameObject bossBarRoot;
    public Slider bossSlider;
    public TextMeshProUGUI bossNameText;

    [Header("Pesan Tengah Layar")]
    public TextMeshProUGUI centerMessage;
    public TextMeshProUGUI subMessage;   // baris kecil di bawah (lore lantai)

    const float LowHpFrac = 0.3f;

    Coroutine msgCo;
    Image hpFill;
    Color hpFillBase;
    bool hpFillCached, hpInit, lowPulse;
    int lastHp = -1, scoreFrom, lastStage = -1, lastWave = -1;
    Vector2 centerBasePos, bossBasePos;
    CanvasGroup bossCg;

    void Awake()
    {
        Instance = this;
        if (bossBarRoot != null)
        {
            bossBarRoot.SetActive(false);
            if (bossBarRoot.transform is RectTransform brt) bossBasePos = brt.anchoredPosition;
        }
        if (centerMessage != null)
        {
            centerMessage.text = "";
            centerBasePos = centerMessage.rectTransform.anchoredPosition;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Aji dan Tenaga berubah tiap frame, jadi bar-nya di-set langsung tanpa
    /// tween. Menween nilai yang sudah kontinu justru membuatnya terasa lamban.
    /// </summary>
    public void SetMana(float current, float max)
    {
        if (manaSlider != null) { manaSlider.maxValue = max; manaSlider.value = current; }
        if (manaText != null) manaText.text = $"AJI {Mathf.RoundToInt(current)}/{Mathf.RoundToInt(max)}";
    }

    public void SetStamina(float current, float max)
    {
        if (staminaSlider != null) { staminaSlider.maxValue = max; staminaSlider.value = current; }
        if (staminaText != null) staminaText.text = $"TENAGA {Mathf.RoundToInt(current)}/{Mathf.RoundToInt(max)}";
    }

    /// <summary>Level pemain + kemajuan XP menuju level berikutnya.</summary>
    public void SetLevel(int level, int xpInto, int xpNext)
    {
        if (levelText == null) return;
        levelText.text = xpNext > 0 ? $"Level {level}   {xpInto}/{xpNext} XP" : $"Level {level}";
    }

    public void SetHealth(int current, int max)
    {
        current = Mathf.Max(0, current);
        if (healthText != null) healthText.text = $"HP: {current}/{max}";

        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            CacheFill();

            if (!hpInit)
            {
                // panggilan pertama langsung snap; tween dari nilai default hanya bikin bar "lahir" aneh
                healthSlider.value = current;
                hpInit = true;
            }
            else
            {
                UIMotion.SliderTo(healthSlider, current, 0.3f);

                bool damaged = current < lastHp;
                float frac = max > 0 ? (float)current / max : 0f;
                var barRt = healthSlider.transform as RectTransform;

                if (damaged && hpFill != null) UIMotion.Flash(hpFill, Color.white, hpFillBase, 0.25f);
                if (damaged && frac <= LowHpFrac && barRt != null) UIMotion.Shake(barRt, 5f, 0.3f);

                // denyut pelan selama sekarat; berhenti begitu pulih (atau mati)
                if (barRt != null)
                {
                    bool wantPulse = frac <= LowHpFrac && frac > 0f;
                    if (wantPulse && !lowPulse) UIMotion.Pulse(barRt, 0.03f, 0.8f);
                    else if (!wantPulse && lowPulse) UIMotion.StopPulse(barRt);
                    lowPulse = wantPulse;
                }
            }
        }

        lastHp = current;
    }

    void CacheFill()
    {
        if (hpFillCached || healthSlider == null || healthSlider.fillRect == null) return;
        hpFill = healthSlider.fillRect.GetComponent<Image>();
        if (hpFill != null) { hpFillBase = hpFill.color; hpFillCached = true; }
    }

    public void SetStage(int stageNumber, int totalStages)
    {
        if (stageText == null) return;
        stageText.text = $"Stage {stageNumber}/{totalStages}";
        if (stageNumber != lastStage && lastStage >= 0) UIMotion.Punch(stageText.rectTransform, 0.12f, 0.25f);
        lastStage = stageNumber;
    }

    /// <summary>
    /// Kemajuan menjelajah lantai. Menggantikan penghitung "Wave" peninggalan
    /// desain lama: di roguelike tidak ada gelombang, dan labelnya tampil kosong
    /// sepanjang permainan.
    /// </summary>
    public void SetRooms(int cleared, int total)
    {
        if (waveText == null) return;
        waveText.text = $"Ruangan {cleared}/{total}";
        if (cleared != lastWave && lastWave >= 0) UIMotion.Punch(waveText.rectTransform, 0.12f, 0.25f);
        lastWave = cleared;
    }

    /// <summary>Dipertahankan untuk StageManager (mode stage lama).</summary>
    public void SetWave(int wave, int totalWaves)
    {
        if (waveText == null) return;
        waveText.text = $"Wave {wave}/{totalWaves}";
        if (wave != lastWave && lastWave >= 0) UIMotion.Punch(waveText.rectTransform, 0.12f, 0.25f);
        lastWave = wave;
    }

    public void SetScore(int score)
    {
        if (scoreText == null) return;
        // hitung berjalan dari target sebelumnya: monoton, tak pernah mundur
        UIMotion.CountTo(scoreText, scoreFrom, score, 0.4f, "Skor: ");
        scoreFrom = score;
    }

    public void ShowBossBar(string bossName, int max)
    {
        if (bossBarRoot != null)
        {
            bossBarRoot.SetActive(true);
            var brt = bossBarRoot.transform as RectTransform;
            if (bossCg == null && brt != null) bossCg = UIScreenFx.Group(brt);
            UIMotion.Kill(bossBarRoot);
            if (brt != null)
            {
                brt.anchoredPosition = bossBasePos;   // reset dulu supaya slide tak menggeser permanen
                UIMotion.SlideFrom(brt, Vector2.up, 26f, 0.4f);
            }
            UIMotion.FadeIn(bossCg, 0.3f);
        }
        if (bossSlider != null) { bossSlider.maxValue = max; bossSlider.value = max; }
        if (bossNameText != null) bossNameText.text = bossName;
    }

    public void UpdateBossBar(int current)
    {
        if (bossSlider != null) UIMotion.SliderTo(bossSlider, Mathf.Max(0, current), 0.25f);
    }

    public void HideBossBar()
    {
        if (bossBarRoot == null || !bossBarRoot.activeSelf) return;
        if (bossCg == null && bossBarRoot.transform is RectTransform brt) bossCg = UIScreenFx.Group(brt);
        if (bossCg != null)
            UIMotion.FadeOut(bossCg, 0.25f, 0f, () => { if (bossBarRoot != null) bossBarRoot.SetActive(false); });
        else
            bossBarRoot.SetActive(false);
    }

    public void ShowMessage(string msg, float seconds = 2f)
    {
        if (centerMessage == null) return;
        if (msgCo != null) StopCoroutine(msgCo);
        msgCo = StartCoroutine(MessageRoutine(msg, seconds));
    }

    IEnumerator MessageRoutine(string msg, float seconds)
    {
        centerMessage.rectTransform.anchoredPosition = centerBasePos;
        centerMessage.maxVisibleCharacters = int.MaxValue;
        centerMessage.text = msg;
        UIMotion.FadeIn(centerMessage, 0.15f);
        UIMotion.SlideFrom(centerMessage.rectTransform, Vector2.up, 14f, 0.25f);
        yield return new WaitForSecondsRealtime(seconds);
        UIMotion.FadeOut(centerMessage, 0.2f);
        yield return new WaitForSecondsRealtime(0.2f);
        centerMessage.text = "";
        msgCo = null;
    }

    /// <summary>Kartu pembuka lantai: judul turun perlahan, lore diketik per huruf, lalu memudar.</summary>
    public void ShowFloorIntro(string title, string lore, float seconds = 3.5f)
    {
        if (centerMessage == null) return;
        if (msgCo != null) StopCoroutine(msgCo);
        msgCo = StartCoroutine(FloorIntroRoutine(title, lore, seconds));
    }

    IEnumerator FloorIntroRoutine(string title, string lore, float seconds)
    {
        centerMessage.rectTransform.anchoredPosition = centerBasePos;
        centerMessage.maxVisibleCharacters = int.MaxValue;
        centerMessage.text = title;
        UIMotion.FadeIn(centerMessage, 0.35f);
        UIMotion.SlideFrom(centerMessage.rectTransform, Vector2.up, 30f, 0.5f);

        if (subMessage != null)
        {
            var c = subMessage.color; c.a = 1f; subMessage.color = c;   // pulih dari fade-out intro sebelumnya
            UIMotion.Typewriter(subMessage, lore, 45f, 0.35f);
        }

        yield return new WaitForSecondsRealtime(seconds);

        UIMotion.FadeOut(centerMessage, 0.3f);
        if (subMessage != null) UIMotion.FadeOut(subMessage, 0.3f);
        yield return new WaitForSecondsRealtime(0.3f);

        centerMessage.text = "";
        if (subMessage != null)
        {
            subMessage.text = "";
            subMessage.maxVisibleCharacters = int.MaxValue;   // jangan biarkan batas ketikan menular ke teks lain
        }
        msgCo = null;
    }
}
