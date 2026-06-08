using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Wajib untuk TextMeshPro

public class PlayerHealth : MonoBehaviour
{
    [Header("Darah Pemain")]
    public int maxHealth = 100;
    private int currentHealth;
    public bool isDead = false; // Mengunci agar tidak memicu fungsi mati berkali-kali

    [Header("Pengaturan Efek Terluka")]
    public float invincibilityDuration = 1.5f; // Durasi kebal setelah kena pukul
    private bool isInvincible = false;

    [Header("Komponen UI, Animasi & Visual")]
    public TextMeshProUGUI healthTextUI; // Slot untuk teks UI HP
    public Animator anim;                // Slot untuk Animator Player
    public SpriteRenderer spriteRend;    // Slot untuk Sprite Renderer (efek kedap-kedip)

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI(); // Tampilkan HP penuh saat game mulai
    }

    public void TakeDamage(int damage)
    {
        // Jika sedang kebal atau sudah mati, abaikan semua serangan musuh
        if (isInvincible || isDead) return;

        currentHealth -= damage;
        
        // Memastikan darah tidak minus di bawah angka 0
        if (currentHealth < 0) currentHealth = 0;
        
        UpdateUI(); // Perbarui teks HP di layar

        if (currentHealth <= 0)
        {
            Die(); // Panggil fungsi mati yang benar
        }
        else
        {
            // Jika masih hidup, putar animasi terluka dan aktifkan efek kebal
            // if (anim != null) anim.SetTrigger("Hurt");
            StartCoroutine(KebalSementara());
        }
    }

    // Fungsi khusus untuk mengatur teks di layar
    void UpdateUI()
    {
        if (healthTextUI != null)
        {
            if (currentHealth <= 0)
            {
                healthTextUI.text = "GAME OVER";
            }
            else
            {
                healthTextUI.text = "HP: " + currentHealth.ToString();
            }
        }
    }

    // Coroutine untuk efek kedap-kedip transparan saat terluka
    IEnumerator KebalSementara()
    {
        isInvincible = true;

        for (int i = 0; i < 3; i++)
        {
            // Berubah jadi warna MERAH saat kena hit
            if (spriteRend != null) spriteRend.color = new Color(1f, 0.3f, 0.3f, 1f); 
            yield return new WaitForSeconds(invincibilityDuration / 6);
            
            // Kembali ke warna NORMAL
            if (spriteRend != null) spriteRend.color = Color.white; 
            yield return new WaitForSeconds(invincibilityDuration / 6);
        }

        isInvincible = false;
    }

    void Die()
    {
        isDead = true;
        Debug.Log("Player Game Over!");

        // 1. Memicu animasi mati (menggunakan aset mati arah bawah yang kamu punya)
        if (anim != null) anim.SetTrigger("Die");

        // 2. MEMATIKAN KONTROL JALAN DAN SERANG
        // Mematikan script pergerakan (WASD + Dash)
        PlayerMovement moveScript = GetComponent<PlayerMovement>();
        if (moveScript != null) moveScript.enabled = false;

        // Mematikan script pertarungan (Spasi + Combo)
        PlayerCombat combatScript = GetComponent<PlayerCombat>();
        if (combatScript != null) combatScript.enabled = false;

        // 3. Menghentikan sisa gaya/kecepatan fisik agar tidak meluncur kaku saat mati
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // CATATAN: Jangan gunakan Destroy(gameObject) di sini agar tubuh karakter 
        // yang terlentang mati tetap terlihat di arena game sebagai pemanis visual.
    }
}