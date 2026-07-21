using UnityEngine;

/// <summary>
/// Serangan combo 3-hit (Spasi). Memakai IDamageable sehingga satu serangan
/// bisa mengenai musuh biasa MAUPUN boss. Mengirim posisi penyerang agar target
/// terdorong (knockback). Berhenti saat game di-pause.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("Pengaturan Pukulan")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public float attackOffset = 0.6f;
    public LayerMask enemyLayers;
    public Animator anim;
    public int attackDamage = 50;

    [Header("Sistem Combo")]
    public int comboStep = 0;
    public float maxComboDelay = 1f;   // batas waktu sebelum combo reset
    public float comboCooldown = 0.4f; // jeda minimal antar pukulan
    float lastAttackTime = 0f;
    float nextAttackTime = 0f;
    PlayerMovement move;
    PlayerStats stats;

    void Awake()
    {
        move = GetComponent<PlayerMovement>();
        stats = GetComponent<PlayerStats>();
        if (anim == null) anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        // arah hadap dimiliki PlayerMovement (set MoveX/MoveY). Di sini cukup arahkan attackPoint.
        Vector2 face = move != null ? move.lastFacing : Vector2.down;
        if (attackPoint != null) attackPoint.localPosition = (Vector3)(face.normalized * attackOffset);

        // reset combo bila kelamaan diam
        if (Time.time - lastAttackTime > maxComboDelay) comboStep = 0;

        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= nextAttackTime)
            Attack();

        // Aji Sepuh: serangan berat, jangkauan dua kali lipat dan damage 2.5x,
        // dibayar dengan Aji. Ini yang membuat bar Aji punya arti.
        if ((Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1)) && Time.time >= nextAttackTime)
            Heavy();
    }

    void Heavy()
    {
        if (res == null) res = GetComponent<PlayerResources>();
        if (res != null && !res.SpendMana(res.heavyCost))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.Play("ui_error", 0.35f);
            return;
        }

        nextAttackTime = Time.time + comboCooldown * 1.6f;
        lastAttackTime = Time.time;

        if (anim != null) { ResetTriggers(); anim.SetTrigger("Attack3"); }
        if (AudioManager.Instance != null) AudioManager.Instance.Play("crit", 0.85f);
        if (DungeonManager.Instance != null) DungeonManager.Instance.Shake(0.3f, 0.2f);

        if (attackPoint == null) return;
        var hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange * 2.1f, enemyLayers);
        int dealt = Mathf.RoundToInt(attackDamage * 2.5f);
        foreach (var col in hits)
        {
            var dmg = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (dmg != null) dmg.TakeDamage(dealt, transform.position);
        }
        FloatingText.Spawn(attackPoint.position, "AJI SEPUH", UIKit.Teal, true);
    }

    PlayerResources res;

    void Attack()
    {
        lastAttackTime = Time.time;
        nextAttackTime = Time.time + comboCooldown;
        comboStep++;

        if (anim != null)
        {
            if (comboStep == 1) { ResetTriggers(); anim.SetTrigger("Attack1"); }
            else if (comboStep == 2) { ResetTriggers(); anim.SetTrigger("Attack2"); }
            else { ResetTriggers(); anim.SetTrigger("Attack3"); }
        }
        if (comboStep >= 3) comboStep = 0;

        if (AudioManager.Instance != null) AudioManager.Instance.Play("swing", 0.8f);

        if (attackPoint == null) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        int landed = 0, totalDealt = 0;

        bool crit = stats != null && Random.value < stats.CritChance;
        int dealt = crit ? attackDamage * 2 : attackDamage;

        foreach (Collider2D col in hits)
        {
            var dmg = col.GetComponent<IDamageable>();
            if (dmg == null) dmg = col.GetComponentInParent<IDamageable>();
            if (dmg != null) { dmg.TakeDamage(dealt, transform.position); landed++; totalDealt += dealt; }
        }

        if (landed > 0)
        {
            if (DungeonManager.Instance != null) DungeonManager.Instance.Shake(crit ? 0.22f : 0.12f, crit ? 0.16f : 0.10f);
            if (crit)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.Play("crit", 0.9f);
                // angka damage-nya sudah dimunculkan tiap musuh; ini penanda kritisnya
                FloatingText.Spawn(attackPoint.position, "KRITIS", new Color(0.89f, 0.70f, 0.25f), true);
            }

            // serap darah: sebagian damage yang masuk kembali jadi HP
            if (stats != null && stats.LifeSteal > 0f)
            {
                int healed = Mathf.RoundToInt(totalDealt * stats.LifeSteal);
                if (healed > 0)
                {
                    var hp = GetComponent<PlayerHealth>();
                    if (hp != null) hp.Heal(healed);
                }
            }
        }
    }

    void ResetTriggers()
    {
        anim.ResetTrigger("Attack1");
        anim.ResetTrigger("Attack2");
        anim.ResetTrigger("Attack3");
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
