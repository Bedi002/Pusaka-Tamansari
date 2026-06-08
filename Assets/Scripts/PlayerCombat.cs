using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Pengaturan Pukulan")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public float attackOffset = 0.6f;
    public LayerMask enemyLayers; 
    public Animator anim;

    [Header("Sistem Combo")]
    public int comboStep = 0; 
    public float maxComboDelay = 1f; // Batas waktu maksimal sebelum combo reset
    public float comboCooldown = 0.4f; // JEDA MINIMAL antar pukulan (Kunci tombol)
    private float lastAttackTime = 0f; 
    private float nextAttackTime = 0f; // Kapan pemain boleh memencet Spasi lagi

    void Update()
    {
        // 1. ATTACK POINT MENGIKUTI ARAH
        float arahX = anim.GetFloat("MoveX");
        float arahY = anim.GetFloat("MoveY");
        Vector2 arahHadap = new Vector2(arahX, arahY).normalized;
        attackPoint.localPosition = arahHadap * attackOffset;

        // 2. RESET COMBO JIKA KELAMAAN DIAM
        if (Time.time - lastAttackTime > maxComboDelay)
        {
            comboStep = 0; 
        }

        // 3. TOMBOL SERANG (Sudah diberi sistem antri/cooldown)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Hanya izinkan memukul jika waktu saat ini sudah melewati waktu jeda
            if (Time.time >= nextAttackTime)
            {
                Attack();
            }
        }
    }

    void Attack()
    {
        lastAttackTime = Time.time;
        // Kunci tombol Spasi selama 0.4 detik ke depan agar tidak bisa di-spam
        nextAttackTime = Time.time + comboCooldown; 
        
        comboStep++; 

        // Tembakkan animasi sesuai urutan combo
        if (comboStep == 1)
        {
            // Reset trigger lain agar tidak menumpuk di memori
            anim.ResetTrigger("Attack2");
            anim.ResetTrigger("Attack3");
            anim.SetTrigger("Attack1"); 
        }
        else if (comboStep == 2)
        {
            anim.ResetTrigger("Attack1");
            anim.ResetTrigger("Attack3");
            anim.SetTrigger("Attack2"); 
        }
        else if (comboStep == 3)
        {
            anim.ResetTrigger("Attack1");
            anim.ResetTrigger("Attack2");
            anim.SetTrigger("Attack3"); 
            comboStep = 0; // Reset ke 0 setelah hantaman terakhir
        }

        // Proses memberi damage
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            Enemy komponenMusuh = enemy.GetComponent<Enemy>();
            if (komponenMusuh != null)
            {
                komponenMusuh.TakeDamage(50); 
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}