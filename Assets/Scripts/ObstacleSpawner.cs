using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Transform player;
    [SerializeField] GameObject obstaclePrefab;

    [Header("Spawn")]
    [SerializeField] float spawnAhead = 25f;    // how far ahead to keep filled
    [SerializeField] float minGap = 6f;         // distance between obstacles
    [SerializeField] float maxGap = 10f;
    [SerializeField] float floorY = -2.5f;      // center when attached to floor
    [SerializeField] float ceilingY =  2.5f;    // center when attached to ceiling

    [Header("Cleanup")]
    [SerializeField] float despawnBehind = 12f; // remove when far behind player

    [Header("Pooling")]
    [SerializeField] int poolSize = 12;

    readonly List<GameObject> active = new();
    Queue<GameObject> pool;
    float nextX;

    void Awake()
    {
        pool = new Queue<GameObject>(poolSize);
        for (int i = 0; i < poolSize; i++)
        {
            var go = Instantiate(obstaclePrefab);
            go.SetActive(false);
            pool.Enqueue(go);
        }
    }

    void Start()
    {
        nextX = Mathf.Max(player ? player.position.x + 10f : 10f, 10f);
    }

    void Update()
    {
        if (!player) return;

        // Fill ahead
        while (nextX < player.position.x + spawnAhead)
            SpawnOne();

        // Despawn behind
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
        bool toCeiling = Random.value < 0.5f;
        float y = toCeiling ? ceilingY : floorY;
        go.transform.position = new Vector3(nextX, y, 0f);
        go.transform.rotation = Quaternion.identity;
        go.SetActive(true);
        active.Add(go);

        nextX += Random.Range(minGap, maxGap);
    }

    GameObject Take()
    {
        if (pool.Count > 0) return pool.Dequeue();
        return Instantiate(obstaclePrefab);
    }

    void Return(GameObject go)
    {
        go.SetActive(false);
        pool.Enqueue(go);
    }
}