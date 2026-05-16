using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Pengaturan Pergerakan")]
    public float moveSpeed = 5f;
    public Rigidbody2D rb;
    private Vector2 movement;

    [Header("Pengaturan Dash")]
    public float dashSpeed = 15f; // Kecepatan saat dash
    public float dashDuration = 0.2f; // Lama dash (sangat singkat)
    public float dashCooldown = 1f; // Jeda waktu bisa dash lagi
    private bool canDash = true; // Syarat bisa dash
    private bool isDashing; // Penanda sedang dash

    void Update()
    {
        if (isDashing) return; // Kalau sedang dash, blokir input lain sementara

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;

        // Jika tombol Shift ditekan dan dash sedang tidak cooldown
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(DashRoutine());
        }
    }

    void FixedUpdate()
    {
        if (isDashing) return; // Kalau sedang dash, biarkan fisika dash yang bekerja
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    // Sistem waktu untuk Dash
    IEnumerator DashRoutine()
    {
        canDash = false;
        isDashing = true;
        
        // Melesat ke arah karakter sedang berjalan
        rb.linearVelocity = movement * dashSpeed; 
        
        yield return new WaitForSeconds(dashDuration); // Tunggu sekian detik
        
        rb.linearVelocity = Vector2.zero; // Berhenti melesat
        isDashing = false;
        
        yield return new WaitForSeconds(dashCooldown); // Tunggu masa cooldown
        canDash = true; // Bisa dash lagi
    }
}