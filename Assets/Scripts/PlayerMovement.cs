using System.Collections;
using UnityEngine;

/// <summary>
/// Pergerakan player (WASD/panah) + dash (Shift). Mendukung "hit stun" singkat
/// supaya knockback dari musuh terasa, dan berhenti saat game di-pause
/// (Time.timeScale == 0).
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Pengaturan Pergerakan")]
    public float moveSpeed = 5f;
    public Rigidbody2D rb;
    public Animator anim;            // opsional (boleh null untuk sprite Kenney)
    public SpriteRenderer spriteRend; // untuk flip kiri/kanan
    bool useFlip = true;              // dimatikan otomatis bila Animator direksional (punya MoveX)
    Vector2 movement;

    /// <summary>Arah hadap terakhir (dipakai PlayerCombat untuk arah serangan).</summary>
    public Vector2 lastFacing = Vector2.down;

    [Header("Pengaturan Dash")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    bool canDash = true;
    bool isDashing;

    float hitStun = 0f;

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponent<Animator>();
        if (spriteRend == null) spriteRend = GetComponentInChildren<SpriteRenderer>();
        if (anim != null)
            foreach (var p in anim.parameters)
                if (p.name == "MoveX") { useFlip = false; break; }   // animasi 4-arah -> jangan flip
    }

    void Update()
    {
        if (isDashing) return;
        if (Time.timeScale == 0f) return; // game di-pause

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // arah hadap di-snap ke kardinal -> blend tree pilih tepat 1 klip (animasi bersih)
        if (movement.sqrMagnitude > 0.01f)
        {
            Vector2 m = movement.normalized;
            lastFacing = Mathf.Abs(m.x) >= Mathf.Abs(m.y)
                ? new Vector2(Mathf.Sign(m.x), 0f)
                : new Vector2(0f, Mathf.Sign(m.y));
            if (useFlip && spriteRend != null && Mathf.Abs(movement.x) > 0.01f)
                spriteRend.flipX = movement.x < 0f;
        }

        if (anim != null)
        {
            anim.SetFloat("MoveX", lastFacing.x);
            anim.SetFloat("MoveY", lastFacing.y);
            anim.SetFloat("Speed", movement.sqrMagnitude);
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && movement.sqrMagnitude > 0.01f)
            StartCoroutine(DashRoutine());
    }

    void FixedUpdate()
    {
        if (isDashing) return;
        if (hitStun > 0f) { hitStun -= Time.fixedDeltaTime; return; } // biarkan knockback bekerja
        if (rb != null) rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    /// <summary>Dipanggil PlayerHealth saat kena hit agar knockback sempat terasa.</summary>
    public void ApplyHitStun(float t) => hitStun = Mathf.Max(hitStun, t);

    IEnumerator DashRoutine()
    {
        canDash = false;
        isDashing = true;

        if (rb != null) rb.linearVelocity = movement * dashSpeed;
        yield return new WaitForSeconds(dashDuration);
        if (rb != null) rb.linearVelocity = Vector2.zero;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}
