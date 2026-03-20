using UnityEngine;
using System;

public static class GameEvents
{
    
    public static Action<int> OnRoomChanged;
    public static Action<int, string> OnAnomalyChanged;

    
    public static Action<bool> OnDoorSelected;
    public static Action OnFlashlightPickedUp;
    public static Action OnNoteFound;
    public static Action<int, int> OnClockActivated;

    
    public static Action<float> OnSanityChanged;
    public static Action<CameraSanitySystem.SanityStage> OnSanityStageChanged;

    
    public static Action<string, VoiceGuideSystem.VoiceType> OnVoiceMessageRequested;

    public static void Initialize()
    {
        OnRoomChanged = null;
        OnDoorSelected = null;
        OnFlashlightPickedUp = null;
        OnNoteFound = null;
        OnClockActivated = null;
        OnSanityChanged = null;
        OnSanityStageChanged = null;
        OnVoiceMessageRequested = null;
        OnAnomalyChanged = null;
    }
}