using UnityEngine;

/// <summary>
/// Memunculkan hero terpilih (GameManager.SelectedHeroPrefab) di titik ini saat
/// lantai dimulai, lalu memberi tag "Player". Dipasang oleh FloorBuilder di tiap
/// floor. Jalan lebih awal (execution order) supaya DungeonManager menemukannya.
/// </summary>
[DefaultExecutionOrder(-50)]
public class HeroSpawner : MonoBehaviour
{
    [Tooltip("Dipakai bila tak ada GameManager/hero terpilih (mis. play langsung dari floor)")]
    public GameObject fallbackHero;

    void Awake()
    {
        // jangan dobel kalau sudah ada Player di scene
        if (GameObject.FindGameObjectWithTag("Player") != null) return;

        GameObject prefab = GameManager.Instance != null ? GameManager.Instance.SelectedHeroPrefab : null;
        if (prefab == null) prefab = fallbackHero;
        if (prefab == null) { Debug.LogWarning("HeroSpawner: tak ada hero prefab untuk di-spawn."); return; }

        var hero = Instantiate(prefab, transform.position, Quaternion.identity);
        hero.tag = "Player";
        hero.name = prefab.name;
    }
}
