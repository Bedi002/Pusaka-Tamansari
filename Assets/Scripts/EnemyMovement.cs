using UnityEngine;

/// <summary>
/// AI gerak musuh: mengejar player, berhenti & menyerang saat dalam jarak
/// serang (berbasis jarak, bukan sekadar tabrakan), dengan "separation" agar
/// musuh tidak menumpuk jadi satu. Kecepatan menyesuaikan tingkat kesulitan.
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    [Header("Gerak (basis sebelum difficulty)")]
    public float speed = 2f;
    [Tooltip("Jarak mulai menyerang")]
    public float attackRange = 0.9f;
    [Tooltip("Jarak berhenti mendekat")]
    public float stopDistance = 0.7f;
    public float attackCooldown = 1.5f;

    [Header("Anti-Numpuk (separation)")]
    public float separationRadius = 0.6f;
    public float separationStrength = 1.2f;
    [Tooltip("Set ke layer 'Enemy' di Inspector")]
    public LayerMask enemyMask;

    Transform player;
    PlayerHealth playerHealth;
    Enemy enemy;
    Animator anim;
    Rigidbody2D rb;
    float nextAttackTime = 0f;
    float speedMult = 1f;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        enemy = GetComponent<Enemy>();

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
            // jangan saling dorong dgn player (tetap kena tembok). Damage lewat jarak.
            var myCol = GetComponent<Collider2D>();
            var pCol = playerObj.GetComponent<Collider2D>();
            if (myCol != null && pCol != null) Physics2D.IgnoreCollision(myCol, pCol, true);
        }

        if (GameManager.Instance != null)
            speedMult = GameManager.Instance.Profile.enemySpeedMult;
    }

    bool PlayerActive => player != null && playerHealth != null && !playerHealth.IsDead;

    void Update()
    {
        if (!PlayerActive) { SafeSetFloat("Speed", 0f); return; }

        float dist = Vector2.Distance(transform.position, player.position);
        Vector2 toPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;

        SafeSetFloat("MoveX", toPlayer.x);
        SafeSetFloat("MoveY", toPlayer.y);

        if (dist <= attackRange)
        {
            SafeSetFloat("Speed", 0f);
            TryAttack();
        }
        else
        {
            SafeSetFloat("Speed", 1f);
        }
    }

    void FixedUpdate()
    {
        if (!PlayerActive) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= stopDistance) return;

        Vector2 toPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        Vector2 move = (toPlayer + Separation() * separationStrength).normalized;
        float step = speed * speedMult * Time.fixedDeltaTime;
        Vector2 target = (Vector2)transform.position + move * step;

        if (rb != null && rb.simulated) rb.MovePosition(target);
        else transform.position = target;
    }

    Vector2 Separation()
    {
        Vector2 sum = Vector2.zero;
        int count = 0;
        var hits = Physics2D.OverlapCircleAll(transform.position, separationRadius, enemyMask);
        foreach (var h in hits)
        {
            if (h.gameObject == gameObject) continue;
            Vector2 away = (Vector2)transform.position - (Vector2)h.transform.position;
            float d = away.magnitude;
            if (d > 0.001f) { sum += away.normalized / d; count++; }
        }
        if (count > 0) sum /= count;
        return sum;
    }

    void TryAttack()
    {
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackCooldown;
        SafeSetTrigger("Attack");
        if (PlayerActive)
        {
            int dmg = enemy != null ? enemy.AttackDamage : 20;
            playerHealth.TakeDamage(dmg, transform.position);
        }
    }

    // ---- helper Animator (aman walau parameter belum ada) ----
    void SafeSetFloat(string p, float v) { if (HasParam(p, AnimatorControllerParameterType.Float)) anim.SetFloat(p, v); }
    void SafeSetTrigger(string p) { if (HasParam(p, AnimatorControllerParameterType.Trigger)) anim.SetTrigger(p); }
    bool HasParam(string p, AnimatorControllerParameterType t)
    {
        if (anim == null) return false;
        foreach (var par in anim.parameters) if (par.name == p && par.type == t) return true;
        return false;
    }
}
