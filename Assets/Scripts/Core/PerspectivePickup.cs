using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Core mechanic script. Attach to the Player Camera or Player GameObject.
/// Handles raycasting to detect ShiftObjects, picking them up, holding them, and dropping them.
/// The object scales based on how far it would land, implementing the perspective illusion.
/// </summary>
public class PerspectivePickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [Tooltip("Max distance the player can pick up an object.")]
    public float pickupRange = 5f;

    [Tooltip("Layer mask for interactable ShiftObjects.")]
    public LayerMask interactableLayer;

    [Tooltip("Layer mask for surfaces (floor, walls) used to determine drop distance.")]
    public LayerMask surfaceLayer;

    [Tooltip("How far in front of the camera the held object floats.")]
    public float holdDistance = 2f;

    [Tooltip("How smoothly the held object follows the camera.")]
    public float holdSmoothing = 10f;

    // State
    private ShiftObject _heldObject;
    private float _pickupDistance;   // distance at the moment of pickup
    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponentInChildren<Camera>();
        if (_cam == null)
            _cam = Camera.main;
    }

    private void Update()
    {
        HandleInput();

        if (_heldObject != null)
            MoveHeldObject();
    }

    // ─── Input ─────────────────────────────────────────────────────────────────

    private void HandleInput()
    {
        // Supports both new Input System (Mouse) and touch
        bool tapped = false;

#if ENABLE_INPUT_SYSTEM
        tapped = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (!tapped && Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            tapped = true;
#else
        tapped = Input.GetMouseButtonDown(0);
#endif

        if (!tapped) return;

        if (_heldObject == null)
            TryPickup();
        else
            Drop();
    }

    // ─── Pickup ─────────────────────────────────────────────────────────────────

    private void TryPickup()
    {
        Ray ray = _cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0));

        if (!Physics.Raycast(ray, out RaycastHit hit, pickupRange, interactableLayer))
            return;

        ShiftObject so = hit.collider.GetComponent<ShiftObject>();
        if (so == null) return;

        _heldObject = so;
        _pickupDistance = hit.distance;
        _heldObject.OnPickedUp();
    }

    // ─── Hold ────────────────────────────────────────────────────────────────────

    private void MoveHeldObject()
    {
        // Keep object in front of camera at holdDistance
        Vector3 targetPos = _cam.transform.position + _cam.transform.forward * holdDistance;
        _heldObject.transform.position = Vector3.Lerp(
            _heldObject.transform.position, targetPos, Time.deltaTime * holdSmoothing);

        // ── Perspective Scale ──────────────────────────────────────────────────
        // Cast a ray from the camera to see where the object would land if dropped
        Ray forwardRay = new Ray(_cam.transform.position, _cam.transform.forward);
        float raycastDistance = holdDistance; // default: use hold distance

        if (Physics.Raycast(forwardRay, out RaycastHit surfaceHit, 50f, surfaceLayer))
            raycastDistance = surfaceHit.distance;

        // GDD formula: newScale = baseScale * (raycastDistance / pickupDistance)
        float scaleMultiplier = raycastDistance / _pickupDistance;
        _heldObject.ApplyScale(scaleMultiplier);
    }

    // ─── Drop ────────────────────────────────────────────────────────────────────

    private void Drop()
    {
        _heldObject.OnDropped();
        _heldObject = null;
    }

    // ─── Debug Gizmos ───────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_cam == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(_cam.transform.position, _cam.transform.forward * pickupRange);
    }
#endif
}
