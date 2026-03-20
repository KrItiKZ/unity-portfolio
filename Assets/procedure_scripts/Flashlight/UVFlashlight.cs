using UnityEngine;
using UnityEngine.InputSystem;

public class UVFlashlight : MonoBehaviour
{
    public Light uvLight;
    public GameObject flashlightModel;
    public float interactionDistance = 3f;
    public bool isPickedUp = false;

    public Sprite itemIcon;

    private bool isUVActive = false;

    private int counter = 0;

    private void Start()
    {
        if (uvLight != null) uvLight.enabled = false;
    }

    private void Update()
    {
        if (!UVFlashlightPuzzleManager.PlayerHasUVFlashlight && isPickedUp)
        {
            Destroy(gameObject);
            return;
        }

        if (!isPickedUp)
        {
            CheckForPickup();
            return;
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            ToggleUVLight();
        }
    }

    private void CheckForPickup()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            RaycastHit hit;

            int layerMask = ~(1 << LayerMask.NameToLayer("Triggers"));

            if (Physics.Raycast(ray, out hit, interactionDistance, layerMask))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    PickUpFlashlight();
                }
            }
        }
    }

    private void PickUpFlashlight()
    {
        isPickedUp = true;
        UVFlashlightPuzzleManager.Instance?.OnFlashlightPickedUp(gameObject);
        AddToInventory();

        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
        counter++;
    }

    private void AddToInventory()
    {
        if (InventorySystem.Instance != null && counter == 0)
        {
            
            gameObject.SetActive(false);
            
            InventorySystem.Instance.AddItem(
                InventorySystem.ItemType.UVFlashlight,
                itemIcon,
                "УФ-фонарик",
                "Фонарик для обнаружения скрытых меток",
                gameObject,
                this,
                ""
            );
        }
    }

    public void ToggleUVLight()
    {
        isUVActive = !isUVActive;
        if (uvLight != null) uvLight.enabled = isUVActive;
    }

    public bool IsUVActive() => isUVActive;
    public bool IsPickedUp() => isPickedUp;
}