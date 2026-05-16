using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnTime = 3f;
    
    [Header("Sistem Ruangan")]
    public int maxEnemies = 3; // Jumlah maksimal musuh dalam 1 ruangan
    public GameObject pintuKeluar; // Objek pintu
    
    private int spawnedCount = 0; // Menghitung musuh yang sudah muncul

    void Start()
    {
        // Sembunyikan pintu di awal game
        if (pintuKeluar != null) pintuKeluar.SetActive(false); 
        
        InvokeRepeating("SpawnEnemy", 1f, spawnTime);
    }

    void SpawnEnemy()
    {
        if (spawnedCount < maxEnemies)
        {
            Instantiate(enemyPrefab, transform.position, Quaternion.identity);
            spawnedCount++;
        }
        else
        {
            // Hentikan spawner jika musuh sudah mencapai batas maksimal
            CancelInvoke("SpawnEnemy"); 
            // Mulai mengecek apakah semua musuh sudah mati
            StartCoroutine(CekRuanganClear()); 
        }
    }

    IEnumerator CekRuanganClear()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f); // Cek setiap 1 detik
            
            // Mencari berapa sisa objek ber-tag "Enemy" di layar
            if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
            {
                Debug.Log("Ruangan Bersih! Pintu Terbuka.");
                if (pintuKeluar != null) pintuKeluar.SetActive(true); // Munculkan pintu
                break; // Hentikan pengecekan
            }
        }
    }
}