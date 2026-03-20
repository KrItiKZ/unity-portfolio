using UnityEngine;
using System.Collections;
using TMPro;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance;

    [Header("Session Tracking")]
    public int currentSession = 1;
    public int currentRoomInSession = 1;
    public int roomsPerSession = 8;

    [Header("UI References")]
    public TextMeshProUGUI sessionText;
    public TextMeshProUGUI roomText;
    public GameObject endingCanvas;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFirstSession();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeFirstSession();
        UpdateUI();
    }

    private void InitializeFirstSession()
    {
        currentSession = 1;
        currentRoomInSession = 1;
        AnomalyManager.Instance?.SetupNewSession(currentSession);

        VoiceGuideSystem.Instance?.QueueMessage(new VoiceGuideSystem.VoiceMessage
        {
            message = "Сессия 1. Пройдите 8 комнат для выхода...",
            voiceType = VoiceGuideSystem.VoiceType.Atmospheric
        });
    }

    public void OnDoorSelected(bool isCorrectDoor)
    {
        if (isCorrectDoor)
        {
            currentRoomInSession++;

            if (currentRoomInSession > roomsPerSession)
            {
                StartCoroutine(ShowEndingSequence());
            }
            else
            {
                UpdateUI();
            }
        }
        else
        {
            currentSession++;
            currentRoomInSession = 1;
            
            
            AnomalyManager.Instance?.SetupNewSession(currentSession);
            
            
            
            NotePuzzleManager.Instance?.ResetPuzzleSystem();
            
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (sessionText != null) sessionText.text = $"СЕССИЯ: {currentSession}";
        if (roomText != null) roomText.text = $"КОМНАТА: {currentRoomInSession}/8";
    }

    private IEnumerator ShowEndingSequence()
    {
        if (endingCanvas != null) endingCanvas.SetActive(true);

        VoiceGuideSystem.Instance?.QueueMessage(new VoiceGuideSystem.VoiceMessage
        {
            message = "Поздравляем! Вы прошли 8 комнат!",
            voiceType = VoiceGuideSystem.VoiceType.Helpful
        });

        yield return new WaitForSeconds(5f);

        if (endingCanvas != null) endingCanvas.SetActive(false);

        currentSession++;
        currentRoomInSession = 1;
        
        
        AnomalyManager.Instance?.SetupNewSession(currentSession);
        NotePuzzleManager.Instance?.ResetPuzzleSystem();
        
        UpdateUI();

        VoiceGuideSystem.Instance?.QueueMessage(new VoiceGuideSystem.VoiceMessage
        {
            message = $"Сессия {currentSession}. Снова начнем...",
            voiceType = VoiceGuideSystem.VoiceType.Atmospheric
        });
    }
}