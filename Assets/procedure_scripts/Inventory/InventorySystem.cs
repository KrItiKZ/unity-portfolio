using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance;

    [Header("UI References")]
    public Canvas inventoryCanvas;
    public Transform slotsContainer;
    public TextMeshProUGUI itemDescriptionText;
    public GameObject descriptionPanel;
    public GameObject inventorySlotPrefab;

    [Header("3D Книга")]
    public BookController bookController;

    [Header("Inventory Settings")]
    public int inventorySize = 2;

    private InventorySlot[] inventorySlots;
    private bool isInventoryOpen = false;
    private int selectedSlotIndex = -1;
    private PlayerController playerController;
    private UVFlashlight currentEquippedFlashlight;

    public enum ItemType { UVFlashlight, Note }

    [System.Serializable]
    public class InventoryItem
    {
        public ItemType itemType;
        public Sprite itemIcon;
        public string itemName;
        public string itemDescription;
        public GameObject physicalItemPrefab;
        public bool isEquippable;
        public bool isEquipped;
        public UVFlashlight flashlightComponent;
        public string noteText;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateInventoryCanvasIfNeeded();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CreateInventoryCanvasIfNeeded()
    {
        if (inventoryCanvas == null)
        {
            GameObject canvasObj = new GameObject("InventoryCanvas");
            inventoryCanvas = canvasObj.AddComponent<Canvas>();
            inventoryCanvas.renderMode = RenderMode.WorldSpace;
            inventoryCanvas.worldCamera = Camera.main;   
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            CreateInventoryUI(canvasObj);
        }
    }

    private void CreateInventoryUI(GameObject canvasObj)
    {
        
        GameObject panel = new GameObject("InventoryPanel");
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(6f, 4f);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.9f);

        
        GameObject container = new GameObject("SlotsContainer");
        container.transform.SetParent(panel.transform, false);
        slotsContainer = container.transform;
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.offsetMin = new Vector2(20, 80);
        containerRect.offsetMax = new Vector2(-20, -20);

        GridLayoutGroup grid = container.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(1.2f, 1.2f);    
        grid.spacing = new Vector2(0.2f, 0.2f);
        grid.padding = new RectOffset(20, 20, 20, 20);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;

        
        GameObject descPanel = new GameObject("DescriptionPanel");
        descPanel.transform.SetParent(panel.transform, false);
        descriptionPanel = descPanel;
        RectTransform descRect = descPanel.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0);
        descRect.anchorMax = new Vector2(1, 0.2f);
        descRect.offsetMin = new Vector2(20, 20);
        descRect.offsetMax = new Vector2(-20, -20);

        Image descBg = descPanel.AddComponent<Image>();
        descBg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        GameObject descText = new GameObject("DescriptionText");
        descText.transform.SetParent(descPanel.transform, false);
        itemDescriptionText = descText.AddComponent<TextMeshProUGUI>();
        RectTransform textRect = descText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        itemDescriptionText.fontSize = 22;
        itemDescriptionText.alignment = TextAlignmentOptions.Center;
        itemDescriptionText.color = Color.white;
        descriptionPanel.SetActive(false);
    }

    private void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        InitializeInventory();
        SetupBookUI();
        if (inventoryCanvas != null)
            inventoryCanvas.gameObject.SetActive(false);
    }

    private void SetupBookUI()
    {
        if (bookController == null || inventoryCanvas == null) return;

        
        inventoryCanvas.transform.SetParent(bookController.transform, false);

        
        
        
        

        
        inventoryCanvas.gameObject.SetActive(false);
    }

    private void InitializeInventory()
    {
        if (slotsContainer == null) return;
        foreach (Transform child in slotsContainer) Destroy(child.gameObject);
        inventorySlots = new InventorySlot[inventorySize];
        for (int i = 0; i < inventorySize; i++)
        {
            if (inventorySlotPrefab != null)
            {
                GameObject slotObj = Instantiate(inventorySlotPrefab, slotsContainer);
                slotObj.name = $"Slot{i}";
                inventorySlots[i] = slotObj.GetComponent<InventorySlot>();
                inventorySlots[i]?.Initialize(i);
            }
        }
    }

    private void Update()
    {
        
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleInventory();
        }

        if (isInventoryOpen && selectedSlotIndex >= 0 && selectedSlotIndex < inventorySlots.Length)
        {
            for (int i = 0; i < inventorySlots.Length; i++)
                inventorySlots[i]?.SetSelected(i == selectedSlotIndex);
        }
    }

    public void ToggleInventory()
    {
        if (bookController == null)
        {
            
            isInventoryOpen = !isInventoryOpen;
            if (inventoryCanvas != null) inventoryCanvas.gameObject.SetActive(isInventoryOpen);
            if (descriptionPanel != null) descriptionPanel.SetActive(false);
            if (playerController != null) playerController.LockControls(isInventoryOpen);
            Cursor.lockState = isInventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isInventoryOpen;

            if (!isInventoryOpen)
            {
                selectedSlotIndex = -1;
                foreach (var slot in inventorySlots) slot?.SetSelected(false);
            }
            else
            {
                EventSystem.current?.SetSelectedGameObject(null);
            }
            return;
        }

        
        if (isInventoryOpen)
        {
            
            if (inventoryCanvas != null) inventoryCanvas.gameObject.SetActive(false);
            if (descriptionPanel != null) descriptionPanel.SetActive(false);
            bookController.StartClosingAnimation(OnCloseAnimationFinished);
        }
        else
        {
            
            bookController.StartOpeningAnimation(OnOpenAnimationFinished);
        }
    }

    public bool AddItem(ItemType itemType, Sprite icon, string name, string description, GameObject physicalItem, UVFlashlight flashlight = null, string noteText = "")
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] != null && inventorySlots[i].IsEmpty())
            {
                var newItem = new InventoryItem
                {
                    itemType = itemType,
                    itemIcon = icon,
                    itemName = name,
                    itemDescription = description,
                    physicalItemPrefab = physicalItem,
                    isEquippable = (itemType == ItemType.UVFlashlight),
                    isEquipped = false,
                    flashlightComponent = flashlight,
                    noteText = noteText
                };
                inventorySlots[i].SetItem(newItem);
                return true;
            }
        }
        Debug.Log("Инвентарь полон!");
        return false;
    }

    public void OnSlotClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Length) return;
        var item = inventorySlots[slotIndex]?.GetItem();
        if (item == null) return;

        if (selectedSlotIndex == slotIndex)
        {
            HandleSecondClick(item, slotIndex);
            selectedSlotIndex = -1;
            inventorySlots[slotIndex].SetSelected(false);
        }
        else
        {
            ShowItemDescription(item);
            selectedSlotIndex = slotIndex;
            inventorySlots[slotIndex].SetSelected(true);
        }
    }

    private void ShowItemDescription(InventoryItem item)
    {
        if (itemDescriptionText == null || descriptionPanel == null) return;
        descriptionPanel.SetActive(true);
        string text = $"<b>{item.itemName}</b>\n\n{item.itemDescription}";
        if (item.itemType == ItemType.Note && !string.IsNullOrEmpty(item.noteText))
            text += $"\n\n<size=80%><i>\"{item.noteText}\"</i></size>";
        itemDescriptionText.text = text;
    }

    private void HandleSecondClick(InventoryItem item, int slotIndex)
    {
        if (item.itemType == ItemType.UVFlashlight)
            EquipFlashlight(item, slotIndex);
        else if (item.itemType == ItemType.Note)
            descriptionPanel?.SetActive(false);
    }

    private void EquipFlashlight(InventoryItem item, int slotIndex)
    {
        Debug.Log($"🔧 Попытка экипировать фонарь. flashlightComponent: {item.flashlightComponent != null}");
        
        
        if (currentEquippedFlashlight != null)
        {
            Debug.Log("🔄 Снимаем текущий фонарь");
            currentEquippedFlashlight.gameObject.SetActive(false);
            currentEquippedFlashlight.isPickedUp = false;
            
            
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                var slotItem = inventorySlots[i]?.GetItem();
                if (slotItem != null && slotItem.flashlightComponent == currentEquippedFlashlight)
                {
                    slotItem.isEquipped = false;
                    inventorySlots[i].UpdateVisual(false);
                    break;
                }
            }
            
            currentEquippedFlashlight = null;
        }

        
        if (item.flashlightComponent != null)
        {
            Debug.Log("✅ Экипируем новый фонарь");
            currentEquippedFlashlight = item.flashlightComponent;
            currentEquippedFlashlight.gameObject.SetActive(true);
            currentEquippedFlashlight.isPickedUp = true;
            item.isEquipped = true;
            inventorySlots[slotIndex].UpdateVisual(true);
            UVFlashlightPuzzleManager.Instance?.OnFlashlightEquipped(currentEquippedFlashlight);
            Debug.Log("✅ Фонарь успешно экипирован");
        }
        else
        {
            Debug.LogError("❌ ОШИБКА: flashlightComponent == null! Фонарь не может быть экипирован.");
        }
        
        descriptionPanel?.SetActive(false);
    }

    public bool HasItem(ItemType type)
    {
        foreach (var slot in inventorySlots)
        {
            var item = slot?.GetItem();
            if (item != null && item.itemType == type) return true;
        }
        return false;
    }

    private void OnOpenAnimationFinished()
    {
        isInventoryOpen = true;
        if (inventoryCanvas != null)
        {
            inventoryCanvas.gameObject.SetActive(true);
            StartCoroutine(FadeInCanvas());
        }
        if (playerController != null) playerController.LockControls(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EventSystem.current?.SetSelectedGameObject(null);
    }

    private IEnumerator FadeInCanvas()
    {
        CanvasGroup cg = inventoryCanvas.GetComponent<CanvasGroup>() ?? inventoryCanvas.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            cg.alpha = t / 0.3f;
            yield return null;
        }
        cg.alpha = 1f;
    }

    private void OnCloseAnimationFinished()
    {
        isInventoryOpen = false;
        if (inventoryCanvas != null)
        {
            inventoryCanvas.gameObject.SetActive(false);
            StartCoroutine(FadeInCanvas());
        }
        if (playerController != null) playerController.LockControls(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        selectedSlotIndex = -1;
        foreach (var slot in inventorySlots) slot?.SetSelected(false);
    }

    public bool IsFlashlightEquipped() => currentEquippedFlashlight != null;
    public UVFlashlight GetEquippedFlashlight() => currentEquippedFlashlight;
    public bool IsInventoryOpen() => isInventoryOpen;
}