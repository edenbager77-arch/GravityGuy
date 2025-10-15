using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerRunner : MonoBehaviour
{
    [SerializeField] float forwardSpeed = 5f;
    [SerializeField] float flipImpulse  = 7f;

    Rigidbody2D rb;
    float baseGravityScale;
    int gravitySign = +1; // +1 = floor, -1 = ceiling

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseGravityScale = rb.gravityScale <= 0 ? 1f : rb.gravityScale;
        rb.freezeRotation = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            Flip();
    }

    void FixedUpdate()
    {
        // constant forward velocity; keep current vertical speed
        rb.velocity = new Vector2(forwardSpeed, rb.velocity.y);
    }

    void Flip()
    {
        gravitySign *= -1;
        rb.gravityScale = baseGravityScale * gravitySign;

        // snap vertical speed, then nudge toward the new "ground"
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(Vector2.up * flipImpulse * gravitySign, ForceMode2D.Impulse);
    }
}