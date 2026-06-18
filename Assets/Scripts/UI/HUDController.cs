using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Mengelola tampilan HUD dalam stage: HP player, info stage/wave, skor,
/// bar boss, dan pesan tengah layar. Semua referensi bersifat opsional
/// (null-safe), jadi HUD tetap jalan walau sebagian slot belum diisi.
/// </summary>
public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("HP Pemain")]
    public Slider healthSlider;
    public TextMeshProUGUI healthText;

    [Header("Info Stage")]
    public TextMeshProUGUI stageText;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI scoreText;

    [Header("Boss (root disembunyikan saat tidak ada boss)")]
    public GameObject bossBarRoot;
    public Slider bossSlider;
    public TextMeshProUGUI bossNameText;

    [Header("Pesan Tengah Layar")]
    public TextMeshProUGUI centerMessage;

    Coroutine msgCo;

    void Awake()
    {
        Instance = this;
        if (bossBarRoot != null) bossBarRoot.SetActive(false);
        if (centerMessage != null) centerMessage.text = "";
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetHealth(int current, int max)
    {
        current = Mathf.Max(0, current);
        if (healthSlider != null) { healthSlider.maxValue = max; healthSlider.value = current; }
        if (healthText != null) healthText.text = $"HP: {current}/{max}";
    }

    public void SetStage(int stageNumber, int totalStages)
    {
        if (stageText != null) stageText.text = $"Stage {stageNumber}/{totalStages}";
    }

    public void SetWave(int wave, int totalWaves)
    {
        if (waveText != null) waveText.text = $"Wave {wave}/{totalWaves}";
    }

    public void SetScore(int score)
    {
        if (scoreText != null) scoreText.text = $"Skor: {score}";
    }

    public void ShowBossBar(string bossName, int max)
    {
        if (bossBarRoot != null) bossBarRoot.SetActive(true);
        if (bossSlider != null) { bossSlider.maxValue = max; bossSlider.value = max; }
        if (bossNameText != null) bossNameText.text = bossName;
    }

    public void UpdateBossBar(int current)
    {
        if (bossSlider != null) bossSlider.value = Mathf.Max(0, current);
    }

    public void HideBossBar()
    {
        if (bossBarRoot != null) bossBarRoot.SetActive(false);
    }

    public void ShowMessage(string msg, float seconds = 2f)
    {
        if (centerMessage == null) return;
        if (msgCo != null) StopCoroutine(msgCo);
        msgCo = StartCoroutine(MessageRoutine(msg, seconds));
    }

    IEnumerator MessageRoutine(string msg, float seconds)
    {
        centerMessage.text = msg;
        yield return new WaitForSecondsRealtime(seconds);
        centerMessage.text = "";
        msgCo = null;
    }
}
