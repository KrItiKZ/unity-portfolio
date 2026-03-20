using UnityEngine;

public class Clock : MonoBehaviour
{
    [Header("Clock Hands")]
    public Transform hourHand;
    public Transform minuteHand;

    [Header("Current Time")]
    public int currentHour = 9;
    public int currentMinute = 5;

    [Header("Rotation Correction")]
    public float hourRotationOffset = -90f;     
    public float minuteRotationOffset = -90f;
    public bool debugMode = true;

    public void SetTime(int hour, int minute = 0)
    {
        currentHour = hour;
        currentMinute = minute;
        UpdateClockHands();

            
        if (NotePuzzleManager.Instance != null)
        {
            NotePuzzleManager.Instance.MarkClockAsActivated();
        }

           
        if (VoiceGuideSystem.Instance != null)
        {
            VoiceGuideSystem.Instance.OnClockPuzzleActivated(hour, minute);
        }
    }

    private void UpdateClockHands()
    {
        if (hourHand != null)
        {
            float hourAngle = ((currentHour % 12) * 30f) + (currentMinute * 0.5f);

            hourAngle += hourRotationOffset;

            hourHand.localRotation = Quaternion.Euler(0f, 0f, hourAngle);
        }

        if (minuteHand != null)
        {
                 
            float minuteAngle = currentMinute * 6f;

                
            minuteAngle += minuteRotationOffset;

            minuteHand.localRotation = Quaternion.Euler(0f, 0f, minuteAngle);
        }
    }

    public bool IsTimeBetweenSixAndNine()
    {
        bool isBetweenDigits = (currentHour >= 6 && currentHour <= 9);
        return isBetweenDigits;
    }

    [ContextMenu("Test 3:00-:3,12")]
    public void Test300()
    {
        SetTime(3, 0);
    }

    [ContextMenu("Test 6:00-:6,12")]
    public void Test600()
    {
        SetTime(6, 0);
    }

    private void Start()
    {
        UpdateClockHands();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateClockHands();
        }
    }
}