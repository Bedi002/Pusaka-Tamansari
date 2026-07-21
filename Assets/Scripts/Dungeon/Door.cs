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

    bool stateKnown = false;

    public void SetLocked(bool value)
    {
        // hanya berbunyi saat status benar-benar berubah, bukan tiap kali
        // ruangan menyegarkan pintunya
        bool changed = !stateKnown || locked != value;
        locked = value;
        stateKnown = true;

        if (blocker != null) blocker.SetActive(value);
        if (visual != null) visual.color = value ? lockedColor : openColor;

        if (changed && AudioManager.Instance != null)
            AudioManager.Instance.Play(value ? "door_close" : "door_open", 0.55f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (locked) return;
        if (!other.CompareTag("Player")) return;
        if (DungeonManager.Instance != null && targetRoom != null)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.Play("door_open", 0.5f);
            DungeonManager.Instance.TransitionTo(targetRoom, this);
        }
    }
}
