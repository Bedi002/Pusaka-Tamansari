using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu pause. Tekan Esc untuk pause/resume (membekukan game via Time.timeScale).
/// Sambungkan tombol ke Resume()/RestartStage()/QuitToMenu().
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    public GameObject pausePanel;
    public KeyCode pauseKey = KeyCode.Escape;
    bool paused = false;

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
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void Resume()
    {
        paused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
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
