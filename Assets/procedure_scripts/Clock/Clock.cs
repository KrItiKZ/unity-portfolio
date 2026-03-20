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

            if (debugMode)
                Debug.Log($" : {hourAngle} (: {currentHour})");
        }

        if (minuteHand != null)
        {
                 
            float minuteAngle = currentMinute * 6f;

                
            minuteAngle += minuteRotationOffset;

            minuteHand.localRotation = Quaternion.Euler(0f, 0f, minuteAngle);

            if (debugMode)
                Debug.Log($" : {minuteAngle} (: {currentMinute})");
        }
    }

    public bool IsTimeBetweenSixAndNine()
    {
        bool isBetweenDigits = (currentHour >= 6 && currentHour <= 9);

        if (debugMode)
        {
            Debug.Log($"===    ===");
            Debug.Log($" : {currentHour}:{currentMinute:D2}");
            Debug.Log($" : {currentHour}");
            Debug.Log($"  6  9 : {isBetweenDigits}");
            Debug.Log($": {currentHour} >= 6 && {currentHour} <= 9");
            Debug.Log("=============================");
        }

        return isBetweenDigits;
    }

    [ContextMenu("Test 9:05 -  :   9,   1")]
    public void Test905()
    {
        SetTime(9, 5);
        Debug.Log(":   9,   1");
    }

    [ContextMenu("Test 12:00 -  :    12")]
    public void Test1200()
    {
        SetTime(12, 0);
        Debug.Log(":    12");
    }

    [ContextMenu("Test 3:00 -  :   3,   12")]
    public void Test300()
    {
        SetTime(3, 0);
        Debug.Log(":   3,   12");
    }

    [ContextMenu("Test 6:00 -  :   6,   12")]
    public void Test600()
    {
        SetTime(6, 0);
        Debug.Log(":   6,   12");
    }

    [ContextMenu("Debug Current State")]
    public void DebugState()
    {
        Debug.Log($"===   ===");
        Debug.Log($" : {currentHour}:{currentMinute:D2}");

        if (hourHand != null)
            Debug.Log($" :  = {hourHand.localEulerAngles.z}");

        if (minuteHand != null)
            Debug.Log($" :  = {minuteHand.localEulerAngles.z}");

        Debug.Log($" : {hourRotationOffset}");
        Debug.Log($" : {minuteRotationOffset}");
        Debug.Log("========================");
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