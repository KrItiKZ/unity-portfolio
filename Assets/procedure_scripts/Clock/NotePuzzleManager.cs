using UnityEngine;

public class NotePuzzleManager : MonoBehaviour
{
    public static NotePuzzleManager Instance;

    [Header("Note Settings")]
    public GameObject notePrefab;

    [Header("Clock Settings")]
    public GameObject clockPrefab;

    public bool HasNoteInCurrentRoom { get; private set; }
    public bool HasClockInCurrentRoom { get; private set; }
    public bool HasNoteBeenFound { get; private set; }  
    public bool HasClockBeenActivated { get; private set; }

    private bool hasNoteSpawnedThisSession = false;
    private bool hasClockSpawnedThisSession = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void MarkNoteAsFound()
    {
        HasNoteBeenFound = true;
        VoiceGuideSystem.Instance?.OnNoteFound();
    }

    public void MarkClockAsActivated()
    {
        HasClockBeenActivated = true;
    }

    public void PrepareForNewRoom(int roomNumber, string roomAnomaly = null)
    {
        if (roomAnomaly == null && AnomalyManager.Instance != null)
        {
            roomAnomaly = AnomalyManager.Instance.currentRoomAnomaly;
            string cached = AnomalyManager.Instance.GetAnomalyForRoom(roomNumber, 
                SessionManager.Instance != null ? SessionManager.Instance.currentSession : 1);
            if (!string.IsNullOrEmpty(cached)) roomAnomaly = cached;
        }

        HasNoteInCurrentRoom = false;
        HasClockInCurrentRoom = false;

        if (string.IsNullOrEmpty(roomAnomaly)) return;

        if (roomAnomaly == "NoteSpawn" && !hasNoteSpawnedThisSession)
        {
            HasNoteInCurrentRoom = true;
            hasNoteSpawnedThisSession = true;
        }
        else if (roomAnomaly == "ClockPuzzle" && HasNoteBeenFound)  
        {
            HasClockInCurrentRoom = true;
        }
    }

    public void SpawnNoteIfNeeded(int roomNumber, Room targetRoom = null)
    {
        if (HasNoteInCurrentRoom && notePrefab != null)
        {
            Room currentRoom = targetRoom != null ? targetRoom : FindAnyObjectByType<Room>();
            if (currentRoom == null) return;

            Note existingNote = currentRoom.GetComponentInChildren<Note>();
            if (existingNote != null) return;

            GameObject note = Instantiate(notePrefab, currentRoom.transform);
            Note noteScript = note.GetComponent<Note>();

            if (noteScript != null)
            {
                noteScript.SetNoteText("Если на часах время с 6 до 9, то верная дверь всегда левая...\n" +
                                      "Если время будет другое, то доверься звуку двери, которая кажется неверной...");
            }
        }
    }

    public void SpawnClockIfNeeded(Room targetRoom = null)
    {
        if (HasClockInCurrentRoom && clockPrefab != null)
        {
            Room currentRoom = targetRoom != null ? targetRoom : FindAnyObjectByType<Room>();
            if (currentRoom == null) return;

            Clock existingClock = currentRoom.GetComponentInChildren<Clock>();
            if (existingClock != null) return;

            GameObject clock = Instantiate(clockPrefab, currentRoom.transform);
            clock.SetActive(true);
            Clock clockScript = clock.GetComponent<Clock>();

            if (clockScript != null)
            {
                int randomHour = Random.Range(0, 24);
                int randomMinute = Random.Range(0, 12) * 5;
                clockScript.SetTime(randomHour, randomMinute);
            }
        }
    }

    public void ResetPuzzleSystem()
    {
        hasNoteSpawnedThisSession = false;
        hasClockSpawnedThisSession = false;  
        HasNoteInCurrentRoom = false;
        HasClockInCurrentRoom = false;
        HasClockBeenActivated = false;  
    }

    public void OnNoteFound()
    {
        HasNoteBeenFound = true;
        VoiceGuideSystem.Instance?.OnNoteFound();
    }
}