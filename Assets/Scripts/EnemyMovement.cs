using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("Pengaturan Gerak Musuh")]
    public float speed = 2f; // Kecepatan musuh
    
    private Transform player; // Target yang akan dikejar
    private PlayerHealth playerHealth; // Mengingat script nyawa player

    void Start()
    {
        // Begitu musuh muncul, dia mencari objek ber-tag "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        
        if (playerObj != null)
        {
            player = playerObj.transform;
            // Langsung simpan script PlayerHealth-nya di awal biar mesin tidak capek mencari
            playerHealth = playerObj.GetComponent<PlayerHealth>(); 
        }
    }

    void Update()
    {
        // Jika player ditemukan DAN status isDead-nya masih false (belum mati)
        if (player != null && playerHealth != null && !playerHealth.isDead)
        {
            // Musuh pelan-pelan bergeser mendekati posisi player
            transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);
        }
        // Kalau isDead bernilai true, kode di atas tidak akan dijalankan sehingga musuh otomatis diam.
    }

    // Fungsi bawaan Unity untuk mendeteksi tabrakan fisik
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Cek apakah yang ditabrak adalah objek ber-tag "Player"
        if (collision.gameObject.CompareTag("Player"))
        {
            // Pastikan tidak mukul "mayat" yang sudah Game Over
            if (playerHealth != null && !playerHealth.isDead)
            {
                playerHealth.TakeDamage(20);
            }
        }
    }
}