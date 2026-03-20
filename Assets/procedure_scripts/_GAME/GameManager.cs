using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Managers")]
    public CameraSanitySystem sanitySystem;
    public VoiceGuideSystem voiceSystem;
    public UVFlashlightPuzzleManager uvManager;
    public NotePuzzleManager noteManager;
    public AnomalyManager anomalyManager;

    [Header("Fake Rooms")]
    public GameObject[] fakeRoomPrefabs;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeManagers();
    }

    private void InitializeManagers()
    {
        if (SessionManager.Instance == null && FindAnyObjectByType<SessionManager>() == null)
        {
            GameObject sessionObj = new GameObject("SessionManager");
            sessionObj.AddComponent<SessionManager>();
        }

        if (AnomalyManager.Instance == null && FindAnyObjectByType<AnomalyManager>() == null)
        {
            GameObject anomalyObj = new GameObject("AnomalyManager");
            anomalyObj.AddComponent<AnomalyManager>();
        }
    }

    private void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        GameEvents.Initialize();

        SetupDependencies();

        PrecacheReferences();

        if (AnomalyManager.Instance != null && SessionManager.Instance != null)
        {
            AnomalyManager.Instance.SetupNewSession(1);
        }

        InitializeFirstRoomIfNeeded();
    }

    private void InitializeFirstRoomIfNeeded()
    {
        StartCoroutine(InitializeFirstRoomWithDelay());
    }

    private IEnumerator InitializeFirstRoomWithDelay()
    {
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.1f);

        Room[] allRooms = FindObjectsByType<Room>(FindObjectsSortMode.None);

        foreach (Room room in allRooms)
        {
            if (room.roomNumber == 1)
            {
                RoomBoundsTrigger trigger = room.GetComponentInChildren<RoomBoundsTrigger>();
                if (trigger != null) trigger.enabled = false;

                yield return WaitForManagers();

                room.ForceSetupRoom(1);

                if (trigger != null) trigger.enabled = true;

                if (VoiceGuideSystem.Instance != null)
                {
                    VoiceGuideSystem.Instance.QueueMessage(new VoiceGuideSystem.VoiceMessage
                    {
                        message = "Где я?... Что это за место?",
                        voiceType = VoiceGuideSystem.VoiceType.Atmospheric
                    });
                }

                break;
            }
        }
    }

    private IEnumerator WaitForManagers()
    {
        int maxAttempts = 10;
        int attempts = 0;

        while ((AnomalyManager.Instance == null || SessionManager.Instance == null) && attempts < maxAttempts)
        {
            attempts++;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void SetupDependencies()
    {
        if (sanitySystem == null) sanitySystem = FindAnyObjectByType<CameraSanitySystem>();
        if (voiceSystem == null) voiceSystem = FindAnyObjectByType<VoiceGuideSystem>();
        if (uvManager == null) uvManager = FindAnyObjectByType<UVFlashlightPuzzleManager>();
        if (noteManager == null) noteManager = FindAnyObjectByType<NotePuzzleManager>();
        if (anomalyManager == null) anomalyManager = FindAnyObjectByType<AnomalyManager>();
    }

    private void PrecacheReferences()
    {
        var rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
        var doors = FindObjectsByType<Door>(FindObjectsSortMode.None);
        var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
    }

    private void OnApplicationQuit()
    {
        GameEvents.Initialize();
        if (UVMarkPool.Instance != null)
        {
            UVMarkPool.Instance.ClearAllMarks();
        }
    }
}