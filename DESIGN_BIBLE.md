# PUSAKA TAMANSARI — Design Bible

Version 1.0 — creative and data layer. All numbers are final and internally balanced; an engineer can transcribe every table directly into ScriptableObjects or plain C# data.

**Premise.** Patih Kelam, wazir keraton yang murtad, mencuri pusaka-pusaka agung dan menenggelamkan taman air Tamansari ke dalam wengi (malam) abadi. Pemain memilih satu ksatria, turun lima lantai menembus pelataran, kolam pemandian, sumur bawah tanah, dan taman abu, untuk merebut kembali pusaka dan menumbangkan sang patih.

**Ground truth assets** (do not invent beyond these):
- Animated 64x64 characters: `Assets/Sprites/Forge/<Name>/` with `_idle _walk _attack _hurt _death`. Names: Warrior, Archer, Mage, Orc, Vampire, Golem, Plant, Demon, DreadKnight, Slime_Fire, Slime_Ice, Slime_Poison. Shared blob shadow: `Assets/Sprites/Forge/_shadow.png`.
- Kenney Tiny Dungeon 16px tiles: `Assets/Art/Kenney/TinyDungeon/Tiles/tile_0000.png` … `tile_0131.png` (132 tiles, verified on disk).
- Upcoming Kenney CC0 packs under `Assets/Art/Kenney/`: generic trees, bushes, rocks, fences, crates, lamps, water, flowers, town props, RPG UI.

Units convention: 1 unit = 1 tile (16px world tile). Player reference: 100 hp, ~20 damage per hit, movespeed 3.0 units/s.

---

## 1. Palette

Base colors already locked: BG `#0F0F1A`, gold `#E3B341`, teal `#2A9D8F`, ember `#E76F51`. Everything below extends them. Floor/wall tints are multiplied over the (brown/grey) Kenney tiles via SpriteRenderer color, so they are mid-value; the darkness comes from `bg_night` behind everything plus the per-floor ambient overlay.

### 1.1 Core

| name | hex | usage |
|---|---|---|
| bg_night | #0F0F1A | camera clear color, void outside rooms, vignette |
| gold_pusaka | #E3B341 | headers, score, top rarity, selected UI, key highlights |
| teal_tirta | #2A9D8F | water, XP bar, secondary accent, visited minimap rooms |
| ember_bara | #E76F51 | danger, fire VFX, boss health bar, warnings |

### 1.2 UI surfaces and text

| name | hex | usage |
|---|---|---|
| ui_surface | #181826 | panel backgrounds |
| ui_surface_raised | #232336 | slots, buttons, tooltips |
| ui_border | #3A3A55 | default 2px borders |
| ui_border_active | #E3B341 | selected/hovered border |
| ui_text_primary | #E8E2D0 | body text (warm parchment white) |
| ui_text_secondary | #9C96B0 | labels, flavor text, hints |
| ui_text_disabled | #55516B | inactive options, version string |
| ui_overlay | #0F0F1A @ 70% alpha | pause/inventory dim layer |
| ui_hp_fill | #D64545 | player health bar fill |
| ui_hp_low | #F25C4A | health bar flash below 25% |
| ui_xp_fill | #2A9D8F | XP bar fill |

### 1.3 Rarity tiers (5)

| tier (display) | engineering id | hex |
|---|---|---|
| Biasa | common | #B8B5C4 |
| Pilihan | uncommon | #57C785 |
| Langka | rare | #4FA3D1 |
| Agung | epic | #A66BD4 |
| Pusaka | legendary | #E3B341 |

### 1.4 Combat floating text

| name | hex | usage |
|---|---|---|
| txt_dmg_dealt | #F2F0E6 | player damage to enemies (VT323, 22px) |
| txt_dmg_taken | #F25C4A | damage the player receives |
| txt_crit | #FFD95E | critical hits (VT323, 30px, slight pop-scale) |
| txt_heal | #57C785 | healing numbers |
| txt_poison | #9BC53D | poison ticks |
| txt_burn | #E76F51 | burn ticks |
| txt_slow | #7FD8E8 | slow/freeze status text |
| txt_xp | #C9A7F0 | xp/score pickup text |

### 1.5 Floor and wall tints (multiplied over tiles)

| name | hex | floor |
|---|---|---|
| tint_floor_lumut | #A3B18A | 1 — Pelataran Beringin |
| tint_wall_lumut | #75855F | 1 |
| tint_floor_tirta | #8FB8B2 | 2 — Umbul Pasiraman |
| tint_wall_tirta | #5F8A88 | 2 |
| tint_floor_gua | #948FA8 | 3 — Sumur Gumuling |
| tint_wall_gua | #6A657F | 3 |
| tint_floor_awu | #A89490 | 4 — Pulo Kenongo |
| tint_wall_awu | #7A6763 | 4 |
| tint_floor_gedhong | #A99878 | 5 — Gedhong Pusaka |
| tint_wall_gedhong | #6E6250 | 5 |

---

## 2. Five Floors

Room-count budget per floor: 1 Start, 6-9 Combat, 1-2 Elite, 1 Treasure, 1 Shrine, 1 Shop, 1 Boss (see section 6). "Density" below = average decorative props per combat room (a room is roughly 14x10 tiles).

### Floor 1 — Pelataran Beringin (The Banyan Forecourt)

- Mood: senja keemasan yang membusuk — sisa festival yang ditinggalkan begitu saja.
- Biome: banyan-root courtyards. Overgrown outdoor court; dirt floors (tiles 000/012/024, tan 048-053), low stone walls, Kenney nature/town props dominate.
- Floor tint: `#A3B18A` - Wall tint: `#75855F` - Ambient overlay: `#2B2138` @ 30% alpha.
- Prop mix (density 8-12/room): trees + banyan roots heavy (3-4), bushes/flowers medium (3-4), crates + fences light (2-3), unlit lanterns light (1), scurrying ambient rats/bats (tiles 120/124, non-combat, 0-2).
- Enemies: Jenang Upas, Sulur Demit, Prajurit Sukma.
- Boss: **Buto Ijo, Penunggu Beringin** (Orc).
- Lore card: "Dulu pelataran ini riuh oleh gamelan dan pedagang sekar. Kini akar beringin mencekik gapura, dan prajurit yang telah gugur masih berbaris menjaga tuan yang lama pergi."

### Floor 2 — Umbul Pasiraman (The Royal Bathing Springs)

- Mood: bulan memantul di air yang tidak pernah tenang.
- Biome: flooded bathing pools. Tan sandstone floors (048-053), pool basins of Kenney water tiles, working fountains (tiles 008/020/032/043/044), stone pillars (006).
- Floor tint: `#8FB8B2` - Wall tint: `#5F8A88` - Ambient overlay: `#12283A` @ 35% alpha. Water uses `teal_tirta` with highlight `#58C8BA`.
- Prop mix (density 7-10/room): water pools heavy (1 large pool most rooms), fountains medium (1-2), pillars medium (2), flowers/lily props light (2-3), barrels light (1).
- Enemies: Jenang Tirta, Telik Sandi, Prajurit Sukma, Jenang Upas (reduced weight).
- Boss: **Reco Pentung, Arca Pasiraman** (Golem) — the bathing pools' guardian statue, woken.
- Lore card: "Di umbul ini para putri dahulu membasuh diri di bawah songsong emas. Airnya tidak pernah surut — ia mengingat setiap wajah yang pernah tenggelam di dalamnya."

### Floor 3 — Sumur Gumuling (The Coiling Well)

- Mood: gema tanpa sumber di lorong yang melingkar turun.
- Biome: underground tunnels/gua. Grey brick walls (057-059), plank/slab floors (036-039), ladders (067/068), minecart and rails (054, 069-071, 079-081, 093-095), ore nodes (102), torches (125-128).
- Floor tint: `#948FA8` - Wall tint: `#6A657F` - Ambient overlay: `#0F0F1A` @ 45% alpha (darkest floor; torch light radius matters).
- Prop mix (density 5-8/room): rubble/rocks medium (2-3), rails + carts light (0-2), torches medium (2, the main light source), tomb reliefs (041) light (0-1), ambient spiders (122) 0-2.
- Enemies: Buto Ijo, Dukun Kelam, Jenang Tirta, Telik Sandi.
- Boss: **Pangeran Wengi, Sang Bangsawan Pucat** (Vampire) — a princeling entombed beneath the winding stair.
- Lore card: "Sumur berpilin ini dahulu tempat doa dipanjatkan, tangganya melingkar seperti tasbih batu. Kini doa-doa itu berbalik arah, dan sesuatu yang haus menunggu di dasar lingkarannya."

### Floor 4 — Pulo Kenongo (The Ashen Isle of Kenanga)

- Mood: taman yang terbakar namun tidak pernah selesai padam.
- Biome: ash gardens. Cracked floors (042, 024), burnt Kenney trees (dead/bare variants), ember particle drift, demon-face walls (019), flame emblems (029).
- Floor tint: `#A89490` - Wall tint: `#7A6763` - Ambient overlay: `#3A1A14` @ 30% alpha; ember particles use `ember_bara`.
- Prop mix (density 6-9/room): burnt trees/stumps heavy (3-4), ash-grey bushes medium (2), broken fences light (1-2), flame emblems + demon-face wall decor medium (1-2 wall pieces), smoldering ground decals (ember tint) 1-2.
- Enemies: Jenang Bara, Reco Pentung, Buto Ijo, Dukun Kelam, Telik Sandi.
- Boss: **Genderuwo, Raja Awu Kenongo** (Demon).
- Lore card: "Pulo ini dahulu harum oleh kembang kenanga yang mekar untuk sultan. Patih Kelam membakarnya guna menutup jejak, tetapi abunya menolak menjadi dingin — dan sesuatu yang besar kini bersarang di kebun yang hangus."

### Floor 5 — Gedhong Pusaka (The Vault of Heirlooms)

- Mood: keagungan yang dijaga oleh pengkhianatnya sendiri.
- Biome: the pusaka vault. Grey slab floors (036-039) gold-tinted, brick walls, chests everywhere (089-091), tomb reliefs (041), candles (125-128), banners/emblems (029), gilded doors (046-047).
- Floor tint: `#A99878` - Wall tint: `#6E6250` - Ambient overlay: `#1A1426` @ 35% alpha; gold rim-light accents `gold_pusaka`.
- Prop mix (density 7-10/room): chests + cabinets heavy (2-3, mostly decorative/locked), candlesticks medium (2-3), banners/reliefs medium (1-2), tables with displayed weapons (072 + weapon tiles) light (1), no nature props.
- Enemies: Pangeran Wengi (elite), Genderuwo (elite), Reco Pentung, Bregada Kelam (Prajurit Sukma elite variant: 2x hp, 1.5x damage, tinted `#6E6250`), Dukun Kelam, Jenang Bara.
- Boss: **Patih Kelam, Pengkhianat Keraton** (DreadKnight).
- Lore card: "Inilah gedhong tempat pusaka keraton disemayamkan, kini penuh oleh barang curian yang menolak tuan barunya. Di tengah ruangan, sang patih menunggu — sebab pusaka sejati tidak dapat dicuri, hanya dapat direbut kembali."

---

## 3. Enemy Roster

All 12 Forge creature sprites are used: the three humanoid class sprites double as possessed palace servants. Baseline: player 100 hp, ~20 damage/hit, movespeed 3.0. `cd` = attack cooldown in seconds, `spd` = movespeed units/s, `range` in units. `xp` doubles as score value.

### 3.1 Regular stats

| sprite | display name | role | hp | dmg | spd | range | cd | xp | floors |
|---|---|---|---|---|---|---|---|---|---|
| Slime_Poison | Jenang Upas | swarm | 28 | 6 | 2.0 | 0.8 | 1.4 | 5 | 1-2 |
| Slime_Ice | Jenang Tirta | swarm | 34 | 8 | 1.9 | 0.8 | 1.4 | 8 | 2-3 |
| Slime_Fire | Jenang Bara | exploder | 26 | 5 | 2.5 | 0.9 | 1.0 | 10 | 4-5 |
| Warrior | Prajurit Sukma | chaser | 55 | 10 | 2.6 | 1.2 | 1.5 | 12 | 1-3, 5 (elite variant) |
| Archer | Telik Sandi | ranged | 40 | 9 | 2.4 | 7.0 | 2.2 | 14 | 2-5 |
| Mage | Dukun Kelam | ranged | 45 | 14 | 2.0 | 8.0 | 3.0 | 18 | 3-5 |
| Plant | Sulur Demit | turret | 60 | 11 | 0.0 | 6.0 | 2.5 | 12 | 1-2 |
| Orc | Buto Ijo | tank | 120 | 18 | 2.1 | 1.5 | 2.0 | 25 | 3-4, elite room on 2 (boss on 1) |
| Golem | Reco Pentung | tank | 160 | 20 | 1.5 | 1.5 | 2.4 | 30 | 4-5, elite room on 3 (boss on 2) |
| Vampire | Pangeran Wengi | elite | 90 | 15 | 3.2 | 1.2 | 1.8 | 40 | 5, elite room on 4 (boss on 3) |
| Demon | Genderuwo | elite | 140 | 22 | 2.8 | 1.4 | 2.0 | 50 | 5 (boss on 4) |
| DreadKnight | Patih Kelam | boss | 1400 | 30 | 2.7 | 1.6 | 1.7 | 600 | 5 (final boss only) |

### 3.2 Distinguishing behaviors (each codeable in a few lines)

| enemy | behavior |
|---|---|
| Jenang Upas | Hits apply `poison 3 dmg/s for 3s`. Wanders randomly; aggros only within 5 units. |
| Jenang Tirta | Hits apply `slow 30% for 2s`. Otherwise identical logic to Jenang Upas. |
| Jenang Bara | Rushes player; at distance < 1.2 flashes for 0.6s then explodes: 22 dmg in radius 1.5, dies. Exploding on death too (same blast) if killed while flashing. |
| Prajurit Sukma | 0.3s windup pause before each swing (telegraph). Simple chase otherwise. |
| Telik Sandi | Kites: if player closer than 3.5 units, retreats directly away at full spd; shoots only when 3.5-7.0. |
| Dukun Kelam | Slow bolt (projectile speed 4.5). Blinks 4 units in a random direction when player comes within 2.0 (blink cooldown 4s). |
| Sulur Demit | Rooted, never moves. At range < 3.0 fires a 3-spore spread (30 degrees apart) instead of a single spore. |
| Buto Ijo | Swing knocks the player back 2 units. |
| Reco Pentung | Flat armor: reduces every incoming hit by 4 damage (minimum 1). |
| Pangeran Wengi | Heals for 50% of damage dealt. Dashes 4 units toward the player every 5s. |
| Genderuwo | Enrage below 50% hp: +40% attack speed and +20% movespeed until death. |
| Patih Kelam | Three phases, see boss table. |

### 3.3 Boss versions

Bosses reuse the same sprite at 1.5x scale, with these overridden stats. Boss rooms lock their doors until the boss dies.

| floor | boss | sprite | hp | dmg | spd | cd | xp | mechanics |
|---|---|---|---|---|---|---|---|---|
| 1 | Buto Ijo, Penunggu Beringin | Orc | 500 | 20 | 2.3 | 2.0 | 150 | Slam knockback 2.5 units. Below 50% hp: summons 3 Jenang Upas every 12s. |
| 2 | Reco Pentung, Arca Pasiraman | Golem | 650 | 24 | 1.7 | 2.4 | 200 | Flat armor 5/hit. Every 10s: ground slam, shockwave radius 3.0, 15 dmg, 1s telegraph ring. |
| 3 | Pangeran Wengi, Sang Bangsawan Pucat | Vampire | 800 | 22 | 3.4 | 1.6 | 300 | Lifesteal 50% of damage dealt. Dash 5 units toward player every 6s. Once at 50% hp: summons 4 Jenang Tirta. |
| 4 | Genderuwo, Raja Awu Kenongo | Demon | 1000 | 28 | 2.9 | 1.9 | 400 | Enrage below 50%: +40% attack speed, +20% spd. Each melee hit leaves a burning ground patch (radius 1.0, 5 dmg/s, lasts 3s). |
| 5 | Patih Kelam, Pengkhianat Keraton | DreadKnight | 1400 | 30 | 2.7 | 1.7 | 600 | Phase 1 (100-66%): two-hit sword combo (second hit 0.4s after first). Phase 2 (66-33%): adds — summons 2 Prajurit Sukma every 15s. Phase 3 (33-0%): ember rain — every 8s, 6 random ground circles radius 1.2, 12 dmg each, 1s telegraph. |

---

## 4. Three Heroes

Crit rule engine-wide: crit = 2x damage; base crit chance 5% for all heroes.

| hero | sprite | hp | dmg | spd | range | cd | starting items |
|---|---|---|---|---|---|---|---|
| Senopati | Warrior | 120 | 24 | 2.9 | 1.5 | 0.7 | keris_lurus, jamu_kunyit_asam x1 |
| Jemparing | Archer | 90 | 18 | 3.3 | 7.0 | 0.9 | busur_bambu, jamu_beras_kencur x1 |
| Resi | Mage | 80 | 26 | 2.8 | 6.5 | 1.4 | tongkat_cendana, jamu_kunyit_asam x1 |

Signature mechanics (one each, few lines of code):

- **Senopati — "Amuk"**: after taking damage, deals +30% damage for 3s (refreshes on each hit taken).
- **Jemparing — "Nyawiji"**: arrows fired while standing still (no movement input for the last 0.5s) deal +40% damage. Named after the meditative focus of jemparingan archery.
- **Resi — "Aji Sepuh"**: every bolt explodes on impact, dealing 50% of its damage to all other enemies within radius 1.2.

Character blurbs (shown on the select screen):

- **Senopati** — "Panglima terakhir yang menolak tunduk ketika Patih Kelam merebut keraton. Kerisnya sederhana, tetapi amarahnya adalah pusaka tersendiri."
- **Jemparing** — "Pemanah gandhewa istana yang melatih napasnya hingga satu dengan angin. Ia tidak pernah meleset — kecuali sekali, pada malam sang patih lolos."
- **Resi** — "Pertapa tua dari lereng Merapi yang turun gunung ketika air Tamansari berubah hitam. Tongkat cendananya menyimpan mantra yang lebih tua dari keraton itu sendiri."

---

## 5. Item Catalog

40 items. `icon` refers to a Kenney Tiny Dungeon tile index (verified against the actual sheet); "approx" means the tile is a visual stand-in until the RPG UI pack provides a closer icon. Inventory stacking: consumables stack to 5, materials/currency to 99, equipment does not stack. Picis (currency) is auto-collected on touch and lives in a counter, not a slot.

### 5.1 Weapons (equip slot: Senjata)

| id | display name | rarity | effect | icon | flavor |
|---|---|---|---|---|---|
| keris_lurus | Keris Lurus | Biasa | add_damage 4 | tile_0105 | "Bilah lurus tanpa luk milik prajurit rendahan; tetap setia meski tuannya tiada." |
| busur_bambu | Busur Bambu | Biasa | add_damage 3; add_range 0.5 | tile_0131 (approx, no bow tile) | "Busur latihan dari bambu apus. Ringan, jujur, dan tidak pernah mengeluh." |
| tongkat_cendana | Tongkat Cendana | Biasa | add_damage 4 | tile_0130 | "Kayu cendana wangi yang menyimpan sisa doa para resi terdahulu." |
| pedang_suduk | Pedang Suduk | Pilihan | add_damage 7 | tile_0104 | "Pedang pendek pengawal kedhaton, diasah untuk lorong sempit." |
| tombak_runcing | Tombak Runcing | Pilihan | add_damage 5; add_range 0.4 | tile_0106 | "Mata tombak bregada; jarak adalah zirah yang paling murah." |
| gada_wesi | Gada Wesi | Pilihan | add_damage 8; add_attack_speed_pct -10 | tile_0107 | "Berat di tangan, lebih berat lagi di kepala lawan." |
| keris_luk_sanga | Keris Luk Sanga | Langka | add_damage 9; on_hit 20% poison 4 dmg/s for 3s | tile_0105 | "Sembilan lekuk berlumur warangan; lukanya kecil, tetapi tidak pernah sembuh sendiri." |
| panah_geni | Panah Geni | Langka | add_damage 6; on_hit 25% burn 5 dmg/s for 2s | tile_0131 | "Anak panah bersumbu api dari upacara labuhan yang gagal." |
| candrasa_kelam | Candrasa Kelam | Agung | add_damage 10; lifesteal_pct 8 | tile_0103 | "Bilah wayang yang jatuh ke dunia; meminum apa yang ia lukai." |
| cakra_baskara | Cakra Baskara | Agung | add_damage 12; add_crit_pct 10 | tile_0118 (approx) | "Roda matahari kecil; berputar paling tajam tepat sebelum senja." |

### 5.2 Armor and charms (equip slots: Busana, Azimat 1, Azimat 2)

| id | display name | rarity | category | effect | icon | flavor |
|---|---|---|---|---|---|---|
| baju_lurik | Baju Lurik | Biasa | armor | add_max_hp 10 | tile_0066 (approx) | "Tenun lurik petani; garis-garisnya dianyam dengan sabar dan doa." |
| jarik_parang | Jarik Parang | Pilihan | armor | add_max_hp 15 | tile_0066 (approx) | "Motif parang hanya untuk kerabat raja. Kain ini tahu siapa yang pantas." |
| sabuk_epek_timang | Sabuk Epek Timang | Pilihan | armor | add_armor 1 | tile_0065 (approx) | "Sabuk upacara berkepala kuningan; menegakkan punggung dan nyali." |
| gelang_akar_bahar | Gelang Akar Bahar | Pilihan | charm | thorns 3 | tile_0101 (approx) | "Akar laut hitam penolak bala; yang menyentuh kasar akan tergores balik." |
| blangkon_sukma | Blangkon Sukma | Langka | charm | add_armor 1; add_xp_gain_pct 15 | tile_0064 (approx) | "Blangkon milik abdi dalem yang arif; lipatannya menyimpan ingatan." |
| selop_kilat | Selop Kilat | Langka | charm | add_movespeed 0.4 | tile_0062 (approx) | "Selop sutra penari srimpi; lantai seakan ikut melangkah." |
| kalung_jimat_aksara | Kalung Jimat Aksara | Langka | charm | dodge_pct 12 | tile_0064 (approx) | "Rajah aksara Jawa dalam bungkus kain mori; membelokkan niat jahat." |
| zirah_bregada | Zirah Bregada | Agung | armor | add_armor 3; add_max_hp 25; add_movespeed -0.2 | tile_0066 (approx) | "Zirah pasukan bregada keraton. Berat, tetapi begitu pula kesetiaan." |
| batik_sidomukti | Batik Sidomukti | Agung | charm | add_max_hp 20; add_xp_gain_pct 20 | tile_0066 (approx) | "Sidomukti: doa agar hidup mulia. Kain ini mendoakan pemakainya setiap langkah." |

### 5.3 Consumables (usable from hotbar)

| id | display name | rarity | effect | icon | flavor |
|---|---|---|---|---|---|
| jamu_kunyit_asam | Jamu Kunyit Asam | Biasa | heal 30 | tile_0115 | "Pahit di lidah, hangat di dada. Resep mbok jamu yang tak pernah salah." |
| jamu_beras_kencur | Jamu Beras Kencur | Biasa | heal 15; buff movespeed +0.5 for 8s | tile_0113 | "Putih susu, manis pedas; kaki terasa dua puluh tahun lebih muda." |
| tape_ketan | Tape Ketan | Biasa | heal 20; buff damage_pct +10 for 8s | tile_0114 | "Ketan hijau fermentasi daun katuk; sedikit memabukkan, sangat memberanikan." |
| wedang_ronde | Wedang Ronde | Pilihan | heal 50 | tile_0127 | "Kuah jahe panas berisi ronde kenyal; malam paling dingin pun menyerah." |
| sekar_setaman | Sekar Setaman | Pilihan | cleanse; heal 20 | tile_0056 (bokor) | "Bokor kembang tujuh rupa; membasuh yang tidak tampak oleh mata." |
| lisah_telon | Lisah Telon | Pilihan | immune poison for 20s | tile_0126 | "Minyak tiga rupa penjaga bayi; racun tua pun segan mendekat." |
| gudeg_komplit | Gudeg Komplit | Pilihan | heal 60 | tile_0066 (approx) | "Nangka muda dimasak semalaman, krecek, telur pindang. Alasan pulang ke Ngayogyakarta." |
| kopi_jos | Kopi Jos | Langka | buff attack_speed_pct +30 for 12s | tile_0125 (approx) | "Kopi ditanggap arang membara — jos! Jantung ikut menabuh kendhang." |
| wedang_uwuh | Wedang Uwuh | Langka | heal 35; immune burn for 15s | tile_0127 | "Ramuan 'sampah' dedaunan Imogiri, merah secang; api luar dilawan api dalam." |

### 5.4 Pusaka (equip slot: Pusaka — the stolen heirlooms; one equipped at a time)

| id | display name | rarity | effect | icon | flavor |
|---|---|---|---|---|---|
| keris_kyai_sengkelat | Keris Kyai Sengkelat | Pusaka | add_damage 15; on_hit 25% burn 6 dmg/s for 3s | tile_0105 | "Keris agung berpamor api. Ia memilih tangan yang menggenggamnya, bukan sebaliknya." |
| tombak_kyai_pleret | Tombak Kyai Pleret | Pusaka | add_damage 12; add_range 0.8 | tile_0106 | "Tombak pusaka Mataram; ujungnya pernah menentukan arah sejarah." |
| songsong_agung | Songsong Agung | Pusaka | dodge_pct 20; add_armor 2 | tile_0129 (approx) | "Payung kebesaran sultan. Yang bernaung di bawahnya berjalan di antara tetes takdir." |
| gong_kyai_sekati | Gong Kyai Sekati | Pusaka | aura: slow 30% for 2s, radius 3.0, pulse every 8s | tile_0066 (approx) | "Gong perayaan sekaten; gemanya membuat waktu ikut menunduk pelan." |
| kembang_wijayakusuma | Kembang Wijayakusuma | Pusaka | revive 50% hp, once per run | tile_0130 (approx) | "Kembang yang hanya mekar tengah malam dan hanya untuk yang belum selesai urusannya." |
| cupu_manik_astagina | Cupu Manik Astagina | Pusaka | reveal_map; add_xp_gain_pct 25 | tile_0056 (approx) | "Cupu wasiat yang memperlihatkan apa yang seharusnya tidak terlihat." |
| selendang_nawangwulan | Selendang Nawangwulan | Pusaka | add_movespeed 0.5; dodge_pct 10 | tile_0062 (approx) | "Selendang bidadari yang dahulu dicuri Jaka Tarub; kini ia tahu rasanya dicuri." |
| batu_akik_geni | Batu Akik Geni | Agung | add_crit_pct 10; on_crit heal 5 | tile_0102 (approx) | "Akik merah delima yang menyimpan satu bara kecil dari gunung." |

### 5.5 Materials and currency

| id | display name | rarity | category | effect | icon | flavor |
|---|---|---|---|---|---|---|
| picis_kuno | Picis Kuno | Biasa | currency | gold 1 | tile_0101 | "Koin berlubang segi empat dari zaman yang lupa namanya sendiri." |
| kantong_picis | Kantong Picis | Pilihan | currency | gold 10 | tile_0066 (approx) | "Kantong kain berisi picis; bunyi gemerincingnya menghibur di lorong gelap." |
| wesi_aji | Wesi Aji | Langka | material | upgrade_weapon: add_damage 2 (consumed at Shrine) | tile_0102 | "Besi bintang jatuh, bahan pamor para empu. Menunggu ditempa ulang." |
| mote_sukma | Mote Sukma | Langka | material | on use: add_max_hp 2 (permanent this run) | tile_0128 (approx) | "Manik cahaya dari jiwa yang akhirnya lega. Hangat di genggaman." |

### 5.6 Effect vocabulary (complete enum for the engineer)

Every effect above is composed only of these verbs. Percent params are integers (25 = 25%).

| verb | params | meaning |
|---|---|---|
| add_max_hp | amount | permanent max hp increase while equipped (or permanent for run if consumed) |
| add_damage | amount | flat damage per hit |
| add_damage_pct | percent | multiplicative damage bonus |
| add_armor | amount | flat reduction per incoming hit (min 1 damage taken) |
| add_movespeed | amount | units/s, may be negative |
| add_attack_speed_pct | percent | reduces attack cooldown by percent, may be negative |
| add_range | amount | attack range in units |
| add_crit_pct | percent | added crit chance (crit = 2x damage) |
| add_xp_gain_pct | percent | bonus xp/score gained |
| lifesteal_pct | percent | heal for percent of damage dealt |
| thorns | amount | reflect flat damage to melee attackers |
| dodge_pct | percent | chance to ignore an incoming hit entirely |
| heal | amount | instant hp restore |
| cleanse | — | remove all active statuses on player |
| buff | stat, amount, duration_s | temporary version of any add_* verb |
| immune | status, duration_s | ignore applications of one status |
| on_hit | chance_pct, status(params) | chance to apply a status on dealing a hit |
| on_crit | effect | trigger an effect when landing a crit |
| aura | status(params), radius, interval_s | periodic pulse applying a status to enemies in radius |
| revive | hp_pct | cancel death once per run, restore percent hp |
| reveal_map | — | uncover the full floor minimap |
| gold | amount | grant picis |
| upgrade_weapon | effect | permanently add effect to equipped weapon (shrine-consumed material) |

Status types (shared by enemies and items): `burn(dps, duration_s)`, `poison(dps, duration_s)`, `slow(percent, duration_s)`, `stun(duration_s)`.

---

## 6. Room Types

Generation budget per floor: 1 Start, 6-9 Combat, 1-2 Elite, 1 Treasure, 1 Shrine, 1 Shop, 1 Boss. Shrine and Shop each have a 100% spawn rate but random placement depth; Elite count is 1 on floors 1-2, 2 on floors 3-5.

| type | purpose | spawns | decoration | reward |
|---|---|---|---|---|
| Start | safe entry, floor title card trigger | none | gapura arch built from door tiles 021/045 + banners; 2 lit torches; floor-themed props only | none |
| Combat | core fights | 3-8 enemies from the floor pool (budget: floor_number x 18 xp worth) | standard biome prop mix; 1-3 destructible barrels/crates (082/063) with 30% chance of 1-3 picis each | enemy xp + picis drops (each enemy: 1-3 picis, 60% chance) |
| Elite | spike challenge | 1 elite + 2-3 regulars. Rule: the elite is the previous floor's boss creature at regular roster stats. By floor: F1 Bregada Kelam (Prajurit Sukma at 2x hp / 1.5x dmg), F2 Buto Ijo, F3 Reco Pentung, F4 Pangeran Wengi, F5 Pangeran Wengi + Genderuwo pair | flame emblem walls (029), floor tinted 15% toward ember_bara, war banners | guaranteed item roll at Langka or better + 10 picis + 1 mote_sukma |
| Treasure | free loot beat | none | centered chest (089; opened swaps to 091), locked-chest decor variants (090), candlesticks | 1 item roll (weights: Biasa 35 / Pilihan 30 / Langka 20 / Agung 10 / Pusaka 5) + 1 consumable + 5-15 picis |
| Shrine | risk/economy sink — "Sanggar Sesaji" | none | stone altar (006) with bokor (056), 4 candles (125-128), flower props, teal light pool | interact: pay 15 hp OR 25 picis for a random blessing: Berkah Bumi +10 max_hp, Berkah Geni +3 damage, Berkah Bayu +0.2 movespeed, Berkah Tirta heal 50 + cleanse. Also the only place to consume wesi_aji (weapon +2 damage, unlimited uses, one per wesi_aji) |
| Shop | economy — "Warung Lelembut" | vendor: Mbah Warung, a ghost grandmother (tile_0100 scaled 4x, slight transparency 85%, teal rim) | market stall: table 072 + cabinet 075 + hanging lantern; wares displayed as item icons on the table | sells 3 random items + 2 random consumables. Prices by rarity: Biasa 15, Pilihan 30, Langka 60, Agung 120, Pusaka 250 picis. Stock does not refresh |
| Boss | floor climax | floor boss (section 3.3); doors lock (021 closed) until victory | oversized arena (approx 20x14), floor-specific set dressing pushed to the walls, boss title banner UI on entry | guaranteed drop: floors 1-2 one Agung item, floors 3-5 one Pusaka item; +30 picis; a healing fountain (008/020) appears granting heal 50 once; stairs down ("undhak") unlock |

---

## 7. UI Direction

Target 1920x1080. World art renders at 4x pixel scale; UI icons from Tiny Dungeon render at 3x-4x with point filtering. Fonts: Press Start 2P for headers/titles only (it is dense — never body text), VT323 for everything else. All panels: `ui_surface` fill, 2px `ui_border`, 4px outer drop shadow of `bg_night`.

### 7.1 Main menu

Full-screen `bg_night`. Bottom third: a still silhouette of the Tamansari gate built from tinted wall/door tiles behind a horizontal water band in `teal_tirta` at 25% alpha with a slow 2px shimmer scroll. Centered, upper third: title "PUSAKA TAMANSARI" in Press Start 2P 64px `gold_pusaka` with a 4px black drop shadow; beneath it "Rebut kembali yang dicuri" in VT323 28px `teal_tirta`. Center: vertical menu in VT323 36px `ui_text_primary` — MULAI, PILIH KSATRIA, PENGATURAN, KELUAR. Hovered item turns `gold_pusaka` and gains a keris-tip cursor (tile_0105 at 2x) on its left. Bottom-right: version string in VT323 18px `ui_text_disabled`.

### 7.2 Character select — "PILIH KSATRIA"

Header top-center: Press Start 2P 32px `gold_pusaka`. Three cards centered horizontally, each 420x640: `ui_surface` fill, `ui_border`; the selected card gets `ui_border_active`, +8px Y offset, and its sprite plays `_walk` instead of `_idle`. Card contents top to bottom: hero sprite idle animation at 4x (256px) on a `ui_surface_raised` pedestal; name in Press Start 2P 24px `gold_pusaka`; class word (Ksatria / Pemanah / Pertapa) in VT323 22px `teal_tirta`; four stat bars (HP, Serangan, Kecepatan, Jangkauan) — VT323 20px labels, 8px bars filled `teal_tirta` on `ui_surface_raised`; signature mechanic name + one line in VT323 20px `ember_bara`; blurb in VT323 20px `ui_text_secondary`. Bottom-center of screen: "MULAI TURUN" button, `ui_surface_raised` with `ui_border_active`, Press Start 2P 20px `gold_pusaka`.

### 7.3 HUD (in run)

- Top-left: HP bar 320x28 — fill `ui_hp_fill` (flashes `ui_hp_low` under 25%), border `ui_border`, numeric "87/120" right-aligned inside in VT323 20px `ui_text_primary`. Directly beneath: XP bar 320x10 filled `ui_xp_fill`. Left of both: level number in a 44px `ui_surface_raised` square, VT323 24px `gold_pusaka`.
- Top-center: floor label "LANTAI 2 — UMBUL PASIRAMAN" VT323 26px `ui_text_secondary`; shows at full size for 4s on floor entry then shrinks to 18px and stays.
- Top-right: score in Press Start 2P 20px `gold_pusaka`; picis counter beneath it (tile_0101 icon at 2x + count, VT323 22px `ui_text_primary`).
- Below score: minimap 160x160, background `ui_surface` at 60% alpha, `ui_border`. Rooms are 14px squares with 4px gaps: current room filled `gold_pusaka`, visited filled `teal_tirta` 40% alpha, adjacent-unvisited outlined `ui_border`, boss room marked with an `ember_bara` dot, shop "W" and shrine "S" letter glyphs in VT323 12px.
- Bottom-center: hotbar of 5 slots, 56px each, 6px gaps — `ui_surface_raised`, `ui_border`; active slot `ui_border_active`; keybind digits 1-5 in VT323 16px `ui_text_secondary` at each slot's top-left; stack counts bottom-right in VT323 16px. Consumables only.
- Boss fight: boss bar 640x20 centered above the hotbar, fill `ember_bara`, border `ui_border`, boss display name centered above in VT323 24px `ui_text_primary`.
- Floating combat text: VT323, world-space, rises 0.5 units over 0.6s then fades; colors and sizes per section 1.4.

### 7.4 Inventory screen (Tab or I — pauses the game)

Dim world with `ui_overlay`. Centered panel 1200x800 `ui_surface`, 2px `ui_border` with 6x6 `gold_pusaka` corner accents.

- Left column (360px): hero idle animation at 3x (192px); name Press Start 2P 20px `gold_pusaka`; stat readout in VT323 22px `ui_text_primary` — HP, Serangan, Kecepatan, Jangkauan, Cooldown, Armor, Crit, Dodge — values tinted `teal_tirta` when modified above base. Beneath: 5 equip slots in a vertical list, 72px each with VT323 18px labels: SENJATA, BUSANA, AZIMAT 1, AZIMAT 2, PUSAKA. Occupied slots draw the item icon at 4x with a border in the item's rarity color.
- Right area: backpack grid, exactly 6 columns x 4 rows = 24 slots, 64px slots, 6px gaps, `ui_surface_raised` with `ui_border`; hovered slot `ui_border_active`. Bottom row beneath the grid mirrors the 5 hotbar slots for drag-assignment.
- Tooltip on hover: `ui_surface_raised` panel, item name in its rarity color (VT323 24px), category + rarity word in `ui_text_secondary` 18px, effect lines in `teal_tirta` 20px (one verb per line, e.g. "+15 HP maksimal"), flavor in italic `ui_text_secondary` 18px.
- Interactions: drag-drop to move/equip; right-click equips equipment or uses consumables; equipping into an occupied slot swaps.

### 7.5 Pause (Esc)

`ui_overlay` dim. Centered panel 480px wide: "JEDA" in Press Start 2P 32px `gold_pusaka`; menu in VT323 30px `ui_text_primary` — LANJUT, PENGATURAN, MENU UTAMA (hover: `gold_pusaka`); beneath a divider (`ui_border` 2px line), run stats in VT323 20px `ui_text_secondary`: lantai, skor, picis, waktu.

---

## Appendix — Tiny Dungeon tile legend (verified against the sheet)

Quick reference for the engineer when dressing rooms; indices are exact.

| category | tiles |
|---|---|
| dirt/brown floors | 000, 012, 024 (rubble variants) |
| tan/sand floors | 048-053, 042 (cracked) |
| grey slab/plank floors | 036-039 |
| stone walls + corners | 004-005, 013-017, 025-028, 057-059 |
| doors (wood, arch, lit) | 021-023, 033-035, 045-047 |
| fountains/wall spouts | 008, 020, 032, 043, 044 |
| decorated walls | 019 (demon face), 029 (flame emblem), 031/040 (windows), 041 (tomb relief) |
| chests | 089 (closed), 090 (locked), 091 (open), 092 (mimic — decor only, unused as enemy) |
| furniture/props | 063/075 (cabinets), 072/073 (tables), 074 (anvil), 082 (barrel), 054-055 (carts), 056 (bokor/urn), 006 (pillar) |
| fences/rails | 067-071, 076-081, 083, 093-095 |
| candles/torches/vials | 125-128 |
| weapons | 103, 104, 105 (keris), 106, 107 (club), 117-119, 129-131 |
| potions | 113 (white), 114 (green), 115 (red), 116 (blue) |
| currency/material | 101 (square-holed picis), 102 (wesi aji ore) |
| NPC sprites | 100 (Mbah Warung vendor), 084-088, 096-099, 111-112 (unused reserves) |
| ambient critters (non-combat decor) | 108, 120-124 |
