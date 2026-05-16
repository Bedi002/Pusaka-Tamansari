using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Wajib ditambahkan untuk memanggil UI TextMeshPro

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;
    
    // Variabel untuk menyambungkan teks UI
    public TextMeshProUGUI healthTextUI; 

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI(); // Panggil saat game mulai
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        UpdateUI(); // Perbarui teks setiap kali kena damage

        if (currentHealth <= 0)
        {
            Destroy(gameObject);
            healthTextUI.text = "GAME OVER"; // Ubah teks kalau mati
        }
    }

    // Fungsi khusus untuk mengubah teks di layar
    void UpdateUI()
    {
        if (healthTextUI != null)
        {
            healthTextUI.text = "HP: " + currentHealth.ToString();
        }
    }
}