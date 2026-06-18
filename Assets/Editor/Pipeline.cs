using UnityEditor;
using UnityEngine;

/// <summary>Jalankan seluruh pipeline art Kenney sekali jalan.</summary>
public static class Pipeline
{
    [MenuItem("Tools/Pusaka/BUILD ALL (Kenney)")]
    public static void BuildAll()
    {
        Debug.Log("PIPE: import tile Kenney...");
        KenneyImporter.Run();
        AssetDatabase.Refresh();
        Debug.Log("PIPE: build floors (karakter LPC + environment Kenney)...");
        FloorBuilder.BuildFloors();
        Debug.Log("PIPE: BUILD ALL SELESAI");
    }
}
