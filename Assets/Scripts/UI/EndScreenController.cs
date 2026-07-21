using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controller untuk scene Victory & GameOver. Menampilkan skor akhir dan
/// menyediakan tombol Retry (ulang dari stage 1, difficulty tetap) & Main Menu.
/// Sambungkan tombol ke Retry()/MainMenu().
/// Skor akhir berjalan naik angka demi angka setelah layar selesai dirakit.
/// </summary>
public class EndScreenController : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public string scorePrefix = "Skor Akhir: ";
    [Tooltip("Musik untuk layar ini (opsional)")]
    public AudioClip screenMusic;

    void Start()
    {
        Time.timeScale = 1f;
        if (scoreText != null && GameManager.Instance != null)
            UIMotion.CountTo(scoreText, 0, GameManager.Instance.score, 0.9f, scorePrefix, "", 0.5f);
        if (screenMusic != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(screenMusic);

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            UIScreenFx.AttachButtonFx(canvas.transform);
            UIScreenFx.PlayEntrance((RectTransform)canvas.transform);
        }
    }

    public void Retry()
    {
        Click();
        if (GameManager.Instance != null) GameManager.Instance.RetryFromStart();
    }

    public void MainMenu()
    {
        Click();
        if (GameManager.Instance != null) GameManager.Instance.ReturnToMenu();
        else SceneManager.LoadScene("MainMenu");
    }

    void Click()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.uiClick);
    }
}
