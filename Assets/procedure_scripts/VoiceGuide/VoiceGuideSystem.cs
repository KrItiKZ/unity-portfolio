using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class VoiceGuideSystem : MonoBehaviour
{
    public static VoiceGuideSystem Instance;

    [Header("UI Elements")]
    public TextMeshProUGUI subtitleText;
    public CanvasGroup subtitlePanel;

    [Header("Audio Settings")]
    public AudioSource voiceAudioSource;    
    public AudioSource typingAudioSource;  
    public AudioClip typingSound;          

    [Header("Voice Settings")]
    public float typingSpeed = 0.05f;
    public float subtitleStayTime = 3f;
    public float fadeDuration = 0.5f;

    [System.Serializable]
    public class VoiceMessage
    {
        public string message;
        public AudioClip audioClip;         
        public VoiceType voiceType;
    }

    public enum VoiceType
    {
        Helpful,    
        Deceptive,  
        Atmospheric 
    }

    private Queue<VoiceMessage> messageQueue = new Queue<VoiceMessage>();
    private bool isShowingMessage = false;
    private Coroutine currentMessageCoroutine;
    private bool playerFoundNote = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void QueueMessage(VoiceMessage msg)
    {
        if (messageQueue.Count >= 15)
        {
            Debug.LogWarning("Voice message queue is full! Skipping message: " + msg.message);
            return;
        }

        foreach (var queuedMsg in messageQueue)
        {
            if (queuedMsg.message == msg.message)
            {
                Debug.Log("Duplicate voice message skipped: " + msg.message);
                return;
            }
        }

        messageQueue.Enqueue(msg);

        if (!isShowingMessage)
        {
            ShowNextMessage();
        }
    }

    public void QueuePriorityMessage(VoiceMessage priorityMsg)
    {
        if (currentMessageCoroutine != null)
        {
            StopCoroutine(currentMessageCoroutine);
            messageQueue.Clear();
        }

        StartCoroutine(ShowPriorityMessage(priorityMsg));
    }

    private void ShowNextMessage()
    {
        if (messageQueue.Count > 0)
        {
            VoiceMessage nextMsg = messageQueue.Dequeue();
            currentMessageCoroutine = StartCoroutine(ShowMessageCoroutine(nextMsg));
        }
    }

    private IEnumerator ShowMessageCoroutine(VoiceMessage currentMsg)
    {
        isShowingMessage = true;

        yield return StartCoroutine(FadeSubtitlePanel(0f, 1f, fadeDuration));

        yield return StartCoroutine(TypeText(currentMsg.message));

        if (currentMsg.audioClip != null && voiceAudioSource != null)
        {
            voiceAudioSource.PlayOneShot(currentMsg.audioClip);
        }

        yield return new WaitForSeconds(subtitleStayTime);

        yield return StartCoroutine(FadeSubtitlePanel(1f, 0f, fadeDuration));

        subtitleText.text = "";

        isShowingMessage = false;
        ShowNextMessage();
    }

    private IEnumerator ShowPriorityMessage(VoiceMessage priorityMsg)
    {
        isShowingMessage = true;

        yield return StartCoroutine(FadeSubtitlePanel(0f, 1f, fadeDuration * 0.5f));

        yield return StartCoroutine(TypeText(priorityMsg.message));

        if (priorityMsg.audioClip != null && voiceAudioSource != null)
        {
            voiceAudioSource.PlayOneShot(priorityMsg.audioClip);
        }

        yield return new WaitForSeconds(subtitleStayTime * 1.5f);
        yield return StartCoroutine(FadeSubtitlePanel(1f, 0f, fadeDuration));

        subtitleText.text = "";

        isShowingMessage = false;
        ShowNextMessage();
    }

    private IEnumerator TypeText(string text)
    {
        subtitleText.text = "";

        if (typingAudioSource != null && typingSound != null)
        {
            typingAudioSource.clip = typingSound;
            typingAudioSource.loop = true;
            typingAudioSource.Play();
        }

        foreach (char letter in text.ToCharArray())
        {
            subtitleText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        if (typingAudioSource != null)
        {
            typingAudioSource.Stop();
        }
    }

    private IEnumerator FadeSubtitlePanel(float fromAlpha, float toAlpha, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            subtitlePanel.alpha = Mathf.Lerp(fromAlpha, toAlpha, timer / duration);
            yield return null;
        }

        subtitlePanel.alpha = toAlpha;
    }


    public void OnRoomEnter(int roomNumber)
    {
        string[] roomEntries = {
            $"Комната {roomNumber}... всё получается?"
        };

        VoiceMessage roomMsg = new VoiceMessage
        {
            message = roomEntries[Random.Range(0, roomEntries.Length)],
            voiceType = VoiceType.Atmospheric
        };
        QueueMessage(roomMsg);
    }

    public void OnUVFlashlightPickedUp()
    {
        string[] flashlightMessages = {
            "Фонарик, может показать то, что не видно без него"
        };

        VoiceMessage flashlightMsg = new VoiceMessage
        {
            message = flashlightMessages[Random.Range(0, flashlightMessages.Length)],
            voiceType = VoiceType.Atmospheric
        };
        QueueMessage(flashlightMsg);
    }

    public void OnClockPuzzleActivated(int hour, int minute)
    {
        if (!playerFoundNote)
        {
            string[] veryVagueHints = {
                "Я чего-то не нашёл?"
            };

            VoiceMessage vagueMsg = new VoiceMessage
            {
                message = veryVagueHints[Random.Range(0, veryVagueHints.Length)],
                voiceType = VoiceType.Atmospheric
            };
            QueueMessage(vagueMsg);
            return;
        }
        string timeMessage = "";

        if (hour >= 6 && hour <= 9)
        {
            string[] vagueHints = {
                "Хмм... Время между 6 и 9, что же это могло значить?"
            };
            timeMessage = vagueHints[Random.Range(0, vagueHints.Length)];
        }
        else
        {
            string[] vagueHints = {
                "Нужно бы сфокусироваться на времени, где там моя записка?"
            };
            timeMessage = vagueHints[Random.Range(0, vagueHints.Length)];
        }

        VoiceMessage clockMsg = new VoiceMessage
        {
            message = timeMessage,
            voiceType = VoiceType.Helpful
        };
        QueueMessage(clockMsg);
    }

    public void OnNoteFound()
    {
        playerFoundNote = true;

        string[] noteReactions = {
            "Записка, интересно, что в ней написано?"
        };

        VoiceMessage noteMsg = new VoiceMessage
        {
            message = noteReactions[Random.Range(0, noteReactions.Length)],
            voiceType = VoiceType.Helpful
        };
        QueueMessage(noteMsg);
    }

    public void OnTimerWarning(float timeLeft)
    {
        string[] timerWarnings = {
            
        };

        VoiceMessage timerMsg = new VoiceMessage
        {
            message = timerWarnings[Random.Range(0, timerWarnings.Length)],
            voiceType = VoiceType.Atmospheric
        };
        QueuePriorityMessage(timerMsg);
    }

    public void OnPlayerMakingMistake()
    {
        string[] deceptiveMessages = {
            "Я допустил ошибку...но где?",
            "Это провал...",
            "Дверь оказалось неверной..."
        };

        VoiceMessage deceptiveMsg = new VoiceMessage
        {
            message = deceptiveMessages[Random.Range(0, deceptiveMessages.Length)],
            voiceType = VoiceType.Deceptive
        };
        QueueMessage(deceptiveMsg);
    }

    public void OnPlayerSuccess()
    {
        string[] successMessages = {
            "Отлично, комната пройдена!",
            "Ответ, верный?",
            "Всё получается?"
        };

        VoiceMessage successMsg = new VoiceMessage
        {
            message = successMessages[Random.Range(0, successMessages.Length)],
            voiceType = VoiceType.Atmospheric
        };
        QueueMessage(successMsg);
    }

    public void OnBreathingWallsActivated()
    {
        string[] breathingHints = {
        "С дверьми что-то не так..."
    };

        VoiceMessage breathingMsg = new VoiceMessage
        {
            message = breathingHints[Random.Range(0, breathingHints.Length)],
            voiceType = VoiceType.Helpful
        };
        QueueMessage(breathingMsg);
    }

    public void ClearAllMessages()
    {
        if (currentMessageCoroutine != null)
        {
            StopCoroutine(currentMessageCoroutine);
            currentMessageCoroutine = null;
        }

        messageQueue.Clear();
        subtitleText.text = "";
        subtitlePanel.alpha = 0f;
        isShowingMessage = false;

        StopAllAudioSafely();
    }

    private void StopAllAudioSafely()
    {
        if (typingAudioSource != null)
        {
            typingAudioSource.Stop();
            typingAudioSource.clip = null;
        }

        if (voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = null;
        }
    }
}