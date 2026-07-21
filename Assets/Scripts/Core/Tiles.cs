/// <summary>
/// Index tile Kenney yang sudah diverifikasi satu per satu terhadap contact sheet.
/// JANGAN menebak angka di sini: index yang salah tampil sebagai kotak placeholder
/// putih bergaris (dungeon 60-62) atau properti yang keliru.
/// </summary>
public static class Tiles
{
    // ---------------------------------------------------------- Tiny Dungeon
    public static class Dun
    {
        // lantai batu -- 48 polos, sisanya bercak supaya lantai tidak rata membosankan
        public static readonly int[] Floor = { 48, 49, 51 };
        public const int FloorPlain = 48;

        // dinding bata
        public static readonly int[] Wall = { 57, 58, 59 };
        public const int WallPlain = 57;

        public const int DoorClosed = 45;      // pintu kayu di bingkai batu
        public const int DoorOpen = 34;        // ambang terbuka
        public const int Torch = 29;           // obor menyala
        public const int Pillar = 18;          // tiang batu
        public const int StatuePillar = 41;    // patung dalam relung
        public const int SkullPillar = 19;     // tiang berukir tengkorak
        public const int Barrel = 66;          // tong kayu tampak atas
        public const int Bookshelf = 63;
        public const int Shelf = 75;
        public const int Table = 72;
        public const int Anvil = 74;
        public const int Bars = 79;            // jeruji besi
        public const int ChestClosed = 89;
        public const int ChestOpen = 90;
        public const int Mimic = 92;
        public const int Coins = 82;
        public const int OreSilver = 102;

        // ikon item
        public const int PotionGrey = 113, PotionGreen = 114, PotionRed = 115, PotionBlue = 116;
        public static readonly int[] Swords = { 103, 104, 105, 106, 107 };
        public const int Hammer = 117, AxeSilver = 118, AxeStone = 119;
        public static readonly int[] Staves = { 125, 126, 127, 128, 129, 130, 131 };
    }

    // ------------------------------- Kenney Roguelike Caves & Dungeons (utama)
    // Diverifikasi dengan me-render contact sheet berlabel lalu memeriksanya, dan
    // dinilai lewat mockup ruangan sebelum dipakai. Ini tileset lingkungan utama:
    // Tiny Dungeon terlalu polos (tile lantainya kotak tan tanpa tekstur).
    public static class RL
    {
        // lantai batu bertekstur; retak dipakai sebagai variasi minoritas
        public static readonly int[] FloorPlain = { 16, 17, 18, 19, 20, 37, 38 };
        public static readonly int[] FloorCrack = { 96, 97, 98, 100, 101, 102 };

        // JANGAN pakai 62-65 sebagai lantai: latarnya opak gelap, tampak seperti lubang.

        // tembok: dua lapis, luar pekat + dalam blok batu
        public static readonly int[] WallOuter = { 137, 142, 143, 144 };
        public static readonly int[] WallInner = { 282, 283, 284, 285, 286, 287, 288, 289 };

        // kolam
        public static readonly int[] Water = { 320, 321, 322, 323 };
        public static readonly int[] WaterEdge = { 290, 291, 292, 296, 297, 319, 324 };

        public const int PillarTop = 145, PillarBottom = 174;
        public const int Brazier = 506, BrazierLit = 507, BrazierCold = 508;

        public static readonly int[] Rocks = { 0, 1, 2, 3, 4, 5 };
        public static readonly int[] Bones = { 58, 59, 60, 61 };
        public static readonly int[] Mushrooms = { 87, 88 };
        public static readonly int[] Crystals = { 358, 359, 361, 362 };
        public static readonly int[] Tools = { 416, 417, 420, 421 };

        public const int DoorWood = 21, DoorArch = 140, Stairs = 45;
        public const int Chest = 449;
    }

    // ------------------------------------------------------------- Tiny Town
    public static class Town
    {
        public static readonly int[] Grass = { 0, 0, 0, 1, 2 };   // 0 dominan, 1/2 aksen
        public const int GrassPlain = 0;
        public const int Pebbles = 43;
        public static readonly int[] Dirt = { 25, 39, 40, 41, 42 };
        public const int DirtPlain = 25;

        // pohon besar -- ditanam sebagai rintangan padat
        public static readonly int[] TreesGreen = { 4, 16, 28 };
        public static readonly int[] TreesAutumn = { 3, 15, 27 };
        public static readonly int[] Bushes = { 5, 30, 31, 32 };
        public const int Sprouts = 17;
        public const int Mushrooms = 29;

        // pagar kayu
        public const int FenceH = 81, FenceV = 47, FenceCornerTL = 44, FencePost = 71;
        public const int Sign = 83;

        public const int StoneWall = 97;
        public const int CaveArch = 112;

        // ikon item
        public const int Bread = 93, GoldRing = 94, Bomb = 105, Pouch = 107;
        public const int Pickaxe = 115, Key = 117, Bow = 118, Arrow = 119;
        public const int Hammer = 128, Axe = 129, BarrelSide = 130, ChestBlue = 131;
    }
}
