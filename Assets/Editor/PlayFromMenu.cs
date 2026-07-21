using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Memaksa tombol Play di editor selalu memulai dari MainMenu, apa pun scene
/// yang sedang terbuka. Tanpa ini, menekan Play saat membuka Floor3 memulai
/// langsung di tengah dungeon tanpa melewati menu -- bukan alur game yang wajar,
/// dan bisa memicu error karena GameManager/hero belum melewati layar pilih.
///
/// Bisa dimatikan lewat menu Tools > Pusaka > Play From Menu (toggle) kalau
/// sedang ingin menguji satu scene secara langsung.
/// </summary>
[InitializeOnLoad]
public static class PlayFromMenu
{
    const string Pref = "Pusaka.PlayFromMenu";
    const string MenuScenePath = "Assets/Scenes/MainMenu.unity";

    static PlayFromMenu() { Apply(); }

    static bool Enabled
    {
        get => EditorPrefs.GetBool(Pref, true);
        set => EditorPrefs.SetBool(Pref, value);
    }

    static void Apply()
    {
        EditorSceneManager.playModeStartScene =
            Enabled ? AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) : null;
    }

    [MenuItem("Tools/Pusaka/Play From Menu", false, 50)]
    static void Toggle() { Enabled = !Enabled; Apply(); }

    [MenuItem("Tools/Pusaka/Play From Menu", true)]
    static bool ToggleValidate() { Menu.SetChecked("Tools/Pusaka/Play From Menu", Enabled); return true; }
}
