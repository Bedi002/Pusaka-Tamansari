using UnityEngine;

/// <summary>
/// Pengelola audio lintas-scene. Punya dua AudioSource: satu untuk musik
/// (loop) dan satu untuk SFX (PlayOneShot). Seret file AudioClip ke slot
/// di Inspector. Aman dipanggil walau clip kosong (akan diabaikan).
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sumber Audio (dibuat otomatis bila kosong)")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("SFX")]
    public AudioClip uiClick;
    public AudioClip playerAttack;
    public AudioClip playerHurt;
    public AudioClip enemyHurt;
    public AudioClip enemyDie;
    public AudioClip bossHurt;
    public AudioClip bossDie;
    public AudioClip stageClear;
    public AudioClip gameOver;
    public AudioClip victory;

    [Header("Musik")]
    public AudioClip menuMusic;
    public AudioClip battleMusic;
    public AudioClip bossMusic;

    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume = 0.55f;
    [Range(0f, 1f)] public float sfxVolume = 0.9f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        if (Instance == null && FindFirstObjectByType<AudioManager>() == null)
        {
            var go = new GameObject("AudioManager (auto)");
            go.AddComponent<AudioManager>();
        }
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null || musicSource == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }
}
