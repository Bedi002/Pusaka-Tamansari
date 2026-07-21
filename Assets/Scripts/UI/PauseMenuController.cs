using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu pause. Tekan Esc untuk pause/resume (membekukan game via Time.timeScale).
/// Sambungkan tombol ke Resume()/RestartStage()/QuitToMenu().
/// Panel dirakit masuk tiap kali pause (animasi unscaled, jadi tetap jalan
/// saat timeScale 0) dan memudar keluar saat resume; toggle cepat aman karena
/// fade-in membatalkan fade-out yang sedang berjalan beserta callback-nya.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    public GameObject pausePanel;
    public KeyCode pauseKey = KeyCode.Escape;
    bool paused = false;
    bool fxAttached = false;

    void Start()
    {
        paused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(pauseKey)) Toggle();
    }

    public void Toggle()
    {
        if (paused) Resume();
        else Pause();
    }

    public void Pause()
    {
        paused = true;
        Time.timeScale = 0f;
        if (pausePanel == null) return;

        pausePanel.SetActive(true);
        var rt = pausePanel.transform as RectTransform;
        if (!fxAttached) { UIScreenFx.AttachButtonFx(pausePanel.transform); fxAttached = true; }
        UIMotion.FadeIn(UIScreenFx.Group(rt), 0.12f);
        UIScreenFx.PlayEntrance(rt);
    }

    public void Resume()
    {
        paused = false;
        Time.timeScale = 1f;
        if (pausePanel == null) return;

        var cg = UIScreenFx.Group(pausePanel.transform as RectTransform);
        if (cg != null)
            UIMotion.FadeOut(cg, 0.1f, 0f, () =>
            {
                if (pausePanel != null) { pausePanel.SetActive(false); cg.alpha = 1f; }
            });
        else
            pausePanel.SetActive(false);
    }

    public void RestartStage()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null) GameManager.Instance.ReturnToMenu();
        else SceneManager.LoadScene("MainMenu");
    }
}
