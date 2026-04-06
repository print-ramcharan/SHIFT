using UnityEngine;

/// <summary>
/// ScriptableObject that acts as the registry of all placeable objects in SHIFT.
/// Create one instance via Assets → Create → SHIFT → Object Database.
/// Assign ShiftObjectData entries in the Inspector.
/// LevelGenerator queries this database when building a room from a seed.
/// </summary>
[CreateAssetMenu(fileName = "ObjectDatabase", menuName = "SHIFT/Object Database")]
public class ObjectDatabase : ScriptableObject
{
    [System.Serializable]
    public class ShiftObjectData
    {
        [Tooltip("Unique ID used by the seed system to deterministically pick objects.")]
        public string id;

        [Tooltip("The prefab to spawn. Must have a ShiftObject component.")]
        public GameObject prefab;

        [Tooltip("Display name shown in hints or UI.")]
        public string displayName;

        [Tooltip("Difficulty weight — higher = less likely to appear in early levels.")]
        [Range(1, 10)]
        public int difficultyWeight = 1;

        [Tooltip("Minimum room tier this object can appear in (1 = always available).")]
        public int minRoomTier = 1;

        [Tooltip("The correct scale multiplier this object must reach to solve a puzzle (0 = no constraint).")]
        public float requiredScaleMultiplier = 0f;
    }

    [Header("All Placeable Objects")]
    public ShiftObjectData[] objects;

    /// <summary>
    /// Returns a ShiftObjectData by its unique ID. Returns null if not found.
    /// </summary>
    public ShiftObjectData GetById(string id)
    {
        foreach (var obj in objects)
            if (obj.id == id) return obj;
        return null;
    }

    /// <summary>
    /// Returns all objects valid for a given room tier.
    /// </summary>
    public ShiftObjectData[] GetForTier(int tier)
    {
        var result = new System.Collections.Generic.List<ShiftObjectData>();
        foreach (var obj in objects)
            if (obj.minRoomTier <= tier) result.Add(obj);
        return result.ToArray();
    }
}
