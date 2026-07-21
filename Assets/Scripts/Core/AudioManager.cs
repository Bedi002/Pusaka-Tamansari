using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pengelola audio lintas-scene.
///
/// Dulu semua clip harus di-drag manual ke slot Inspector, dan karena tidak pernah
/// diisi, game praktis bisu. Sekarang seluruh clip dimuat otomatis dari
/// Assets/Resources/Audio saat Awake, dikelompokkan berdasarkan nama dasar:
/// swing_1/swing_2/swing_3 menjadi satu bank "swing" yang dipilih acak.
/// Variasi acak + sedikit geseran pitch inilah yang membuat pukulan berulang
/// tidak terdengar seperti tombol yang sama ditekan terus-menerus.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sumber Audio (dibuat otomatis)")]
    public AudioSource musicSource;

    [Tooltip("Beberapa source supaya suara yang menumpuk tidak saling memotong")]
    public int sfxVoices = 8;

    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.85f;

    // ---- slot lama: tetap ada supaya semua pemanggil yang sudah ada ikut berbunyi
    [HideInInspector] public AudioClip uiClick, playerAttack, playerHurt;
    [HideInInspector] public AudioClip enemyHurt, enemyDie, bossHurt, bossDie;
    [HideInInspector] public AudioClip stageClear, gameOver, victory;
    [HideInInspector] public AudioClip menuMusic, battleMusic, bossMusic;

    readonly Dictionary<string, List<AudioClip>> banks = new Dictionary<string, List<AudioClip>>();
    AudioSource[] voices;
    int nextVoice;

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

        voices = new AudioSource[Mathf.Max(2, sfxVoices)];
        for (int i = 0; i < voices.Length; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            // SFX UI tidak boleh ikut berhenti saat game di-pause (timeScale 0)
            src.ignoreListenerPause = true;
            voices[i] = src;
        }

        LoadBanks();
        EnsureSingleListener();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureSingleListener();

    /// <summary>
    /// Satu-satunya AudioListener di seluruh game hidup di objek ini.
    ///
    /// Sebelumnya listener menempel di kamera tiap scene, dan scene menu
    /// (MainMenu, GameOver, Victory, CharacterSelect, DifficultySelect) sama
    /// sekali tidak punya -- Unity membanjiri console dengan peringatan dan
    /// TIDAK ADA suara sama sekali di layar-layar itu. Karena AudioManager
    /// bertahan lintas-scene, menaruh listener di sini menjamin jumlahnya
    /// selalu tepat satu, di scene mana pun.
    /// </summary>
    void EnsureSingleListener()
    {
        var keep = GetComponent<AudioListener>();
        if (keep == null) keep = gameObject.AddComponent<AudioListener>();

        var all = FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
        foreach (var l in all)
            if (l != null && l != keep) Destroy(l);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        if (Instance == null && FindAnyObjectByType<AudioManager>() == null)
        {
            var go = new GameObject("AudioManager (auto)");
            go.AddComponent<AudioManager>();
        }
    }

    // ------------------------------------------------------------------ memuat
    void LoadBanks()
    {
        var clips = Resources.LoadAll<AudioClip>("Audio");
        foreach (var clip in clips)
        {
            if (clip == null) continue;
            string key = BaseName(clip.name);
            if (!banks.TryGetValue(key, out var list))
            {
                list = new List<AudioClip>();
                banks[key] = list;
            }
            list.Add(clip);
        }

        if (clips.Length == 0)
            Debug.LogWarning("AudioManager: tak ada clip di Resources/Audio. Game akan bisu.");

        // isi slot lama supaya kode yang memanggilnya tetap bersuara
        uiClick = First("ui_click");
        playerAttack = First("swing");
        playerHurt = First("player_hurt");
        enemyHurt = First("hit_flesh");
        enemyDie = First("enemy_die");
        bossHurt = First("hit_metal");
        bossDie = First("boss_die");
        stageClear = First("room_clear");
        gameOver = First("player_die");
        victory = First("victory");

        // Musik dimuat lewat konvensi nama, jadi menambah lagu cukup dengan
        // menaruh berkas di Resources/Audio -- tanpa menyentuh kode atau Inspector.
        battleMusic = First("music_battle");
        menuMusic = First("music_menu") ?? battleMusic;      // lebih baik satu lagu daripada senyap
        bossMusic = First("music_boss") ?? battleMusic;

        if (battleMusic == null)
            Debug.Log("AudioManager: belum ada musik latar. Taruh music_battle.ogg " +
                      "(dan music_menu / music_boss) di Assets/Resources/Audio.");
    }

    /// <summary>"swing_2" -> "swing". Angka di belakang adalah nomor variasi.</summary>
    static string BaseName(string name)
    {
        int i = name.LastIndexOf('_');
        if (i <= 0) return name;
        for (int k = i + 1; k < name.Length; k++)
            if (!char.IsDigit(name[k])) return name;
        return name.Substring(0, i);
    }

    AudioClip First(string key)
        => banks.TryGetValue(key, out var list) && list.Count > 0 ? list[0] : null;

    // ----------------------------------------------------------------- memutar
    /// <summary>Mainkan satu variasi acak dari sebuah bank, dengan pitch sedikit bergeser.</summary>
    public void Play(string key, float volumeScale = 1f, float pitchJitter = 0.08f)
    {
        if (!banks.TryGetValue(key, out var list) || list.Count == 0) return;
        PlayClip(list[Random.Range(0, list.Count)], volumeScale, pitchJitter);
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f) => PlayClip(clip, volumeScale, 0.05f);

    void PlayClip(AudioClip clip, float volumeScale, float pitchJitter)
    {
        if (clip == null || voices == null) return;

        var src = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Length;

        src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        src.PlayOneShot(clip, Mathf.Clamp01(sfxVolume * volumeScale));
    }

    /// <summary>Apakah sebuah bank tersedia (untuk fallback pemanggil).</summary>
    public bool Has(string key) => banks.ContainsKey(key) && banks[key].Count > 0;

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
