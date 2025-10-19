using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;          // Player
    [SerializeField] float lookAheadX = 3f;     // how far ahead
    [SerializeField] float smoothTime = 0.12f;  // SmoothDamp time
    [SerializeField] bool lockY = true;

    float fixedY, fixedZ;
    Vector3 _vel;

    void Awake()
    {
        var p = transform.position;
        fixedY = p.y;
        fixedZ = p.z;
        if (!target) target = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void LateUpdate()
    {
        if (!target) return;

        float y = lockY ? fixedY : target.position.y;
        Vector3 desired = new Vector3(target.position.x + lookAheadX, y, fixedZ);

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _vel, smoothTime);
    }
}