using UnityEngine;
using System.Collections;

public class BreathingWallsAnomaly : MonoBehaviour
{
    [Header("Breathing Settings")]
    public float correctBreathSpeed = 1.2f;         
    public float incorrectBreathSpeed = 2.5f;        
    public float minScale = 0.998f;                 
    public float maxScale = 1.002f;                  

    private Door[] roomDoors;
    private Door correctDoor;
    private bool isAnomalyActive = false;

    void Start()
    {
        Debug.Log("BreathingWallsAnomaly");
    }

    private void FindRoomDoors()
    {
        roomDoors = FindObjectsByType<Door>(FindObjectsSortMode.None);
        foreach (Door door in roomDoors)
        {
            if (door != null && door.isCorrectDoor)
            {
                correctDoor = door;
                break;
            }
        }
    }

    public void ActivateBreathingAnomaly()
    {
        if (isAnomalyActive) return;

        isAnomalyActive = true;
        FindRoomDoors();

        if (correctDoor == null)
        {
            Debug.LogError("BreathingWalls: correctDoor is null!");
            isAnomalyActive = false;
            return;
        }

        StartCoroutine(BreathingCoroutine());

        if (VoiceGuideSystem.Instance != null)
        {
            VoiceGuideSystem.Instance.QueueMessage(new VoiceGuideSystem.VoiceMessage
            {
                message = "С комнатой что-то не так... Двери, дышат?...",
                voiceType = VoiceGuideSystem.VoiceType.Helpful
            });
        }
    }

private IEnumerator BreathingCoroutine()
{
    while (isAnomalyActive && roomDoors != null)
    {
        foreach (Door door in roomDoors)
        {
            if (door == null) continue;

            if (correctDoor == null)
            {
                FindRoomDoors();
            }

            if (door.isCorrectDoor)
            {
                float correctTimer = Time.time * correctBreathSpeed;
                float correctBreath = (Mathf.Sin(correctTimer) + 1f) * 0.5f; 
                float correctScale = Mathf.Lerp(minScale, maxScale, correctBreath);
                door.transform.localScale = Vector3.one * correctScale;
            }
            else
            {
                float incorrectTimer = Time.time * incorrectBreathSpeed;

                float wave1 = Mathf.Sin(incorrectTimer);
                float wave2 = Mathf.Sin(incorrectTimer * 1.7f + 2f);
                float wave3 = Mathf.Sin(incorrectTimer * 2.3f + 5f);

                float chaoticBreath = (wave1 + wave2 + wave3 + 3f) / 6f; 
                chaoticBreath = Mathf.PingPong(chaoticBreath * 1.5f, 1f); 

                float incorrectScale = Mathf.Lerp(minScale * 0.999f, maxScale * 1.001f, chaoticBreath);
                door.transform.localScale = Vector3.one * incorrectScale;
            }
        }

        yield return null;
    }
}

    public void StopBreathingAnomaly()
    {
        isAnomalyActive = false;

        if (roomDoors != null)
        {
            foreach (Door door in roomDoors)
            {
                if (door != null)
                {
                    door.transform.localScale = Vector3.one;
                }
            }
        }
    }

    void OnDestroy()
    {
        StopBreathingAnomaly();
    }
}