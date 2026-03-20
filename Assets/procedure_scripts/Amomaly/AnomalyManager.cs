using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AnomalyManager : MonoBehaviour
{
    public static AnomalyManager Instance;

    [System.Serializable]
    public class AnomalyConfig
    {
        public string anomalyName;
        public bool isEnabled = true;
        [Range(0.1f, 5.0f)] public float spawnWeight = 1.0f;
        [Range(0.1f, 1.0f)] public float fatigueMultiplier = 0.5f;
        public int minSession = 1;
        public int maxSession = 999;
    }

    [System.Serializable]
    public class AnomalyDependency
    {
        public string anomalyName;
        public int minRoomNumber = 1;
        public int maxRoomNumber = 999;
        public string[] requiredAnomalies;
        public string[] forbiddenAnomalies;
        public bool requiresUVFlashlight = false;
        public bool oncePerSession = true;
        public bool oncePerGame = false;  
        public int priority = 0;
    }

    [Header("Anomaly Configuration")]
    public AnomalyConfig[] allAnomalies;

    [Header("Anomaly Dependencies")]
    public AnomalyDependency[] anomalyDependencies;

    public string currentRoomAnomaly = "None";

    private Dictionary<int, string> roomAnomalies = new Dictionary<int, string>();
    private List<AnomalyConfig> availableAnomalies = new List<AnomalyConfig>();
    private List<string> spawnedAnomaliesThisSession = new List<string>();
    private Dictionary<string, float> anomalyFatigue = new Dictionary<string, float>();
    private Queue<string> anomalyHistory = new Queue<string>();
    private int historySize = 3;
    private Room lastEnteredRoom = null;
    private Dictionary<string, bool> hasAssignedOncePerGame = new Dictionary<string, bool>();  

    private Dictionary<int, bool> roomIsFake = new Dictionary<int, bool>();

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
        }

        InitializeDependencies();
    }

    private void InitializeDependencies()
    {
        if (anomalyDependencies == null || anomalyDependencies.Length == 0)
        {
            anomalyDependencies = new AnomalyDependency[]
            {
                new AnomalyDependency { anomalyName = "SoundHints", minRoomNumber = 1, maxRoomNumber = 999, oncePerSession = false, oncePerGame = false, priority = 1 },
                new AnomalyDependency { anomalyName = "UVMarks", minRoomNumber = 1, maxRoomNumber = 999, requiresUVFlashlight = true, oncePerSession = false, oncePerGame = false, priority = 2 },
                new AnomalyDependency { anomalyName = "UVFlashlightSpawn", minRoomNumber = 1, maxRoomNumber = 5, oncePerSession = true, oncePerGame = false, priority = 3 },
                new AnomalyDependency { anomalyName = "NoteSpawn", minRoomNumber = 1, maxRoomNumber = 999, oncePerSession = true, oncePerGame = true, priority = 2 },  
                new AnomalyDependency { anomalyName = "ClockPuzzle", minRoomNumber = 1, maxRoomNumber = 999, requiredAnomalies = null, oncePerSession = true, oncePerGame = false, priority = 2 },  
                new AnomalyDependency { anomalyName = "MirrorAnomaly", minRoomNumber = 1, maxRoomNumber = 999, oncePerSession = false, oncePerGame = false, priority = 2 },
                new AnomalyDependency { anomalyName = "BreathingWalls", minRoomNumber = 1, maxRoomNumber = 8, oncePerSession = false, oncePerGame = false, priority = 1},
                new AnomalyDependency { anomalyName = "ColorAura", minRoomNumber = 2, maxRoomNumber = 9, oncePerSession = false, oncePerGame = false, priority = 1}
            };
        }

        
        foreach (var dep in anomalyDependencies)
        {
            if (!hasAssignedOncePerGame.ContainsKey(dep.anomalyName))
            {
                hasAssignedOncePerGame[dep.anomalyName] = false;
            }
        }
    }

    public bool ShouldSpawnFakeRoom(int roomNumber)
    {
        if (roomIsFake.ContainsKey(roomNumber))
            return roomIsFake[roomNumber];
        bool isFake = (roomNumber >= 6);
        roomIsFake[roomNumber] = isFake;
        return isFake;
    }

    public void SetupNewSession(int sessionNumber)
    {
        RefreshAvailableAnomalies(sessionNumber);
        roomAnomalies.Clear();
        spawnedAnomaliesThisSession.Clear();
        anomalyHistory.Clear();
        currentRoomAnomaly = "SoundHints";
        roomIsFake.Clear();                    
    }

    private void RefreshAvailableAnomalies(int sessionNumber)
    {
        availableAnomalies.Clear();
        if (allAnomalies == null) return;

        foreach (AnomalyConfig anomaly in allAnomalies)
        {
            if (anomaly.isEnabled && sessionNumber >= anomaly.minSession && sessionNumber <= anomaly.maxSession)
            {
                availableAnomalies.Add(anomaly);
            }
        }
    }

    public string GetAnomalyForRoom(int roomNumber, int sessionNumber)
    {
        
        if (availableAnomalies.Count == 0)
        {
            RefreshAvailableAnomalies(sessionNumber);
        }

        
        if (roomAnomalies.ContainsKey(roomNumber))
        {
            string cachedAnomaly = roomAnomalies[roomNumber];
            currentRoomAnomaly = cachedAnomaly;
            
            
            
            AnomalyDependency dependency = System.Array.Find(anomalyDependencies, d => d.anomalyName == cachedAnomaly);
            if (dependency != null && dependency.oncePerSession && !spawnedAnomaliesThisSession.Contains(cachedAnomaly))
            {
                spawnedAnomaliesThisSession.Add(cachedAnomaly);
            }
            
            return currentRoomAnomaly;
        }

        
        List<AnomalyConfig> tempAvailableAnomalies = availableAnomalies
            .Where(anomaly => CanAnomalySpawn(anomaly.anomalyName, roomNumber, sessionNumber))
            .ToList();

        if (tempAvailableAnomalies.Count == 0)
        {
            currentRoomAnomaly = "SoundHints";
            roomAnomalies[roomNumber] = currentRoomAnomaly;
            return currentRoomAnomaly;
        }

        string chosenAnomaly = SelectAnomalyWithProgression(tempAvailableAnomalies, roomNumber, sessionNumber);
        currentRoomAnomaly = chosenAnomaly;
        roomAnomalies[roomNumber] = currentRoomAnomaly;
        spawnedAnomaliesThisSession.Add(chosenAnomaly);

        
        AnomalyDependency dep = System.Array.Find(anomalyDependencies, d => d.anomalyName == chosenAnomaly);
        if (dep != null && dep.oncePerGame)
        {
            hasAssignedOncePerGame[chosenAnomaly] = true;
        }

        return chosenAnomaly;
    }

    public bool CanAnomalySpawn(string anomalyName, int roomNumber, int sessionNumber)
    {
        
        AnomalyConfig config = System.Array.Find(allAnomalies, a => a.anomalyName == anomalyName);
        if (config != null && !config.isEnabled) return false;

        AnomalyDependency dependency = System.Array.Find(anomalyDependencies, d => d.anomalyName == anomalyName);
        if (dependency == null) return true;

        if (roomNumber < dependency.minRoomNumber || roomNumber > dependency.maxRoomNumber)
            return false;

        if (dependency.oncePerSession && spawnedAnomaliesThisSession.Contains(anomalyName))
            return false;

        
        if (dependency.oncePerGame && hasAssignedOncePerGame.ContainsKey(anomalyName) && hasAssignedOncePerGame[anomalyName])
        {
            return false;
        }

        if (dependency.forbiddenAnomalies != null && dependency.forbiddenAnomalies.Any(forbidden => spawnedAnomaliesThisSession.Contains(forbidden)))
            return false;

        if (dependency.requiredAnomalies != null && !dependency.requiredAnomalies.All(required => spawnedAnomaliesThisSession.Contains(required)))
            return false;

        
        if (dependency.requiresUVFlashlight)
        {
            bool hasFlashlight = UVFlashlightPuzzleManager.PlayerHasUVFlashlight;
            if (!hasFlashlight)
            {
                return false; 
            }
        }

        
        if (anomalyName == "UVFlashlightSpawn" && UVFlashlightPuzzleManager.PlayerHasUVFlashlight)
        {
            return false;
        }

        
        if (anomalyName == "NoteSpawn" && NotePuzzleManager.Instance != null && NotePuzzleManager.Instance.HasNoteBeenFound)
        {
            return false;
        }

        
        if (anomalyName == "ClockPuzzle" && (NotePuzzleManager.Instance == null || !NotePuzzleManager.Instance.HasNoteBeenFound))
        {
            return false;
        }

        return true;
    }

    public void ForceSetRoomAnomaly(int roomNumber, string anomalyName)
    {
        
        if (CanAnomalySpawnStrict(anomalyName, roomNumber, SessionManager.Instance.currentSession))
        {
            currentRoomAnomaly = anomalyName;
            roomAnomalies[roomNumber] = currentRoomAnomaly;

            if (!spawnedAnomaliesThisSession.Contains(anomalyName))
            {
                spawnedAnomaliesThisSession.Add(anomalyName);
            }

            
            AnomalyDependency dep = System.Array.Find(anomalyDependencies, d => d.anomalyName == anomalyName);
            if (dep != null && dep.oncePerGame)
            {
                hasAssignedOncePerGame[anomalyName] = true;
            }

            Debug.Log($"Принудительно установлена аномалия: {anomalyName} для комнаты {roomNumber}");
        }
        else
        {
            
            currentRoomAnomaly = "SoundHints";
            roomAnomalies[roomNumber] = currentRoomAnomaly;
            Debug.Log($"{anomalyName} не может быть установлена, используется SoundHints для комнаты {roomNumber}");
        }
    }

    private bool CanAnomalySpawnStrict(string anomalyName, int roomNumber, int sessionNumber)
    {
        AnomalyDependency dependency = System.Array.Find(anomalyDependencies, d => d.anomalyName == anomalyName);
        if (dependency == null) return true;

        
        if (dependency.requiresUVFlashlight)
        {
            bool hasFlashlight = UVFlashlightPuzzleManager.PlayerHasUVFlashlight;
            if (!hasFlashlight) return false;
        }

        return CanAnomalySpawn(anomalyName, roomNumber, sessionNumber);
    }

    private string SelectAnomalyWithProgression(List<AnomalyConfig> anomalies, int roomNumber, int sessionNumber)
    {
        bool playerHasFlashlight = UVFlashlightPuzzleManager.PlayerHasUVFlashlight;

        Dictionary<string, float> progressionWeights = new Dictionary<string, float>();

        foreach (var anomaly in anomalies)
        {
            float baseWeight = anomaly.spawnWeight;

            if (anomaly.anomalyName == "UVFlashlightSpawn" && !playerHasFlashlight && roomNumber <= 3)
                baseWeight *= 2.0f;

            if (anomaly.anomalyName == "UVMarks" && playerHasFlashlight)
                baseWeight *= 2.0f;

            
            

            progressionWeights[anomaly.anomalyName] = baseWeight;
        }

        var sortedAnomalies = anomalies.OrderByDescending(a =>
            System.Array.Find(anomalyDependencies, d => d.anomalyName == a.anomalyName)?.priority ?? 0
        ).ToList();

        float totalWeight = progressionWeights.Values.Sum();
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var anomaly in sortedAnomalies)
        {
            currentWeight += progressionWeights[anomaly.anomalyName];
            if (randomValue <= currentWeight)
            {
                string selectedAnomaly = anomaly.anomalyName;

                if (anomalyHistory.Contains(selectedAnomaly) && selectedAnomaly != "UVMarks" && selectedAnomaly != "MirrorAnomaly" && sortedAnomalies.Count > 1)
                    continue;

                UpdateAnomalyHistory(selectedAnomaly);
                return selectedAnomaly;
            }
        }

        return "SoundHints";
    }

    private void UpdateAnomalyHistory(string anomalyName)
    {
        anomalyHistory.Enqueue(anomalyName);
        while (anomalyHistory.Count > historySize)
            anomalyHistory.Dequeue();
    }

    public bool IsAnomalyActive(string anomalyName) => currentRoomAnomaly == anomalyName;

    public void ForceUpdateRoomAnomaly(int roomNumber, int sessionNumber)
    {
        if (roomAnomalies.ContainsKey(roomNumber))
        {
            roomAnomalies.Remove(roomNumber);
        }
        GetAnomalyForRoom(roomNumber, sessionNumber);
    }

    public void UpdateCurrentRoomAnomaly(Room room)
    {
        if (room != null)
        {
            lastEnteredRoom = room;
            currentRoomAnomaly = room.CurrentAnomaly;
        }
    }
}