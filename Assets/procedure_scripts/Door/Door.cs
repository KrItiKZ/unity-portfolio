using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{

    [Header("Basic Settings")]
    public int doorNumber = 1;
    public bool isCorrectDoor = false;

    [Header("Audio Settings")]
    public AudioSource AudioSource;
    public AudioClip AudioClipTrue;
    public AudioClip AudioClipFalse;
    public AudioClip DoorOpenSound;

    [Header("Animation")]
    public DoorAnimator doorAnimator;

    [Header("Room Spawning")]
    public GameObject roomPrefab;
    public Vector3 roomSpawnOffset;
    public float roomRotationY = 0f;

    [Header("Door State")]
    public bool isLockedAfterUse = false;
    public bool isOneTimeDoor = true;

    private DoorSoundTrigger soundTrigger;
    public bool isOpening = false;
    public bool doorOpened = false;
    private bool isAudioInitialized = false;

    private void Start()
    {
        InitializeAudio();
        InitializeDoorAnimator();
        SetupSoundTrigger();
    }

    private void InitializeAudio()
    {
        if (AudioSource == null)
        {
            AudioSource = GetComponent<AudioSource>();
            if (AudioSource == null)
            {
                AudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        AudioSource.spatialBlend = 1f;
        AudioSource.maxDistance = 10f;
        isAudioInitialized = true;
    }

    private void InitializeDoorAnimator()
    {
        if (doorAnimator == null)
            doorAnimator = GetComponent<DoorAnimator>();
    }

    private void SetupSoundTrigger()
    {
        RemoveExistingSoundTrigger();

        if (ShouldCreateSoundTrigger())
        {
            CreateSoundTrigger();
        }
    }

    private bool ShouldCreateSoundTrigger()
    {
        Room currentRoom = GetComponentInParent<Room>();
        if (currentRoom == null) return true;

        if (currentRoom.CurrentAnomaly == "UVMarks" || currentRoom.CurrentAnomaly == "UVFlashlightSpawn")
        {
            return false;
        }

        UVFlashlightPuzzleManager uvManager = UVFlashlightPuzzleManager.Instance;
        if (uvManager != null && uvManager.HasUVFlashlightInCurrentRoom)
        {
            UVFlashlight[] flashlightsInRoom = currentRoom.GetComponentsInChildren<UVFlashlight>(true);
            foreach (UVFlashlight flashlight in flashlightsInRoom)
            {
                if (flashlight != null && flashlight.gameObject.activeInHierarchy && !flashlight.IsPickedUp())
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void RemoveExistingSoundTrigger()
    {
        DoorSoundTrigger[] existingTriggers = GetComponentsInChildren<DoorSoundTrigger>();
        foreach (DoorSoundTrigger trigger in existingTriggers)
        {
            if (trigger != null) Destroy(trigger.gameObject);
        }
        soundTrigger = null;
    }

    private void CreateSoundTrigger()
    {
        GameObject soundTriggerObj = new GameObject("DoorSoundTrigger");
        soundTriggerObj.transform.SetParent(transform);
        soundTriggerObj.transform.localPosition = Vector3.zero;
        soundTriggerObj.layer = LayerMask.NameToLayer("Triggers");

        BoxCollider triggerCollider = soundTriggerObj.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector3(2f, 2f, 2f);

        Rigidbody rb = soundTriggerObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        soundTrigger = soundTriggerObj.AddComponent<DoorSoundTrigger>();
        soundTrigger.door = this;
        soundTrigger.triggerDelay = 2f;
        soundTrigger.fadeOutDuration = 1f;
    }

    public void ResetDoorState()
    {
        isOpening = false;
        doorOpened = false;

        if (doorAnimator != null)
        {
            doorAnimator.ForceClose();
        }
    }

    public void LockDoor()
    {
        isLockedAfterUse = true;
        StopDoorSounds();

        if (doorAnimator != null)
        {
            doorAnimator.ForceClose();
        }
    }

    public void Interact()
    {
        if (isLockedAfterUse || isOpening || !gameObject.activeInHierarchy)
        {
            return;
        }

        StartCoroutine(OpenDoorSequence());
    }

    private IEnumerator OpenDoorSequence()
    {
        isOpening = true;
        doorOpened = true;

        LockAllDoorsInRoom();

        if (isOneTimeDoor)
        {
            isLockedAfterUse = true;
        }

        StopDoorSounds();

        if (DoorOpenSound != null && AudioSource != null)
        {
            AudioSource.Stop();
            AudioSource.PlayOneShot(DoorOpenSound);
        }

        if (doorAnimator != null)
        {
            doorAnimator.PlayOpenAnimation();
            yield return new WaitForSeconds(0.5f);
        }

        if (PerfectRunManager.Instance != null)
            PerfectRunManager.Instance.OnRoomCompleted(isCorrectDoor);

        if (CameraSanitySystem.Instance != null)
        {
            if (isCorrectDoor)
                CameraSanitySystem.Instance.OnCorrectDoorSelected();
            else
                CameraSanitySystem.Instance.OnWrongDoorSelected();
        }

        SpawnRoom();
        isOpening = false;
    }

    private void LockAllDoorsInRoom()
    {
        Room currentRoom = GetComponentInParent<Room>();
        if (currentRoom == null) return;

        Door[] roomDoors = currentRoom.GetComponentsInChildren<Door>();
        foreach (Door door in roomDoors)
        {
            if (door != null && door != this)
            {
                door.LockDoor();
            }
        }
    }

    private void SpawnRoom()
    {
        if (roomPrefab == null) return;

        Room currentRoom = GetComponentInParent<Room>();
        if (currentRoom == null) return;

        ClosePreviousDoor(currentRoom);

        currentRoom.CleanupAnomalies();
        CleanupConflictingPuzzleObjects();

        Vector3 spawnPosition = CalculateSpawnPosition(currentRoom.transform);
        Quaternion spawnRotation = CalculateRotation(currentRoom.transform);

        bool spawnFake = false;
        GameObject prefabToSpawn = roomPrefab;
        int fakeTypeIndex = -1;
        int newRoomNumber = isCorrectDoor ? currentRoom.roomNumber + 1 : 1;

        if (isCorrectDoor && AnomalyManager.Instance != null)
        {
            spawnFake = AnomalyManager.Instance.ShouldSpawnFakeRoom(newRoomNumber);

            if (spawnFake && GameManager.Instance != null && 
                GameManager.Instance.fakeRoomPrefabs != null && 
                GameManager.Instance.fakeRoomPrefabs.Length == 3)
            {
                fakeTypeIndex = Random.Range(0, 3);
                prefabToSpawn = GameManager.Instance.fakeRoomPrefabs[fakeTypeIndex];
            }
        }

        GameObject spawnedRoom = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);
        Room roomComponent = spawnedRoom.GetComponent<Room>();

        if (spawnFake && roomComponent != null)
        {
            roomComponent.isFakeRoom = true;

            FakeRoomConfig config = spawnedRoom.GetComponent<FakeRoomConfig>();
            if (config != null)
            {
                Vector3 realPos = spawnedRoom.transform.position + 
                                spawnedRoom.transform.rotation * config.realRoomOffset;
                Quaternion realRot = spawnedRoom.transform.rotation * config.realRoomRotation;

                GameObject realRoomObj = Instantiate(roomPrefab, realPos, realRot);
                Room realRoom = realRoomObj.GetComponent<Room>();

                realRoom.SetupRoom(newRoomNumber);
                realRoom.isFakeRoom = false;

                roomComponent.linkedRealRoom = realRoom;
            }
        }

        if (VoiceGuideSystem.Instance != null)
        {
            VoiceGuideSystem.Instance.OnRoomEnter(newRoomNumber);
        }

        if (roomComponent != null)
        {
            if (isCorrectDoor)
            {
                MirrorAnomaly.Instance?.ForceRestoreMirror();
                currentRoom.GetComponent<BreathingWallsAnomaly>()?.StopBreathingAnomaly();
                currentRoom.GetComponent<ColorAuraAnomaly>()?.StopAuraAnomaly();

                roomComponent.SetupRoom(newRoomNumber);
                SessionManager.Instance?.OnDoorSelected(true);

                StartCoroutine(DelayedPuzzleSetup(newRoomNumber, roomComponent));
            }
            else
            {
                SessionManager.Instance?.OnDoorSelected(false);
                ResetPuzzles();
                roomComponent.SetupRoom(1);

                StartCoroutine(DelayedPuzzleSetup(1, roomComponent));
                RestoreDoorsAudioInRoom(spawnedRoom);
            }

            StartCoroutine(DelayedUVMarksSetup(roomComponent));
        }

        SetupRoomTrigger(spawnedRoom, currentRoom);
        CleanupOldRooms(roomComponent);
    }


    private void ClosePreviousDoor(Room currentRoom)
    {
        if (currentRoom == null) return;

        RoomBoundsTrigger trigger = currentRoom.GetRoomBoundsTrigger();
        if (trigger != null && trigger.entranceDoor != null && trigger.entranceDoor != this)
        {
            Door previousDoor = trigger.entranceDoor;
            if (previousDoor != null && previousDoor.doorOpened)
            {
                previousDoor.StopDoorSounds();
                previousDoor.isOpening = false;
                
                if (currentRoom.roomNumber > 1)
                {
                    previousDoor.ResetDoorState();
                }
            }
        }
    }

    private IEnumerator DelayedPuzzleSetup(int roomNumber, Room room)
    {
        yield return new WaitForEndOfFrame();

        UVFlashlightPuzzleManager uvManager = UVFlashlightPuzzleManager.Instance;

        if (uvManager != null)
        {
            uvManager.PrepareForNewRoom(roomNumber, room.CurrentAnomaly);
        }
    }

    private IEnumerator DelayedUVMarksSetup(Room room)
    {
        yield return new WaitForEndOfFrame();

        if (UVFlashlightPuzzleManager.PlayerHasUVFlashlight)
        {
            room.SetupUVMarksIfNeeded();
        }
    }

    private void CleanupConflictingPuzzleObjects()
    {
        Room currentRoom = GetComponentInParent<Room>();
        UVFlashlight[] flashlights = FindObjectsByType<UVFlashlight>(FindObjectsSortMode.None);
        foreach (UVFlashlight flashlight in flashlights)
        {
            if (flashlight != null && !flashlight.IsPickedUp())
            {
                Room flashlightRoom = flashlight.GetComponentInParent<Room>();
                if (flashlightRoom != currentRoom)
                {
                    Destroy(flashlight.gameObject);
                }
            }
        }
    }

    private void RestoreDoorsAudioInRoom(GameObject roomObject)
    {
        Door[] doors = roomObject.GetComponentsInChildren<Door>();
        foreach (Door door in doors)
        {
            if (door != null)
            {
                if (door.AudioSource == null)
                {
                    door.AudioSource = door.gameObject.AddComponent<AudioSource>();
                    door.AudioSource.spatialBlend = 1f;
                    door.AudioSource.maxDistance = 10f;
                }
                door.ReinitializeAudio();
                door.RecheckSoundTrigger();
            }
        }
    }

    private void CleanupRoomsExcept(Room keepRoom)
    {
        Room[] allRooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
        foreach (Room room in allRooms)
        {
            if (room != keepRoom)
            {
                CleanupPuzzleObjectsFromRoom(room);
                Destroy(room.gameObject);
            }
        }
    }
    private void CleanupOldRooms(Room newRoom)
    {
        Room[] allRooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
        
        if (allRooms.Length <= 3)
        {
            return;
        }

        Room currentRoom = GetComponentInParent<Room>();
        
        List<Room> sortedRooms = new List<Room>(allRooms);
        sortedRooms.Sort((a, b) => a.CreationTime.CompareTo(b.CreationTime));

        List<Room> roomsToKeep = new List<Room>();
        
        if (newRoom != null)
        {
            roomsToKeep.Add(newRoom);
        }
        
        if (currentRoom != null && currentRoom != newRoom)
        {
            roomsToKeep.Add(currentRoom);
        }

        int maxRooms = 3;
        int roomsToDelete = sortedRooms.Count - maxRooms;

        if (roomsToDelete > 0)
        {
            int deleted = 0;
            foreach (Room room in sortedRooms)
            {
                if (deleted >= roomsToDelete)
                {
                    break;
                }

                if (roomsToKeep.Contains(room))
                {
                    continue;
                }

                CleanupPuzzleObjectsFromRoom(room);
                Destroy(room.gameObject);
                deleted++;
            }
        }
    }

    private void CleanupPuzzleObjectsFromRoom(Room room)
    {
        if (room == null) return;

        Note[] notes = room.GetComponentsInChildren<Note>();
        foreach (Note note in notes)
        {
            if (note != null) Destroy(note.gameObject);
        }

        Clock[] clocks = room.GetComponentsInChildren<Clock>();
        foreach (Clock clock in clocks)
        {
            if (clock != null) Destroy(clock.gameObject);
        }

        UVFlashlight[] flashlights = room.GetComponentsInChildren<UVFlashlight>();
        foreach (UVFlashlight flashlight in flashlights)
        {
            if (flashlight != null && !flashlight.IsPickedUp())
            {
                Destroy(flashlight.gameObject);
            }
        }
    }

    public void ClearUVMarks()
    {
        UvMark[] uvMarks = GetComponentsInChildren<UvMark>();
        foreach (UvMark uvMark in uvMarks)
        {
            if (uvMark != null) Destroy(uvMark.gameObject);
        }

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null && renderer.gameObject.name.Contains("claw-scratch"))
            {
                Destroy(renderer.gameObject);
            }
        }
    }

    private Vector3 CalculateSpawnPosition(Transform currentRoom)
    {
        Vector3 baseOffset = roomSpawnOffset;
        Vector3 rotatedOffset = currentRoom.rotation * baseOffset;
        return currentRoom.position + rotatedOffset;
    }

    private Quaternion CalculateRotation(Transform currentRoom)
    {
        float currentY = currentRoom.rotation.eulerAngles.y;
        return Quaternion.Euler(0f, currentY + roomRotationY, 0f);
    }

    private void SetupRoomTrigger(GameObject newRoom, Room previousRoom)
    {
        RoomBoundsTrigger existingTrigger = newRoom.GetComponentInChildren<RoomBoundsTrigger>();
        if (existingTrigger == null)
        {
            GameObject triggerObj = new GameObject("RoomBoundsTrigger");
            triggerObj.transform.SetParent(newRoom.transform);
            triggerObj.transform.localPosition = Vector3.zero;

            RoomBoundsTrigger trigger = triggerObj.AddComponent<RoomBoundsTrigger>();
            trigger.currentRoom = newRoom.GetComponent<Room>();
            trigger.entranceDoor = this; 
        }
        else
        {
            existingTrigger.entranceDoor = this;
            existingTrigger.currentRoom = newRoom.GetComponent<Room>();
        }
    }

    private void SetupPuzzlesForRoom(int roomNumber)
    {
        NotePuzzleManager noteManager = NotePuzzleManager.Instance;
        UVFlashlightPuzzleManager uvManager = UVFlashlightPuzzleManager.Instance;

        if (noteManager != null)
        {
            noteManager.PrepareForNewRoom(roomNumber);
            noteManager.SpawnNoteIfNeeded(roomNumber);
            noteManager.SpawnClockIfNeeded();
        }

        if (uvManager != null)
        {
            uvManager.PrepareForNewRoom(roomNumber);
            uvManager.SpawnUVFlashlightIfNeeded();
        }
    }

    private void ResetPuzzles()
    {
        NotePuzzleManager.Instance?.ResetPuzzleSystem();
        UVFlashlightPuzzleManager.Instance?.ResetPuzzleSystem();
    }

    public void StopDoorSounds()
    {
        if (AudioSource != null && AudioSource.isPlaying)
        {
            AudioSource.Stop();
        }

        if (soundTrigger != null)
        {
            soundTrigger.DisableTrigger();
        }
    }

    public void PlayDoorSound()
    {
        if (doorOpened || !isAudioInitialized || AudioSource == null)
        {
            return;
        }

        Room currentRoom = GetComponentInParent<Room>();
        if (currentRoom != null)
        {
            string roomAnomaly = GetCurrentRoomAnomaly(currentRoom);

            if (roomAnomaly == "BreathingWalls" || roomAnomaly == "ColorAura" ||
                roomAnomaly == "MirrorAnomaly" ||
                (roomAnomaly == "UVMarks" && UVFlashlightPuzzleManager.PlayerHasUVFlashlight))
            {
                return;
            }
        }

        NotePuzzleManager puzzleManager = NotePuzzleManager.Instance;
        Clock clock = currentRoom != null ? currentRoom.GetComponentInChildren<Clock>(true) : null;
        bool useDeceptiveSounds = false;

        if (puzzleManager != null && puzzleManager.HasClockInCurrentRoom && clock != null)
        {
            useDeceptiveSounds = !clock.IsTimeBetweenSixAndNine();
        }

        AudioClip soundToPlay;
        if (isCorrectDoor)
        {
            soundToPlay = useDeceptiveSounds ? AudioClipFalse : AudioClipTrue;
        }
        else
        {
            soundToPlay = useDeceptiveSounds ? AudioClipTrue : AudioClipFalse;
        }

        if (soundToPlay == null)
        {
            return;
        }

        AudioSource.Stop();
        AudioSource.clip = soundToPlay;
        AudioSource.loop = false;
        AudioSource.volume = 1f;
        AudioSource.spatialBlend = 1f;
        AudioSource.maxDistance = 10f;
        AudioSource.playOnAwake = false;

        AudioSource.Play();
    }

    private string GetCurrentRoomAnomaly(Room room)
    {
        return room.CurrentAnomaly;
    }

    public void ReinitializeAudio()
    {
        if (AudioSource == null)
        {
            AudioSource = gameObject.AddComponent<AudioSource>();
        }

        AudioSource.spatialBlend = 1f;
        AudioSource.maxDistance = 10f;
        AudioSource.playOnAwake = false;
        AudioSource.loop = false;
        AudioSource.volume = 1f;
        AudioSource.enabled = true;

        isAudioInitialized = true;
    }

    public void RecheckSoundTrigger()
    {
        RemoveExistingSoundTrigger();
        if (ShouldCreateSoundTrigger())
        {
            CreateSoundTrigger();
        }
    }
}
