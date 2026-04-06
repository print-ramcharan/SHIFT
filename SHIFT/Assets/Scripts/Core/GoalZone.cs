using UnityEngine;

/// <summary>
/// Attach this to a trigger collider that marks the win condition.
/// When the target ShiftObject enters at the correct scale, the puzzle is solved.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GoalZone : MonoBehaviour
{
    [Header("Goal Settings")]
    [Tooltip("If set, only this specific ShiftObject can win. Leave null to accept any ShiftObject.")]
    public ShiftObject requiredObject;

    [Tooltip("The exact scale the object must be at to trigger the win (0 = any scale accepted).")]
    public float requiredScale = 0f;

    [Tooltip("How close to requiredScale the object's X scale must be (tolerance).")]
    public float scaleTolerance = 0.1f;

    [Header("Visual Feedback")]
    public GameObject highlightEffect;
    public ParticleSystem successParticles;

    private Collider _trigger;

    private void Awake()
    {
        _trigger = GetComponent<Collider>();
        _trigger.isTrigger = true;

        if (highlightEffect != null)
            highlightEffect.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // GDD: check by tag for reliability
        if (!other.CompareTag(Constants.TAG_SHIFT_OBJECT)) return;

        ShiftObject so = other.GetComponent<ShiftObject>();
        if (so == null) return;

        // If a specific object is required, make sure it matches
        if (requiredObject != null && so != requiredObject) return;

        // Check scale if required
        if (requiredScale > 0f)
        {
            float currentScaleFactor = so.transform.localScale.x / so.baseScale.x;
            if (Mathf.Abs(currentScaleFactor - requiredScale) > scaleTolerance) return;
        }

        TriggerWin();
    }

    private void OnTriggerExit(Collider other)
    {
        if (highlightEffect != null)
            highlightEffect.SetActive(false);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(Constants.TAG_SHIFT_OBJECT)) return;

        // Highlight when a shiftable object is inside the zone
        if (highlightEffect != null)
            highlightEffect.SetActive(true);
    }

    private void TriggerWin()
    {
        if (successParticles != null)
            successParticles.Play();

        if (highlightEffect != null)
            highlightEffect.SetActive(false);

        GameManager.Instance?.WinGame();
        Debug.Log("[GoalZone] Win triggered!");
    }
}
