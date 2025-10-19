using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float coyoteTime = 0.12f;       // grace after leaving ground
    [SerializeField] float inputBufferTime = 0.12f;  // remember a press briefly
    [SerializeField] float flipCooldown = 0.08f;     // prevent double flips

    float _lastGroundedTime;
    float _lastFlipPressedTime;
    float _nextFlipAllowedAt;

    
    public float gravityScale = 5f;
    public AudioClip flipSfx;
    public AudioClip hitSfx;

    Rigidbody2D rb;
    AudioSource audioSrc;
    bool gravityUp = false; // false = downwards gravity (default)

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSrc = GetComponent<AudioSource>();
        rb.gravityScale = gravityScale; // starts pulling down
        rb.freezeRotation = true;
    }

    void Update()
    {
        if (GameManager.I == null || GameManager.I.State != GameState.Playing) return;

        bool flipPressed = Input.GetKeyDown(KeyCode.Space)
                           || Input.GetMouseButtonDown(0)
                           || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

        if (flipPressed) _lastFlipPressedTime = Time.time;

        bool buffered    = Time.time <= _lastFlipPressedTime + inputBufferTime; // keep your 0.12s
        bool offCooldown = Time.time >= _nextFlipAllowedAt;                    // e.g., 0.08–0.10s

        if (buffered && offCooldown)
        {
            FlipGravity();
            _nextFlipAllowedAt   = Time.time + flipCooldown;
            _lastFlipPressedTime = -999f; // consume buffer
        }
    }



    
    [SerializeField] Transform groundProbe;     // drag an empty child here
    [SerializeField] float probeRadius = 0.1f;  // adjust as needed
    [SerializeField] LayerMask groundMask;      // set to “Ground” layer

    bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundProbe.position, probeRadius, groundMask);
    }


    void FixedUpdate()
    {
        if (GameManager.I == null || GameManager.I.State != GameState.Playing) return;

        // Constant forward run (keep Y velocity from gravity)
        var v = rb.linearVelocity;
        v.x = GameManager.I.CurrentSpeed;

        rb.linearVelocity = v;
    }

    void FlipGravity()
    {
        gravityUp = !gravityUp;
        rb.gravityScale = gravityUp ? -Mathf.Abs(gravityScale) : Mathf.Abs(gravityScale);

        // Visually flip vertically so feet match surface
        var s = transform.localScale;
        s.y = Mathf.Abs(s.y) * (gravityUp ? -1f : 1f);
        transform.localScale = s;

        if (flipSfx) audioSrc.PlayOneShot(flipSfx);
        
        GameManager.I?.HideHelp();

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Spikes") || other.CompareTag("Kill"))
        {
            if (hitSfx) audioSrc.PlayOneShot(hitSfx);
            GameManager.I.GameOver(); // death → main
        }
    }
}