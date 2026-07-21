/// <summary>
/// Satu-satunya sumber narasi game (judul, prolog, lore tiap lantai, lore hero,
/// epilog). Murni data — dipakai builder menu & DungeonManager. Ubah cerita di
/// sini saja, seluruh game ikut. ponytail: data statis, bukan dialogue engine.
/// </summary>
public static class StoryData
{
    public const string Title   = "PUSAKA TAMANSARI";
    public const string Tagline = "Pusaka yang tertidur di bawah taman air telah retak.";

    public const string Prologue =
        "Di bawah Tamansari, kraton air para Sultan, tersegel sebuah keris bernama\n" +
        "KALANJANA — pusaka yang meminum cahaya. Saat segelnya retak, lelembut taman\n" +
        "bangkit dan lorong bawah tanah dipenuhi gelap.\n\n" +
        "Kau turun untuk merebut Pusaka, dan menyegelnya kembali.";

    public const string Epilogue =
        "Kalanjana kembali tersegel. Air Tamansari jernih lagi, taman kembali hening.\n" +
        "Namun pusaka tak pernah benar-benar tidur...";

    public const string GameOverLine =
        "Gelap menelan sang petualang. Tamansari menyimpan satu jiwa lagi.";

    // index = lantai-1 (StageNumber 1..3)
    static readonly string[] FloorNames =
    {
        "I · SUMUR GUMULING",
        "II · LORONG SENDANG",
        "III · SANGGAR PAMELENGAN",
    };
    static readonly string[] FloorLore =
    {
        "Masjid bawah tanah yang tenggelam. Roh sesajen bangkit dari air keruh.",
        "Terowongan rahasia para abdi dalem, kini dijaga yang murka.",
        "Bilik semadi tempat Pusaka disegel. Penjaga batu menanti di ujung.",
    };

    // hero: urutan = Warrior, Archer, Mage (samakan dgn heroPrefabs)
    public static readonly string[] HeroNames = { "SENOPATI", "JEMPARING", "RESI" };
    public static readonly string[] HeroLore =
    {
        "Panglima perang kraton.\nPedang besar, dada baja.",
        "Ahli jemparingan.\nMemanah dari jauh, tenang & mematikan.",
        "Pertapa penjaga aji.\nTongkat sihir, kuasa arkana.",
    };

    static string At(string[] a, int stage1) =>
        (stage1 >= 1 && stage1 <= a.Length) ? a[stage1 - 1] : a[a.Length - 1];

    public static string FloorName(int stage1) => At(FloorNames, stage1);
    public static string FloorLoreOf(int stage1) => At(FloorLore, stage1);
}
