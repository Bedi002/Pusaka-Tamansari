using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemStack
{
    public string id;
    public int count = 1;
    public ItemStack() { }
    public ItemStack(string id, int count) { this.id = id; this.count = count; }
    public ItemDef Def => ItemDatabase.Get(id);
}

/// <summary>
/// Isi tas satu run. Disimpan di GameManager (DontDestroyOnLoad) supaya barang
/// tidak hilang saat pindah lantai -- player di-spawn ulang tiap lantai.
/// </summary>
/// <summary>Bonus permanen satu run (berkah sanggar, tempaan wesi aji).</summary>
[Serializable]
public class PermanentBonus
{
    public EffectKind kind;
    public float value;
    public PermanentBonus() { }
    public PermanentBonus(EffectKind k, float v) { kind = k; value = v; }
}

[Serializable]
public class RunInventory
{
    /// <summary>
    /// Bonus permanen WAJIB tinggal di sini, bukan di PlayerStats. Player
    /// di-spawn ulang tiap ganti lantai, jadi apa pun yang disimpan di komponennya
    /// akan hilang -- berkah sanggar dan tempaan senjata lenyap begitu turun lantai.
    /// </summary>
    public List<PermanentBonus> permanent = new List<PermanentBonus>();

    /// <summary>Berapa kali senjata sudah ditempa di sanggar.</summary>
    public int weaponLevel = 0;

    public List<ItemStack> slots = new List<ItemStack>();
    public string weaponId = "";
    public string armorId = "";
    public string charmAId = "";
    public string charmBId = "";
    public List<string> pusaka = new List<string>();   // relik permanen, tak bisa dilepas
    public int gold = 0;
    [Tooltip("Kemampuan bangkit hanya sekali per run")]
    public bool reviveUsed = false;

    public void Clear()
    {
        slots.Clear(); pusaka.Clear(); permanent.Clear();
        weaponId = armorId = charmAId = charmBId = "";
        gold = 0;
        weaponLevel = 0;
        reviveUsed = false;
    }

    public void AddPermanent(EffectKind kind, float value)
    {
        foreach (var p in permanent)
            if (p.kind == kind) { p.value += value; return; }
        permanent.Add(new PermanentBonus(kind, value));
    }
}

/// <summary>
/// Tas pemain: menambah, memakai, memakai-pakai (equip), dan membuang item.
/// Semua perubahan langsung memicu PlayerStats.Recalculate().
/// </summary>
public class Inventory : MonoBehaviour
{
    public const int Capacity = 24;

    public static Inventory Instance { get; private set; }

    /// <summary>Dipanggil tiap isi tas berubah (untuk refresh UI).</summary>
    public event Action Changed;

    RunInventory run;
    PlayerStats stats;

    public RunInventory Run => run;
    public int Gold => run != null ? run.gold : 0;

    void Awake()
    {
        Instance = this;
        stats = GetComponent<PlayerStats>();
        run = (GameManager.Instance != null) ? GameManager.Instance.run : new RunInventory();
    }

    void Start()
    {
        // stats baru bisa dihitung setelah PlayerHealth.Start menetapkan nilai dasar
        if (stats != null) stats.Recalculate();
        Changed?.Invoke();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // ------------------------------------------------------------------ tambah
    /// <summary>Masukkan item. Mengembalikan false bila tas penuh.</summary>
    public bool Add(ItemDef def, int count = 1)
    {
        if (def == null) return false;

        if (def.category == ItemCategory.Pusaka)
        {
            // pusaka langsung menempel permanen, tidak makan slot tas
            if (!run.pusaka.Contains(def.id)) run.pusaka.Add(def.id);
            Notify($"PUSAKA: {def.displayName}");
            Touch();
            return true;
        }

        if (def.stackMax > 1)
        {
            foreach (var s in run.slots)
                if (s.id == def.id && s.count < def.stackMax)
                {
                    int room = def.stackMax - s.count;
                    int put = Mathf.Min(room, count);
                    s.count += put;
                    count -= put;
                    if (count <= 0) { Notify($"+{def.displayName}"); Touch(); return true; }
                }
        }

        while (count > 0)
        {
            if (run.slots.Count >= Capacity) { Notify("Tas penuh!"); Touch(); return false; }
            int put = Mathf.Min(Mathf.Max(1, def.stackMax), count);
            run.slots.Add(new ItemStack(def.id, put));
            count -= put;
        }

        Notify($"+{def.displayName}");
        Touch();
        return true;
    }

    public bool Add(string id, int count = 1) => Add(ItemDatabase.Get(id), count);

    public void AddGold(int amount)
    {
        run.gold = Mathf.Max(0, run.gold + amount);
        Touch();
    }

    // -------------------------------------------------------------- pakai/lepas
    /// <summary>Klik item di tas: consumable diminum, perlengkapan dipakai.</summary>
    public void UseSlot(int index)
    {
        if (index < 0 || index >= run.slots.Count) return;
        var stack = run.slots[index];
        var def = stack.Def;
        if (def == null) { run.slots.RemoveAt(index); Touch(); return; }

        if (def.IsConsumable) { Consume(def); Decrement(index); return; }
        if (def.IsEquipment) { Equip(index); return; }
    }

    void Consume(ItemDef def)
    {
        if (def.effects == null) return;
        foreach (var e in def.effects)
        {
            if (e == null) continue;
            switch (e.kind)
            {
                case EffectKind.Heal:
                    var hp = GetComponent<PlayerHealth>();
                    if (hp != null) hp.Heal(Mathf.RoundToInt(e.value));
                    break;
                case EffectKind.Gold:
                    AddGold(Mathf.RoundToInt(e.value));
                    break;
                default:
                    // efek stat: permanen bila duration 0, sementara bila > 0
                    if (stats != null)
                    {
                        if (e.duration > 0f) stats.AddTimed(e.kind, e.value, e.duration);
                        else stats.AddPermanent(e.kind, e.value);
                    }
                    break;
            }
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.uiClick);
        Notify(def.displayName);
    }

    /// <summary>Pasang perlengkapan; yang lama kembali ke tas.</summary>
    public void Equip(int index)
    {
        if (index < 0 || index >= run.slots.Count) return;
        var def = run.slots[index].Def;
        if (def == null || !def.IsEquipment) return;

        string previous;
        switch (def.category)
        {
            case ItemCategory.Weapon: previous = run.weaponId; run.weaponId = def.id; break;
            case ItemCategory.Armor: previous = run.armorId; run.armorId = def.id; break;
            default:
                // dua slot jimat: isi yang kosong dulu, kalau penuh ganti slot A
                if (string.IsNullOrEmpty(run.charmAId)) { previous = ""; run.charmAId = def.id; }
                else if (string.IsNullOrEmpty(run.charmBId)) { previous = ""; run.charmBId = def.id; }
                else { previous = run.charmAId; run.charmAId = def.id; }
                break;
        }

        Decrement(index, notify: false);
        if (!string.IsNullOrEmpty(previous)) Add(ItemDatabase.Get(previous));
        Notify($"Dipakai: {def.displayName}");
        Touch();
    }

    /// <summary>Lepas perlengkapan dari sebuah slot pakai, kembalikan ke tas.</summary>
    public void Unequip(ItemCategory slot, bool charmB = false)
    {
        string id = "";
        switch (slot)
        {
            case ItemCategory.Weapon: id = run.weaponId; run.weaponId = ""; break;
            case ItemCategory.Armor: id = run.armorId; run.armorId = ""; break;
            default:
                if (charmB) { id = run.charmBId; run.charmBId = ""; }
                else { id = run.charmAId; run.charmAId = ""; }
                break;
        }
        if (!string.IsNullOrEmpty(id)) Add(ItemDatabase.Get(id));
        Touch();
    }

    public void Drop(int index)
    {
        if (index < 0 || index >= run.slots.Count) return;
        var def = run.slots[index].Def;
        Decrement(index);
        if (def != null) ItemPickup.Spawn(def, transform.position + (Vector3)(UnityEngine.Random.insideUnitCircle * 1.5f));
    }

    void Decrement(int index, bool notify = true)
    {
        var stack = run.slots[index];
        stack.count--;
        if (stack.count <= 0) run.slots.RemoveAt(index);
        Touch();
    }

    // ------------------------------------------------------------------ query
    public IReadOnlyList<ItemStack> Slots => run.slots;
    public ItemDef Weapon => ItemDatabase.Get(run.weaponId);
    public ItemDef Armor => ItemDatabase.Get(run.armorId);
    public ItemDef CharmA => ItemDatabase.Get(run.charmAId);
    public ItemDef CharmB => ItemDatabase.Get(run.charmBId);

    /// <summary>Semua item yang efeknya sedang aktif (dipakai + pusaka).</summary>
    public IEnumerable<ItemDef> ActiveItems()
    {
        var w = Weapon; if (w != null) yield return w;
        var a = Armor; if (a != null) yield return a;
        var c1 = CharmA; if (c1 != null) yield return c1;
        var c2 = CharmB; if (c2 != null) yield return c2;
        foreach (var id in run.pusaka)
        {
            var p = ItemDatabase.Get(id);
            if (p != null) yield return p;
        }
    }

    void Touch()
    {
        if (stats != null) stats.Recalculate();
        Changed?.Invoke();
    }

    void Notify(string msg)
    {
        if (HUDController.Instance != null) HUDController.Instance.ShowMessage(msg, 1.4f);
    }
}
