using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Status Musuh")]
    public int maxHealth = 100; // Darah maksimal
    private int currentHealth;

    void Start()
    {
        // Saat game mulai, darah musuh penuh
        currentHealth = maxHealth; 
    }

    // Fungsi ini akan dipanggil oleh Player saat memukul
    public void TakeDamage(int damage)
    {
        currentHealth -= damage; // Kurangi darah sesuai damage
        Debug.Log("Darah musuh sisa: " + currentHealth);

        // Cek apakah darah habis
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Musuh Kalah!");
        // Menghancurkan (menghapus) objek musuh dari layar
        Destroy(gameObject); 
    }
}