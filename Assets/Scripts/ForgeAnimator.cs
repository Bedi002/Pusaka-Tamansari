using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Animator sprite 4-arah berbasis KODE (deterministik, anti-bug). Membaca
/// MoveX/MoveY/Speed + trigger Attack/Attack1..3/Die/Hurt dari komponen Animator
/// (yang diisi script gameplay yang sudah ada), lalu memutar frame langsung ke
/// SpriteRenderer. Tidak memakai blend tree. Diisi otomatis oleh ForgeImporter.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ForgeAnimator : MonoBehaviour
{
    [System.Serializable]
    public class Clip
    {
        public string key;          // "idle_down", "walk_up", "attack_left", ...
        public Sprite[] frames;
        public float fps = 10f;
        public bool loop = true;
    }

    public Clip[] clips;
    public SpriteRenderer sr;
    public Animator animator;       // dipakai sebagai "papan tulis" parameter

    readonly Dictionary<string, Clip> map = new Dictionary<string, Clip>();
    Clip cur;
    int frame;
    float timer;
    bool oneShot;                   // attack/hurt sedang main
    bool dead;
    Vector2 facing = Vector2.down;

    void Awake()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        if (clips != null)
            foreach (var c in clips)
                if (c != null && !string.IsNullOrEmpty(c.key)) map[c.key] = c;
    }

    bool Has(string p)
    {
        if (animator == null) return false;
        foreach (var par in animator.parameters) if (par.name == p) return true;
        return false;
    }
    float GetF(string p) => Has(p) ? animator.GetFloat(p) : 0f;
    bool GetTrig(string p) => Has(p) && animator.GetBool(p);
    void Clear(string p) { if (Has(p)) animator.ResetTrigger(p); }

    string Dir()
    {
        if (Mathf.Abs(facing.x) >= Mathf.Abs(facing.y)) return facing.x < 0f ? "left" : "right";
        return facing.y < 0f ? "down" : "up";
    }

    // baca setelah semua Update (script gameplay sudah set param)
    void LateUpdate()
    {
        float mx = GetF("MoveX"), my = GetF("MoveY"), spd = GetF("Speed");
        if (Mathf.Abs(mx) > 0.01f || Mathf.Abs(my) > 0.01f)
            facing = Mathf.Abs(mx) >= Mathf.Abs(my)
                ? new Vector2(Mathf.Sign(mx), 0f)
                : new Vector2(0f, Mathf.Sign(my));

        if (dead) { Advance(); return; }

        if (!oneShot)
        {
            if (GetTrig("Die")) { Clear("Die"); dead = true; PlayOnce("death"); Advance(); return; }
            if (GetTrig("Hurt")) { Clear("Hurt"); PlayOnce("hurt"); }
            else if (GetTrig("Attack") || GetTrig("Attack1") || GetTrig("Attack2") || GetTrig("Attack3"))
            {
                Clear("Attack"); Clear("Attack1"); Clear("Attack2"); Clear("Attack3");
                PlayOnce("attack");
            }
        }

        if (!oneShot)
            SetLoco((spd > 0.01f ? "walk_" : "idle_") + Dir());

        Advance();
    }

    void SetLoco(string key)
    {
        if (cur != null && cur.key == key) return;
        if (map.TryGetValue(key, out var c)) { cur = c; frame = 0; timer = 0f; }
    }

    void PlayOnce(string anim)
    {
        Clip c = map.TryGetValue(anim + "_" + Dir(), out var cc) ? cc
               : (map.TryGetValue(anim + "_down", out var cd) ? cd : null);
        if (c == null) return;
        cur = c; frame = 0; timer = 0f; oneShot = true;
    }

    void Advance()
    {
        if (cur == null || cur.frames == null || cur.frames.Length == 0) return;
        timer += Time.deltaTime;
        float dt = 1f / Mathf.Max(cur.fps, 1f);
        while (timer >= dt)
        {
            timer -= dt;
            frame++;
            if (frame >= cur.frames.Length)
            {
                if (dead) frame = cur.frames.Length - 1;
                else if (oneShot) { oneShot = false; frame = cur.frames.Length - 1; }
                else if (cur.loop) frame = 0;
                else frame = cur.frames.Length - 1;
            }
        }
        sr.sprite = cur.frames[Mathf.Clamp(frame, 0, cur.frames.Length - 1)];
    }
}
