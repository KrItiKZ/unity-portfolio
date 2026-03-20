using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3.0f;
    public float sensitivity = 0.8f;
    public float interactionDistance = 5f;
    public float rightClickZoomFOV = 30f;

    [Header("Realistic Movement")]
    public float acceleration = 8.0f;
    public float deceleration = 10.0f;
    public float stepInterval = 0.4f;
    public float headBobAmount = 0.015f;
    public float headBobSpeed = 3.5f;
    public AudioSource footstepAudio;
    public AudioClip[] footstepSounds;

    private Camera playerCamera;
    private CharacterController characterController;
    private Transform playerBody;

    private float xRotation;
    public Vector3 originalCameraLocalPosition;
    public Quaternion originalCameraLocalRotation;
    private Door currentDoor;
    private bool isRightClickZoomed = false;
    private bool controlsLocked = false;

    private float originalFOV;
    private bool isOriginalFOVSet = false;

    private Vector3 currentVelocity;
    private Vector2 movementInput;
    private bool isMoving = false;
    private float stepTimer = 0f;
    private float headBobTimer = 0f;

    private Vector2 currentMouseDelta;
    private Vector2 mouseDeltaVelocity;
    private float mouseSmoothTime = 0.03f;

    private Vector3 headBobOffset;
    private float headBobReturnSpeed = 12f;

    private bool doorInteractionEnabled = true;

    private Camera mainCamera;
    private bool isMainCameraCached = false;


    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;

        
        playerCamera = GetComponent<Camera>();
        playerBody = transform.parent;

        
        if (playerBody != null)
        {
            characterController = playerBody.GetComponent<CharacterController>();
        }

        
        originalCameraLocalPosition = transform.localPosition;
        originalCameraLocalRotation = transform.localRotation;

        if (playerCamera != null && !isOriginalFOVSet)
        {
            originalFOV = playerCamera.fieldOfView;
            isOriginalFOVSet = true;
        }

        
        if (characterController == null)
        {
            Debug.LogError("CharacterController ?? ?????? ?? ???????????? ???????!");
        }
    }

    public void LockControls(bool locked)
    {
        controlsLocked = locked;

        if (locked)
        {
            StopAllCoroutines();
            
            movementInput = Vector2.zero;
            currentVelocity = Vector3.zero;
            currentMouseDelta = Vector2.zero;
        }
    }

    public void ForceExitZoom()
    {
        currentDoor = null;
    }

    public void ResetCamera()
    {
        StopAllCoroutines();
        currentDoor = null;
        controlsLocked = false;
        doorInteractionEnabled = true;

        transform.localPosition = originalCameraLocalPosition;
        transform.localRotation = originalCameraLocalRotation;

        xRotation = 0f;
        transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

        if (playerBody != null)
        {
            playerBody.rotation = Quaternion.identity;
        }

        movementInput = Vector2.zero;
        currentVelocity = Vector3.zero;
        currentMouseDelta = Vector2.zero;
        headBobOffset = Vector3.zero;

        if (playerCamera != null)
        {
            playerCamera.fieldOfView = originalFOV;
        }

        isRightClickZoomed = false;
    }

    public void TeleportPlayer(Vector3 position, Quaternion rotation)
    {
        Debug.Log($"Teleporting player to: {position}");

        
        StopAllCoroutines();

        
        currentDoor = null;
        doorInteractionEnabled = true;

        
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        
        if (playerBody != null)
        {
            playerBody.position = position;
            playerBody.rotation = rotation;
        }
        else
        {
            transform.position = position;
            transform.rotation = rotation;
        }

        
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        
        transform.localPosition = originalCameraLocalPosition;
        transform.localRotation = originalCameraLocalRotation;
        xRotation = 0f;

        
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = originalFOV;
        }

        
        movementInput = Vector2.zero;
        currentVelocity = Vector3.zero;
        currentMouseDelta = Vector2.zero;
        headBobOffset = Vector3.zero;

        Debug.Log("Teleport completed successfully");
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        if (controlsLocked) return;

        HandleMovement();
        HandleCameraRotation();
        HandleHeadBob();

        if (doorInteractionEnabled)
        {
            CheckForDoor();
        }

        
        if (Mouse.current.leftButton.wasPressedThisFrame && doorInteractionEnabled && currentDoor != null)
        {
            currentDoor.Interact();
        }

        HandleRightClickZoom();
    }

    public void DisableDoorInteraction()
    {
        doorInteractionEnabled = false;
        currentDoor = null;
    }

    public void EnableDoorInteraction()
    {
        doorInteractionEnabled = true;
    }

    private void HandleMovement()
    {
        if (characterController == null) return;

        
        movementInput = Vector2.zero;

        if (Keyboard.current.wKey.isPressed) movementInput.y += 1f;
        if (Keyboard.current.sKey.isPressed) movementInput.y -= 1f;
        if (Keyboard.current.aKey.isPressed) movementInput.x -= 1f;
        if (Keyboard.current.dKey.isPressed) movementInput.x += 1f;

        
        if (movementInput.magnitude > 1f)
        {
            movementInput.Normalize();
        }

        
        Vector3 moveDirection = transform.forward * movementInput.y + transform.right * movementInput.x;
        moveDirection.y = 0f;

        
        float targetSpeed = movementInput.magnitude > 0.1f ? walkSpeed : 0f;

        if (targetSpeed > 0f)
        {
            currentVelocity = Vector3.Lerp(currentVelocity, moveDirection.normalized * targetSpeed, acceleration * Time.deltaTime);
            isMoving = currentVelocity.magnitude > 0.1f;
        }
        else
        {
            currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, deceleration * Time.deltaTime);
            if (currentVelocity.magnitude < 0.01f)
            {
                currentVelocity = Vector3.zero;
                isMoving = false;
            }
        }

        
        if (characterController != null && characterController.enabled)
        {
            if (!characterController.isGrounded)
            {
                currentVelocity.y -= 9.81f * Time.deltaTime;
            }
            else
            {
                currentVelocity.y = -0.5f;
            }

            characterController.Move(currentVelocity * Time.deltaTime);
        }

        
        HandleFootsteps();
    }

    private void HandleHeadBob()
    {
        if (!isMoving || characterController == null || !characterController.isGrounded || currentVelocity.magnitude < 0.2f)
        {
            headBobOffset = Vector3.Lerp(headBobOffset, Vector3.zero, headBobReturnSpeed * Time.deltaTime);
            transform.localPosition = originalCameraLocalPosition + headBobOffset;
            headBobTimer = 0f;
            return;
        }

        float speedFactor = Mathf.Clamp01(currentVelocity.magnitude / walkSpeed);
        headBobTimer += Time.deltaTime * headBobSpeed * speedFactor;

        float bobY = Mathf.Sin(headBobTimer * 2.5f) * headBobAmount * speedFactor;
        float bobX = Mathf.Sin(headBobTimer) * headBobAmount * 0.1f * speedFactor;

        headBobOffset = new Vector3(bobX, bobY, 0f);
        transform.localPosition = originalCameraLocalPosition + headBobOffset;
    }

    private void HandleFootsteps()
    {
        if (isMoving && characterController != null && characterController.isGrounded && currentVelocity.magnitude > 0.2f)
        {
            stepTimer += Time.deltaTime;

            if (stepTimer >= stepInterval)
            {
                PlayFootstepSound();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = stepInterval * 0.3f;
        }
    }

    private void PlayFootstepSound()
    {
        if (footstepAudio != null && footstepSounds != null && footstepSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, footstepSounds.Length);
            float volume = Random.Range(0.08f, 0.15f);
            float pitch = Random.Range(0.98f, 1.02f);

            footstepAudio.pitch = pitch;
            footstepAudio.PlayOneShot(footstepSounds[randomIndex], volume);
        }
    }

    private void HandleRightClickZoom()
    {
        if (playerCamera == null || controlsLocked) return;

        if (Mouse.current.rightButton.isPressed)
        {
            isRightClickZoomed = true;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, rightClickZoomFOV, 5f * Time.deltaTime);
        }
        else if (isRightClickZoomed)
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, originalFOV, 5f * Time.deltaTime);

            if (Mathf.Abs(playerCamera.fieldOfView - originalFOV) < 0.1f)
            {
                playerCamera.fieldOfView = originalFOV;
                isRightClickZoomed = false;
            }
        }
    }
    private Camera GetMainCamera()
    {
        if (!isMainCameraCached || mainCamera == null)
        {
            mainCamera = Camera.main;
            isMainCameraCached = mainCamera != null;
        }
        return mainCamera;
    }

    private void CheckForDoor()
    {
        Camera camera = GetMainCamera();
        if (camera == null) return;

        Ray ray = camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        int layerMask = ~(1 << LayerMask.NameToLayer("Triggers"));

        if (Physics.Raycast(ray, out hit, interactionDistance, layerMask))
        {
            Door door = hit.collider.GetComponent<Door>();
            if (door != null)
            {
                
                if (door.isLockedAfterUse)
                {
                    
                    currentDoor = null;
                    
                }
                else
                {
                    currentDoor = door;
                }
                return;
            }
        }

        currentDoor = null;
    }

    private void HandleCameraRotation()
    {
        if (controlsLocked) return;

        Vector2 targetMouseDelta = Mouse.current.delta.ReadValue();

        currentMouseDelta = Vector2.SmoothDamp(currentMouseDelta, targetMouseDelta, ref mouseDeltaVelocity, mouseSmoothTime);

        if (currentMouseDelta.magnitude > 0.01f)
        {
            xRotation -= currentMouseDelta.y * sensitivity * 0.01f;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            if (playerBody != null)
            {
                playerBody.Rotate(Vector3.up * currentMouseDelta.x * sensitivity * 0.01f);
            }
        }
    }
}