using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Logika landing page / menu utama. Sambungkan PlayGame() & QuitGame() ke
/// tombol di Canvas lewat event OnClick.
/// Saat masuk, isi kanvas dianimasikan bergiliran dan semua tombol diberi
/// UIButtonFx supaya menu tidak lagi muncul mendadak sebagai kotak diam.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public string difficultyScene = "DifficultySelect";

    void Start()
    {
        Time.timeScale = 1f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(AudioManager.Instance.menuMusic);

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            UIScreenFx.AttachButtonFx(canvas.transform);
            UIScreenFx.PlayEntrance((RectTransform)canvas.transform);
        }
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
