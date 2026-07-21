using System;
using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Darah player. Mendukung HP bar (Slider) lewat HUDController, teks TMP lama,
/// knockback + hit-stun, efek kebal/kedip, pengali damage dari difficulty,
/// dan alur kalah -> GameManager.GameOver().
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Darah Pemain")]
    public int maxHealth = 100;
    public bool isDead = false;
    public bool IsDead => isDead;

    [Header("Efek Terluka")]
    public float invincibilityDuration = 1.2f;
    public float knockbackForce = 5f;
    public float hitStun = 0.15f;
    bool isInvincible = false;

    [Header("Komponen (opsional, auto-isi bila kosong)")]
    public TextMeshProUGUI healthTextUI;
    public Animator anim;
    public SpriteRenderer spriteRend;
    public Rigidbody2D rb;

    /// <summary>(current, max) — berguna untuk UI/efek lain.</summary>
    public event Action<int, int> HealthChanged;

    int currentHealth;
    const float gameOverDelay = 1.5f;

    PlayerStats stats;

    // Awake (bukan Start) supaya PlayerStats boleh mengubah maxHealth lebih awal
    // tanpa bergantung pada urutan Start antar-komponen.
    void Awake()
    {
        currentHealth = maxHealth;
        if (anim == null) anim = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRend == null) spriteRend = GetComponentInChildren<SpriteRenderer>();
        stats = GetComponent<PlayerStats>();
    }

    void Start() => PushUI();

    [Header("Regenerasi")]
    [Tooltip("HP pulih per detik saat tidak sedang terluka. 0 = mati regen.")]
    public float regenPerSecond = 1.5f;
    [Tooltip("Jeda setelah kena pukul sebelum regen mulai (detik)")]
    public float regenDelay = 4f;
    float regenHold, regenCarry;

    void Update()
    {
        if (isDead || regenPerSecond <= 0f || currentHealth >= maxHealth) return;

        // regen berhenti sejenak tiap kali terluka; kalau tidak, bertahan terasa
        // gratis dan tiap pukulan musuh kehilangan arti
        if (regenHold > 0f) { regenHold -= Time.deltaTime; return; }

        regenCarry += regenPerSecond * Time.deltaTime;
        if (regenCarry >= 1f)
        {
            int whole = Mathf.FloorToInt(regenCarry);
            regenCarry -= whole;
            currentHealth = Mathf.Min(maxHealth, currentHealth + whole);
            PushUI();
        }
    }

    /// <summary>HP saat ini (dibaca UI/efek).</summary>
    public int Current => currentHealth;

    /// <summary>Pulihkan HP, tidak melebihi maksimum.</summary>
    public void Heal(int amount)
    {
        if (isDead || amount <= 0) return;
        int before = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        if (currentHealth > before)
            FloatingText.SpawnHeal(transform.position, currentHealth - before);
        PushUI();
    }

    /// <summary>
    /// Ubah HP maksimum dari item. Selisihnya ditambahkan ke HP sekarang supaya
    /// memakai jimat +HP terasa langsung, bukan cuma memperbesar bar kosong.
    /// </summary>
    public void SetMaxHealth(int newMax)
    {
        newMax = Mathf.Max(1, newMax);
        int delta = newMax - maxHealth;
        maxHealth = newMax;
        currentHealth = Mathf.Clamp(currentHealth + Mathf.Max(0, delta), 1, maxHealth);
        PushUI();
    }

    public void TakeDamage(int damage) => TakeDamage(damage, transform.position);

    public void TakeDamage(int damage, Vector2 hitFrom)
    {
        if (isInvincible || isDead) return;

        if (GameManager.Instance != null)
            damage = Mathf.RoundToInt(damage * GameManager.Instance.Profile.playerDamageTakenMult);

        // zirah mengurangi damage masuk; duri memantulkan sebagian ke penyerang
        if (stats != null)
        {
            if (stats.Dodge > 0f && UnityEngine.Random.value < stats.Dodge)
            {
                FloatingText.SpawnDodge(transform.position);
                return;
            }
            if (stats.Armor > 0f) damage = Mathf.RoundToInt(damage * (1f - stats.Armor));
            if (stats.Thorns > 0f) ReflectThorns(hitFrom, Mathf.RoundToInt(stats.Thorns));
        }
        damage = Mathf.Max(1, damage);

        currentHealth = Mathf.Max(0, currentHealth - damage);
        regenHold = regenDelay;     // tunda regen: baru saja terluka
        PushUI();

        if (AudioManager.Instance != null) AudioManager.Instance.Play("player_hurt", 0.9f);
        FloatingText.Spawn(transform.position, "-" + damage, new Color(0.91f, 0.44f, 0.32f));
        ApplyKnockback(hitFrom);
        if (DungeonManager.Instance != null) DungeonManager.Instance.Shake(0.22f, 0.16f);

        if (currentHealth <= 0) Die();
        else StartCoroutine(InvincibleFlash());
    }

    /// <summary>Balas damage ke penyerang terdekat di titik pukulan.</summary>
    void ReflectThorns(Vector2 hitFrom, int amount)
    {
        if (amount <= 0) return;
        var hits = Physics2D.OverlapCircleAll(hitFrom, 1.2f);
        foreach (var col in hits)
        {
            if (col.gameObject == gameObject) continue;
            var dmg = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (dmg != null) { dmg.TakeDamage(amount, transform.position); return; }
        }
    }

    void ApplyKnockback(Vector2 hitFrom)
    {
        if (rb != null)
        {
            Vector2 dir = ((Vector2)transform.position - hitFrom).normalized;
            rb.linearVelocity = dir * knockbackForce;
        }
        var move = GetComponent<PlayerMovement>();
        if (move != null) move.ApplyHitStun(hitStun);
    }

    void PushUI()
    {
        if (healthTextUI != null)
            healthTextUI.text = currentHealth <= 0 ? "GAME OVER" : $"HP: {currentHealth}";
        if (HUDController.Instance != null) HUDController.Instance.SetHealth(currentHealth, maxHealth);
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    IEnumerator InvincibleFlash()
    {
        isInvincible = true;
        for (int i = 0; i < 3; i++)
        {
            if (spriteRend != null) spriteRend.color = new Color(1f, 0.3f, 0.3f, 1f);
            yield return new WaitForSeconds(invincibilityDuration / 6);
            if (spriteRend != null) spriteRend.color = Color.white;
            yield return new WaitForSeconds(invincibilityDuration / 6);
        }
        isInvincible = false;
    }

    void Die()
    {
        if (isDead) return;

        // Kembang Wijayakusuma dan sejenisnya: sekali per run kematian dibatalkan
        var run = GameManager.Instance != null ? GameManager.Instance.run : null;
        if (stats != null && stats.RevivePercent > 0f && run != null && !run.reviveUsed)
        {
            run.reviveUsed = true;
            currentHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * stats.RevivePercent));
            PushUI();
            StartCoroutine(InvincibleFlash());
            if (HUDController.Instance != null) HUDController.Instance.ShowMessage("WIJAYAKUSUMA MEKAR", 2.2f);
            return;
        }

        isDead = true;

        if (anim != null) SafeSetTrigger("Die");
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.gameOver);

        var move = GetComponent<PlayerMovement>(); if (move != null) move.enabled = false;
        var combat = GetComponent<PlayerCombat>(); if (combat != null) combat.enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(gameOverDelay);
        if (GameManager.Instance != null) GameManager.Instance.GameOver();
    }

    void SafeSetTrigger(string p)
    {
        foreach (var par in anim.parameters)
            if (par.type == AnimatorControllerParameterType.Trigger && par.name == p) { anim.SetTrigger(p); return; }
    }
}
