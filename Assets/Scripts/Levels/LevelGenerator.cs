using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedurally generates a single puzzle room from a seed integer.
/// Same seed = same room layout, every time, on any device.
///
/// USAGE:
///   1. Attach to a persistent GameObject (e.g. the GameManager object).
///   2. Assign roomParent, floorPrefab, wallPrefab, and objectDatabase in Inspector.
///   3. Subscribe to DailySeed.OnSeedReady OR call GenerateRoom(seed) manually.
///
/// Pass condition (GDD Phase 2): same seed produces same room on 2 different devices.
/// </summary>
public class LevelGenerator : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("Parent transform that will hold all generated room objects.")]
    public Transform roomParent;

    [Tooltip("ObjectDatabase ScriptableObject — assign the one you created.")]
    public ObjectDatabase objectDatabase;

    [Header("Room Prefabs")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject ceilingPrefab;

    [Header("Room Dimensions")]
    [Tooltip("Room width in Unity units (X).")]
    public float roomWidth  = 10f;

    [Tooltip("Room length in Unity units (Z).")]
    public float roomLength = 10f;

    [Tooltip("Room height in Unity units (Y).")]
    public float roomHeight = 4f;

    [Header("Object Spawning")]
    [Tooltip("How many ShiftObjects to spawn per room.")]
    [Range(1, 6)]
    public int objectCount = 3;

    [Tooltip("Room difficulty tier — controls which objects can appear.")]
    [Range(1, 5)]
    public int roomTier = 1;

    [Tooltip("Minimum distance between spawned objects.")]
    public float minObjectSpacing = 1.5f;

    // ─── State ───────────────────────────────────────────────────────────────────

    private System.Random _rng;
    private readonly List<GameObject> _spawnedObjects = new List<GameObject>();

    // ─── Lifecycle ───────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        DailySeed.OnSeedReady += GenerateRoom;
    }

    private void OnDisable()
    {
        DailySeed.OnSeedReady -= GenerateRoom;
    }

    // ─── Public API ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Main entry point. Called automatically when DailySeed fires,
    /// or call manually from tests/editor tools.
    /// </summary>
    public void GenerateRoom(int seed)
    {
        ClearRoom();

        _rng = new System.Random(seed);

        Debug.Log($"[LevelGenerator] Generating room with seed {seed}");

        BuildRoomGeometry();
        SpawnObjects();

        Debug.Log($"[LevelGenerator] Room ready — {_spawnedObjects.Count} objects placed.");
    }

    // ─── Room Geometry ───────────────────────────────────────────────────────────

    private void BuildRoomGeometry()
    {
        // Floor
        SpawnPanel(floorPrefab,
            position: new Vector3(0, 0, 0),
            scale:    new Vector3(roomWidth, 1f, roomLength),
            tag:      "Surface");

        // Ceiling
        if (ceilingPrefab != null)
            SpawnPanel(ceilingPrefab,
                position: new Vector3(0, roomHeight, 0),
                scale:    new Vector3(roomWidth, 1f, roomLength));

        // Walls (N, S, E, W)
        float halfW = roomWidth  * 0.5f;
        float halfL = roomLength * 0.5f;
        float halfH = roomHeight * 0.5f;

        // North
        SpawnPanel(wallPrefab,
            position: new Vector3(0, halfH, halfL),
            scale:    new Vector3(roomWidth, roomHeight, 1f),
            tag:      "Surface");

        // South
        SpawnPanel(wallPrefab,
            position: new Vector3(0, halfH, -halfL),
            scale:    new Vector3(roomWidth, roomHeight, 1f),
            tag:      "Surface");

        // East
        SpawnPanel(wallPrefab,
            position: new Vector3(halfW, halfH, 0),
            scale:    new Vector3(1f, roomHeight, roomLength),
            tag:      "Surface");

        // West
        SpawnPanel(wallPrefab,
            position: new Vector3(-halfW, halfH, 0),
            scale:    new Vector3(1f, roomHeight, roomLength),
            tag:      "Surface");
    }

    private void SpawnPanel(GameObject prefab, Vector3 position, Vector3 scale, string tag = null)
    {
        if (prefab == null) return;
        GameObject go = Instantiate(prefab, position, Quaternion.identity, roomParent);
        go.transform.localScale = scale;
        if (tag != null) go.tag = tag;
        _spawnedObjects.Add(go);
    }

    // ─── Object Spawning ─────────────────────────────────────────────────────────

    private void SpawnObjects()
    {
        if (objectDatabase == null)
        {
            Debug.LogWarning("[LevelGenerator] No ObjectDatabase assigned!");
            return;
        }

        ObjectDatabase.ShiftObjectData[] pool = objectDatabase.GetForTier(roomTier);
        if (pool.Length == 0)
        {
            Debug.LogWarning("[LevelGenerator] ObjectDatabase has no entries for this tier.");
            return;
        }

        List<Vector3> placedPositions = new List<Vector3>();
        int attempts = 0;
        int spawned  = 0;

        while (spawned < objectCount && attempts < objectCount * 20)
        {
            attempts++;

            // Deterministic random position inside room (margins from walls)
            float margin = 1.5f;
            float x = Lerp(-roomWidth  * 0.5f + margin, roomWidth  * 0.5f - margin, NextFloat());
            float z = Lerp(-roomLength * 0.5f + margin, roomLength * 0.5f - margin, NextFloat());
            Vector3 candidate = new Vector3(x, 0.5f, z);

            // Reject if too close to another object
            if (IsTooClose(candidate, placedPositions)) continue;

            // Pick a random object from pool (weighted by difficultyWeight inverse)
            ObjectDatabase.ShiftObjectData data = PickRandom(pool);
            if (data?.prefab == null) continue;

            GameObject obj = Instantiate(data.prefab, candidate, Quaternion.identity, roomParent);
            obj.tag = Constants.TAG_SHIFT_OBJECT;
            _spawnedObjects.Add(obj);
            placedPositions.Add(candidate);
            spawned++;

            Debug.Log($"[LevelGenerator] Spawned '{data.displayName}' at {candidate}");
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private bool IsTooClose(Vector3 candidate, List<Vector3> placed)
    {
        foreach (var p in placed)
            if (Vector3.Distance(candidate, p) < minObjectSpacing) return true;
        return false;
    }

    private ObjectDatabase.ShiftObjectData PickRandom(ObjectDatabase.ShiftObjectData[] pool)
    {
        // Build inverse-weight list (lower difficultyWeight = more likely at low tiers)
        float totalWeight = 0f;
        foreach (var item in pool)
            totalWeight += 1f / Mathf.Max(1, item.difficultyWeight);

        float roll = (float)(NextFloat() * totalWeight);
        float cumulative = 0f;

        foreach (var item in pool)
        {
            cumulative += 1f / Mathf.Max(1, item.difficultyWeight);
            if (roll <= cumulative) return item;
        }

        return pool[pool.Length - 1];
    }

    /// <summary>Float in [0,1) from the seeded RNG — deterministic.</summary>
    private float NextFloat() => (float)_rng.NextDouble();

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    // ─── Cleanup ─────────────────────────────────────────────────────────────────

    public void ClearRoom()
    {
        foreach (var go in _spawnedObjects)
            if (go != null) Destroy(go);
        _spawnedObjects.Clear();
    }
}
