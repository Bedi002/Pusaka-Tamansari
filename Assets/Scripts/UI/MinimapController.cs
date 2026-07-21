using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimap: menggambar tiap Room sebagai node kotak pada panel UI, diwarnai
/// menurut tipe & status (current/cleared/unvisited/boss/treasure). Posisi node
/// memakai Room.gridPos. Dibangun oleh DungeonManager saat lantai dimulai.
/// </summary>
public class MinimapController : MonoBehaviour
{
    public static MinimapController Instance { get; private set; }

    [Tooltip("Panel (RectTransform) tempat node digambar")]
    public RectTransform mapRoot;
    public float cell = 26f;
    public float spacing = 4f;

    [Header("Warna (tema Pusaka Tamansari)")]
    public Color colCurrent = new Color(0.95f, 0.82f, 0.30f);
    public Color colCleared = new Color(0.55f, 0.45f, 0.22f);
    public Color colUnvisited = new Color(0.20f, 0.20f, 0.28f);
    public Color colBoss = new Color(0.85f, 0.35f, 0.25f);
    public Color colTreasure = new Color(0.20f, 0.70f, 0.60f);

    readonly Dictionary<Room, Image> nodes = new Dictionary<Room, Image>();
    Room current;

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    public void Build(Room[] rooms, Room start)
    {
        if (mapRoot == null || rooms == null) return;
        for (int i = mapRoot.childCount - 1; i >= 0; i--) Destroy(mapRoot.GetChild(i).gameObject);
        nodes.Clear();

        int order = 0;
        foreach (var r in rooms)
        {
            var go = new GameObject("Node_" + r.name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(mapRoot, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(cell, cell);
            rt.anchoredPosition = new Vector2(r.gridPos.x * (cell + spacing), r.gridPos.y * (cell + spacing));
            nodes[r] = go.GetComponent<Image>();
            Paint(r);
            // peta ikut "dirakit": node muncul beruntun, dibatasi agar lantai besar tidak lelet
            UIMotion.PopIn(rt, Mathf.Min(0.3f, order * 0.015f), 0.2f);
            order++;
        }
    }

    void Paint(Room r)
    {
        if (r == null || !nodes.TryGetValue(r, out var img) || img == null) return;
        Color c;
        if (r == current) c = colCurrent;
        else if (r.type == RoomType.Boss) c = colBoss;
        else if (r.type == RoomType.Treasure) c = colTreasure;
        else if (r.IsCleared) c = colCleared;
        else c = colUnvisited;
        img.color = c;
    }

    public void SetCurrent(Room r)
    {
        var prev = current;
        current = r;
        if (prev != null)
        {
            Paint(prev);
            if (nodes.TryGetValue(prev, out var pi) && pi != null)
                UIMotion.StopPulse((RectTransform)pi.transform);
        }
        Paint(r);
        // ruangan aktif berdenyut supaya mata langsung menemukannya
        if (r != null && nodes.TryGetValue(r, out var img) && img != null)
            UIMotion.Pulse((RectTransform)img.transform, 0.18f, 1.1f);
    }

    public void MarkCleared(Room r)
    {
        Paint(r);
        if (r != null && nodes.TryGetValue(r, out var img) && img != null)
            UIMotion.Flash(img, Color.white, 0.3f);
    }
}
