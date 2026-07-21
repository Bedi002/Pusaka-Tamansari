using UnityEngine;

/// <summary>
/// Memunculkan hero terpilih di titik ini saat lantai dimulai, memasang stat
/// dari HeroCatalog, dan melengkapinya dengan Inventory + PlayerStats.
/// Jalan lebih awal (-50) supaya DungeonManager menemukannya.
/// </summary>
[DefaultExecutionOrder(-50)]
public class HeroSpawner : MonoBehaviour
{
    [Tooltip("Dipakai bila katalog kosong (mis. play langsung dari scene floor)")]
    public GameObject fallbackHero;

    void Awake()
    {
        if (GameObject.FindGameObjectWithTag("Player") != null) return;   // jangan dobel

        int index = GameManager.Instance != null ? GameManager.Instance.selectedHero : 0;
        var hero = HeroCatalog.Get(index);
        var prefab = hero != null && hero.prefab != null ? hero.prefab : fallbackHero;

        if (prefab == null)
        {
            Debug.LogWarning("HeroSpawner: tak ada hero untuk di-spawn. Jalankan Tools > Pusaka > Rebuild Content.");
            return;
        }

        var go = Instantiate(prefab, transform.position, Quaternion.identity);
        go.tag = "Player";
        go.name = prefab.name;

        HeroCatalog.Apply(hero, go);

        // urutan penting: stat hero dulu, baru PlayerStats merekamnya sebagai nilai dasar
        if (go.GetComponent<Inventory>() == null) go.AddComponent<Inventory>();
        if (go.GetComponent<PlayerStats>() == null) go.AddComponent<PlayerStats>();
        if (go.GetComponent<PlayerResources>() == null) go.AddComponent<PlayerResources>();
        if (go.GetComponent<PlayerLevel>() == null) go.AddComponent<PlayerLevel>();

        // supaya hero bisa berjalan di belakang pohon dan pilar
        if (go.GetComponent<YSort>() == null) go.AddComponent<YSort>();
    }
}
