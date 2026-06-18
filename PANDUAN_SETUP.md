# Panduan Setup — Pusaka Tamansari (folder DEV)

Dokumen ini menjelaskan cara merangkai (wiring) di Unity Editor untuk semua script
baru yang sudah ditulis. Kode sudah jadi; tinggal sambungkan di editor.

> Buka folder **`Pusaka Tamansari - DEV`** lewat Unity Hub (bukan folder asli).
> Tunggu Unity selesai regen Library, pastikan **Console = 0 error** dulu.

---

## 0) Peta script (sudah ada di `Assets/Scripts/`)
- **Core/**: `GameManager`, `AudioManager`, `Difficulty`, `IDamageable`
- **Stage/**: `StageManager`
- **Enemies/**: `Boss`, `Projectile`
- **UI/**: `HUDController`, `MainMenuController`, `DifficultySelectController`, `PauseMenuController`, `EndScreenController`
- **(root Scripts)**: `PlayerMovement`, `PlayerCombat`, `PlayerHealth`, `Enemy`, `EnemyMovement`, `Spawner`

---

## 1) Bootstrap: GameManager + AudioManager (sekali saja)
1. Buka scene **MainMenu** (dibuat di langkah 3).
2. Buat GameObject kosong → namai **`GameManager`** → Add Component **GameManager**.
   - Isi **Stage Scenes** = `Stage1`, `Stage2`, `Stage3` (urutan = urutan main).
   - `mainMenuScene`/`difficultyScene`/`victoryScene`/`gameOverScene` sudah default; samakan dgn nama scene-mu.
3. Buat GameObject kosong → **`AudioManager`** → Add Component **AudioManager**.
   - Slot AudioSource boleh dikosongkan (dibuat otomatis). Seret AudioClip ke slot SFX/Musik (lihat `Assets/Audio/README_AUDIO.md`).
> Keduanya `DontDestroyOnLoad` → cukup ada di MainMenu, otomatis ikut ke scene lain.

---

## 2) Tag, Layer & Build Settings
- **Player** GameObject → Tag = `Player`.
- **Enemy & Boss** prefab → Tag = `Enemy`, Layer = `Enemy`.
- **File ▸ Build Settings ▸ Add Open Scenes** untuk SEMUA scene, urutan:
  `MainMenu (0)`, `DifficultySelect (1)`, `Stage1`, `Stage2`, `Stage3`, `Victory`, `GameOver`.

---

## 3) Scene Menu (MainMenu, DifficultySelect, Victory, GameOver)
Untuk tiap scene: **GameObject ▸ UI ▸ Canvas** (otomatis bikin EventSystem).

### MainMenu
- Tambah Canvas: Judul (TMP Text) + tombol **Play** + tombol **Quit**.
- GameObject kosong **`Menu`** → Add Component **MainMenuController**.
- Tombol Play → OnClick → `MainMenuController.PlayGame`. Quit → `QuitGame`.

### DifficultySelect
- Canvas: 3 tombol **Easy / Normal / Hard**.
- GameObject **`DifficultyMenu`** → **DifficultySelectController**.
- Easy→`SelectEasy`, Normal→`SelectNormal`, Hard→`SelectHard`.

### Victory & GameOver
- Canvas: teks judul (mis. "MENANG!" / "GAME OVER"), teks **Skor** (TMP), tombol **Retry** + **Main Menu**.
- GameObject **`EndScreen`** → **EndScreenController**; assign `scoreText`.
- Retry→`Retry`, Main Menu→`MainMenu`.

---

## 4) Scene Stage (Stage1, Stage2, Stage3)
1. Duplikat **SampleScene** → rename **Stage1**, lalu duplikat lagi jadi Stage2 & Stage3.
2. Tiap stage harus punya: **Player**, arena/tilemap + dinding (Collider2D), dan:

### a. StageManager
- GameObject kosong **`StageManager`** → Add Component **StageManager**.
- Assign **Enemy Prefab** (`Assets/Prefabs/Enemy.prefab`).
- **Boss Prefab**: HANYA di stage terakhir (Stage3). Kosongkan di Stage1/2.
- **Spawn Points**: buat beberapa GameObject kosong di pinggir arena, beri masing-masing komponen **Spawner**, lalu seret ke list `spawnPoints` — ATAU biarkan kosong & StageManager akan otomatis pakai semua objek ber-`Spawner`.
- Atur `waveCount`, `baseEnemiesPerWave`, dll sesuai selera (sudah ada default).

### b. HUD
- **GameObject ▸ UI ▸ Canvas** → namai **HUD** → Add Component **HUDController**.
- Buat & assign: `healthSlider` (Slider), `stageText`/`waveText`/`scoreText` (TMP),
  `bossBarRoot` (panel berisi `bossSlider` + `bossNameText`), `centerMessage` (TMP besar di tengah).
- `bossBarRoot` biarkan aktif di editor (script menyembunyikannya saat start).

### c. Pause
- GameObject **`PauseMenu`** → **PauseMenuController**; buat panel UI (tombol Resume/Restart/Quit) → assign ke `pausePanel`.
- Resume→`Resume`, Restart→`RestartStage`, Quit→`QuitToMenu`.

---

## 5) Player (assign field di Inspector)
Di GameObject Player:
- **PlayerMovement**: `rb` (Rigidbody2D), `anim` (Animator) — auto-isi via Awake, tapi cek.
- **PlayerCombat**: `attackPoint` (child Transform), `anim`, `enemyLayers` = layer `Enemy`, `attackDamage`.
- **PlayerHealth**: `anim`, `rb`, `spriteRend` (auto-isi bila kosong). `healthTextUI` opsional (HUD slider sudah menangani HP).

> Karena beberapa field diperbarui, cek ulang assignment yang mungkin ter-reset.

---

## 6) Enemy prefab (`Enemy.prefab`)
- Pastikan ada: **Enemy**, **EnemyMovement**, **Animator**, **Rigidbody2D** (Gravity 0), **Collider2D**, **SpriteRenderer**.
- **EnemyMovement** → `enemyMask` = layer `Enemy` (untuk anti-numpuk).
- Atur `maxHealth`, `attackDamage`, `scoreReward`, `attackRange`, `attackCooldown`.

---

## 7) Boss prefab (untuk Stage3)
1. Duplikat `Enemy.prefab` → **`Boss.prefab`**. Ganti sprite/animator (lebih besar).
2. Hapus **EnemyMovement** & **Enemy**, lalu Add Component **Boss**.
3. Set: `bossName`, `maxHealth` besar (mis. 1500), `meleeDamage`, ambang fase, dll.
4. (Opsional) **Projectile**: buat prefab bola → Add **Projectile** + Rigidbody2D (Kinematic) + Collider2D (IsTrigger) → assign ke `projectilePrefab` di Boss.
5. (Opsional) `minionPrefab` = `Enemy.prefab` untuk summon di fase 3.
6. Assign **Boss.prefab** ke `bossPrefab` di StageManager Stage3.

---

## 8) Animator — parameter yang dipakai script
Tambahkan jika belum ada (Window ▸ Animation ▸ Animator):
- **Player**: `MoveX` (Float), `MoveY` (Float), `Speed` (Float), `Attack1/2/3` (Trigger), `Die` (Trigger).
- **Enemy & Boss**: `MoveX`, `MoveY`, `Speed` (Float), `Attack` (Trigger), `Die` (Trigger).
> Script aman walau parameter belum lengkap (pakai pengecekan), tapi animasi terkait tak akan jalan sampai parameter ada.

---

## 9) Sistem Kesulitan (Easy / Normal / Hard)
Otomatis lewat `DifficultyProfile` (`Core/Difficulty.cs`) × scaling stage (GameManager `stageGrowth`).
Tweak angka pengali langsung di `Difficulty.cs` bila ingin lebih seimbang.

---

## Verifikasi cepat
1. Play dari **MainMenu** → Play → pilih **Easy** → masuk Stage1.
2. Musuh muncul per-wave, mengejar & menyerang dari jarak, tidak menumpuk; HUD update.
3. Bersihkan semua wave → "STAGE CLEAR" → otomatis Stage2 → Stage3.
4. Stage3: setelah wave → Boss + boss bar; fase berubah saat HP turun; boss mati → **Victory** + skor.
5. Mati → **GameOver** → Retry mulai dari Stage1 (difficulty tetap).
6. Coba **Hard**: musuh jelas lebih banyak/kuat/cepat.
7. **Esc** → pause (game beku).
