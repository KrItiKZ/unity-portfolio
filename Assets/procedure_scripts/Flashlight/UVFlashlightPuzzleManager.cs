using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UVFlashlightPuzzleManager : MonoBehaviour
{
    public static UVFlashlightPuzzleManager Instance;

    [Header("UV Flashlight Settings")]
    public GameObject uvFlashlightPrefab; 

    [Header("Table Settings")]
    public GameObject tablePrefab; 

    [Header("Other Settings")]
    public GameObject uvMarkPrefab;
    public Transform flashlightHolder;
    public Vector3 flashlightLocalPosition = new Vector3(0.3f, -0.2f, 0.3f);
    public Vector3 flashlightLocalRotation = new Vector3(5f, 0f, 0f);

    [Header("UV Mark Settings - 2D")]
    public Vector2[] uvMarkLocalOffsets = new Vector2[]
    {
        new Vector2(0f, 0.5f), new Vector2(0f, 1.0f), new Vector2(-0.2f, 0.7f),
        new Vector2(0.2f, 0.7f), new Vector2(-0.3f, 0.3f), new Vector2(0.3f, 0.3f)
    };

    public bool HasUVFlashlightInCurrentRoom { get; private set; }
    public static bool PlayerHasUVFlashlight { get; private set; }

    private bool hasUVFlashlightSpawnedThisSession = false;
    private List<GameObject> currentUVMarks = new List<GameObject>();
    private UVFlashlight currentFlashlight;
    public int flashlightSpawnRoomNumber = -1;  

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void PrepareForNewRoom(int roomNumber, string roomAnomaly = null)
    {
        ClearUVMarks();
        HasUVFlashlightInCurrentRoom = false;

        if (roomAnomaly == null && AnomalyManager.Instance != null)
        {
            
            roomAnomaly = AnomalyManager.Instance.currentRoomAnomaly;

            
            string cachedAnomaly = AnomalyManager.Instance.GetAnomalyForRoom(roomNumber, SessionManager.Instance != null ? SessionManager.Instance.currentSession : 1);
            if (!string.IsNullOrEmpty(cachedAnomaly))
            {
                roomAnomaly = cachedAnomaly;
            }
        }

        if (!string.IsNullOrEmpty(roomAnomaly))
        {
            if (roomAnomaly == "UVFlashlightSpawn" && !hasUVFlashlightSpawnedThisSession && !UVFlashlightPuzzleManager.PlayerHasUVFlashlight)
            {
                HasUVFlashlightInCurrentRoom = true;
            }
        }
    }

    public void SpawnUVFlashlightIfNeeded(Room targetRoom = null)
    {
        if (!HasUVFlashlightInCurrentRoom) return;

        if (uvFlashlightPrefab != null && tablePrefab != null)
        {
            Room currentRoom = targetRoom != null ? targetRoom : FindAnyObjectByType<Room>();
            if (currentRoom == null) return;

            
            Transform existingTable = currentRoom.transform.Find("Table");  
            if (existingTable != null)
            {
                Destroy(existingTable.gameObject);
            }

            
            GameObject tableObj = Instantiate(tablePrefab, currentRoom.transform);
            

            
            GameObject flashlightObj = Instantiate(uvFlashlightPrefab, currentRoom.transform);
            
            
            currentFlashlight = flashlightObj.GetComponent<UVFlashlight>();

            
            flashlightSpawnRoomNumber = currentRoom.roomNumber;

            hasUVFlashlightSpawnedThisSession = true;
        }
    }

    public void OnFlashlightPickedUp(GameObject pickedUpFlashlight)
    {
        UVFlashlightPuzzleManager.PlayerHasUVFlashlight = true;

        VoiceGuideSystem.Instance?.OnUVFlashlightPickedUp();

        if (AnomalyManager.Instance != null && SessionManager.Instance != null)
        {
            Room currentRoom = FindObjectsByType<RoomBoundsTrigger>(FindObjectsSortMode.None).FirstOrDefault(rt => rt.HasPlayerEntered)?.currentRoom;
            if (currentRoom != null)
            {
                AnomalyManager.Instance.ForceSetRoomAnomaly(currentRoom.roomNumber, "UVMarks");
                currentRoom.UpdateRoomAnomaly();
            }
        }

        UpdateAllDoorsSoundTriggers();
    }

    private void UpdateAllDoorsSoundTriggers()
    {
        Door[] allDoors = FindObjectsByType<Door>(FindObjectsSortMode.None);
        foreach (Door door in allDoors)
        {
            if (door != null) door.RecheckSoundTrigger();
        }
    }

    private void CreateUVMarksInCurrentRoom()
    {
        Room currentRoom = FindObjectsByType<RoomBoundsTrigger>(FindObjectsSortMode.None).FirstOrDefault(rt => rt.HasPlayerEntered)?.currentRoom;
        if (currentRoom == null || !UVFlashlightPuzzleManager.PlayerHasUVFlashlight) return;
        currentRoom.SetupUVMarksIfNeeded();
    }

    public void SpawnUVMarksOnIncorrectDoors(Door[] incorrectDoors)
    {
        if (uvMarkPrefab == null || incorrectDoors == null || incorrectDoors.Length == 0 || !UVFlashlightPuzzleManager.PlayerHasUVFlashlight)
            return;

        foreach (Door door in incorrectDoors)
        {
            if (door != null && door.gameObject.activeSelf && door.isCorrectDoor == false)
            {
                
                UvMark[] existingMarks = door.GetComponentsInChildren<UvMark>();
                if (existingMarks.Length == 0)
                {
                    SpawnMarksOnDoor(door.transform);
                }
            }
        }
    }

    private void SpawnMarksOnDoor(Transform doorTransform)
    {
        if (UVMarkPool.Instance == null) return;

        Door doorComponent = doorTransform.GetComponent<Door>();
        if (doorComponent != null && doorComponent.isCorrectDoor) return;

        int marksCount = Random.Range(2, 4);

        for (int i = 0; i < marksCount; i++)
        {
            if (uvMarkLocalOffsets == null || uvMarkLocalOffsets.Length == 0) return;

            Vector2 randomOffset = uvMarkLocalOffsets[Random.Range(0, uvMarkLocalOffsets.Length)];
            GameObject uvMark = UVMarkPool.Instance.GetUVMark(doorTransform, randomOffset);

            if (uvMark != null)
            {
                currentUVMarks.Add(uvMark);
            }
        }
    }

    public void ClearUVMarks()
    {
        if (UVMarkPool.Instance != null)
        {
            UVMarkPool.Instance.ClearAllMarks();
        }
        currentUVMarks.Clear();
        ClearAllDoorsUVMarks();
    }

    private void ClearAllDoorsUVMarks()
    {
        Door[] allDoors = FindObjectsByType<Door>(FindObjectsSortMode.None);
        foreach (Door door in allDoors)
        {
            if (door != null) door.ClearUVMarks();
        }
    }

    public void ResetPuzzleSystem()
    {
        hasUVFlashlightSpawnedThisSession = false;
        HasUVFlashlightInCurrentRoom = false;
        
        
        ClearUVMarks();
        flashlightSpawnRoomNumber = -1;  
    }

    public void OnFlashlightEquipped(UVFlashlight flashlight)
    {
        if (flashlight == null)
        {
            Debug.LogError("❌ OnFlashlightEquipped: flashlight == null");
            return;
        }

        Debug.Log($"🔦 OnFlashlightEquipped вызван. flashlightHolder: {flashlightHolder != null}");

        currentFlashlight = flashlight;
        currentFlashlight.isPickedUp = true;

        
        if (flashlightHolder != null)
        {
            Debug.Log("📍 Перемещаем фонарик к flashlightHolder");
            currentFlashlight.transform.SetParent(flashlightHolder);
            currentFlashlight.transform.localPosition = flashlightLocalPosition;
            currentFlashlight.transform.localRotation = Quaternion.Euler(flashlightLocalRotation);
        }
        else
        {
            Debug.LogError("flashlightHolder == null! Фонарик не может быть прикреплен к игроку");
        }

        if (currentFlashlight.uvLight != null)
        {
            currentFlashlight.uvLight.enabled = false;
        }

        if (currentFlashlight.flashlightModel != null)
        {
            currentFlashlight.flashlightModel.SetActive(true);
        }

        CreateUVMarksInCurrentRoom();
        Debug.Log("Фонарик успешно экипирован и прикреплен к игроку");
    }
}