using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] int value = 100;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!GameManager.Instance || !GameManager.Instance.IsPlaying) return;
        if (other.GetComponent<PlayerRunner>())
        {
            GameManager.Instance.AddCoins(value);
            gameObject.SetActive(false); // simple “collect”
        }
    }
}