using UnityEngine;

/// <summary>
/// Penanda titik spawn musuh. Komponen ini sekarang HANYA penanda posisi:
/// StageManager akan otomatis memakai semua objek ber-komponen Spawner sebagai
/// titik kemunculan musuh bila daftar spawnPoints-nya dibiarkan kosong.
///
/// (Sebelumnya komponen ini mengatur spawn sendiri; orkestrasi wave kini
/// dipindah ke StageManager agar mendukung sistem stage, difficulty, & boss.)
/// </summary>
public class Spawner : MonoBehaviour
{
    public Color gizmoColor = new Color(1f, 0.45f, 0.1f, 0.9f);
    public float gizmoSize = 0.4f;

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoSize);
        Vector3 p = transform.position;
        Gizmos.DrawLine(p + Vector3.up * gizmoSize, p - Vector3.up * gizmoSize);
        Gizmos.DrawLine(p + Vector3.right * gizmoSize, p - Vector3.right * gizmoSize);
    }
}
