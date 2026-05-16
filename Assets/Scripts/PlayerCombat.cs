using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Pengaturan Pukulan")]
    public Transform attackPoint; // Titik pusat pukulan
    public float attackRange = 0.5f; // Jarak jangkauan pukulan
    public LayerMask enemyLayers; // Deteksi mana yang musuh

    void Update()
    {
        // Tekan Spasi untuk memukul
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Attack();
        }
    }

    void Attack()
    {
        // Mendeteksi musuh
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        // Memberikan damage ke semua musuh yang kena
        foreach (Collider2D enemy in hitEnemies)
        {
            // Mengambil script 'Enemy' dari musuh yang terpukul, lalu panggil fungsi TakeDamage
            // Kita beri nilai damage 50, artinya musuh (HP 100) akan mati dalam 2x pukul
            enemy.GetComponent<Enemy>().TakeDamage(50); 
        }
    }

    // Fungsi tambahan agar kita bisa melihat lingkaran pukulan di layar Unity
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}