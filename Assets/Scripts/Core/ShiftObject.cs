using UnityEngine;

/// <summary>
/// Attach this to any interactable object in the scene.
/// Manages base scale, current scale, mass recalculation, and rigidbody state.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ShiftObject : MonoBehaviour
{
    [Header("Shift Settings")]
    [Tooltip("The scale this object starts at. Set automatically on Awake.")]
    public Vector3 baseScale;

    [Tooltip("The mass this object starts at. Set automatically on Awake.")]
    public float baseMass = 1f;

    [Tooltip("Min/Max scale multiplier to prevent objects from becoming too small or huge.")]
    public float minScaleMultiplier = 0.1f;
    public float maxScaleMultiplier = 5f;

    // Internal state
    public bool IsHeld { get; private set; }
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        baseScale = transform.localScale;
        baseMass = _rb.mass;
    }

    /// <summary>
    /// Called by PerspectivePickup when the player picks this object up.
    /// Makes the object kinematic (no physics while held).
    /// </summary>
    public void OnPickedUp()
    {
        IsHeld = true;
        _rb.isKinematic = true;
    }

    /// <summary>
    /// Called by PerspectivePickup when the player drops this object.
    /// Re-enables physics and recalculates mass based on new scale.
    /// </summary>
    public void OnDropped()
    {
        IsHeld = false;
        _rb.isKinematic = false;
        RecalculateMass();
    }

    /// <summary>
    /// Applies the new scale based on the GDD formula:
    /// newScale = baseScale * (raycastDistance / pickupDistance)
    /// </summary>
    public void ApplyScale(float scaleMultiplier)
    {
        scaleMultiplier = Mathf.Clamp(scaleMultiplier, minScaleMultiplier, maxScaleMultiplier);
        transform.localScale = baseScale * scaleMultiplier;
    }

    /// <summary>
    /// GDD formula: newMass = baseMass * (currentScale ^ 3)
    /// Uses the X component of scale as the uniform scale factor.
    /// </summary>
    private void RecalculateMass()
    {
        float scaleFactor = transform.localScale.x / baseScale.x;
        _rb.mass = baseMass * Mathf.Pow(scaleFactor, 3f);
    }
}
