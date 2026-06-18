using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Alat diagnosa & perbaikan aset (sprite import, material URP, binding animasi).
/// Diagnose: hanya melaporkan (read-only). Fix: perbaiki import + material.
/// Jalankan headless: -executeMethod AssetDoctor.Diagnose  /  AssetDoctor.FixImports
/// </summary>
public static class AssetDoctor
{
    [MenuItem("Tools/Pusaka/Asset Doctor - Diagnose")]
    public static void Diagnose()
    {
        Debug.Log("ASSETDOC: === MULAI DIAGNOSA ===");
        DiagnoseTextures();
        DiagnoseClips();
        DiagnoseRenderers();
        DiagnoseControllers();
        Debug.Log("ASSETDOC: === SELESAI DIAGNOSA ===");
    }

    static void DiagnoseTextures()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites" });
        Debug.Log($"ASSETDOC: {guids.Length} tekstur di Assets/Sprites");
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            int spriteCount = 0;
            var subs = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
            foreach (var s in subs) if (s is Sprite) spriteCount++;
            Debug.Log($"ASSETDOC: TEX '{System.IO.Path.GetFileName(path)}' mode={ti.spriteImportMode} filter={ti.filterMode} ppu={ti.spritePixelsPerUnit} sprites={spriteCount}");
        }
    }

    static void DiagnoseClips()
    {
        var guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Sprites" });
        Debug.Log($"ASSETDOC: {guids.Length} AnimationClip");
        int brokenClips = 0;
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) continue;
            var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            int keys = 0, missing = 0;
            foreach (var b in bindings)
            {
                var frames = AnimationUtility.GetObjectReferenceCurve(clip, b);
                foreach (var fr in frames) { keys++; if (fr.value == null) missing++; }
            }
            if (missing > 0)
            {
                brokenClips++;
                Debug.LogWarning($"ASSETDOC: CLIP RUSAK '{System.IO.Path.GetFileName(path)}' frame={keys} hilang={missing}");
            }
            else if (keys == 0)
            {
                Debug.LogWarning($"ASSETDOC: CLIP KOSONG '{System.IO.Path.GetFileName(path)}' (tak ada keyframe sprite)");
            }
        }
        Debug.Log($"ASSETDOC: total clip rusak = {brokenClips}");
    }

    static void DiagnoseRenderers()
    {
        string[] prefabs = { "Assets/Prefabs/Enemy.prefab", "Assets/Prefabs/Boss.prefab" };
        foreach (var p in prefabs)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go == null) { Debug.Log($"ASSETDOC: (lewati, tak ada) {p}"); continue; }
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string mat = sr.sharedMaterial != null ? sr.sharedMaterial.name : "NULL";
                string shader = sr.sharedMaterial != null && sr.sharedMaterial.shader != null ? sr.sharedMaterial.shader.name : "-";
                string spr = sr.sprite != null ? sr.sprite.name : "NULL";
                Debug.Log($"ASSETDOC: PREFAB '{go.name}' SR sprite={spr} material={mat} shader={shader}");
            }
        }
    }

    static void DiagnoseControllers()
    {
        var guids = AssetDatabase.FindAssets("t:AnimatorController", new[] { "Assets/Sprites" });
        Debug.Log($"ASSETDOC: {guids.Length} AnimatorController");
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ac == null) continue;
            var pars = new List<string>();
            foreach (var pr in ac.parameters) pars.Add(pr.name + ":" + pr.type);
            int layers = ac.layers.Length;
            int states = layers > 0 ? ac.layers[0].stateMachine.states.Length : 0;
            string def = layers > 0 && ac.layers[0].stateMachine.defaultState != null ? ac.layers[0].stateMachine.defaultState.name : "NONE";
            Debug.Log($"ASSETDOC: CTRL '{System.IO.Path.GetFileName(path)}' params=[{string.Join(",", pars)}] states={states} default={def}");
        }
    }

    // ---------------------------------------------------------------- FIX
    [MenuItem("Tools/Pusaka/Asset Doctor - Fix Imports")]
    public static void FixImports()
    {
        Debug.Log("ASSETDOC: === MULAI FIX ===");

        // 1) Semua sprite di Assets/Sprites -> filter Point (pixel tajam). TIDAK re-slice (binding aman).
        int fixedTex = 0;
        foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            bool changed = false;
            if (ti.filterMode != FilterMode.Point) { ti.filterMode = FilterMode.Point; changed = true; }
            if (ti.textureCompression != TextureImporterCompression.Uncompressed)
            { ti.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (changed) { ti.SaveAndReimport(); fixedTex++; }
        }
        Debug.Log($"ASSETDOC: tekstur diperbaiki = {fixedTex}");

        // 2) Sprite default Enemy/Boss -> frame idle asli (hilangkan placeholder 'Square').
        var idle = FirstSpriteOfClip("Assets/Sprites/Enemy 1/idle.anim");
        Debug.Log("ASSETDOC: enemy idle sprite = " + (idle != null ? idle.name : "NULL"));
        SetPrefabDefaultSprite("Assets/Prefabs/Enemy.prefab", idle);
        SetPrefabDefaultSprite("Assets/Prefabs/Boss.prefab", idle);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("ASSETDOC: === SELESAI FIX ===");
    }

    static Sprite FirstSpriteOfClip(string clipPath)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null) return null;
        foreach (var b in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            var frames = AnimationUtility.GetObjectReferenceCurve(clip, b);
            if (frames != null && frames.Length > 0 && frames[0].value is Sprite s) return s;
        }
        return null;
    }

    static void SetPrefabDefaultSprite(string prefabPath, Sprite spr)
    {
        if (spr == null) return;
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null) return;
        var sr = root.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null) { sr.sprite = spr; Debug.Log($"ASSETDOC: set default sprite '{spr.name}' di {System.IO.Path.GetFileName(prefabPath)}"); }
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }
}
