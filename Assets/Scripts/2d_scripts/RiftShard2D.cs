using UnityEngine;

// A bit of sky that breaks off while the chapter 2 rift opens: tumbles down, fades, deletes itself.
public class RiftShard2D : MonoBehaviour
{
    public float life = 1.1f;

    private SpriteRenderer sr;
    private Vector2 velocity;
    private float spin;
    private float age;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        velocity = new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(0.5f, 2f));
        spin = Random.Range(-360f, 360f);
        transform.localScale = Vector3.one * Random.Range(0.8f, 1.6f);
    }

    void Update()
    {
        age += Time.deltaTime;
        velocity.y -= 9f * Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        transform.Rotate(0f, 0f, spin * Time.deltaTime);
        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Clamp01(1f - age / life);
            sr.color = c;
        }
        if (age >= life) Destroy(gameObject);
    }
}
