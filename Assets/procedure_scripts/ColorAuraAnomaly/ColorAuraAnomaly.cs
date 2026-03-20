using UnityEngine;
using System.Collections;

public class ColorAuraAnomaly : MonoBehaviour
{
    [Header("Aura Settings")]
    public float auraPulseSpeed = 1.2f;

    [Header("Color Settings - ")]
    public Color warmColor = new Color(1.5f, 1.2f, 0.5f, 1f);    
    public Color coldColor = new Color(0.5f, 0.7f, 1.5f, 1f);    
    public float colorIntensity = 0.8f;                          

    private Door[] roomDoors;
    private Door correctDoor;
    private bool isAnomalyActive = false;
    private Color[] originalDoorColors;

    void Start()
    {
        Debug.Log("?? ColorAuraAnomaly ");
    }

private void FindRoomDoors()
{
    
    Room currentRoom = GetComponentInParent<Room>();
    if (currentRoom == null)
    {
        Debug.LogError("ColorAuraAnomaly: Could not find parent Room!");
        return;
    }

    
    roomDoors = currentRoom.doors;
    if (roomDoors == null)
    {
        roomDoors = currentRoom.GetComponentsInChildren<Door>();
    }

    if (roomDoors == null || roomDoors.Length == 0)
    {
        Debug.LogError("ColorAuraAnomaly: No doors found in current room!");
        return;
    }

    originalDoorColors = new Color[roomDoors.Length];
    correctDoor = null; 

    int correctDoorCount = 0;

    for (int i = 0; i < roomDoors.Length; i++)
    {
        if (roomDoors[i] == null) continue;

        Renderer renderer = roomDoors[i].GetComponent<Renderer>();
        if (renderer != null)
        {
            
            originalDoorColors[i] = renderer.material.color;
        }
        else
        {
            originalDoorColors[i] = Color.white; 
        }

        if (roomDoors[i].isCorrectDoor)
        {
            correctDoor = roomDoors[i];
            correctDoorCount++;
        }
    }

    if (correctDoorCount != 1)
    {
        Debug.LogError($"ColorAuraAnomaly: Found {correctDoorCount} correct doors in current room, expected exactly 1!");
        correctDoor = null;
    }
}

    public void ActivateAuraAnomaly()
    {
        if (isAnomalyActive) return;

        isAnomalyActive = true;
        FindRoomDoors();

if (correctDoor == null)
{
    Debug.LogError("? ColorAura: correctDoor is null or invalid!");
    isAnomalyActive = false;
    return;
}

        StartCoroutine(AuraPulseCoroutine());

        if (VoiceGuideSystem.Instance != null)
        {
            VoiceGuideSystem.Instance.QueueMessage(new VoiceGuideSystem.VoiceMessage
            {
                message = "Всё ли в этой комнате нормально?... У дверей странная аура...",
                voiceType = VoiceGuideSystem.VoiceType.Helpful
            });
        }

        Debug.Log("?? ColorAuraAnomaly  - : " + correctDoor.doorNumber);
    }

private IEnumerator AuraPulseCoroutine()
{
    while (isAnomalyActive && roomDoors != null)
    {
        
        if (correctDoor == null || !correctDoor.gameObject.activeInHierarchy)
        {
            Debug.LogError("? ColorAura: correctDoor became invalid during coroutine!");
            StopAuraAnomaly();
            yield break;
        }

        float pulse = (Mathf.Sin(Time.time * auraPulseSpeed) + 1f) * 0.5f; 

        for (int i = 0; i < roomDoors.Length; i++)
        {
            if (roomDoors[i] == null) continue;

            Renderer renderer = roomDoors[i].GetComponent<Renderer>();
            if (renderer == null) continue;

            Color targetColor;
            float intensity = colorIntensity;

            if (roomDoors[i] == correctDoor)
            {
                targetColor = Color.Lerp(originalDoorColors[i], warmColor, intensity + pulse * 0.2f);
            }
            else
            {
                targetColor = Color.Lerp(originalDoorColors[i], coldColor, intensity * 0.8f);
            }

            renderer.material.color = targetColor;
        }

        yield return null;
    }
}

    public void StopAuraAnomaly()
    {
        isAnomalyActive = false;

        
        if (roomDoors != null)
        {
            for (int i = 0; i < roomDoors.Length; i++)
            {
                if (roomDoors[i] != null)
                {
                    Renderer renderer = roomDoors[i].GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = Color.white;
                    }
                }
            }
        }

        Debug.Log("?? ColorAuraAnomaly ");
    }

    void OnDestroy()
    {
        StopAuraAnomaly();
    }
}
