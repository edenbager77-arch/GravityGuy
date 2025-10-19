using UnityEngine;

public class BackgroundTiler3 : MonoBehaviour
{
    [SerializeField] Transform cam;                 // Main Camera
    [SerializeField] SpriteRenderer leftTile;       // BG_A
    [SerializeField] SpriteRenderer centerTile;     // BG_B
    [SerializeField] SpriteRenderer rightTile;      // BG_C
    [SerializeField, Range(0f,1f)] float parallax = 0f;

    float tileW;

    void Awake()
    {
        if (!cam) cam = Camera.main.transform;

        // reset scales
        leftTile.transform.localScale   = Vector3.one;
        centerTile.transform.localScale = Vector3.one;
        rightTile.transform.localScale  = Vector3.one;

        var camComp = Camera.main;
        float camH = 2f * camComp.orthographicSize;
        float camW = camH * camComp.aspect;

        // sprite world size before scaling
        float sW = centerTile.bounds.size.x;
        float sH = centerTile.bounds.size.y;

        // uniform scale so one tile covers full camera; add extra bleed
        float scale = Mathf.Max(camW / sW, camH / sH) * 1.08f; // 8% bleed
        leftTile.transform.localScale   = new Vector3(scale, scale, 1f);
        centerTile.transform.localScale = new Vector3(scale, scale, 1f);
        rightTile.transform.localScale  = new Vector3(scale, scale, 1f);

        tileW = centerTile.bounds.size.x;
    }

    void LateUpdate()
    {
        if (!cam) return;

        float x = cam.position.x * (1f - parallax);

        // snap a center tile to the tile grid around camera
        float baseX = Mathf.Floor(x / tileW) * tileW;

        Vector3 p = centerTile.transform.position;
        centerTile.transform.position = new Vector3(baseX, p.y, p.z);
        leftTile.transform.position   = new Vector3(baseX - tileW, p.y, p.z);
        rightTile.transform.position  = new Vector3(baseX + tileW, p.y, p.z);
    }
}
