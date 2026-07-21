using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Musuh standar. Mengimplementasi IDamageable supaya bisa dipukul player.
/// Status (HP & damage) otomatis menyesuaikan tingkat kesulitan + nomor stage
/// lewat Configure() yang dipanggil StageManager. Ada hit-flash, knockback,
/// pemberian skor, dan event Died agar StageManager bisa melacak musuh hidup.
/// </summary>
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Status Basis (sebelum dikali difficulty/stage)")]
    public int maxHealth = 100;
    public int attackDamage = 20;
    public int scoreReward = 10;

    [Header("Efek")]
    public float hitFlashDuration = 0.1f;
    public float knockbackForce = 4f;
    public float deathCleanupDelay = 2f;

    [Header("Suara (nama bank di Resources/Audio)")]
    [Tooltip("Terdengar saat kena pukul. Musuh berzirah/batu memakai hit_metal.")]
    public string hitSoundKey = "hit_flesh";
    [Tooltip("Terdengar saat mati. Slime memakai slime_die.")]
    public string deathSoundKey = "enemy_die";

    /// <summary>Dipicu tepat saat musuh mati (sebelum mayat dibersihkan).</summary>
    public event Action<Enemy> Died;

    /// <summary>Damage serangan setelah scaling — dibaca EnemyMovement.</summary>
    public int AttackDamage { get; private set; }
    public bool IsDead => isDead;

    int currentHealth;
    bool isDead = false;
    bool configured = false;

    Animator anim;
    SpriteRenderer sprite;
    Rigidbody2D rb;
    Color baseColor = Color.white;
    Coroutine flashCo;

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
        // Bila di-spawn manual tanpa StageManager, tetap punya stat yang valid.
        if (!configured) ApplyStats();
    }

    /// <summary>Dipanggil StageManager / Boss sesaat setelah Instantiate.</summary>
    public void Configure(DifficultyProfile p, float scale)
    {
        if (p != null) profile = p;
        stageScale = scale;
        ApplyStats();
    }

    void ApplyStats()
    {
        configured = true;
        currentHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * profile.enemyHealthMult * stageScale));
        AttackDamage = Mathf.Max(1, Mathf.RoundToInt(attackDamage * profile.enemyDamageMult * stageScale));
    }

    // Kompatibel dengan kode lama yang memanggil TakeDamage(int).
    public void TakeDamage(int damage) => TakeDamage(damage, transform.position);

    public void TakeDamage(int damage, Vector2 hitFrom)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (AudioManager.Instance != null) AudioManager.Instance.Play(hitSoundKey, 0.7f);
        FloatingText.SpawnDamage(transform.position, damage);

        if (sprite != null)
        {
            if (flashCo != null) StopCoroutine(flashCo);
            flashCo = StartCoroutine(HitFlash());
        }
        ApplyKnockback(hitFrom);

        if (currentHealth <= 0) Die();
        else SafeSetTrigger("Hurt");   // mainkan animasi terluka (flinch)
    }

    void ApplyKnockback(Vector2 hitFrom)
    {
        if (rb == null || !rb.simulated) return;
        Vector2 dir = ((Vector2)transform.position - hitFrom).normalized;
        rb.linearVelocity = dir * knockbackForce;
    }

    IEnumerator HitFlash()
    {
        sprite.color = new Color(1f, 0.4f, 0.4f, 1f);
        yield return new WaitForSeconds(hitFlashDuration);
        sprite.color = baseColor;
        flashCo = null;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        SafeSetTrigger("Die");
        if (AudioManager.Instance != null) AudioManager.Instance.Play(deathSoundKey);
        if (GameManager.Instance != null) GameManager.Instance.AddScore(scoreReward);
        LootTable.DropFromEnemy(transform.position);

        var move = GetComponent<EnemyMovement>();
        if (move != null) move.enabled = false;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }

        Died?.Invoke(this);          // beri tahu StageManager (musuh berkurang)
        Destroy(gameObject, deathCleanupDelay);
    }

    void SafeSetTrigger(string param)
    {
        if (anim == null) return;
        foreach (var p in anim.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == param)
            { anim.SetTrigger(param); return; }
    }
}
