using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Layar pilih hero. Tiap tombol memanggil SelectWarrior/Archer/Mage yang menyimpan
/// pilihan ke GameManager.selectedHero lalu lanjut ke DifficultySelect.
/// </summary>
public class CharacterSelectController : MonoBehaviour
{
    public string nextScene = "DifficultySelect";

    public void SelectWarrior() => Choose(0);
    public void SelectArcher() => Choose(1);
    public void SelectMage() => Choose(2);

    void Choose(int index)
    {
        if (GameManager.Instance != null) GameManager.Instance.selectedHero = index;
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.uiClick);
        SceneManager.LoadScene(nextScene);
    }
}
