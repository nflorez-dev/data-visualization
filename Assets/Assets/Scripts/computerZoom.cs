using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ComputerWebZoom : MonoBehaviour
{
    [Header("Posición de Destino")]
    [SerializeField] private Transform zoomTarget;

    [Header("Ajustes de Transición")]
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private float interactDistance = 4.0f;

    [Header("Referencias del Jugador")]
    [SerializeField] private PlayerMovement playerMovementScript;
    [SerializeField] private Transform playerCamera;

    [Header("Eventos al Completar Zoom")]
    public UnityEvent OnZoomCompleted;
    public UnityEvent OnExitCompleted;

    [Header("Controles")]
    [SerializeField] private KeyCode exitKey = KeyCode.Escape;

    private bool isZoomed;
    private bool isTransitioning;

    private Vector3 originalCamLocalPosition;
    private Quaternion originalCamLocalRotation;
    private Transform originalCamParent;
    private Coroutine activeTransition;

    private void Awake()
    {
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;
    }

    private void Update()
    {
        if (isZoomed && !isTransitioning && Input.GetKeyDown(exitKey))
        {
            StartExitZoom();
        }
    }

    private void OnMouseDown()
    {
        if (isZoomed || isTransitioning || playerCamera == null || zoomTarget == null) return;

        float distance = Vector3.Distance(playerMovementScript.transform.position, transform.position);
        if (distance <= interactDistance)
        {
            StartEnterZoom();
        }
    }

    private void StartEnterZoom()
    {
        if (activeTransition != null) StopCoroutine(activeTransition);

        originalCamParent = playerCamera.parent;
        originalCamLocalPosition = playerCamera.localPosition;
        originalCamLocalRotation = playerCamera.localRotation;

        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        playerCamera.SetParent(null);
        SetCursorState(unlocked: true);

        activeTransition = StartCoroutine(TransitionRoutine(zoomTarget.position, zoomTarget.rotation, () =>
        {
            isZoomed = true;
            OnZoomCompleted?.Invoke(); // Invoca la carga o activación de la UI Web
        }));
    }

    private void StartExitZoom()
    {
        if (activeTransition != null) StopCoroutine(activeTransition);

        OnExitCompleted?.Invoke(); // Desactiva la web antes de alejarse
        SetCursorState(unlocked: false);

        Vector3 targetWorldPos = originalCamParent.TransformPoint(originalCamLocalPosition);
        Quaternion targetWorldRot = originalCamParent.rotation * originalCamLocalRotation;

        activeTransition = StartCoroutine(TransitionRoutine(targetWorldPos, targetWorldRot, () =>
        {
            playerCamera.SetParent(originalCamParent);
            playerCamera.localPosition = originalCamLocalPosition;
            playerCamera.localRotation = originalCamLocalRotation;

            if (playerMovementScript != null)
                playerMovementScript.enabled = true;

            isZoomed = false;
        }));
    }

    private IEnumerator TransitionRoutine(Vector3 targetPosition, Quaternion targetRotation, System.Action onComplete)
    {
        isTransitioning = true;

        Vector3 startPosition = playerCamera.position;
        Quaternion startRotation = playerCamera.rotation;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float percent = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);

            playerCamera.position = Vector3.Lerp(startPosition, targetPosition, percent);
            playerCamera.rotation = Quaternion.Slerp(startRotation, targetRotation, percent);

            yield return null;
        }

        playerCamera.position = targetPosition;
        playerCamera.rotation = targetRotation;

        isTransitioning = false;
        onComplete?.Invoke();
    }

    private void SetCursorState(bool unlocked)
    {
        Cursor.lockState = unlocked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = unlocked;
    }
}