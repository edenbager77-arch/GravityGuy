using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float smoothSpeed = 5f;
    float startY;

    void Start() => startY = transform.position.y;

    void LateUpdate()
    {
        if (!target) return;
        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, target.position.x, smoothSpeed * Time.deltaTime);
        pos.y = startY; // lock Y
        transform.position = pos;
    }
}