using UnityEngine;

/// <summary>
/// Layar pemilihan kesulitan. Sambungkan tiga tombol (Easy/Normal/Hard) ke
/// SelectEasy()/SelectNormal()/SelectHard(). Memilih akan menyetel difficulty
/// di GameManager lalu memulai stage pertama.
/// Isi kanvas dianimasikan masuk dan tombol diberi UIButtonFx saat Start.
/// </summary>
public class DifficultySelectController : MonoBehaviour
{
    void Start()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            UIScreenFx.AttachButtonFx(canvas.transform);
            UIScreenFx.PlayEntrance((RectTransform)canvas.transform);
        }
    }

    public void SelectEasy() => Choose(Difficulty.Easy);
    public void SelectNormal() => Choose(Difficulty.Normal);
    public void SelectHard() => Choose(Difficulty.Hard);

    void Choose(Difficulty d)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.uiClick);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetDifficulty(d);
            GameManager.Instance.StartGame();
        }
    }
}
