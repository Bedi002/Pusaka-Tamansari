# Rework Pusaka Tamansari

Catatan perubahan dan cara menjalankannya. Detail desain (palet, lore, tabel stat,
katalog 40 item) ada di `DESIGN_BIBLE.md`.

## Cara membangun ulang (WAJIB dijalankan sekali di Unity)

Satu tombol, urutannya sudah benar:

**Tools > Pusaka > Rebuild EVERYTHING**

Atau bertahap, harus urut:

1. `Tools > Pusaka > Import Forge Characters` - membangun 12 prefab musuh + 5 prefab boss dari sprite Forge.
2. `Tools > Pusaka > Rebuild Content` - membangun 4 aset di `Assets/Resources/`: ArtLibrary, ItemDatabase, HeroCatalog, FloorCatalog.
3. `Tools > Pusaka > Build Dungeon Floors` - membangun scene Floor1..Floor5 dan mendaftarkannya ke Build Settings.

Urutan wajib karena FloorCatalog menunjuk prefab, dan FloorBuilder membaca FloorCatalog.

Mulai main dari scene `MainMenu`.

## Apa yang diperbaiki

**Bug "cuma satu hero yang bisa dipilih".** Akar masalahnya: ketiga hero hanya ada
sebagai referensi yang di-serialize di scene MainMenu, dan kartu pilih hero dipahat
manual sehingga panel hiasan layar penuh menutupi raycast sebagian tombol. Sekarang
hero dibaca dari `HeroCatalog` di Resources (tidak bergantung scene mana pun) dan
kartu pilih hero dirakit runtime dari isi katalog - jumlah kartu selalu mengikuti
jumlah hero, dan tiap tombol membawa index-nya sendiri.

**Bug sprite salah.** `FloorBuilder` lama memakai konstanta tile yang keliru:
`T_TOMB = 62` sebenarnya tile placeholder putih bergaris, `T_BARREL = 65` sebuah
kotak mesin abu-abu. Keduanya muncul di setiap ruangan Combat dan Boss. Seluruh
index tile sekarang terkumpul di `Assets/Scripts/Core/Tiles.cs`, diverifikasi satu
per satu terhadap contact sheet tileset.

**Import art tidak konsisten.** `Assets/Editor/PixelArtImporter.cs` menyeragamkan
otomatis: Point filter, tanpa kompresi, tanpa mipmap; PPU 16 untuk tile Kenney,
PPU 32 untuk karakter Forge. Berlaku juga untuk pack yang ditambahkan nanti.

## Apa yang baru

**Peta prosedural.** `DungeonGenerator` merakit lantai saat runtime, jadi denahnya
berbeda tiap main. Dulu 7 ruangan tetap hasil hardcode; sekarang 13-20 ruangan per
lantai dengan ukuran bervariasi (ruangan tempur 28x19 sampai 38x25, arena boss 42x28).
Tipe ruangan: Start, Combat, Elite, Treasure, Shop, Shrine, Boss.

**Lima lantai** (dulu tiga), masing-masing bertema: Pelataran Beringin, Umbul
Pasiraman, Sumur Gumuling, Pulo Kenongo, Gedhong Pusaka. Lantai terbuka memakai
rumput dan pohon dari Kenney Tiny Town; lantai tertutup memakai batu Tiny Dungeon.

**Musuh.** Dari 7 tipe jadi 12 tipe reguler + 5 boss berbeda per lantai. Sprite
kelas Warrior/Archer/Mage dipakai ulang sebagai abdi keraton yang kerasukan.

**Tas dan item.** 40 item dalam 5 kategori dengan efek bernomor. Tas 24 slot +
4 slot pakai (senjata, zirah, 2 jimat) + pusaka permanen. Buka dengan `Tab` atau `I`.
Item jatuh dari musuh, peti, dan ruangan yang dibersihkan. Isi tas ikut pindah
antar-lantai karena disimpan di GameManager.

**Art tambahan (CC0, Kenney):** `Assets/Art/Kenney/TinyTown` (132 tile: pohon,
semak, jamur, pagar, tanah, batu) dan `Assets/Art/Kenney/UI_RPG` (87 elemen UI).

## Putaran kedua

**Satu hero.** Hanya Senopati yang bisa dimainkan; `Player_Archer.prefab` dan
`Player_Mage.prefab` dihapus. Sprite Archer dan Mage tetap terpakai sebagai musuh
(`Enemy_Archer`, `Enemy_Mage`). MainMenu kini langsung ke DifficultySelect.

**Audio.** `AudioManager` dulu mengandalkan clip yang di-drag manual di Inspector,
dan karena tidak pernah diisi, game bisu total. Sekarang semua clip dimuat otomatis
dari `Assets/Resources/Audio/` dan dikelompokkan per nama dasar: `swing_1`,
`swing_2`, `swing_3` menjadi satu bank "swing" yang dipilih acak dengan sedikit
geseran pitch, supaya pukulan berulang tidak terdengar seperti tombol yang sama.

36 clip terpasang: ayunan senjata, pukulan kena daging dan kena zirah, kritis,
player terluka dan mati, musuh mati, koin, ramuan, peti, pintu, langkah kaki, dan
sembilan bunyi UI. SFX slime mati memakai file Anda (`slime_die_1.mp3`; ekstensi
`.mpeg` diganti karena importer Unity tidak mengenalinya). Sumber lain: pack CC0
Kenney (RPG Audio, Interface Sounds, Impact Sounds).

**Lingkungan yang lebih hidup.** Masalahnya bukan tile-nya jelek, melainkan cara
memakainya. Yang ditambahkan:

- Selubung gelap per lantai di atas lantai (bukan di atas karakter), sehingga
  cahaya obor punya latar untuk dibaca sebagai cahaya.
- Obor memancarkan bola cahaya berwarna sesuai tema lantai, berkedip dengan dua
  gelombang beda frekuensi supaya tidak berdenyut serempak.
- Lapisan bercak lantai: tile varian diputar acak dengan alpha rendah, memecah
  keseragaman lantai polos yang di-tint rata.
- Properti ditabur berumpun, bukan merata, sehingga terbaca sebagai rumpun pohon
  atau tumpukan puing.
- Y-sorting: karakter kini berjalan di belakang pohon dan pilar. Collider properti
  dibuat pendek supaya yang menabrak adalah kaki pohon, bukan pucuknya.

**Warning dibersihkan.** Sebelas warning API usang (`FindFirstObjectByType`,
`FindObjectsSortMode`, `enableWordWrapping`) diperbaiki. Kompilasi kini 0 error,
0 warning untuk 53 script.

**UI bergerak.** Sebelumnya semua layar hanya kotak warna solid yang muncul
seketika. Sekarang ada mesin tween sendiri di `Assets/Scripts/UI/UIMotion.cs`
(tanpa paket eksternal, unscaled time karena tas dan jeda menyetel `timeScale = 0`,
nol alokasi per frame):

- Layar masuk bertahap: judul turun, garis aksen mengembang dari tengah, kartu
  dan panel pop beruntun 60ms, tombol paling akhir. Total di bawah 0.8 detik.
- Tombol hidup: hover terangkat 4px, skala 1.045, aksen emas meluncur naik;
  ditekan menyusut 0.955 dengan snap-back pegas.
- HUD bereaksi: bar HP meluncur bukan meloncat, berkilat putih saat kena pukul,
  bergetar dan berdenyut saat HP di bawah 30%; skor berjalan angka demi angka;
  kartu judul lantai turun perlahan dengan lore diketik 45 huruf per detik.
- Tas terasa berbobot: slot pop bergelombang diagonal, slot terpilih berdenyut,
  panel rincian cross-fade saat pilihan berganti.
- Angka damage melayang di atas target: damage merah, kritis emas lebih besar,
  penyembuhan teal, elakan abu.

## Putaran ketiga: lingkungan dibongkar

Screenshot dari playtest menunjukkan padang oranye datar tanpa tembok. Tiga sebab,
semuanya kesalahan saya:

1. `tile_0048` di Tiny Dungeon adalah kotak tan polos tanpa tekstur. Satu sprite
   ditarik melintasi ruangan 30x20 unit menghasilkan bidang warna, bukan lantai.
   Varian 49 dan 51 hampir identik, jadi lapisan bercak pun tidak terlihat.
2. Ruangan jauh lebih besar dari bingkai kamera, sehingga tembok tidak pernah masuk
   layar dan ruangan terasa tak berbatas.
3. Tembok memakai tint yang sama dengan lantai, jadi menyatu.

**Tileset utama diganti** ke Kenney Roguelike Caves & Dungeons (CC0, 522 tile 16px)
di `Assets/Art/Kenney/RoguelikeDungeon/`. Berbeda dari Tiny Dungeon, tileset ini
punya lantai batu bertekstur, varian retak, air, tembok gelap, pilar, dan brazier
menyala. Tiny Dungeon dan Tiny Town tetap ada untuk ikon item dan properti tanaman.

**Lantai dipanggang jadi satu tekstur per ruangan** (`RoomFloorBaker.cs`). Variasi
per-tile itu yang membuat lantai terbaca sebagai batu, tetapi satu GameObject per
tile berarti 950 objek per ruangan dan 19.000 per lantai. Dipanggang: variasi penuh,
satu SpriteRenderer. Tile sumber di-set Read/Write enabled oleh `PixelArtImporter`.

**Tembok jadi dua lapis** dengan tint 0.42x lantai, plus collider di tepi dalamnya.

**Semua angka tema disetel lewat mockup, bukan ditebak.** `scratchpad/mock.py`
menyusun ruangan memakai tile yang sama dan meniru urutan render Unity
(tint, selubung gelap, cahaya additive, properti), lalu hasilnya diperiksa dengan
mata dan diulang sampai benar. Tiga iterasi: tile kerikil 62-65 dibuang karena
latarnya opak dan tampak seperti lubang di lantai; intensitas cahaya diturunkan
dari 0.85 ke 0.26-0.38 karena versi pertama memucatkan seluruh ruangan; tembok
dipisah jadi lapisan sendiri karena ikut tint lantai membuatnya tak terlihat.
Nilai akhir per lantai ada di `ContentBuilder.BuildFloorCatalog`.

## Putaran keempat: NPC, sumber daya, arsitektur denah

**Mbah Warung dan Sanggar Sesaji.** Ruangan Shop dan Shrine sebelumnya tergenerate
tetapi kosong. Sekarang berisi:

- Warung Lelembut menjual 5 barang yang diundi sekali seumur ruangan dan tidak
  pernah diisi ulang, jadi keputusan membeli punya bobot.
- Sanggar Sesaji memberi berkah dengan bayaran 15 HP **atau** 25 picis. Opsi
  bayar-nyawa membuat sanggar tetap berguna saat kantong kosong.
- Tempa senjata: Wesi Aji dikorbankan, senjata naik tingkat (+2 serangan). Disimpan
  sebagai tingkat, bukan bonus lepas, supaya terbaca sebagai kemajuan dan ikut
  berpindah saat ganti senjata.

**Bonus permanen dipindah ke RunInventory.** Sebelumnya tersimpan di `PlayerStats`,
padahal player di-spawn ulang tiap ganti lantai -- semua berkah sanggar dan tempaan
akan hilang begitu turun lantai. Bug ini ketahuan saat menyambungkan sanggar.

**Aji dan Tenaga jadi sistem nyata**, bukan sekadar bar hiasan: dash memakan Tenaga,
serangan berat (Q atau klik kanan, jangkauan 2x damage 2.5x) memakan Aji. Ketiganya
duduk dalam satu panel berbingkai emas di tengah atas dengan angkanya masing-masing.

**Lima arsitektur denah, satu per lantai.** Gagasannya dipinjam dari RogueElements
(generasi sebagai langkah yang bisa ditukar); library-nya sendiri tidak dipakai
karena model datanya satu peta tile menyambung, sedangkan game ini memakai
ruangan-pulau yang dihubungkan pintu.

| Lantai | Gaya | Sambungan/ruangan | Buntu |
|---|---|---|---|
| Pelataran Beringin | Bercabang | 1.90 | 6 |
| Umbul Pasiraman | Melingkar | 2.01 | 1 |
| Sumur Gumuling | Gua | 2.33 | 6 |
| Pulo Kenongo | Tulang | 1.97 | 8 |
| Gedhong Pusaka | Petak | 2.55 | 7 |

**`Tools > Pusaka > Validate Layouts`** menguji tiap gaya 25 kali: semua ruangan
harus terjangkau dari awal, boss tepat satu, jumlah ruangan dalam 60-140% target.

Validator ini langsung menemukan dua bug. Pertama, gaya Petak meninggalkan 6 dari
16 ruangan terputus -- ruangan boss bisa berada di balik dinding dan lantai tidak
bisa ditamatkan. Kedua, jumlah ruangannya meleset jauh (8, lalu 29, untuk target 20).

Perbaikan pertama salah arah: menambal graf setelah jadi, sehingga penambalan dan
pemangkasan saling meniadakan. Karena satu iterasi lewat Unity makan tiga menit,
logika grafnya di-port ke `mockup_denah.py` dan diuji 300 kali dalam sedetik.
Solusi akhir menjamin dari dua arah alih-alih menambal: pohon rentang acak
(tersambung secara konstruksi), lalu tumbuhkan bila kurang dan pangkas bila lebih,
dengan pembatalan pemangkasan yang memutus lantai.

## Putaran kelima: art AI dari user

User membuat sendiri sprite sheet (gaya Midjourney/niji) dan minta dipakai. Semua
gambar itu OPAK (tanpa alpha) dengan latar seragam + chrome UI, jadi tidak
siap-pakai; tiap sprite dipotong dan latarnya dihapus lewat flood-fill dari tepi
(`scratchpad/cut_items.py`, `cut_props.py`).

**Menu** (`Assets/Art/Generated/BG_MainMenu.png`, `BG_Difficulty.png`): ilustrasi
penuh dipakai langsung sebagai latar layar, dengan tombol tak terlihat di atas
tombol yang sudah tergambar. Posisi tombol diverifikasi lewat overlay hit-box
sebelum jadi kode. `PusakaSceneBuilder.MakeImageBackground` + `MakeInvisibleButton`.

**Ikon item** (`Assets/Art/Generated/Items/`, 11 ikon): keris celestial, tombak,
gada, busur, pedang, perisai Barong, helm Garuda, pauldron Ganesha, selop
bersayap, greaves, jubah gamelan. `ItemDef.iconOverride` (Sprite ter-serialize
ke ItemDatabase) dipetakan ke 11 item di `ContentBuilder.ApplyIcons`.

**Prop dungeon** (`Assets/Art/Generated/Props/`, 14 prop): brazier api, patung
Ganesha, topeng Barong, gamelan, pilar patah, sulur, tulang, sarang laba-laba,
pot, buku, puing, kristal. `PropLibrary` (aset Resources, dibangun
`ContentBuilder.BuildPropLibrary`). DungeonGenerator memakainya: brazier jadi
sumber cahaya tiap ruangan, patung/topeng/gamelan di shrine & boss, dan tiap
arketipe ruangan dapat 1-2 prop detail-tinggi. `PropArt()` menskalakan tiap prop
ke tinggi dunia target (bukan skala tetap) karena ilustrasinya PPU 100.

Semua loader art AI memakai `AssetDatabase.ImportAsset(..., ForceSynchronousImport)`
sebelum load: di batchmode bersih PNG bisa belum terimpor saat metode jalan.

**Yang TIDAK dipakai:** `Dungeon_Environtment.png` (satu adegan isometrik, bukan
tile -- perspektifnya tak cocok peta top-down) dan `Player_Sprites.png` (grid
berlabel tak beraturan, armor kelabu di latar kelabu -- risiko tinggi mengganti
karakter yang sudah beranimasi; ditunda ke pass tersendiri).

## Yang sengaja belum dikerjakan

- Sistem status (burn/poison/slow/stun). Item yang di design bible memakai `on_hit`
  status untuk sementara diberi bonus damage setara; ditandai di komentar
  `ContentBuilder.cs`.
- Verb `aura`, `reveal_map`, `cleanse`, `immune`, `upgrade_weapon` juga diganti efek
  setara terdekat.
- Perilaku khas per musuh (kiting pemanah, blink dukun, ledakan slime api) dan
  mekanik fase boss dari design bible bagian 3.2/3.3 belum diimplementasikan;
  musuh masih memakai AI kejar-dan-pukul yang lama.
- Mekanik khas hero (Amuk, Nyawiji, Aji Sepuh) belum ada.
- Ruangan Shop dan Shrine sudah tergenerate lengkap dengan dekorasinya, tetapi
  belum ada interaksi jual-beli dan berkah.
