using UnityEngine;

/// <summary>
/// Pintu penghubung antar-ruangan. Punya trigger collider. Saat terkunci,
/// 'blocker' (collider solid) aktif menghalangi player. Saat terbuka & player
/// menyentuh trigger, DungeonManager memindah player ke ruangan tujuan.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Door : MonoBehaviour
{
    [Tooltip("Ruangan yang dituju saat pintu dilewati")]
    public Room targetRoom;
    [Tooltip("Posisi player setelah masuk (di sisi ruangan tujuan)")]
    public Transform entryPoint;
    [Tooltip("Objek penghalang fisik saat pintu terkunci")]
    public GameObject blocker;
    [Tooltip("Opsional: visual yang ganti warna saat buka/tutup")]
    public SpriteRenderer visual;
    public Color openColor = new Color(0.90f, 0.75f, 0.30f, 1f);
    public Color lockedColor = new Color(0.40f, 0.20f, 0.20f, 1f);

    bool locked = false;

    void Reset()
    {
        var c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    public void SetLocked(bool value)
    {
        locked = value;
        if (blocker != null) blocker.SetActive(value);
        if (visual != null) visual.color = value ? lockedColor : openColor;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (locked) return;
        if (!other.CompareTag("Player")) return;
        if (DungeonManager.Instance != null && targetRoom != null)
            DungeonManager.Instance.TransitionTo(targetRoom, this);
    }
}
