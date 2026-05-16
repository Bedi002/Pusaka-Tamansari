using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("Pengaturan Gerak Musuh")]
    public float speed = 2f; // Kecepatan musuh (buat lebih lambat dari Player)
    
    private Transform player; // Target yang akan dikejar

    void Start()
    {
        // Begitu musuh muncul, dia otomatis mencari objek ber-tag "Player" di arena
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        // Jika player masih hidup/ada di arena
        if (player != null)
        {
            // Musuh pelan-pelan bergeser mendekati posisi player
            transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);
        }
    }
    // Fungsi bawaan Unity untuk mendeteksi tabrakan fisik
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Cek apakah yang ditabrak adalah objek ber-tag "Player"
        if (collision.gameObject.CompareTag("Player"))
        {
            // Kurangi darah player sebanyak 20 setiap kali ditabrak
            collision.gameObject.GetComponent<PlayerHealth>().TakeDamage(20);
        }
    }
}