using UnityEngine;

/// <summary>
/// Core mechanic script. Attach to the Player Camera or Player GameObject.
/// Handles raycasting to detect ShiftObjects, picking them up, holding them, and dropping them.
/// Uses legacy input (works out of the box with no extra packages).
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

    private ShiftObject _heldObject;
    private float _pickupDistance;
    private Camera _cam;
    private Quaternion _holdRotationOffset;

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
        if (Input.GetMouseButtonDown(0))
        {
            if (_heldObject == null) TryPickup();
            else Drop();
        }
    }

    // ─── Pickup ─────────────────────────────────────────────────────────────────

    private void TryPickup()
    {
        Ray ray = _cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0));
        if (!Physics.Raycast(ray, out RaycastHit hit, pickupRange, interactableLayer)) return;

        ShiftObject so = hit.collider.GetComponent<ShiftObject>();
        if (so == null) return;

        _heldObject = so;
        _pickupDistance = hit.distance;
        _holdRotationOffset = Quaternion.Inverse(_cam.transform.rotation) * _heldObject.transform.rotation;
        _heldObject.OnPickedUp();
        AudioManager.Instance?.Play(AudioManager.SFX.PickUp);
    }

    private void MoveHeldObject()
    {
        Ray forwardRay = new Ray(_cam.transform.position, _cam.transform.forward);
        float raycastDistance = 10f; // fallback if pointing at sky

        if (Physics.Raycast(forwardRay, out RaycastHit surfaceHit, 50f, surfaceLayer))
        {
            raycastDistance = surfaceHit.distance;
        }
        else
        {
            raycastDistance = 50f; // cap at 50 if looking at sky
        }

        // Calculate scale multiplier based on distance
        float scaleMultiplier = raycastDistance / _pickupDistance;

        // Offset backwards by the object's bounds so it sits flush against the wall
        float objectDepthOffset = _heldObject.baseScale.z * scaleMultiplier * 0.5f;
        float actualDistance = Mathf.Max(0.5f, raycastDistance - objectDepthOffset);

        // Re-calculate the visual scale based on the *actual* placed distance 
        // This ensures it perfectly matches screen size!
        float visualScale = actualDistance / _pickupDistance;
        _heldObject.ApplyScale(visualScale);

        // Position and rotation
        Vector3 targetPos = _cam.transform.position + _cam.transform.forward * actualDistance;
        Quaternion targetRot = _cam.transform.rotation * _holdRotationOffset;

        _heldObject.transform.position = Vector3.Lerp(
            _heldObject.transform.position, targetPos, Time.deltaTime * holdSmoothing);
            
        _heldObject.transform.rotation = Quaternion.Lerp(
            _heldObject.transform.rotation, targetRot, Time.deltaTime * holdSmoothing);
    }

    // ─── Drop ────────────────────────────────────────────────────────────────────

    private void Drop()
    {
        AudioManager.Instance?.Play(AudioManager.SFX.Drop);
        _heldObject.OnDropped();
        _heldObject = null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_cam == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(_cam.transform.position, _cam.transform.forward * pickupRange);
    }
#endif
}
