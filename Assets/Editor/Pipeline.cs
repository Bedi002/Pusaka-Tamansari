using UnityEditor;
using UnityEngine;

/// <summary>Jalankan seluruh pipeline art Kenney sekali jalan.</summary>
public static class Pipeline
{
    [MenuItem("Tools/Pusaka/BUILD ALL")]
    public static void BuildAll()
    {
        Debug.Log("PIPE: 1/4 import tile Kenney...");
        KenneyImporter.Run();
        AssetDatabase.Refresh();

        Debug.Log("PIPE: 2/4 import karakter Forge (slice + animasi + prefab)...");
        ForgeImporter.Import();

        Debug.Log("PIPE: 3/4 build floors dungeon (hero spawn + musuh Forge)...");
        FloorBuilder.BuildFloors();

        Debug.Log("PIPE: 4/4 build menu (MainMenu/CharacterSelect/Difficulty/Victory/GameOver)...");
        PusakaSceneBuilder.BuildMenus();

        Debug.Log("PIPE: BUILD ALL SELESAI");
    }
}
