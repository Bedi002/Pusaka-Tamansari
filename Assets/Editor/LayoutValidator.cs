using UnityEditor;
using UnityEngine;

/// <summary>
/// Menjalankan tiap algoritma denah puluhan kali dan memeriksa syarat yang
/// tidak boleh dilanggar: semua ruangan terjangkau dari ruangan awal, dan
/// ada tepat satu ruangan boss. Denah yang terputus berarti pemain terkurung
/// dan lantai tidak bisa diselesaikan -- itu tidak akan terlihat sampai
/// seseorang kebetulan mendapat seed yang buruk.
///
/// Jalankan: Tools > Pusaka > Validate Layouts
/// </summary>
public static class LayoutValidator
{
    [MenuItem("Tools/Pusaka/Validate Layouts")]
    public static void Validate()
    {
        var host = new GameObject("LayoutValidator (sementara)");
        // AddComponent di edit mode tidak memanggil Awake, jadi generator tidak
        // langsung membangun seluruh lantai saat komponennya dipasang
        var gen = host.AddComponent<DungeonGenerator>();

        bool allOk = true;
        foreach (FloorLayout style in System.Enum.GetValues(typeof(FloorLayout)))
        {
            var profile = new FloorProfile { layout = style, roomCount = 16, loopChance = 0.25f };
            string result = gen.ValidateGraph(profile);
            if (result.StartsWith("GAGAL")) { Debug.LogError("LAYOUT: " + result); allOk = false; }
            else Debug.Log("LAYOUT: " + result);
        }

        // sekalian uji tiap lantai sungguhan dengan jumlah ruangan aslinya
        for (int i = 0; i < FloorCatalog.Count; i++)
        {
            var p = FloorCatalog.Get(i);
            if (p == null) continue;
            string result = gen.ValidateGraph(p);
            if (result.StartsWith("GAGAL")) { Debug.LogError($"LAYOUT {p.title}: " + result); allOk = false; }
            else Debug.Log($"LAYOUT {p.title}: " + result);
        }

        Object.DestroyImmediate(host);
        Debug.Log(allOk ? "LAYOUT: semua algoritma denah lolos." : "LAYOUT: ADA YANG GAGAL, lihat error di atas.");
    }
}
