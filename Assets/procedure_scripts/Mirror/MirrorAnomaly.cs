using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class MirrorAnomaly : MonoBehaviour
{
    public static MirrorAnomaly Instance;

    [Header("Mirror Prefab")]
    public GameObject mirrorPrefab;

    [Header("Anomaly Settings")]
    public float realityDelay = 2f;
    public float truthChance = 0.7f;
    public int activationRoomNumber = 8;

    [Header("Visual Effects")]
    public Material glitchMaterial;
    public AudioClip mirrorActivationSound;
    public AudioClip mirrorHumSound;

    [Header("Marker Settings")]
    public Color truthColor = new Color(0.3f, 0.8f, 0.3f, 1f);
    public Color deceptionColor = new Color(0.8f, 0.3f, 0.3f, 1f);
    public Material truthMaterial;
    public Material deceptionMaterial;



    
    private GameObject mirrorObject;
    private Camera mirrorCamera;
    private Renderer mirrorRenderer;
    private Coroutine anomalyCoroutine;
    private Door[] roomDoors;
    private Door correctDoor;
    private bool isAnomalyActive = false;
    private bool isShowingTruth = false;

    private List<GameObject> currentMarkers = new List<GameObject>();
    private AudioSource audioSource;
    private Camera mainCamera;
    private RenderTexture mirrorTexture;
    private Material originalMirrorMaterial;

    
    private Transform mirrorTransform;
    private Vector3 initialCameraPosition;
    private Vector3 initialLocalCameraPosition;
    private Quaternion initialCameraRotation;
    private float minZOffset = -0.85f; 
    private float maxZOffset = -13f; 

    void Awake()
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

    void Start()
    {
        mainCamera = Camera.main;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
    }

    void Update()
    {
        if (isAnomalyActive && mirrorCamera != null && mainCamera != null && mirrorObject != null)
        {
            UpdatePositionOnlyMirror();
        }
    }

    public void OnRoomChanged(int roomNumber)
    {
        if (isAnomalyActive)
        {
            UpdateMirrorState(roomNumber);
        }
    }

    private void UpdateMirrorState(int roomNumber)
    {
        
        if (isShowingTruth)
        {
            Debug.Log($"Mirror still showing TRUTH in room {roomNumber}");
        }
        else
        {
            Debug.Log($"Mirror still showing LIE in room {roomNumber}");
        }
        
        
        FindRoomDoors();
        CreateMaterialMarkers();
    }

    public void ActivateMirrorAnomaly(int roomNumber)
    {
        Debug.Log($"ActivateMirrorAnomaly called for room {roomNumber}");

        if (isAnomalyActive)
        {
            return;
        }

        if (mirrorPrefab == null)
        {
            Debug.LogError("mirrorPrefab = null");
            return;
        }

        
        Camera prefabCamera = mirrorPrefab.GetComponentInChildren<Camera>();
        if (prefabCamera != null)
        {
            initialLocalCameraPosition = prefabCamera.transform.localPosition;
        }
        else
        {
            Debug.LogError("prefabCamera = null");
            return;
        }

        Room currentRoom = System.Array.Find(FindObjectsOfType<Room>(), r => r.roomNumber == roomNumber);

        if (currentRoom != null)
        {
            
            mirrorObject = Instantiate(mirrorPrefab, currentRoom.transform);
            mirrorObject.name = "DynamicMirror_Room" + roomNumber;
            mirrorObject.SetActive(true);
        }
        else
        {
            mirrorObject = Instantiate(mirrorPrefab, Vector3.zero, Quaternion.identity);
            mirrorObject.name = "DynamicMirror_Room" + roomNumber;
            mirrorObject.SetActive(true);
        }

        mirrorCamera = mirrorObject.GetComponentInChildren<Camera>();
        mirrorRenderer = mirrorObject.GetComponent<Renderer>();
        mirrorTransform = mirrorObject.transform;

        if (mirrorCamera == null)
        {
            Debug.LogError("mirrorCamera == null");
            return;
        }

        if (mirrorRenderer == null)
        {
            Debug.LogError("mirrorRender == null");
            return;
        }

        
        initialCameraPosition = mirrorCamera.transform.position;
        initialCameraRotation = mirrorCamera.transform.rotation;

        SetupDynamicMirror();

        isAnomalyActive = true;
        isShowingTruth = Random.value <= truthChance;

        if (anomalyCoroutine != null)
            StopCoroutine(anomalyCoroutine);

        anomalyCoroutine = StartCoroutine(MirrorAnomalySequence());

        if (mirrorActivationSound != null)
        {
            audioSource.PlayOneShot(mirrorActivationSound);
        }

        if (VoiceGuideSystem.Instance != null)
        {
            string message = isShowingTruth ?
                "Зеркало показывает правду... смотри в отражение..." :
                "Отражение обманывает... не верь зеркалу...";
            VoiceGuideSystem.Instance.QueueMessage(new VoiceGuideSystem.VoiceMessage
            {
                message = message,
                voiceType = VoiceGuideSystem.VoiceType.Atmospheric
            });
        }
    }

    void SetupDynamicMirror()
    {
        if (mirrorCamera == null) return;

        if (mirrorTexture != null)
        {
            mirrorTexture.Release();
            DestroyImmediate(mirrorTexture);
        }

        mirrorTexture = new RenderTexture(1024, 1024, 16)
        {
            name = "DynamicMirror",
            antiAliasing = 8,
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp,
            autoGenerateMips = false,
            useMipMap = false
        };

        mirrorTexture.Create();
        mirrorCamera.targetTexture = mirrorTexture;

        mirrorCamera.enabled = true;
        mirrorCamera.renderingPath = RenderingPath.Forward;
        mirrorCamera.allowHDR = false;
        mirrorCamera.fieldOfView = mainCamera.fieldOfView;
        mirrorCamera.nearClipPlane = 0.01f;
        mirrorCamera.farClipPlane = 100f;
        mirrorCamera.ResetProjectionMatrix();

        
        

        if (mirrorRenderer != null)
        {
            originalMirrorMaterial = mirrorRenderer.material;
            if (originalMirrorMaterial != null)
            {
                originalMirrorMaterial.mainTexture = mirrorTexture;
            }
            else
            {
                Debug.LogError("mirrorRender == null");
            }
        }

        
        int mirrorMarkersLayer = LayerMask.NameToLayer("MirrorMarkers");
        if (mirrorMarkersLayer >= 0)
        {
            mainCamera.cullingMask &= ~(1 << mirrorMarkersLayer);
        }

        mirrorCamera.Render();
    }

    void UpdatePositionOnlyMirror()
    {
        if (mirrorCamera == null || mainCamera == null || mirrorTransform == null) return;

        
        Vector3 mirrorPos = mirrorTransform.position;
        Vector3 mirrorNormal = mirrorTransform.forward; 

        
        Vector3 playerPos = mainCamera.transform.position;

        
        Vector3 toPlayer = playerPos - mirrorPos;

        
        
        float dotProduct = Vector3.Dot(toPlayer, mirrorNormal);
        Vector3 reflectedOffset = toPlayer - 2 * dotProduct * mirrorNormal;
        Vector3 reflectedPos = mirrorPos + reflectedOffset;

        
        Vector3 localReflectedPos = mirrorTransform.InverseTransformPoint(reflectedPos);

        
        if (localReflectedPos.z < minZOffset)
        {
            localReflectedPos.z = minZOffset;
        }

        
        localReflectedPos.y = initialLocalCameraPosition.y;
        localReflectedPos.z = initialLocalCameraPosition.z;

        
        localReflectedPos.x *= -1;

        reflectedPos = mirrorTransform.TransformPoint(localReflectedPos);

        
        
        float distanceToMirror = Vector3.Distance(playerPos, mirrorPos);
        if (distanceToMirror < 0.5f)
        {
            Vector3 safeOffset = mirrorTransform.forward * 0.1f;
            reflectedPos += safeOffset;
        }

        
        mirrorCamera.transform.position = reflectedPos;

        
        mirrorCamera.transform.rotation = initialCameraRotation;
    }

    private IEnumerator MirrorAnomalySequence()
    {
        yield return new WaitForSeconds(realityDelay);

        FindRoomDoors();
        CreateMaterialMarkers();

        if (mirrorHumSound != null)
        {
            audioSource.clip = mirrorHumSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void FindRoomDoors()
    {
        
        Room currentRoom = System.Array.Find(FindObjectsOfType<Room>(), r => r.CurrentAnomaly == "MirrorAnomaly");

        if (currentRoom != null)
        {
            roomDoors = currentRoom.GetComponentsInChildren<Door>();
        }
        else
        {
            roomDoors = FindObjectsByType<Door>(FindObjectsSortMode.None);
        }

        correctDoor = null;

        foreach (Door door in roomDoors)
        {
            if (door != null && door.isCorrectDoor)
            {
                correctDoor = door;
                break;
            }
        }

        if (correctDoor == null && roomDoors.Length > 0)
        {
            correctDoor = roomDoors[Random.Range(0, roomDoors.Length)];
        }
    }

    private void CreateMaterialMarkers()
    {
        RemoveAllMarkers();

        if (roomDoors == null || correctDoor == null) return;

        bool isLying = !isShowingTruth;

        foreach (Door door in roomDoors)
        {
            if (door != null)
            {
                bool isDoorCorrect = (door == correctDoor);
                bool isThisCorrectInMirror = isLying ? !isDoorCorrect : isDoorCorrect;
                CreateDoorMarker(door, isThisCorrectInMirror);
            }
        }
    }

    private Door GetRandomWrongDoor()
    {
        List<Door> wrongDoors = new List<Door>();
        foreach (Door door in roomDoors)
        {
            if (door != null && door != correctDoor)
            {
                wrongDoors.Add(door);
            }
        }
        return wrongDoors.Count > 0 ? wrongDoors[Random.Range(0, wrongDoors.Count)] : correctDoor;
    }

    private void CreateDoorMarker(Door door, bool isCorrectInMirror)
    {
        try
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = isCorrectInMirror ? "TruthMarker" : "DeceptionMarker";

            Vector3 markerPosition = door.transform.position + Vector3.up * 1f;
            marker.transform.position = markerPosition;
            marker.transform.localScale = Vector3.one * 0.3f;

            DestroyImmediate(marker.GetComponent<Collider>());

            
            marker.layer = LayerMask.NameToLayer("MirrorMarkers");

            Renderer renderer = marker.GetComponent<Renderer>();
            Material material = isCorrectInMirror ?
                (truthMaterial != null ? truthMaterial : CreateDefaultMaterial(truthColor)) :
                (deceptionMaterial != null ? deceptionMaterial : CreateDefaultMaterial(deceptionColor));

            renderer.material = material;
            currentMarkers.Add(marker);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error creating marker: {e.Message}");
        }
    }

    private Material CreateDefaultMaterial(Color color)
    {
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        Material material = new Material(urpShader != null ? urpShader : Shader.Find("Standard"));
        material.color = color;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * 0.5f);
        return material;
    }

    private void RemoveAllMarkers()
    {
        foreach (GameObject marker in currentMarkers)
        {
            if (marker != null) Destroy(marker);
        }
        currentMarkers.Clear();
    }

    private void StopMirrorAnomaly()
    {
        if (anomalyCoroutine != null)
            StopCoroutine(anomalyCoroutine);

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }

        RemoveAllMarkers();

        if (mirrorObject != null)
        {
            mirrorCamera = null;
            mirrorRenderer = null;
            mirrorTransform = null;
            DestroyImmediate(mirrorObject);
            mirrorObject = null;
        }

        if (mirrorTexture != null)
        {
            mirrorTexture.Release();
            mirrorTexture = null;
        }

        
        if (mainCamera != null)
        {
            int mirrorMarkersLayer = LayerMask.NameToLayer("MirrorMarkers");
            if (mirrorMarkersLayer >= 0)
            {
                mainCamera.cullingMask |= (1 << mirrorMarkersLayer);
            }
        }

        isAnomalyActive = false;
    }

    public void ForceRestoreMirror()
    {
        
        GameObject[] existingMirrors = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in existingMirrors)
        {
            if (obj != null && obj.name.StartsWith("DynamicMirror_"))
            {
                Destroy(obj);
            }
        }

        StopMirrorAnomaly();
    }

    public bool IsAnomalyActive() => isAnomalyActive;
    public bool WasLastHintTruth() => isShowingTruth;

    void OnDestroy()
    {
        StopMirrorAnomaly();
    }
}
