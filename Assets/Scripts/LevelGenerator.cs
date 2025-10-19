using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Transform player;
    [SerializeField] Transform mainCamera;

    // ---------- SPIKES ----------
    [Header("SPIKES — Prefabs (Tag='Spikes' & Trigger colliders)")]
    [SerializeField] GameObject spikeBottomPrefab; // sits on ground lane
    [SerializeField] GameObject spikeTopPrefab;    // hangs from ceiling lane

    [Header("SPIKES — Spawn / Despawn")]
    [SerializeField] float firstSpawnX = 12f;
    [SerializeField, Range(3f,12f)] float spawnEveryX = 6f; // base spacing (will tighten slowly)
    [SerializeField] float keepAheadByX = 24f;              // keep content this far ahead of camera
    [SerializeField] float despawnBehindX = -12f;           // negative value (how far behind to recycle)

    [Header("SPIKES — Clusters & Chances")]
    [SerializeField, Range(1,4)] int   minCluster = 1;
    [SerializeField, Range(1,4)] int   maxCluster = 3;
    [SerializeField, Range(0f,1f)] float topProbability   = 0.55f;
    [SerializeField, Range(0f,1f)] float emptyLaneChance  = 0.22f; // baseline; eased in dynamically
    [SerializeField, Range(0f,1f)] float doubleLaneChance = 0.10f; // baseline; eased in dynamically

    [Header("SPIKES — Difficulty Ramp")]
    [SerializeField] float minSpacing = 4.8f;          // never go tighter than this
    [SerializeField] float spacingDecreasePerSec = 0.10f;

    [Header("SPIKES — Safety / Fairness")]
    [SerializeField] bool  forbidBackToBackDouble = true;
    [SerializeField] float recoveryGapAfterDouble = 1.35f;
    [SerializeField] int   largeClusterThreshold  = 3;
    [SerializeField] float recoveryGapAfterLarge  = 1.15f;
    [SerializeField] int   maxConsecutiveSameLane = 2;
    [SerializeField] float noDoubleBeforeSeconds  = 5f;

    [Header("SPIKES — Empty Slot Control")]
    [SerializeField] int   maxEmptyStreak = 2;
    int emptyStreak = 0;

    [Header("SPIKES — Pool")]
    [SerializeField] int poolSizePerType = 8;
    
    
    [SerializeField] Transform bottomLaneParent;
    [SerializeField] Transform topLaneParent;

    readonly List<GameObject> poolBottom = new();
    readonly List<GameObject> poolTop    = new();

    float nextSpawnX;
    float _elapsed;
    bool  lastWasDouble;
    bool? lastLaneTop = null;
    int   sameLaneStreak = 0;

    // ---------- PLATFORMS ----------
    [Header("PLATFORMS — Prefabs & Pool (solid BoxCollider2D, NOT trigger)")]
    [SerializeField] GameObject bottomPlatformPrefab;
    [SerializeField] GameObject topPlatformPrefab;
    [SerializeField] int platformPoolSizePerLane = 24;

    [Header("PLATFORMS — Layout")]
    [SerializeField] float tileWidth = 5.95f;  // 595px @ PPU=100 × scaleX=1 → 5.95
    [SerializeField] float bottomBaseY = -1.8f;
    [SerializeField] float topBaseY    = +1.8f;

    [Header("PLATFORMS — Gaps (tile counts)")]
    [SerializeField] int minChunkTiles = 2;
    [SerializeField] int maxChunkTiles = 6;
    [SerializeField] int minGapTiles   = 1;
    [SerializeField] int maxGapTiles   = 3;

    [Header("PLATFORMS — Safety")]
    [SerializeField] bool forbidBackToBackGaps = true;

    [Header("PLATFORMS — Vertical Jitter")]
    [SerializeField] float maxJitterStart = 0.00f; // no wobble at start
    [SerializeField] float maxJitterEnd   = 0.25f; // grows to ~0.25 by 40s
    [SerializeField] float jitterPerlinScale = 0.15f;

    [Header("PLATFORMS — Safe Intro Runway")]
    [SerializeField] float introStartX = -12f;
    [SerializeField] float introEndX   =  18f;

    readonly List<GameObject> poolPlatBottom = new();
    readonly List<GameObject> poolPlatTop    = new();

    float nextPlatformX_Bottom;
    float nextPlatformX_Top;
    bool  lastWasGap_Bottom;
    bool  lastWasGap_Top;

    // Tile registries so spikes only spawn where a platform exists
    readonly Dictionary<int, float> bottomTileY = new(); // key=index, value=Y used
    readonly Dictionary<int, float> topTileY    = new();

    void Start()
    {
        if (!player)     player     = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (!mainCamera) mainCamera = Camera.main?.transform;

        // spikes (no lane parent)
        BuildPool(spikeBottomPrefab, poolBottom, poolSizePerType);
        BuildPool(spikeTopPrefab,    poolTop,    poolSizePerType);

// platforms (lane parents!)
        BuildPool(bottomPlatformPrefab, poolPlatBottom, platformPoolSizePerLane, bottomLaneParent);
        BuildPool(topPlatformPrefab,    poolPlatTop,    platformPoolSizePerLane, topLaneParent);
        
        foreach (var go in poolPlatBottom) go.transform.SetParent(bottomLaneParent, true);
        foreach (var go in poolPlatTop)    go.transform.SetParent(topLaneParent,    true);



        // Prefill a bottom runway so player never drops at start
        for (float x = introStartX; x <= introEndX; x += tileWidth)
        {
            var go = GetFromPool(poolPlatBottom);
            if (!go) break;
            go.transform.SetParent(bottomLaneParent, false); 
            float yB = bottomBaseY;
            go.transform.position = new Vector3(x, yB, 0f);
            go.SetActive(true);
            bottomTileY[TileIndex(x)] = yB;
        }

        nextPlatformX_Bottom = introEndX + tileWidth; // generator continues after intro
        nextPlatformX_Top    = 0f;

        nextSpawnX = firstSpawnX;
    }

    void Update()
    {
        if (!mainCamera) return;

        _elapsed += Time.deltaTime;
        
        float targetAheadX = mainCamera.position.x + keepAheadByX;


        // ---------- SPIKES (difficulty bias) ----------
        // --- Difficulty bias for SPIKES (start gentle, ramp later)
        float t = Mathf.Clamp01(_elapsed / 30f);   // 0..1 over ~30s

// Start SUPER empty (85%) → fade to your inspector value
        float dynEmptyLaneChance  = Mathf.Lerp(0.85f, emptyLaneChance,  t);
// Start with no doubles → fade to your inspector value
        float dynDoubleLaneChance = Mathf.Lerp(0.00f, doubleLaneChance, t);
// Lane bias eases in (balanced at start)
        float dynTopProbability   = Mathf.Lerp(0.50f, topProbability,   t);

// spacing also “opens up” at the very beginning, then returns to normal
        float currentSpacing = Mathf.Max(
            minSpacing,
            (spawnEveryX - spacingDecreasePerSec * _elapsed) * Mathf.Lerp(1.6f, 1.0f, Mathf.Clamp01(_elapsed / 8f))
        );


        while (nextSpawnX <= targetAheadX)
        {
            // empty streak limiter
            bool skip = (Random.value < dynEmptyLaneChance) && (emptyStreak < maxEmptyStreak);
            if (skip) { emptyStreak++; nextSpawnX += currentSpacing; continue; }
            emptyStreak = 0;

            // cluster size
            int count = (_elapsed < 10f) ? 1 : Random.Range(minCluster, maxCluster + 1);

            // double-lane rules
            bool wantDouble = Random.value < dynDoubleLaneChance;
            if (_elapsed < noDoubleBeforeSeconds) wantDouble = false;     // grace early
            if (forbidBackToBackDouble && lastWasDouble) wantDouble = false;

            // lane choose with streak limiter
            bool topLane = Random.value < dynTopProbability;
            if (lastLaneTop.HasValue && lastLaneTop.Value == topLane)
                if (sameLaneStreak >= maxConsecutiveSameLane - 1) topLane = !topLane;

            int spawnedThisCluster = 0;
            for (int i = 0; i < count; i++)
            {
                if (wantDouble)
                {
                    int idx = TileIndex(nextSpawnX);
                    bool spawnedAny = false;
                    if (bottomTileY.TryGetValue(idx, out float yB)) { SpawnAt(nextSpawnX, yB, false); spawnedAny = true; }
                    if (topTileY.TryGetValue(idx, out float yT))     { SpawnAt(nextSpawnX, yT, true);  spawnedAny = true; }
                    if (!spawnedAny) { nextSpawnX += currentSpacing * 0.65f; continue; }
                }
                else
                {
                    int idx = TileIndex(nextSpawnX);
                    if (topLane)
                    {
                        if      (topTileY.TryGetValue(idx, out float yT)) { SpawnAt(nextSpawnX, yT, true); }
                        else if (bottomTileY.TryGetValue(idx, out float yB)) { SpawnAt(nextSpawnX, yB, false); }
                        else { nextSpawnX += currentSpacing * 0.65f; continue; }
                    }
                    else
                    {
                        if      (bottomTileY.TryGetValue(idx, out float yB)) { SpawnAt(nextSpawnX, yB, false); }
                        else if (topTileY.TryGetValue(idx,    out float yT)) { SpawnAt(nextSpawnX, yT, true);  }
                        else { nextSpawnX += currentSpacing * 0.65f; continue; }
                    }
                }

                spawnedThisCluster++;
                nextSpawnX += currentSpacing * 0.65f; // intra-cluster
            }

            // post-cluster recovery
            float postMul = 1.25f;
            if (wantDouble) { postMul = Mathf.Max(postMul, recoveryGapAfterDouble); lastWasDouble = true; }
            else            { lastWasDouble = false; }
            if (spawnedThisCluster >= largeClusterThreshold) postMul = Mathf.Max(postMul, recoveryGapAfterLarge);

            nextSpawnX += currentSpacing * postMul;

            // lane streak bookkeeping
            if (!wantDouble)
            {
                if (!lastLaneTop.HasValue || lastLaneTop.Value != topLane) sameLaneStreak = 1;
                else sameLaneStreak++;
                lastLaneTop = topLane;
            }
        }

        // ---------- PLATFORMS (stable + gentle ramp) ----------
        float viewAheadX = mainCamera.position.x + keepAheadByX;

        float gapBiasP = Mathf.Clamp01(_elapsed / 30f);
        int dynMinGap   = minGapTiles;
        int dynMaxGap   = Mathf.RoundToInt(Mathf.Lerp(maxGapTiles, maxGapTiles + 2, gapBiasP));
        int dynMinChunk = minChunkTiles;
        int dynMaxChunk = Mathf.RoundToInt(Mathf.Lerp(maxChunkTiles, Mathf.Max(2, maxChunkTiles - 2), gapBiasP));

        // bottom lane
        while (nextPlatformX_Bottom <= viewAheadX)
        {
            bool wantGap = (Random.value < 0.5f + 0.3f * gapBiasP);
            if (forbidBackToBackGaps && lastWasGap_Bottom) wantGap = false;

            int tiles = wantGap
                ? Random.Range(dynMinGap,   dynMaxGap + 1)
                : Random.Range(dynMinChunk, dynMaxChunk + 1);

            if (wantGap)
            {
                nextPlatformX_Bottom += tiles * tileWidth;
                lastWasGap_Bottom = true;
            }
            else
            {
                for (int i = 0; i < tiles; i++)
                {
                    // vertical jitter grows over time
                    float jt  = Mathf.Lerp(maxJitterStart, maxJitterEnd, Mathf.Clamp01(_elapsed / 40f));
                    float yJ  = (Mathf.PerlinNoise(nextPlatformX_Bottom * jitterPerlinScale, 0.123f) - 0.5f) * 2f * jt;
                    float yB  = bottomBaseY + yJ;
if (nextPlatformX_Bottom < introEndX) yB = bottomBaseY;              // no jitter in intro stretch
yB = Mathf.Clamp(yB, bottomBaseY - 0.35f, bottomBaseY + 0.35f);      // tiny clamp (matches maxJitterEnd)

                    var go = GetFromPool(poolPlatBottom);
                    if (!go) break;
                    go.transform.SetParent(bottomLaneParent, false);   
                    go.transform.position = new Vector3(nextPlatformX_Bottom, yB, 0f);
                    go.SetActive(true);

                    bottomTileY[TileIndex(nextPlatformX_Bottom)] = yB;
                    nextPlatformX_Bottom += tileWidth;

                }
                lastWasGap_Bottom = false;
            }
        }

        // top lane
        while (nextPlatformX_Top <= viewAheadX)
        {
            bool wantGap = (Random.value < 0.5f + 0.3f * gapBiasP);
            if (forbidBackToBackGaps && lastWasGap_Top) wantGap = false;

            int tiles = wantGap
                ? Random.Range(dynMinGap,   dynMaxGap + 1)
                : Random.Range(dynMinChunk, dynMaxChunk + 1);

            if (wantGap)
            {
                nextPlatformX_Top += tiles * tileWidth;
                lastWasGap_Top = true;
            }
            else
            {
                for (int i = 0; i < tiles; i++)
                {
                    float jt  = Mathf.Lerp(maxJitterStart, maxJitterEnd, Mathf.Clamp01(_elapsed / 40f));
                    float yJ  = (Mathf.PerlinNoise(nextPlatformX_Top * jitterPerlinScale, 0.789f) - 0.5f) * 2f * jt;
                    float yT  = topBaseY + yJ;
if (nextPlatformX_Top < introEndX) yT = topBaseY;                    // no jitter in intro stretch
yT = Mathf.Clamp(yT, topBaseY - 0.35f, topBaseY + 0.35f);

                    var go = GetFromPool(poolPlatTop);
                    if (!go) break;
                    go.transform.SetParent(topLaneParent, false);      
                    go.transform.position = new Vector3(nextPlatformX_Top, yT, 0f);
                    go.SetActive(true);

                    topTileY[TileIndex(nextPlatformX_Top)] = yT; // register tile

                    nextPlatformX_Top += tileWidth;
                }
                lastWasGap_Top = false;
            }
        }

        // Recycle behind camera (platforms unregister their tile indices)
        RecycleOldPlatforms(poolPlatBottom, isTop:false);
        RecycleOldPlatforms(poolPlatTop,    isTop:true);
        // Spikes can use simple recycle (no registry)
        RecycleOld(poolBottom);
        RecycleOld(poolTop);
    }

    // ---------- Helpers ----------
    int TileIndex(float x) => Mathf.RoundToInt(x / tileWidth);

    void BuildPool(GameObject prefab, List<GameObject> pool, int count, Transform parent = null)
    {
        if (!prefab) return;
        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(prefab, parent ? parent : transform);
            go.SetActive(false);
            pool.Add(go);
        }
    }



    GameObject GetFromPool(List<GameObject> pool)
    {
        for (int i = 0; i < pool.Count; i++)
            if (!pool[i].activeSelf) return pool[i];

        if (pool.Count == 0) return null;
        var extra = Instantiate(pool[0], transform);
        extra.SetActive(false);
        pool.Add(extra);
        return extra;
    }

    void RecycleOld(List<GameObject> pool)
    {
        if (!mainCamera) return;
        float cutoff = mainCamera.position.x + despawnBehindX; // despawnBehindX is negative
        foreach (var go in pool)
            if (go.activeSelf && go.transform.position.x < cutoff)
                go.SetActive(false);
    }

    void RecycleOldPlatforms(List<GameObject> pool, bool isTop)
    {
        if (!mainCamera) return;
        float cutoff = mainCamera.position.x + despawnBehindX;
        foreach (var go in pool)
        {
            if (go.activeSelf && go.transform.position.x < cutoff)
            {
                go.SetActive(false);
                int idx = TileIndex(go.transform.position.x);
                if (isTop) topTileY.Remove(idx); else bottomTileY.Remove(idx);
            }
        }
    }

    // spikes from correct pool at (x, y)
    void SpawnAt(float x, float y, bool top)
    {
        var go = GetFromPool(top ? poolTop : poolBottom);
        if (!go) return;
        go.transform.position = new Vector3(x, y, 0f);
        go.SetActive(true);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green; Gizmos.DrawLine(new Vector3(-100, bottomBaseY, 0), new Vector3(100, bottomBaseY, 0));
        Gizmos.color = Color.red;   Gizmos.DrawLine(new Vector3(-100, topBaseY,    0), new Vector3(100, topBaseY,    0));
    }
#endif
}
