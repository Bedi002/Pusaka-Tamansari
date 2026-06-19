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

    void Awake()
    {
        move = GetComponent<PlayerMovement>();
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
    }

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

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.playerAttack);

        if (attackPoint == null) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider2D col in hits)
        {
            var dmg = col.GetComponent<IDamageable>();
            if (dmg == null) dmg = col.GetComponentInParent<IDamageable>();
            if (dmg != null) dmg.TakeDamage(attackDamage, transform.position);
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
