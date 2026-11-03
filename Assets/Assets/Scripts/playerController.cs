using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5.0f;
    public float runSpeed = 8.0f;
    public float jumpHeight = 1.2f;
    public float gravity = -19.62f;

    [Header("Look Settings")]
    public Transform cameraTransform;
    public float mouseSensitivity = 15.0f;
    public float topLookLimit = 85.0f;
    public float bottomLookLimit = -85.0f;

    [Header("Interaction Settings")]
    public float interactDistance = 5.0f;
    public LayerMask interactableLayer = ~0; // Por defecto detecta todas las capas

    private CharacterController controller;
    private Vector3 verticalVelocity;
    private bool isGrounded;
    private float xRotation = 0f;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        // Buscar cámara si no está asignada en el Inspector
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cameraTransform = cam.transform;
            }
            else if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleLook();
        HandleMovement();
    }

    private void HandleLook()
    {
        if (Mouse.current == null || cameraTransform == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, bottomLookLimit, topLookLimit);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }

        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) vertical += 1f;
            if (Keyboard.current.sKey.isPressed) vertical -= 1f;
            if (Keyboard.current.aKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed) horizontal += 1f;
        }

        bool isRunning = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        Vector3 moveInput = (transform.right * horizontal) + (transform.forward * vertical);

        if (moveInput.magnitude > 1f)
        {
            moveInput.Normalize();
        }

        controller.Move(moveInput * currentSpeed * Time.deltaTime);

        bool jumpPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        if (jumpPressed && isGrounded)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }
}