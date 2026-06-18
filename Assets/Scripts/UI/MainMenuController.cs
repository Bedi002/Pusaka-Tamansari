using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Logika landing page / menu utama. Sambungkan PlayGame() & QuitGame() ke
/// tombol di Canvas lewat event OnClick.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public string difficultyScene = "DifficultySelect";

    void Start()
    {
        Time.timeScale = 1f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(AudioManager.Instance.menuMusic);
    }

    public void PlayGame()
    {
        Click();
        SceneManager.LoadScene(difficultyScene);
    }

    public void QuitGame()
    {
        Click();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void Click()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.uiClick);
    }
}
