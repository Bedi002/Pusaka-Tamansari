using UnityEngine;

/// <summary>
/// Proyektil sederhana untuk serangan jarak jauh boss. Bergerak lurus,
/// merusak player saat kena, dan hancur saat membentur dinding atau habis umur.
/// Pakai Rigidbody2D (Kinematic, dengan Collider2D bertanda IsTrigger) bila ada;
/// kalau tidak, bergerak via transform.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    public float lifeTime = 5f;

    int damage = 10;
    Vector2 velocity;
    Rigidbody2D rb;

    public void Launch(Vector2 dir, float speed, int dmg)
    {
        velocity = dir.normalized * speed;
        damage = dmg;
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = velocity;
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (rb == null) transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var hp = other.GetComponent<PlayerHealth>();
            if (hp == null) hp = other.GetComponentInParent<PlayerHealth>();
            if (hp != null) hp.TakeDamage(damage, transform.position);
            Destroy(gameObject);
        }
        else if (!other.isTrigger && !other.CompareTag("Enemy"))
        {
            // membentur dinding / environment solid
            Destroy(gameObject);
        }
    }
}
