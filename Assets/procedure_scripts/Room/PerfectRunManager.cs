using UnityEngine;

public class PerfectRunManager : MonoBehaviour
{
    public static PerfectRunManager Instance;

    [Header("Perfect Run Settings")]
    public int requiredPerfectRooms = 25;
    public int currentPerfectStreak = 0;
    public int totalRoomsAttempted = 0;

    [Header("UI Display ()")]
    public UnityEngine.UI.Text streakText; 

    public event System.Action<int> OnStreakUpdated;
    public event System.Action OnPerfectRunAchieved;
    public event System.Action OnStreakBroken;

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
    }

    public void OnRoomCompleted(bool wasCorrectDoor)
    {
        totalRoomsAttempted++;

        if (wasCorrectDoor)
        {
            currentPerfectStreak++;
            Debug.Log($"?  ! : {currentPerfectStreak}/{requiredPerfectRooms}");

            OnStreakUpdated?.Invoke(currentPerfectStreak);

            
            UpdateUI();

            
            if (currentPerfectStreak >= requiredPerfectRooms)
            {
                AchieveVictory();
                return;
            }

            
            if (currentPerfectStreak >= 20)
            {
                OnHighStakesWarning();
            }
            else if (currentPerfectStreak >= 15)
            {
                OnApproachingVictory();
            }
            else if (currentPerfectStreak >= 10)
            {
                OnGoodProgress();
            }
        }
        else
        {
            
            int lostStreak = currentPerfectStreak;
            currentPerfectStreak = 0;
            Debug.Log($"?? !  . : {lostStreak} ");

            OnStreakBroken?.Invoke();
            UpdateUI();

            
            if (lostStreak >= 20)
            {
                OnDevastatingLoss();
            }
            else if (lostStreak >= 15)
            {
                OnHeartbreakingLoss();
            }
            else if (lostStreak >= 10)
            {
                OnFrustratingLoss();
            }
        }
    }

    private void AchieveVictory()
    {
        Debug.Log("?? !  25   !");
        OnPerfectRunAchieved?.Invoke();

        
        if (VoiceGuideSystem.Instance != null)
        {
            VoiceGuideSystem.Instance.QueuePriorityMessage(new VoiceGuideSystem.VoiceMessage
            {
                message = "Поздравляю! 25 Были пройдены...",
                voiceType = VoiceGuideSystem.VoiceType.Helpful
            });
        }
    }

    private void OnHighStakesWarning()
    {
        if (VoiceGuideSystem.Instance != null)
        {
            string[] warnings = {

            };

            VoiceGuideSystem.Instance.QueueMessage(new VoiceGuideSystem.VoiceMessage
            {
                message = warnings[Random.Range(0, warnings.Length)],
                voiceType = VoiceGuideSystem.VoiceType.Atmospheric
            });
        }
    }

    private void OnApproachingVictory()
    {
        if (VoiceGuideSystem.Instance != null)
        {
            string[] messages = {

            };

            VoiceGuideSystem.Instance.QueueMessage(new VoiceGuideSystem.VoiceMessage
            {
                message = messages[Random.Range(0, messages.Length)],
                voiceType = VoiceGuideSystem.VoiceType.Helpful
            });
        }
    }

    private void OnGoodProgress()
    {
        if (VoiceGuideSystem.Instance != null)
        {
            string[] messages = {

            };

            VoiceGuideSystem.Instance.QueueMessage(new VoiceGuideSystem.VoiceMessage
            {
                message = messages[Random.Range(0, messages.Length)],
                voiceType = VoiceGuideSystem.VoiceType.Helpful
            });
        }
    }

    private void OnFrustratingLoss()
    {
        if (VoiceGuideSystem.Instance != null)
        {
            string[] reactions = {

            };

            VoiceGuideSystem.Instance.QueueMessage(new VoiceGuideSystem.VoiceMessage
            {
                message = reactions[Random.Range(0, reactions.Length)],
                voiceType = VoiceGuideSystem.VoiceType.Deceptive
            });
        }
    }

    private void OnHeartbreakingLoss()
    {
        if (VoiceGuideSystem.Instance != null)
        {
            string[] reactions = {

            };

            VoiceGuideSystem.Instance.QueuePriorityMessage(new VoiceGuideSystem.VoiceMessage
            {
                message = reactions[Random.Range(0, reactions.Length)],
                voiceType = VoiceGuideSystem.VoiceType.Deceptive
            });
        }
    }

    private void OnDevastatingLoss()
    {
        if (VoiceGuideSystem.Instance != null)
        {
            string[] reactions = {
                
            };

            VoiceGuideSystem.Instance.QueuePriorityMessage(new VoiceGuideSystem.VoiceMessage
            {
                message = reactions[Random.Range(0, reactions.Length)],
                voiceType = VoiceGuideSystem.VoiceType.Deceptive
            });
        }

        
        if (CameraSanitySystem.Instance != null)
        {
            CameraSanitySystem.Instance.OnWrongDoorSelected();
            CameraSanitySystem.Instance.OnWrongDoorSelected(); 
        }
    }

    private void UpdateUI()
    {
        
        if (streakText != null)
        {
            streakText.text = $" : {currentPerfectStreak}/{requiredPerfectRooms}";
        }
    }

    
    public int GetCurrentStreak() => currentPerfectStreak;
    public int GetRemainingRooms() => Mathf.Max(0, requiredPerfectRooms - currentPerfectStreak);
    public float GetProgressPercentage() => (float)currentPerfectStreak / requiredPerfectRooms;
    public bool IsOnHighStakes() => currentPerfectStreak >= 20;

    public void ResetPerfectRun()
    {
        currentPerfectStreak = 0;
        totalRoomsAttempted = 0;
        OnStreakUpdated?.Invoke(0);
        UpdateUI();
    }
}