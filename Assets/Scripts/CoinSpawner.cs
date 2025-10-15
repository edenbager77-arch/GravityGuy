using System.Collections.Generic;
using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] GameObject coinPrefab;

    [Header("Spawn")]
    [SerializeField] float spawnAhead = 25f;
    [SerializeField] float minGap = 4f;
    [SerializeField] float maxGap = 7f;
    [SerializeField] float floorY = -1.7f;
    [SerializeField] float ceilingY =  1.7f;

    [Header("Cleanup")]
    [SerializeField] float despawnBehind = 12f;

    [SerializeField] int poolSize = 16;

    readonly List<GameObject> active = new();
    Queue<GameObject> pool;
    float nextX;

    void Awake()
    {
        pool = new Queue<GameObject>(poolSize);
        for (int i = 0; i < poolSize; i++)
        {
            var go = Instantiate(coinPrefab);
            go.SetActive(false);
            pool.Enqueue(go);
        }
    }

    void Start()
    {
        nextX = (player ? player.position.x : 0f) + 12f;
    }

    void Update()
    {
        if (!player || !GameManager.Instance || !GameManager.Instance.IsPlaying) return;

        // fill ahead
        while (nextX < player.position.x + spawnAhead)
            SpawnOne();

        // despawn behind
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (active[i].transform.position.x < player.position.x - despawnBehind)
            {
                Return(active[i]);
                active.RemoveAt(i);
            }
        }
    }

    void SpawnOne()
    {
        var go = Take();
        // random track: near floor or near ceiling
        bool high = Random.value < 0.5f;
        float y = high ? ceilingY : floorY;
        go.transform.position = new Vector3(nextX, y, 0f);
        go.transform.rotation = Quaternion.identity;
        go.SetActive(true);
        active.Add(go);

        nextX += Random.Range(minGap, maxGap);
    }

    GameObject Take() => pool.Count > 0 ? pool.Dequeue() : Instantiate(coinPrefab);
    void Return(GameObject go) { go.SetActive(false); pool.Enqueue(go); }
}