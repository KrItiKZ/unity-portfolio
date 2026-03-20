using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Image backgroundImage;
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public GameObject equippedIndicator;

    [Header("Colors")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color selectedColor = new Color(0.3f, 0.3f, 0.5f, 0.8f);
    public Color emptyColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
    public Color hoverColor = new Color(0.4f, 0.4f, 0.6f, 0.8f);

    private int slotIndex;
    private InventorySystem.InventoryItem currentItem;
    private bool isSelected = false;

    public void Initialize(int index)
    {
        slotIndex = index;
        ClearSlot();
    }

    public void SetItem(InventorySystem.InventoryItem item)
    {
        currentItem = item;

        if (itemIcon != null)
        {
            itemIcon.sprite = item.itemIcon;
            itemIcon.color = Color.white;
            itemIcon.gameObject.SetActive(true);
        }

        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
            itemNameText.gameObject.SetActive(true);
        }

        UpdateVisual(item.isEquipped);
        UpdateBackgroundColor();
    }

    public void ClearSlot()
    {
        currentItem = null;

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.color = Color.clear;
            itemIcon.gameObject.SetActive(false);
        }

        if (itemNameText != null)
        {
            itemNameText.text = "";
            itemNameText.gameObject.SetActive(false);
        }

        if (equippedIndicator != null)
            equippedIndicator.SetActive(false);

        UpdateBackgroundColor();
    }

    public void UpdateVisual(bool isEquipped)
    {
        if (equippedIndicator != null)
            equippedIndicator.SetActive(isEquipped);

        UpdateBackgroundColor();
    }

    private void UpdateBackgroundColor()
    {
        if (backgroundImage != null)
        {
            if (currentItem == null)
            {
                backgroundImage.color = emptyColor;
            }
            else if (isSelected)
            {
                backgroundImage.color = selectedColor;
            }
            else
            {
                backgroundImage.color = normalColor;
            }
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateBackgroundColor();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsEmpty())
        {
            InventorySystem.Instance.OnSlotClicked(slotIndex);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        
        if (backgroundImage != null && currentItem != null && !isSelected)
        {
            backgroundImage.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelected)
        {
            UpdateBackgroundColor();
        }
    }

    public InventorySystem.InventoryItem GetItem()
    {
        return currentItem;
    }

    public bool IsEmpty()
    {
        return currentItem == null;
    }
}