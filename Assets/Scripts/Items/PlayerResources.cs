using UnityEngine;

/// <summary>
/// Tenaga (stamina) dan Aji (mana).
///
/// Keduanya ditambahkan supaya bar di HUD punya arti: dash memakan Tenaga, dan
/// serangan berat memakan Aji. Tanpa biaya, dash jadi tombol yang ditekan
/// terus-menerus dan bar-nya cuma hiasan.
///
/// Regenerasi ditunda sesaat setelah dipakai supaya pemain merasakan jeda,
/// bukan sekadar melihat angka naik lagi seketika.
/// </summary>
public class PlayerResources : MonoBehaviour
{
    [Header("Tenaga (dash, lari)")]
    public float maxStamina = 100f;
    public float staminaRegen = 26f;
    public float dashCost = 25f;
    [Tooltip("Jeda sebelum tenaga mulai pulih, dalam detik")]
    public float staminaDelay = 0.7f;

    [Header("Aji (serangan berat)")]
    public float maxMana = 80f;
    public float manaRegen = 7f;
    public float heavyCost = 25f;
    public float manaDelay = 1.2f;

    public float Stamina { get; private set; }
    public float Mana { get; private set; }

    float staminaHold, manaHold;

    void Awake()
    {
        Stamina = maxStamina;
        Mana = maxMana;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (staminaHold > 0f) staminaHold -= dt;
        else if (Stamina < maxStamina) Stamina = Mathf.Min(maxStamina, Stamina + staminaRegen * dt);

        if (manaHold > 0f) manaHold -= dt;
        else if (Mana < maxMana) Mana = Mathf.Min(maxMana, Mana + manaRegen * dt);

        Push();
    }

    /// <summary>Coba pakai tenaga. false bila tidak cukup.</summary>
    public bool SpendStamina(float amount)
    {
        if (Stamina < amount) return false;
        Stamina -= amount;
        staminaHold = staminaDelay;
        Push();
        return true;
    }

    /// <summary>Coba pakai aji. false bila tidak cukup.</summary>
    public bool SpendMana(float amount)
    {
        if (Mana < amount) return false;
        Mana -= amount;
        manaHold = manaDelay;
        Push();
        return true;
    }

    public bool CanDash => Stamina >= dashCost;
    public bool CanHeavy => Mana >= heavyCost;

    void Push()
    {
        var hud = HUDController.Instance;
        if (hud == null) return;
        hud.SetStamina(Stamina, maxStamina);
        hud.SetMana(Mana, maxMana);
    }
}
