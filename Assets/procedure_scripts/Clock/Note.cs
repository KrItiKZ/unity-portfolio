using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class Note : MonoBehaviour
{
    public Canvas noteCanvas;
    public TextMeshProUGUI noteText;
    public float interactionDistance = 3f;

    public Sprite itemIcon;

    private bool isNoteOpen = false;
    private bool hasBeenRead = false;

    public void SetNoteText(string text)
    {
        if (noteText != null)
            noteText.text = text;
    }

    private void Start()
    {
        if (noteCanvas != null)
            noteCanvas.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            CheckNoteInteraction();
        }

        if (isNoteOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseNote();
        }
    }

    private void CheckNoteInteraction()
    {
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance) && hit.collider.gameObject == gameObject)
        {
            AddNoteToInventory();
        }
    }

    private void ToggleNote()
    {
        isNoteOpen = !isNoteOpen;

        if (noteCanvas != null)
        {
            noteCanvas.gameObject.SetActive(isNoteOpen);
        }

        Cursor.lockState = isNoteOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isNoteOpen;
        Time.timeScale = isNoteOpen ? 0f : 1f;

        if (isNoteOpen && !hasBeenRead && NotePuzzleManager.Instance != null)
        {
            hasBeenRead = true;
            NotePuzzleManager.Instance.MarkNoteAsFound();
            AddNoteToInventory();
        }
    }

    private void AddNoteToInventory()
    {
        if (NotePuzzleManager.Instance != null && !hasBeenRead)
        {
            hasBeenRead = true;
            NotePuzzleManager.Instance.MarkNoteAsFound();
        }

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.AddItem(
                InventorySystem.ItemType.Note,
                itemIcon,
                "Записка",
                "На записке написаны подсказки для прохождения",
                gameObject,
                null,
                noteText.text
            );
            Destroy(gameObject);
        }
    }

    private void CloseNote()
    {
        isNoteOpen = false;

        if (noteCanvas != null)
        {
            noteCanvas.gameObject.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
    }
}