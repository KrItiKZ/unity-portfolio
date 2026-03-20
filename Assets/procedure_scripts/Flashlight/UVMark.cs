
using UnityEngine;

public class UvMark : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer markRenderer;
    public Sprite uvReactiveSprite;

    [Header("Settings")]
    public float detectionAngle = 30f;
    public float maxDistance = 5f;
    public float fadeSpeed = 5f;

    private UVFlashlight cachedFlashlight;
    private float visibility = 0f;
    private float nextFlashlightCheck = 0f;
    private const float FLASHLIGHT_CHECK_INTERVAL = 0.5f;
    private bool hasValidFlashlight = false;

    private void Start()
    {
        
        if (markRenderer != null && uvReactiveSprite != null)
        {
            markRenderer.sprite = uvReactiveSprite;
            markRenderer.color = new Color(1f, 1f, 1f, 0f);
        }

        CacheFlashlightReference();
    }

    private void Update()
    {
        
        if (Time.time >= nextFlashlightCheck)
        {
            UpdateFlashlightReference();
            nextFlashlightCheck = Time.time + FLASHLIGHT_CHECK_INTERVAL;
        }

        if (!hasValidFlashlight)
        {
            SetVisibility(0f);
            return;
        }

        UpdateMarkVisibility();
    }

    private void CacheFlashlightReference()
    {
        cachedFlashlight = FindAnyObjectByType<UVFlashlight>();
        hasValidFlashlight = cachedFlashlight != null &&
                           UVFlashlightPuzzleManager.PlayerHasUVFlashlight;
    }

    private void UpdateFlashlightReference()
    {
        if (cachedFlashlight == null || !cachedFlashlight.IsPickedUp())
        {
            CacheFlashlightReference();
        }

        hasValidFlashlight = cachedFlashlight != null &&
                           cachedFlashlight.IsPickedUp() &&
                           UVFlashlightPuzzleManager.PlayerHasUVFlashlight;
    }

    private void UpdateMarkVisibility()
    {
        if (cachedFlashlight == null || markRenderer == null)
        {
            SetVisibility(0f);
            return;
        }

        bool isHit = CheckIfUVLightHitting();
        float targetAlpha = isHit ? 1f : 0f;
        visibility = Mathf.MoveTowards(visibility, targetAlpha, fadeSpeed * Time.deltaTime);
        SetVisibility(visibility);
    }

    private void SetVisibility(float alpha)
    {
        if (markRenderer != null)
        {
            Color newColor = markRenderer.color;
            newColor.a = alpha;
            markRenderer.color = newColor;
        }
    }

    private bool CheckIfUVLightHitting()
    {
        if (cachedFlashlight == null || !cachedFlashlight.IsUVActive() || !cachedFlashlight.IsPickedUp())
            return false;

        Camera playerCamera = Camera.main;
        if (playerCamera == null) return false;

        Vector3 rayOrigin = playerCamera.transform.position;
        Vector3 rayDirection = playerCamera.transform.forward;
        Vector3 toMark = transform.position - rayOrigin;
        float distance = toMark.magnitude;

        if (distance > maxDistance) return false;

        float angle = Vector3.Angle(rayDirection, toMark.normalized);
        if (angle > detectionAngle) return false;

        
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, toMark.normalized, maxDistance);

        
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Triggers"))
            {
                return hit.collider.gameObject == gameObject;
            }
        }

        return false;
    }

    public void ForceHide()
    {
        SetVisibility(0f);
        visibility = 0f;
    }
}
