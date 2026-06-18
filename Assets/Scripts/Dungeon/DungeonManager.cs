using UnityEngine;

/// <summary>
/// Pengatur 1 lantai dungeon: kumpulan Room, ruangan aktif, perpindahan player
/// antar-ruangan via Door, dan kamera yang "snap" ke ruangan aktif. Boss kalah
/// atau ruangan exit -> lanjut lantai berikut (GameManager.AdvanceStage()).
/// </summary>
public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance { get; private set; }

    [Header("Referensi")]
    public Room startRoom;
    public Transform player;
    public Camera cam;

    [Header("Kamera")]
    public float cameraLerp = 8f;
    public float cameraZ = -10f;
    public bool fitCameraToRoom = true;

    Room[] rooms;
    Room current;

    void Awake()
    {
        Instance = this;
        rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
        if (cam == null) cam = Camera.main;
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        if (startRoom == null && rooms != null)
            foreach (var r in rooms) if (r.type == RoomType.Start) { startRoom = r; break; }
        if (startRoom == null && rooms != null && rooms.Length > 0) startRoom = rooms[0];

        if (HUDController.Instance != null && GameManager.Instance != null)
        {
            HUDController.Instance.SetStage(GameManager.Instance.StageNumber, GameManager.Instance.TotalStages);
            HUDController.Instance.SetScore(GameManager.Instance.score);
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(AudioManager.Instance.battleMusic);

        if (MinimapController.Instance != null) MinimapController.Instance.Build(rooms, startRoom);

        if (startRoom != null)
        {
            if (player != null) player.position = startRoom.Center;
            EnterRoom(startRoom, true);
        }
    }

    void LateUpdate()
    {
        if (cam == null || current == null) return;
        Vector3 target = new Vector3(current.Center.x, current.Center.y, cameraZ);
        cam.transform.position = Vector3.Lerp(cam.transform.position, target, Time.deltaTime * cameraLerp);
    }

    public void EnterRoom(Room room, bool snap = false)
    {
        current = room;
        if (cam != null)
        {
            if (snap) cam.transform.position = new Vector3(room.Center.x, room.Center.y, cameraZ);
            if (fitCameraToRoom && cam.orthographic)
                cam.orthographicSize = Mathf.Max(room.size.y, room.size.x * 0.6f) * 0.5f + 0.5f;
        }
        room.OnPlayerEnter();
        if (MinimapController.Instance != null) MinimapController.Instance.SetCurrent(room);
    }

    public void TransitionTo(Room room, Door via)
    {
        if (player != null)
            player.position = (via != null && via.entryPoint != null) ? via.entryPoint.position : room.Center;
        EnterRoom(room);
    }

    public void OnRoomCleared(Room room)
    {
        if (MinimapController.Instance != null) MinimapController.Instance.MarkCleared(room);
    }

    public void OnBossDefeated()
    {
        if (HUDController.Instance != null)
        {
            HUDController.Instance.HideBossBar();
            HUDController.Instance.ShowMessage("MENANG!", 2.5f);
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.victory);
        Invoke(nameof(NextFloor), 2.5f);
    }

    /// <summary>Dipanggil ruangan exit (lantai non-final) atau setelah boss (final).</summary>
    public void GoToNextFloor() => NextFloor();

    void NextFloor()
    {
        if (GameManager.Instance != null) GameManager.Instance.AdvanceStage(); // -> floor berikut / Victory bila terakhir
    }
}
