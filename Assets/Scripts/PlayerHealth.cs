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

    void Start()
    {
        currentHealth = maxHealth;
        if (anim == null) anim = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRend == null) spriteRend = GetComponentInChildren<SpriteRenderer>();
        PushUI();
    }

    public void TakeDamage(int damage) => TakeDamage(damage, transform.position);

    public void TakeDamage(int damage, Vector2 hitFrom)
    {
        if (isInvincible || isDead) return;

        if (GameManager.Instance != null)
            damage = Mathf.RoundToInt(damage * GameManager.Instance.Profile.playerDamageTakenMult);

        currentHealth = Mathf.Max(0, currentHealth - damage);
        PushUI();

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.playerHurt);
        ApplyKnockback(hitFrom);

        if (currentHealth <= 0) Die();
        else StartCoroutine(InvincibleFlash());
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
