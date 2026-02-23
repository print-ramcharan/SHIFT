using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First-person player controller for SHIFT.
/// Handles WASD movement and mouse/touch look.
/// Designed to work with Unity's new Input System.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float gravity   = -9.81f;

    [Header("Look")]
    public float mouseSensitivity = 0.15f;
    [Tooltip("Transform of the camera (child of player root).")]
    public Transform cameraTransform;

    // Internal
    private CharacterController _cc;
    private Vector3 _velocity;
    private float   _xRotation;
    private bool    _cursorLocked;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (cameraTransform == null)
            cameraTransform = Camera.main?.transform;
    }

    private void Start()
    {
        LockCursor(true);
    }

    private void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        HandleLook();
        HandleMovement();
        HandleCursorToggle();
    }

    // ─── Look ────────────────────────────────────────────────────────────────────

    private void HandleLook()
    {
        if (!_cursorLocked) return;

        Vector2 delta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            delta = Mouse.current.delta.ReadValue();
#else
        delta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif

        float mouseX = delta.x * mouseSensitivity;
        float mouseY = delta.y * mouseSensitivity;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -85f, 85f);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    // ─── Movement ────────────────────────────────────────────────────────────────

    private void HandleMovement()
    {
        Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)    input.y += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)  input.y -= 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  input.x -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1;
        }
#else
        input.x = Input.GetAxis("Horizontal");
        input.y = Input.GetAxis("Vertical");
#endif

        Vector3 move = transform.right * input.x + transform.forward * input.y;
        _cc.Move(move * moveSpeed * Time.deltaTime);

        // Gravity
        if (_cc.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;

        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    // ─── Cursor ──────────────────────────────────────────────────────────────────

    private void HandleCursorToggle()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
#else
        if (Input.GetKeyDown(KeyCode.Escape))
#endif
        {
            LockCursor(!_cursorLocked);
        }
    }

    private void LockCursor(bool locked)
    {
        _cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }
}
