using UnityEngine;

/// <summary>
/// Item yang tergeletak di lantai. Melayang naik-turun supaya mudah terlihat,
/// dan masuk ke tas begitu pemain menyentuhnya.
/// </summary>
public class ItemPickup : MonoBehaviour
{
    public string itemId;
    [Tooltip("Untuk tumpukan uang: jumlah koin, bukan item")]
    public int goldAmount = 0;

    float phase;
    Vector3 origin;

    [Tooltip("Jarak barang mulai tersedot ke arah pemain")]
    public float magnetRange = 3.2f;

    Transform player;
    bool flying;

    /// <summary>Buat pickup item di dunia.</summary>
    public static ItemPickup Spawn(ItemDef def, Vector3 pos)
    {
        if (def == null) return null;
        var go = Make($"Pickup_{def.id}", pos, def.Icon, Color.white, 1.9f,
                      ItemDef.RarityColor(def.rarity));
        var pick = go.AddComponent<ItemPickup>();
        pick.itemId = def.id;
        return pick;
    }

    /// <summary>Buat tumpukan uang di dunia.</summary>
    public static ItemPickup SpawnGold(int amount, Vector3 pos)
    {
        var go = Make("Pickup_Gold", pos, ArtLibrary.D(Tiles.Dun.Coins), Color.white, 1.5f,
                      new Color(0.89f, 0.70f, 0.25f));
        var pick = go.AddComponent<ItemPickup>();
        pick.goldAmount = Mathf.Max(1, amount);
        return pick;
    }

    /// <summary>
    /// Barang di lantai gelap mudah terlewat, apalagi ikon 16px. Tiap pickup
    /// diberi bola cahaya seukuran kelangkaannya supaya terbaca dari jauh.
    /// </summary>
    static GameObject Make(string name, Vector3 pos, Sprite icon, Color iconTint, float scale, Color glow)
    {
        var go = new GameObject(name);
        go.transform.position = pos;

        var halo = new GameObject("Halo");
        halo.transform.SetParent(go.transform, false);
        halo.transform.localScale = Vector3.one * 2.6f;
        var hs = halo.AddComponent<SpriteRenderer>();
        hs.sprite = RoomDressing.Glow();
        hs.sortingOrder = 7;
        glow.a = 0.55f;
        hs.color = glow;

        var body = new GameObject("Icon");
        body.transform.SetParent(go.transform, false);
        body.transform.localScale = Vector3.one * scale;
        var sr = body.AddComponent<SpriteRenderer>();
        sr.sprite = icon;
        sr.sortingOrder = 9;
        sr.color = iconTint;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.7f;
        return go;
    }

    void Start()
    {
        origin = transform.position;
        phase = Random.value * Mathf.PI * 2f;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        // tersedot ke pemain saat cukup dekat: mengambil barang tidak boleh jadi
        // permainan ketepatan berdiri
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null)
        {
            float d = Vector2.Distance(transform.position, player.position);
            if (flying || d < magnetRange)
            {
                flying = true;
                transform.position = Vector3.MoveTowards(transform.position, player.position,
                                                         Time.deltaTime * Mathf.Lerp(4f, 14f, 1f - d / magnetRange));
                return;
            }
        }

        transform.position = origin + new Vector3(0f, Mathf.Sin(Time.time * 3f + phase) * 0.16f, 0f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var inv = other.GetComponent<Inventory>() ?? other.GetComponentInParent<Inventory>();
        if (inv == null) return;

        if (goldAmount > 0)
        {
            inv.AddGold(goldAmount);
            if (AudioManager.Instance != null) AudioManager.Instance.Play("coin", 0.7f);
            FloatingText.Spawn(transform.position, $"+{goldAmount}", new Color(0.89f, 0.70f, 0.25f));
            Destroy(gameObject);
            return;
        }

        var def = ItemDatabase.Get(itemId);
        if (def == null) { Destroy(gameObject); return; }
        if (inv.Add(def))
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.Play(def.IsConsumable ? "potion" : "pickup", 0.8f);
            FloatingText.Spawn(transform.position, def.displayName, ItemDef.RarityColor(def.rarity));
            Destroy(gameObject);
        }
    }
}

/// <summary>Peti: sekali dibuka memuntahkan beberapa hadiah.</summary>
public class Chest : MonoBehaviour
{
    [Min(1)] public int rolls = 1;
    public bool opened;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (opened || !other.CompareTag("Player")) return;
        opened = true;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = ArtLibrary.D(Tiles.Dun.ChestOpen);

        LootTable.DropChest(transform.position, rolls);
        if (AudioManager.Instance != null) AudioManager.Instance.Play("chest", 0.9f);
    }
}

/// <summary>
/// Aturan jatuhnya barang. Semua peluang di satu tempat supaya balance
/// bisa disetel tanpa menyentuh logika ruangan atau musuh.
/// </summary>
public static class LootTable
{
    static float Luck
    {
        get
        {
            var inv = Inventory.Instance;
            if (inv == null) return 0f;
            var stats = inv.GetComponent<PlayerStats>();
            return stats != null ? stats.Luck : 0f;
        }
    }

    /// <summary>Kelangkaan acak; Luck menggeser peluang ke tingkat lebih tinggi.</summary>
    static Rarity RollRarity(float bonus = 0f)
    {
        float r = Random.value - (Luck + bonus) * 0.15f;
        if (r < 0.03f) return Rarity.Legenda;
        if (r < 0.11f) return Rarity.Sakti;
        if (r < 0.28f) return Rarity.Pusaka;
        if (r < 0.55f) return Rarity.Langka;
        return Rarity.Umum;
    }

    /// <summary>Hadiah saat sebuah ruangan dibersihkan.</summary>
    public static void DropRoomReward(Vector3 pos, RoomType type)
    {
        switch (type)
        {
            case RoomType.Combat:
                ItemPickup.SpawnGold(Random.Range(5, 16), pos + Off());
                if (Random.value < 0.35f) DropItem(pos + Off(), RollRarity());
                break;

            case RoomType.Elite:
                ItemPickup.SpawnGold(Random.Range(20, 41), pos + Off());
                DropItem(pos + Off(), RollRarity(0.5f));
                if (Random.value < 0.4f) DropItem(pos + Off(), RollRarity());
                break;

            case RoomType.Boss:
                ItemPickup.SpawnGold(Random.Range(60, 121), pos + Off());
                DropItem(pos + Off(), Rarity.Sakti);
                DropItem(pos + Off(), RollRarity(1f));
                break;
        }
    }

    /// <summary>Isi peti.</summary>
    public static void DropChest(Vector3 pos, int rolls)
    {
        for (int i = 0; i < rolls; i++) DropItem(pos + Off(), RollRarity(0.35f));
        ItemPickup.SpawnGold(Random.Range(10, 31), pos + Off());
    }

    /// <summary>
    /// Jatuhan tiap musuh mati. Peluangnya dinaikkan dari 45%/8% ke 75%/20%:
    /// dengan angka lama pemain bisa membunuh selusin musuh tanpa melihat satu
    /// pun barang jatuh, dan menyimpulkan sistem loot-nya tidak jalan.
    /// </summary>
    public static void DropFromEnemy(Vector3 pos)
    {
        if (Random.value < 0.75f) ItemPickup.SpawnGold(Random.Range(2, 8), pos);
        if (Random.value < 0.20f) DropItem(pos + Off(), RollRarity());
    }

    static void DropItem(Vector3 pos, Rarity rarity)
    {
        var def = ItemDatabase.RandomOf(rarity);
        if (def != null) ItemPickup.Spawn(def, pos);
    }

    static Vector3 Off() => (Vector3)(Random.insideUnitCircle * 2.2f);
}
