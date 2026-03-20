using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PSXEffectController : MonoBehaviour
{
    [Header("Volume Reference")]
    public Volume postProcessVolume;

    [Header("PS1 Resolution Settings")]
    public bool enablePixelation = true;
    [Range(64, 480)] public int verticalResolution = 240;

    [Header("Vertex Wobble (PS1 Jitter)")]
    public bool enableVertexWobble = true;
    [Range(0f, 0.05f)] public float vertexPrecision = 0.015f;

    [Header("Color Depth Reduction")]
    public bool enableColorDepthReduction = true;
    [Range(2, 256)] public int colorDepth = 64;

    [Header("PSX Post-Processing")]
    [Range(0.5f, 2f)] public float contrast = 1.3f;
    [Range(0f, 1f)] public float grainIntensity = 0.4f;
    [Range(0f, 1f)] public float vignetteIntensity = 0.4f;
    [Range(0f, 0.5f)] public float ditheringIntensity = 0.2f;

    
    private ColorAdjustments colorAdjustments;
    private FilmGrain filmGrain;
    private Vignette vignette;
    private LiftGammaGain liftGammaGain;

    
    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private bool cameraFound = true;

    private void Start()
    {
        
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            mainCamera = FindAnyObjectByType<Camera>();
        }

        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.position;
            cameraFound = true;
            Debug.Log("Camera found: " + mainCamera.name);
        }
        else
        {
            cameraFound = false;
            Debug.LogError("No camera found in scene!");
        }

        SetupVolumeComponents();
    }

    private void SetupVolumeComponents()
    {
        if (postProcessVolume == null)
        {
            postProcessVolume = FindAnyObjectByType<Volume>();
        }

        if (postProcessVolume?.profile == null)
        {
            Debug.LogWarning("No Volume found. Creating one...");
            CreateVolume();
            return;
        }

        
        postProcessVolume.profile.TryGet(out colorAdjustments);
        postProcessVolume.profile.TryGet(out filmGrain);
        postProcessVolume.profile.TryGet(out vignette);
        postProcessVolume.profile.TryGet(out liftGammaGain);

        InitializeComponents();
    }

    private void CreateVolume()
    {
        GameObject volumeGO = new GameObject("PSX Volume");
        postProcessVolume = volumeGO.AddComponent<Volume>();
        postProcessVolume.isGlobal = true;

        
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        postProcessVolume.profile = profile;

        
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        if (postProcessVolume?.profile == null) return;

        
        if (colorAdjustments == null)
        {
            colorAdjustments = postProcessVolume.profile.Add<ColorAdjustments>();
            colorAdjustments.active = true;
        }

        
        if (filmGrain == null)
        {
            filmGrain = postProcessVolume.profile.Add<FilmGrain>();
            filmGrain.active = true;
            filmGrain.type.value = FilmGrainLookup.Thin2;
        }

        
        if (vignette == null)
        {
            vignette = postProcessVolume.profile.Add<Vignette>();
            vignette.active = true;
            vignette.color.value = Color.black;
        }

        
        if (liftGammaGain == null && enableColorDepthReduction)
        {
            liftGammaGain = postProcessVolume.profile.Add<LiftGammaGain>();
            liftGammaGain.active = true;
        }
    }

    private void Update()
    {
        if (!cameraFound) return;

        ApplyPSXEffects();

        if (enableVertexWobble)
        {
            ApplyVertexWobble();
        }
    }

    private void ApplyPSXEffects()
    {
        
        if (colorAdjustments != null)
        {
            colorAdjustments.contrast.value = (contrast - 1f) * 100f;
            colorAdjustments.saturation.value = -15f; 
        }

        
        if (filmGrain != null)
        {
            filmGrain.intensity.value = grainIntensity;
            filmGrain.response.value = 0.8f;
        }

        
        if (vignette != null)
        {
            vignette.intensity.value = vignetteIntensity;
            vignette.smoothness.value = 0.4f;
        }

        
        if (enableColorDepthReduction && liftGammaGain != null)
        {
            float reduction = Mathf.Clamp01(1f - (colorDepth / 256f));
            liftGammaGain.gamma.value = new Vector4(1f + reduction * 0.3f, 1f + reduction * 0.3f, 1f + reduction * 0.3f, 1f);
        }
    }

    private void ApplyVertexWobble()
    {
        if (mainCamera == null) return;

        
        float jitterX = Mathf.Sin(Time.time * 12f) * vertexPrecision;
        float jitterY = Mathf.Cos(Time.time * 10f) * vertexPrecision;
        float jitterZ = Mathf.Sin(Time.time * 8f) * vertexPrecision * 0.1f;

        
        mainCamera.transform.position = originalCameraPosition + new Vector3(jitterX, jitterY, jitterZ);
    }

    
    public void SetVerticalResolution(float value)
    {
        verticalResolution = Mathf.RoundToInt(value);
    }

    public void SetVertexPrecision(float value)
    {
        vertexPrecision = value;
    }

    public void SetColorDepth(float value)
    {
        colorDepth = Mathf.RoundToInt(value);
    }

    public void TogglePixelation(bool value)
    {
        enablePixelation = value;
    }

    public void ToggleVertexWobble(bool value)
    {
        enableVertexWobble = value;

        
        if (!value && mainCamera != null)
        {
            mainCamera.transform.position = originalCameraPosition;
        }
    }

    
    private void OnValidate()
    {
        if (mainCamera != null && !enableVertexWobble)
        {
            mainCamera.transform.position = originalCameraPosition;
        }
    }
}