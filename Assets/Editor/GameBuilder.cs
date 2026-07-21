using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Build game jadi executable siap-bagi. Pastikan game sudah jalan benar
/// (Tools ▸ Pusaka ▸ BUILD ALL + playtest) sebelum build.
/// Jalankan: Tools ▸ Pusaka ▸ Build Windows EXE
/// </summary>
public static class GameBuilder
{
    static readonly string[] Scenes =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/CharacterSelect.unity",
        "Assets/Scenes/DifficultySelect.unity",
        "Assets/Scenes/Floor1.unity",
        "Assets/Scenes/Floor2.unity",
        "Assets/Scenes/Floor3.unity",
        "Assets/Scenes/Victory.unity",
        "Assets/Scenes/GameOver.unity",
    };

    [MenuItem("Tools/Pusaka/Build Windows EXE")]
    public static void BuildWindows()
    {
        // pastikan scene ada
        var missing = Scenes.Where(s => !File.Exists(s)).ToArray();
        if (missing.Length > 0)
        {
            Debug.LogError("BUILD batal: scene belum ada -> " + string.Join(", ", missing) +
                           ". Jalankan Tools ▸ Pusaka ▸ BUILD ALL dulu.");
            return;
        }

        Configure();

        string dir = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Windows");
        Directory.CreateDirectory(dir);
        string exe = Path.Combine(dir, "PusakaTamansari.exe");

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = exe,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        });

        var s = report.summary;
        if (s.result == BuildResult.Succeeded)
        {
            Debug.Log($"BUILD OK ({s.totalSize / (1024 * 1024)} MB) -> {dir}");
            EditorUtility.RevealInFinder(exe);
        }
        else
        {
            Debug.LogError($"BUILD GAGAL: {s.result}, {s.totalErrors} error. " +
                           "Cek Console: biasanya error kompilasi atau scene hilang.");
        }
    }

    static void Configure()
    {
        PlayerSettings.productName = "Pusaka Tamansari";
        PlayerSettings.companyName = "Pusaka Studio";
        EditorBuildSettings.scenes = Scenes.Select(s => new EditorBuildSettingsScene(s, true)).ToArray();
    }
}
