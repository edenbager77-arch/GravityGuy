using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class PlayerRunner : MonoBehaviour
{
    [SerializeField] float forwardSpeed = 5f;
    [SerializeField] float flipImpulse  = 7f;

    // NEW: flip quality
    [SerializeField] LayerMask groundMask;   // set to Ground in Inspector
    [SerializeField] float coyoteTime = 0.10f;
    [SerializeField] float inputBufferTime = 0.10f;

    Rigidbody2D rb;
    BoxCollider2D col;
    float baseGravityScale;
    int gravitySign = +1;

    float coyoteCounter;
    float bufferCounter;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        baseGravityScale = rb.gravityScale <= 0 ? 1f : rb.gravityScale;
        rb.freezeRotation = true;
    }

    void Update()
    {
        if (!GameManager.Instance || !GameManager.Instance.IsPlaying) return;

        // buffer input
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            bufferCounter = inputBufferTime;
    }

    void FixedUpdate()
    {
        if (!GameManager.Instance || !GameManager.Instance.IsPlaying) return;

        rb.linearVelocity = new Vector2(forwardSpeed, rb.linearVelocity.y);

        // grounded = touching Ground layer (works for floor & ceiling)
        bool grounded = col.IsTouchingLayers(groundMask);

        // coyote timer
        coyoteCounter = grounded ? coyoteTime : Mathf.Max(0f, coyoteCounter - Time.fixedDeltaTime);

        // consume buffered input if allowed
        if (bufferCounter > 0f)
        {
            bufferCounter -= Time.fixedDeltaTime;
            if (coyoteCounter > 0f) { DoFlip(); bufferCounter = 0f; }
        }
    }

    void DoFlip()  // renamed to avoid confusion
    {
        gravitySign *= -1;
        rb.gravityScale = baseGravityScale * gravitySign;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * flipImpulse * gravitySign, ForceMode2D.Impulse);
    }

    // keep game over triggers
    void OnCollisionEnter2D(Collision2D c)
    {
        if (c.collider.CompareTag("Obstacle")) GameManager.Instance.GameOver();
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Obstacle")) GameManager.Instance.GameOver();
    }
}