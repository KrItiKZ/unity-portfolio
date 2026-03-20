using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class Room : MonoBehaviour
{
    [Header("Basic Settings")]
    public int roomNumber = 1;
    public Door[] doors;

    [Header("Anomaly Objects")]
    public GameObject mirrorObject;
    public GameObject clockObject;

    [SerializeField] private string currentAnomaly;
    private bool hasBeenInitialized = false;
    private float creationTime;
    public bool mirrorAnomalyActivated = false;

    public string CurrentAnomaly => currentAnomaly;
    public bool isFakeRoom = false;
    public Room linkedRealRoom;
    public float CreationTime => creationTime;
    public bool IsInitialized => hasBeenInitialized;

    void Awake()
    {
        creationTime = Time.time;
    }

    void Start()
    {
        if (!hasBeenInitialized && Application.isPlaying)
        {
            CreateRoomBoundsTriggerIfNeeded();
        }
    }

    public void InitializeFirstRoomManually()
    {
        if (!hasBeenInitialized)
        {
            SetupRoom(1);
        }
    }

    private void CreateRoomBoundsTriggerIfNeeded()
    {
        if (GetComponentInChildren<RoomBoundsTrigger>() == null)
        {
            GameObject triggerObj = new GameObject("RoomBoundsTrigger");
            triggerObj.transform.SetParent(transform);
            triggerObj.transform.localPosition = Vector3.zero;

            RoomBoundsTrigger trigger = triggerObj.AddComponent<RoomBoundsTrigger>();
            trigger.currentRoom = this;
            trigger.entranceDoor = null; 
        }
    }
    public RoomBoundsTrigger GetRoomBoundsTrigger()
    {
        return GetComponentInChildren<RoomBoundsTrigger>();
    }

    public void SetupRoom(int newRoomNumber)
    {
        hasBeenInitialized = true;

        if (isFakeRoom)
        {
            currentAnomaly = "SoundHints";
            CleanupExistingAnomalies();
            CleanupUVTableIfNeeded();

            Door[] fakeDoors = GetComponentsInChildren<Door>(true);
            foreach (Door door in fakeDoors)
            {
                if (door != null)
                {
                    door.isLockedAfterUse = true;
                    door.LockDoor();           
                    door.StopDoorSounds();
                    door.RecheckSoundTrigger(); 
                }
            }
            return; 
        }

        roomNumber = newRoomNumber;

        if (AnomalyManager.Instance != null && SessionManager.Instance != null)
        {
            currentAnomaly = AnomalyManager.Instance.GetAnomalyForRoom(
                roomNumber,
                SessionManager.Instance.currentSession
            );
            
            AnomalyManager.Instance.currentRoomAnomaly = currentAnomaly;

            if (string.IsNullOrEmpty(currentAnomaly))
            {
                currentAnomaly = "SoundHints";
                AnomalyManager.Instance.currentRoomAnomaly = currentAnomaly;
            }
        }
        else
        {
            currentAnomaly = "SoundHints";
            if (AnomalyManager.Instance != null)
                AnomalyManager.Instance.currentRoomAnomaly = currentAnomaly;
        }

        CleanupExistingAnomalies();
        CleanupUVTableIfNeeded();

        NotePuzzleManager noteManager = NotePuzzleManager.Instance;
        UVFlashlightPuzzleManager uvManager = UVFlashlightPuzzleManager.Instance;

        if (noteManager != null)
            noteManager.PrepareForNewRoom(roomNumber, currentAnomaly);
        if (uvManager != null)
            uvManager.PrepareForNewRoom(roomNumber, currentAnomaly);

        SpawnPuzzleObjectsSynchronously();
        SetupCorrectDoors();

        ApplyRoomAnomaly();

        if (currentAnomaly == "SoundHints" || currentAnomaly == "NoteSpawn")
            InitializeSoundHints();

        StartCoroutine(SpawnAnomaliesDelayed(currentAnomaly));
    }

    private void CleanupExistingAnomalies()
    {
        UVFlashlight[] flashlights = GetComponentsInChildren<UVFlashlight>();
        foreach (UVFlashlight flashlight in flashlights)
        {
            if (flashlight != null && !flashlight.IsPickedUp() && currentAnomaly != "UVFlashlightSpawn")
            {
                Destroy(flashlight.gameObject);
            }
        }

        if (currentAnomaly != "NoteSpawn" || !NotePuzzleManager.Instance.HasNoteInCurrentRoom)
        {
            Note[] notes = GetComponentsInChildren<Note>();
            foreach (Note note in notes)
            {
                if (note != null) Destroy(note.gameObject);
            }
        }

        Clock[] clocks = GetComponentsInChildren<Clock>();
        foreach (Clock clock in clocks)
        {
            if (clock != null) Destroy(clock.gameObject);
        }
    }

    private void CleanupUVTableIfNeeded()
    {
        if (UVFlashlightPuzzleManager.Instance == null) return;

        int spawnRoomNumber = UVFlashlightPuzzleManager.Instance.flashlightSpawnRoomNumber;

        if (currentAnomaly != "UVFlashlightSpawn" && spawnRoomNumber != roomNumber)
        {
            foreach (Transform child in transform)
            {
                if (child.name.Contains("Table")) 
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    private void InitializeSoundHints()
    {
        Door[] allDoors = GetComponentsInChildren<Door>();
        foreach (Door door in allDoors)
        {
            if (door != null)
            {
                door.ReinitializeAudio();
                door.RecheckSoundTrigger();
            }
        }
    }

    public void ApplyRoomAnomaly()
    {
        DeactivateLocalAnomalies();

        switch (currentAnomaly)
        {
            case "SoundHints":
                InitializeSoundHints();
                break;

            case "UVMarks":
                break;

            case "MirrorAnomaly":
                if (MirrorAnomaly.Instance != null)
                {
                    MirrorAnomaly.Instance.ActivateMirrorAnomaly(roomNumber);
                }
                mirrorAnomalyActivated = true;
                break;

            case "ClockPuzzle":
                // ActivateClockPuzzle();
                break;

            case "UVFlashlightSpawn":
                break;

            case "NoteSpawn":
                InitializeSoundHints();
                break;

            case "BreathingWalls":
                ActivateBreathingWallsAnomaly();
                break;

            case "ColorAura":
                ActivateColorAuraAnomaly();
                break;

            default:
                currentAnomaly = "SoundHints";
                InitializeSoundHints();
                break;
        }
    }

    public void ForceSetupRoom(int newRoomNumber)
    {
        hasBeenInitialized = true;
        roomNumber = newRoomNumber;

        if (AnomalyManager.Instance == null || SessionManager.Instance == null)
        {
            currentAnomaly = "SoundHints";
            if (AnomalyManager.Instance != null)
            {
                AnomalyManager.Instance.currentRoomAnomaly = currentAnomaly;
            }
        }
        else
        {
            currentAnomaly = AnomalyManager.Instance.GetAnomalyForRoom(roomNumber, SessionManager.Instance.currentSession);
            AnomalyManager.Instance.currentRoomAnomaly = currentAnomaly;

            if (string.IsNullOrEmpty(currentAnomaly))
            {
                currentAnomaly = "SoundHints";
                AnomalyManager.Instance.currentRoomAnomaly = currentAnomaly;
            }
        }


        CleanupExistingAnomalies();
        CleanupUVTableIfNeeded();

        SetupCorrectDoors();

        ApplyRoomAnomaly();

        if (currentAnomaly == "SoundHints")
        {
            InitializeSoundHints();
        }

        if (roomNumber == 1)
        {
            NotePuzzleManager.Instance?.PrepareForNewRoom(1, currentAnomaly);
            UVFlashlightPuzzleManager.Instance?.PrepareForNewRoom(1, currentAnomaly);
        }
        string anomalyToSpawn = currentAnomaly;
        StartCoroutine(SpawnAnomaliesDelayed(anomalyToSpawn));
    }

    private IEnumerator SpawnAnomaliesDelayed(string anomalyToSpawn)
    {
        yield return new WaitForEndOfFrame();
    }


    private void ActivateBreathingWallsAnomaly()
    {
        BreathingWallsAnomaly breathingAnomaly = GetComponent<BreathingWallsAnomaly>();
        if (breathingAnomaly == null)
            breathingAnomaly = gameObject.AddComponent<BreathingWallsAnomaly>();

        breathingAnomaly.ActivateBreathingAnomaly();
    }

    private void ActivateColorAuraAnomaly()
    {
        ColorAuraAnomaly auraAnomaly = GetComponent<ColorAuraAnomaly>();
        if (auraAnomaly == null)
            auraAnomaly = gameObject.AddComponent<ColorAuraAnomaly>();

        auraAnomaly.ActivateAuraAnomaly();
    }

    public void UpdateRoomAnomaly()
    {
        if (AnomalyManager.Instance != null && SessionManager.Instance != null)
        {
            string newAnomaly = AnomalyManager.Instance.currentRoomAnomaly;
            if (newAnomaly != currentAnomaly)
            {
                currentAnomaly = newAnomaly;
                ApplyRoomAnomaly();
                CleanupUVTableIfNeeded();

                if (currentAnomaly == "UVMarks")
                {
                    SetupUVMarksIfNeeded();
                }
            }
        }
    }

    public void DeactivateLocalAnomalies()
    {

        if (mirrorObject != null)
        {
            mirrorObject.SetActive(false);
        }

        if (clockObject != null)
            clockObject.SetActive(false);

        BreathingWallsAnomaly breathing = GetComponent<BreathingWallsAnomaly>();
        if (breathing != null)
        {
            breathing.StopBreathingAnomaly();
        }

        if (currentAnomaly != "ColorAura")
        {
            ColorAuraAnomaly aura = GetComponent<ColorAuraAnomaly>();
            if (aura != null)
            {
                aura.StopAuraAnomaly();
            }
        }

        Door[] roomDoors = GetComponentsInChildren<Door>();
        foreach (Door door in roomDoors)
        {
            if (door != null)
            {
                door.StopDoorSounds();
                door.ClearUVMarks();
            }
        }
    }

    public void DeactivateAllAnomalies()
    {
        if (MirrorAnomaly.Instance != null)
        {
            MirrorAnomaly.Instance.ForceRestoreMirror();
        }

        DeactivateLocalAnomalies();
    }

    public void CleanupAnomalies()
    {
        BreathingWallsAnomaly breathing = GetComponent<BreathingWallsAnomaly>();
        if (breathing != null)
        {
            breathing.StopBreathingAnomaly();
            Destroy(breathing);
        }

        ColorAuraAnomaly aura = GetComponent<ColorAuraAnomaly>();
        if (aura != null)
        {
            aura.StopAuraAnomaly();
            Destroy(aura);
        }

        Door[] roomDoors = GetComponentsInChildren<Door>();
        foreach (Door door in roomDoors)
        {
            if (door != null)
                door.StopDoorSounds();
        }
    }

    public Bounds GetRoomBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(transform.position, new Vector3(10f, 3f, 10f));
        }

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                bounds.Encapsulate(renderer.bounds);
        }

        bounds.Expand(0.5f);

        return bounds;
    }

    public void SetupUVMarksIfNeeded()
    {
        UVFlashlightPuzzleManager uvManager = UVFlashlightPuzzleManager.Instance;
        if (uvManager != null && UVFlashlightPuzzleManager.PlayerHasUVFlashlight)
        {
            string actualAnomaly = currentAnomaly;

            if (actualAnomaly == "UVMarks")
            {
                if (doors == null || doors.Length == 0)
                {
                    doors = GetComponentsInChildren<Door>();
                }

                List<Door> incorrectDoors = new List<Door>();
                foreach (Door door in doors)
                {
                    if (door != null && door.gameObject.activeSelf && !door.isCorrectDoor)
                    {
                        incorrectDoors.Add(door);
                    }
                }

                if (incorrectDoors.Count > 0)
                {
                    uvManager.SpawnUVMarksOnIncorrectDoors(incorrectDoors.ToArray());
                }
            }
        }
    }

    private void SetupCorrectDoors()
    {
        if (doors == null || doors.Length == 0) return;

        foreach (Door door in doors)
        {
            if (door != null)
            {
                door.isCorrectDoor = false;
                door.isLockedAfterUse = false;
                door.isOpening = false;
                door.doorOpened = false;
                door.ResetDoorState();

                Renderer doorRenderer = door.GetComponent<Renderer>();
                if (doorRenderer != null)
                {
                    doorRenderer.material.color = Color.white;
                }
            }
        }

        List<Door> availableDoors = new List<Door>();
        foreach (Door door in doors)
        {
            if (door != null && door.gameObject.activeSelf && !door.doorOpened)
            {
                availableDoors.Add(door);
            }
        }

        if (availableDoors.Count == 0)
        {
            foreach (Door door in doors)
            {
                if (door != null && door.gameObject.activeSelf)
                {
                    availableDoors.Add(door);
                    door.doorOpened = false;
                    door.isOpening = false;
                }
            }
        }

        if (availableDoors.Count > 0)
        {
            Clock clock = GetComponentInChildren<Clock>(true);
            NotePuzzleManager noteManager = NotePuzzleManager.Instance;

            bool shouldForceLeftDoor = false;

            if (clock != null && noteManager != null && noteManager.HasClockInCurrentRoom)
            {
                shouldForceLeftDoor = clock.IsTimeBetweenSixAndNine();
            }

            int correctDoorIndex;

            if (shouldForceLeftDoor)
            {
                availableDoors.Sort((a, b) => 
                    a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));
                
                correctDoorIndex = 0;
            }
            else
            {
                correctDoorIndex = Random.Range(0, availableDoors.Count);
            }

            availableDoors[correctDoorIndex].isCorrectDoor = true;

            foreach (Door door in availableDoors)
            {
                door.doorOpened = false;
                door.isOpening = false;
            }
        }
    }

    private void SpawnPuzzleObjectsSynchronously()
    {
        if (currentAnomaly == "NoteSpawn")
        {
            NotePuzzleManager.Instance?.SpawnNoteIfNeeded(roomNumber, this);
        }
        else if (currentAnomaly == "ClockPuzzle")
        {
            NotePuzzleManager.Instance?.SpawnClockIfNeeded(this);
        }
        else if (currentAnomaly == "UVFlashlightSpawn")
        {
            UVFlashlightPuzzleManager.Instance?.SpawnUVFlashlightIfNeeded(this);
        }
    }
}
