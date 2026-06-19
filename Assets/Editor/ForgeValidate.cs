using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Cek hasil ForgeImporter: tiap prefab punya SpriteRenderer+sprite, Animator+controller
/// dgn parameter yang dipakai script, ForgeAnimator berisi clip, dan komponen gameplay.
/// Jalankan: Tools ▸ Pusaka ▸ Validate Forge Setup
/// </summary>
public static class ForgeValidate
{
    [MenuItem("Tools/Pusaka/Validate Forge Setup")]
    public static void Run()
    {
        var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Forge" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
            .Where(g => g != null).ToList();

        if (prefabs.Count == 0) { Debug.LogWarning("VALIDATE: belum ada prefab di Assets/Prefabs/Forge. Jalankan Import Forge Characters dulu."); return; }

        int problems = 0, totalClips = 0;
        foreach (var go in prefabs)
        {
            string n = go.name;

            var sprr = go.GetComponent<SpriteRenderer>();
            if (sprr == null || sprr.sprite == null) { Debug.LogError($"VALIDATE {n}: SpriteRenderer/sprite kosong"); problems++; }

            var an = go.GetComponent<Animator>();
            var ctrl = an != null ? an.runtimeAnimatorController as AnimatorController : null;
            if (ctrl == null) { Debug.LogError($"VALIDATE {n}: tak ada Animator/Controller"); problems++; }
            else
                foreach (var p in new[] { "MoveX", "MoveY", "Speed", "Attack", "Die" })
                    if (!ctrl.parameters.Any(q => q.name == p)) { Debug.LogError($"VALIDATE {n}: kurang parameter {p}"); problems++; }

            var fa = go.GetComponent<ForgeAnimator>();
            if (fa == null || fa.clips == null || fa.clips.Length == 0) { Debug.LogError($"VALIDATE {n}: tak ada ForgeAnimator/clips"); problems++; }
            else
            {
                totalClips += fa.clips.Length;
                if (!fa.clips.Any(c => c.key == "idle_down")) Debug.LogWarning($"VALIDATE {n}: tak ada clip idle_down");
                foreach (var c in fa.clips) if (c.frames == null || c.frames.Length == 0) { Debug.LogError($"VALIDATE {n}: clip {c.key} kosong"); problems++; }
            }

            bool hasGameplay = go.GetComponent<PlayerMovement>() || go.GetComponent<Enemy>() || go.GetComponent<Boss>();
            if (!hasGameplay) { Debug.LogError($"VALIDATE {n}: tak ada komponen gameplay (Player/Enemy/Boss)"); problems++; }
        }

        Debug.Log($"VALIDATE: {prefabs.Count} prefab, {totalClips} clip arah, {problems} masalah. " +
                  (problems == 0 ? "SEMUA OK" : "ADA MASALAH - lihat error di atas."));
    }
}
