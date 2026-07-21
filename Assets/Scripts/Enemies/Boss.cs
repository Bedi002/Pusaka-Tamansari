using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Boss berfase. Mengejar player & menyerang melee; saat HP turun ke ambang
/// tertentu, fase berubah dan pola serang bertambah:
///   Fase 1: kejar + melee.
///   Fase 2: + serangan proyektil jarak jauh (jika projectilePrefab diisi).
///   Fase 3 (enrage): gerak & serang lebih cepat, + memanggil minion (jika diisi).
/// Mengimplementasi IDamageable, melapor ke boss-bar HUD, dan memicu Victory()
/// lewat event Defeated saat kalah.
/// </summary>
public class Boss : MonoBehaviour, IDamageable
{
    [Header("Identitas")]
    public string bossName = "Penjaga Pusaka";

    [Header("Status Basis (sebelum dikali difficulty/stage)")]
    public int maxHealth = 1500;
    public int meleeDamage = 30;
    public float moveSpeed = 1.8f;
    public float attackRange = 1.3f;
    public float meleeCooldown = 1.6f;

    [Header("Ambang Fase (persen HP)")]
    [Range(0f, 1f)] public float phase2Threshold = 0.66f;
    [Range(0f, 1f)] public float phase3Threshold = 0.33f;
    public float enrageSpeedMult = 1.5f;
    public float enrageCooldownMult = 0.6f;

    [Header("Serangan Jarak Jauh (fase 2+, opsional)")]
    public GameObject projectilePrefab;
    public float rangedCooldown = 2.5f;
    public int projectileDamage = 20;
    public float projectileSpeed = 6f;

    [Header("Summon Minion (fase 3, opsional)")]
    public GameObject minionPrefab;
    public float summonCooldown = 6f;
    public int summonCount = 2;

    [Header("Efek")]
    public float hitFlashDuration = 0.08f;
    public float deathDelay = 2f;

    /// <summary>Dipicu saat boss kalah.</summary>
    public event Action<Boss> Defeated;

    int currentHealth;
    int maxScaled;
    int phase = 1;
    bool isDead = false;

    Transform player;
    PlayerHealth playerHealth;
    Animator anim;
    SpriteRenderer sprite;
    Rigidbody2D rb;
    Color baseColor = Color.white;
    Coroutine flashCo;

    float nextMelee, nextRanged, nextSummon;
    float speedMult = 1f, cdMult = 1f;

    DifficultyProfile profile = DifficultyTable.Get(Difficulty.Normal);
    float stageScale = 1f;

    void Awake()
    {
        anim = GetComponent<Animator>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        if (sprite != null) baseColor = sprite.color;
    }

    void Start()
    {
        if (maxScaled == 0) ApplyStats();

        var pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            player = pObj.transform; playerHealth = pObj.GetComponent<PlayerHealth>();
            var myCol = GetComponent<Collider2D>();
            var pCol = pObj.GetComponent<Collider2D>();
            if (myCol != null && pCol != null) Physics2D.IgnoreCollision(myCol, pCol, true);  // player tak bisa dorong boss
        }

        if (HUDController.Instance != null) HUDController.Instance.ShowBossBar(bossName, maxScaled);
    }

    public void Configure(DifficultyProfile p, float scale)
    {
        if (p != null) profile = p;
        stageScale = scale;
        ApplyStats();
        if (HUDController.Instance != null) HUDController.Instance.ShowBossBar(bossName, maxScaled);
    }

    void ApplyStats()
    {
        maxScaled = Mathf.Max(1, Mathf.RoundToInt(maxHealth * profile.enemyHealthMult * stageScale));
        currentHealth = maxScaled;
    }

    int MeleeDmg => Mathf.Max(1, Mathf.RoundToInt(meleeDamage * profile.enemyDamageMult * stageScale));
    int RangedDmg => Mathf.Max(1, Mathf.RoundToInt(projectileDamage * profile.enemyDamageMult * stageScale));
    bool PlayerActive => player != null && playerHealth != null && !playerHealth.IsDead;

    void Update()
    {
        if (isDead || !PlayerActive) { SafeSetFloat("Speed", 0f); return; }

        UpdatePhase();

        float dist = Vector2.Distance(transform.position, player.position);
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        SafeSetFloat("MoveX", dir.x);
        SafeSetFloat("MoveY", dir.y);

        if (dist > attackRange) SafeSetFloat("Speed", 1f);
        else { SafeSetFloat("Speed", 0f); TryMelee(); }

        if (phase >= 2) TryRanged();
        if (phase >= 3) TrySummon();
    }

    void FixedUpdate()
    {
        if (isDead || !PlayerActive) return;
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= attackRange * 0.9f) return;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        float step = moveSpeed * profile.enemySpeedMult * speedMult * Time.fixedDeltaTime;
        Vector2 target = (Vector2)transform.position + dir * step;
        if (rb != null && rb.simulated) rb.MovePosition(target);
        else transform.position = target;
    }

    void UpdatePhase()
    {
        float pct = maxScaled > 0 ? (float)currentHealth / maxScaled : 0f;
        int newPhase = pct <= phase3Threshold ? 3 : (pct <= phase2Threshold ? 2 : 1);
        if (newPhase != phase)
        {
            phase = newPhase;
            if (phase == 3) { speedMult = enrageSpeedMult; cdMult = enrageCooldownMult; }
            if (HUDController.Instance != null) HUDController.Instance.ShowMessage($"FASE {phase}!", 1.2f);
        }
    }

    void TryMelee()
    {
        if (Time.time < nextMelee) return;
        nextMelee = Time.time + meleeCooldown * cdMult;
        SafeSetTrigger("Attack");
        if (PlayerActive) playerHealth.TakeDamage(MeleeDmg, transform.position);
    }

    void TryRanged()
    {
        if (projectilePrefab == null || Time.time < nextRanged) return;
        nextRanged = Time.time + rangedCooldown * cdMult;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        var go = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        var proj = go.GetComponent<Projectile>();
        if (proj != null) proj.Launch(dir, projectileSpeed, RangedDmg);
    }

    void TrySummon()
    {
        if (minionPrefab == null || Time.time < nextSummon) return;
        nextSummon = Time.time + summonCooldown;
        for (int i = 0; i < summonCount; i++)
        {
            Vector2 off = UnityEngine.Random.insideUnitCircle * 1.5f;
            var go = Instantiate(minionPrefab, (Vector2)transform.position + off, Quaternion.identity);
            var e = go.GetComponent<Enemy>();
            if (e != null) e.Configure(profile, stageScale);
        }
    }

    public void TakeDamage(int damage) => TakeDamage(damage, transform.position);

    public void TakeDamage(int damage, Vector2 hitFrom)
    {
        if (isDead) return;
        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (HUDController.Instance != null) HUDController.Instance.UpdateBossBar(currentHealth);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.bossHurt);

        if (sprite != null)
        {
            if (flashCo != null) StopCoroutine(flashCo);
            flashCo = StartCoroutine(Flash());
        }

        if (currentHealth <= 0) Die();
    }

    IEnumerator Flash()
    {
        sprite.color = new Color(1f, 0.5f, 0.5f, 1f);
        yield return new WaitForSeconds(hitFlashDuration);
        sprite.color = baseColor;
        flashCo = null;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        SafeSetTrigger("Die");
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.bossDie);
        if (DungeonManager.Instance != null) DungeonManager.Instance.Shake(0.45f, 0.6f);

        var col = GetComponent<Collider2D>(); if (col != null) col.enabled = false;
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }

        Defeated?.Invoke(this);
        Destroy(gameObject, deathDelay);
    }

    // ---- helper Animator ----
    void SafeSetFloat(string p, float v) { if (HasParam(p, AnimatorControllerParameterType.Float)) anim.SetFloat(p, v); }
    void SafeSetTrigger(string p) { if (HasParam(p, AnimatorControllerParameterType.Trigger)) anim.SetTrigger(p); }
    bool HasParam(string p, AnimatorControllerParameterType t)
    {
        if (anim == null) return false;
        foreach (var par in anim.parameters) if (par.name == p && par.type == t) return true;
        return false;
    }
}
