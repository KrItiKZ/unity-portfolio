using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraSanitySystem : MonoBehaviour
{
    public static CameraSanitySystem Instance;

    [Header("Main References")]
    public Camera playerCamera;
    public PSXEffectController psxEffect;

    [Header("Sanity Settings")]
    public float maxSanity = 100f;
    public float currentSanity = 100f;
    public float sanityLossPerMistake = 15f;
    public float passiveSanityRecovery = 10f;

    [Header("Sanity Stages")]
    public float stage1Threshold = 70f;
    public float stage2Threshold = 40f;
    public float stage3Threshold = 20f;

    [Header("Visual Effects - ���������")]
    public Volume postProcessVolume;
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;
    private FilmGrain filmGrain;
    private ColorAdjustments colorAdjustments;
    private WhiteBalance whiteBalance;

    [Header("Effect Intensities - ���������")]
    [Range(0f, 0.6f)] public float maxVignette = 0.5f;
    [Range(0f, 0.5f)] public float maxChromatic = 0.4f;
    [Range(0f, 0.8f)] public float maxGrain = 0.7f;
    [Range(-80f, 0f)] public float maxSaturation = -60f;
    [Range(-100f, 100f)] public float maxTint = 40f;

    private int consecutiveMistakes = 0;
    private SanityStage currentStage = SanityStage.Normal;
    private Coroutine colorShiftCoroutine;

    public enum SanityStage
    {
        Normal,
        Stage1,
        Stage2,
        Stage3
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Start()
    {
        if (psxEffect == null)
            psxEffect = FindAnyObjectByType<PSXEffectController>();

        SetupPostProcessing();
        UpdateSanityEffects();
    }

    private void SetupPostProcessing()
    {
        if (postProcessVolume == null)
        {
            postProcessVolume = FindAnyObjectByType<Volume>();
        }

        if (postProcessVolume?.profile != null)
        {
            postProcessVolume.profile.TryGet(out vignette);
            postProcessVolume.profile.TryGet(out chromaticAberration);
            postProcessVolume.profile.TryGet(out filmGrain);
            postProcessVolume.profile.TryGet(out colorAdjustments);
            postProcessVolume.profile.TryGet(out whiteBalance);
        }
    }

    public void OnWrongDoorSelected()
    {
        consecutiveMistakes++;
        currentSanity -= sanityLossPerMistake;

        if (consecutiveMistakes > 1)
        {
            currentSanity -= consecutiveMistakes * 5f;
        }

        currentSanity = Mathf.Max(0f, currentSanity);
        TriggerMistakeEffect();
        UpdateSanityStage();
    }

    public void OnCorrectDoorSelected()
    {
        consecutiveMistakes = 0;
        currentSanity = Mathf.Min(maxSanity, currentSanity + passiveSanityRecovery);
        UpdateSanityStage();
    }

    private void TriggerMistakeEffect()
    {
        StartCoroutine(ErrorFlash());
    }

    private IEnumerator ErrorFlash()
    {
        if (chromaticAberration != null)
        {
            float originalChromatic = chromaticAberration.intensity.value;
            chromaticAberration.intensity.value = 0.6f;

            yield return new WaitForSeconds(0.4f);

            chromaticAberration.intensity.value = originalChromatic;
        }
    }

    private void UpdateSanityStage()
    {
        SanityStage newStage = GetSanityStage();

        if (newStage != currentStage)
        {
            OnSanityStageChanged(newStage);
            currentStage = newStage;
        }

        UpdateSanityEffects();
    }

    private SanityStage GetSanityStage()
    {
        if (currentSanity >= stage1Threshold) return SanityStage.Normal;
        if (currentSanity >= stage2Threshold) return SanityStage.Stage1;
        if (currentSanity >= stage3Threshold) return SanityStage.Stage2;
        return SanityStage.Stage3;
    }

    private void OnSanityStageChanged(SanityStage newStage)
    {
        if (colorShiftCoroutine != null)
            StopCoroutine(colorShiftCoroutine);

        switch (newStage)
        {
            case SanityStage.Stage1:
                colorShiftCoroutine = StartCoroutine(RandomColorShifts(10f, 20f));
                break;
            case SanityStage.Stage2:
                colorShiftCoroutine = StartCoroutine(IntenseColorEffects());
                break;
            case SanityStage.Stage3:
                colorShiftCoroutine = StartCoroutine(CriticalColorEffects());
                break;
        }
    }

    private void UpdateSanityEffects()
    {
        float sanityRatio = currentSanity / maxSanity;
        float effectIntensity = 1f - sanityRatio;

        
        if (vignette != null && vignette.active)
        {
            vignette.intensity.value = Mathf.Lerp(0f, maxVignette, effectIntensity);
            vignette.color.value = Color.Lerp(Color.black, new Color(0.6f, 0f, 0.4f, 1f), effectIntensity);
        }

        if (chromaticAberration != null && chromaticAberration.active)
        {
            chromaticAberration.intensity.value = Mathf.Lerp(0f, maxChromatic, effectIntensity);
        }

        if (filmGrain != null && filmGrain.active)
        {
            filmGrain.intensity.value = Mathf.Lerp(0f, maxGrain, effectIntensity);
        }

        if (colorAdjustments != null && colorAdjustments.active)
        {
            colorAdjustments.saturation.value = Mathf.Lerp(0f, maxSaturation, effectIntensity);
            colorAdjustments.contrast.value = Mathf.Lerp(0f, 30f, effectIntensity);
        }

        if (whiteBalance != null && whiteBalance.active)
        {
            whiteBalance.tint.value = Mathf.Lerp(0f, maxTint, effectIntensity);
        }

        if (psxEffect != null)
        {
            psxEffect.contrast = Mathf.Lerp(1.0f, 1.8f, effectIntensity);
            psxEffect.grainIntensity = Mathf.Lerp(0.2f, 1.0f, effectIntensity);
            psxEffect.vignetteIntensity = Mathf.Lerp(0.2f, 0.8f, effectIntensity);
        }
    }

    private IEnumerator RandomColorShifts(float minDelay, float maxDelay)
    {
        while (currentStage >= SanityStage.Stage1)
        {
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
            StartCoroutine(ColorPulse());
        }
    }

    private IEnumerator ColorPulse()
    {
        if (colorAdjustments != null)
        {
            float originalSaturation = colorAdjustments.saturation.value;
            colorAdjustments.saturation.value = -80f;

            yield return new WaitForSeconds(0.8f);

            colorAdjustments.saturation.value = originalSaturation;
        }
    }

    private IEnumerator IntenseColorEffects()
    {
        while (currentStage >= SanityStage.Stage2)
        {
            if (colorAdjustments != null)
            {
                float pulse = Mathf.Sin(Time.time * 3f) * 15f;
                colorAdjustments.saturation.value = maxSaturation + pulse;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator CriticalColorEffects()
    {
        while (currentStage == SanityStage.Stage3)
        {
            yield return new WaitForSeconds(Random.Range(2f, 5f));

            if (Random.value > 0.2f)
            {
                StartCoroutine(IntenseColorFlash());
            }
        }
    }

    private IEnumerator IntenseColorFlash()
    {
        if (chromaticAberration != null)
        {
            float originalChromatic = chromaticAberration.intensity.value;
            chromaticAberration.intensity.value = 0.8f;

            if (colorAdjustments != null)
            {
                float originalSaturation = colorAdjustments.saturation.value;
                colorAdjustments.saturation.value = -100f;

                yield return new WaitForSeconds(1.2f);

                colorAdjustments.saturation.value = originalSaturation;
            }

            chromaticAberration.intensity.value = originalChromatic;
        }
    }

    public void ResetSanity()
    {
        currentSanity = maxSanity;
        consecutiveMistakes = 0;

        if (colorShiftCoroutine != null)
            StopCoroutine(colorShiftCoroutine);

        UpdateSanityStage();
    }

    private void OnDestroy()
    {
        
        if (colorShiftCoroutine != null)
        {
            StopCoroutine(colorShiftCoroutine);
            colorShiftCoroutine = null;
        }
    }

}