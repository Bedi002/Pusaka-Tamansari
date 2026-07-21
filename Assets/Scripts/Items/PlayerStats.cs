using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Menggabungkan stat dasar hero dengan bonus dari item yang dipakai, pusaka,
/// dan ramuan bertenggat, lalu mendorong hasilnya ke PlayerHealth / PlayerCombat /
/// PlayerMovement. Satu-satunya tempat stat akhir dihitung.
/// </summary>
[RequireComponent(typeof(Inventory))]
public class PlayerStats : MonoBehaviour
{
    class Timed
    {
        public EffectKind kind;
        public float value;
        public float expiresAt;
    }

    // nilai dasar dari prefab hero, direkam sekali sebelum bonus apa pun
    int baseMaxHp, baseDamage;
    float baseSpeed, baseRange, baseCooldown;
    bool captured;

    readonly Dictionary<EffectKind, float> permanent = new Dictionary<EffectKind, float>();
    readonly List<Timed> timed = new List<Timed>();

    PlayerHealth health;
    PlayerCombat combat;
    PlayerMovement move;
    Inventory inv;

    // stat turunan yang dibaca sistem lain
    public float Armor { get; private set; }
    public float CritChance { get; private set; }
    public float LifeSteal { get; private set; }
    public float Thorns { get; private set; }
    public float Luck { get; private set; }
    public float Dodge { get; private set; }
    public float XpGain { get; private set; }
    /// <summary>Persen HP saat bangkit kembali; 0 = tidak punya kemampuan bangkit.</summary>
    public float RevivePercent { get; private set; }

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
        combat = GetComponent<PlayerCombat>();
        move = GetComponent<PlayerMovement>();
        inv = GetComponent<Inventory>();
        Capture();
    }

    void Capture()
    {
        if (captured) return;
        baseMaxHp = health != null ? health.maxHealth : 100;
        baseDamage = combat != null ? combat.attackDamage : 20;
        baseRange = combat != null ? combat.attackRange : 0.5f;
        baseCooldown = combat != null ? combat.comboCooldown : 0.4f;
        baseSpeed = move != null ? move.moveSpeed : 5f;
        captured = true;
    }

    void Update()
    {
        if (timed.Count == 0) return;
        bool dirty = false;
        for (int i = timed.Count - 1; i >= 0; i--)
            if (Time.time >= timed[i].expiresAt) { timed.RemoveAt(i); dirty = true; }
        if (dirty) Recalculate();
    }

    /// <summary>
    /// Bonus selamanya (ramuan permanen, berkah sanggar, tempaan senjata).
    /// Disimpan di RunInventory, bukan di komponen ini: player di-spawn ulang
    /// tiap ganti lantai, jadi apa pun yang tinggal di sini akan hilang.
    /// </summary>
    public void AddPermanent(EffectKind kind, float value)
    {
        var run = inv != null ? inv.Run : null;
        if (run != null) run.AddPermanent(kind, value);
        else
        {
            permanent.TryGetValue(kind, out float cur);
            permanent[kind] = cur + value;     // cadangan bila dijalankan tanpa GameManager
        }
        Recalculate();
    }

    /// <summary>Bonus sementara (ramuan tempur).</summary>
    public void AddTimed(EffectKind kind, float value, float duration)
    {
        timed.Add(new Timed { kind = kind, value = value, expiresAt = Time.time + duration });
        Recalculate();
    }

    float Sum(EffectKind kind)
    {
        permanent.TryGetValue(kind, out float total);

        // bonus permanen yang bertahan antar-lantai
        var run = inv != null ? inv.Run : null;
        if (run != null && run.permanent != null)
        {
            foreach (var p in run.permanent) if (p != null && p.kind == kind) total += p.value;

            // tiap tingkat tempaan menambah serangan senjata yang sedang dipakai
            if (kind == EffectKind.Damage && run.weaponLevel > 0 && !string.IsNullOrEmpty(run.weaponId))
                total += run.weaponLevel * 2f;
        }

        if (inv != null)
            foreach (var def in inv.ActiveItems())
            {
                if (def.effects == null) continue;
                foreach (var e in def.effects)
                    if (e != null && e.kind == kind && e.duration <= 0f) total += e.value;
            }

        foreach (var t in timed) if (t.kind == kind) total += t.value;
        return total;
    }

    public void Recalculate()
    {
        Capture();

        Armor = Mathf.Clamp(Sum(EffectKind.Armor), 0f, 0.8f);          // maksimal tahan 80%
        CritChance = Mathf.Clamp01(Sum(EffectKind.CritChance));
        LifeSteal = Mathf.Clamp(Sum(EffectKind.LifeSteal), 0f, 1f);
        Thorns = Mathf.Max(0f, Sum(EffectKind.Thorns));
        Luck = Sum(EffectKind.Luck);
        Dodge = Mathf.Clamp(Sum(EffectKind.Dodge), 0f, 0.6f);     // maksimal 60%
        XpGain = Sum(EffectKind.XpGain);
        RevivePercent = Mathf.Clamp01(Sum(EffectKind.Revive));

        if (health != null)
            health.SetMaxHealth(Mathf.Max(1, baseMaxHp + Mathf.RoundToInt(Sum(EffectKind.MaxHp))));

        if (combat != null)
        {
            float dmg = (baseDamage + Sum(EffectKind.Damage)) * (1f + Sum(EffectKind.DamagePct));
            combat.attackDamage = Mathf.Max(1, Mathf.RoundToInt(dmg));
            combat.attackRange = Mathf.Max(0.2f, baseRange + Sum(EffectKind.Range));

            // AttackSpeed memperpendek jeda; dibatasi supaya tidak jadi 0
            float haste = 1f + Mathf.Max(-0.75f, Sum(EffectKind.AttackSpeed));
            combat.comboCooldown = Mathf.Max(0.08f, baseCooldown / Mathf.Max(0.25f, haste));
        }

        if (move != null)
            move.moveSpeed = Mathf.Max(1f, baseSpeed + Sum(EffectKind.MoveSpeed));
    }
}
