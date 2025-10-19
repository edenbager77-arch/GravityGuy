using UnityEngine;

public class MenuBackground : MonoBehaviour
{
    void Update()
    {
        float move = Mathf.Sin(Time.time * 0.2f) * 0.05f;
        transform.localPosition = new Vector3(move * 50f, 0, 0);
    }
}