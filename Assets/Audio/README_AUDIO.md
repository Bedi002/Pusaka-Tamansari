# Audio — cara isi & sumber gratis

Taruh file audio (`.wav`/`.ogg`/`.mp3`) di folder **`Assets/Audio/`** ini, lalu seret
ke slot komponen **AudioManager** (di GameObject AudioManager scene MainMenu).

## Pemetaan slot AudioManager
| Slot | Dipakai saat | Saran |
|---|---|---|
| `uiClick` | klik tombol menu | klik pendek |
| `playerAttack` | player menyerang (Spasi) | swing/whoosh |
| `playerHurt` | player kena hit | grunt pendek |
| `enemyHurt` | musuh kena pukul | impact kecil |
| `enemyDie` | musuh mati | pop/impact |
| `bossHurt` | boss kena pukul | impact berat |
| `bossDie` | boss kalah | ledakan/dramatis |
| `stageClear` | stage selesai | jingle pendek |
| `gameOver` | player mati | nada kalah |
| `victory` | boss kalah / menang | fanfare |
| `menuMusic` | scene MainMenu (loop) | musik tenang |
| `battleMusic` | dalam stage (loop) | musik aksi |
| `bossMusic` | saat boss muncul (loop) | musik tegang |

## Sumber gratis (legal)
- **Kenney** — https://kenney.nl/assets (CC0, bebas total). Cari pack:
  "RPG Audio", "Impact Sounds", "UI Audio", "Music Jingles".
- **freesound.org** — filter lisensi **CC0** untuk SFX satuan.
- **OpenGameArt.org** — filter CC0 / CC-BY untuk SFX & musik.
- **incompetech.com** (Kevin MacLeod) — musik latar, lisensi CC-BY (wajib kredit).
- **Pixabay Audio** — https://pixabay.com/music & /sound-effects (bebas, tanpa atribusi).

> Tips: untuk loop musik, set Import Settings clip → centang **Loop** bila perlu,
> dan untuk SFX pendek set **Load Type = Decompress On Load**.
