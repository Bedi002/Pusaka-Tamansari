using UnityEngine;

/// <summary>
/// Antarmuka untuk apa pun yang bisa menerima damage (Enemy, Boss, dll).
/// Dipakai PlayerCombat supaya satu serangan bisa mengenai musuh biasa MAUPUN boss.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int damage, Vector2 hitFrom);
}
