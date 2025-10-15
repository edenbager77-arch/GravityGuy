using TMPro;
using UnityEngine;

public class RunScore : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] float unitsToPoints = 5f; // points per world-unit

    float startX;

    void Start()
    {
        if (player) startX = player.position.x;
        if (scoreText) scoreText.text = "0";
    }

    void Update()
    {
        if (!player || !scoreText) return;
        if (!GameManager.Instance || !GameManager.Instance.IsPlaying) return;

        float dist = Mathf.Max(0f, player.position.x - startX);
        int distPoints = Mathf.FloorToInt(dist * unitsToPoints);
        int total = distPoints + GameManager.Instance.CoinScore;
        scoreText.text = total.ToString();
    }
}