using UnityEngine;

public class DoorAnimator : MonoBehaviour
{
    public Animator doorAnimator;
    public string openTriggerName = "Open";
    public string closeTriggerName = "Close";


    public void PlayOpenAnimation()
    {
        if (doorAnimator != null && doorAnimator.isActiveAndEnabled)
        {
            doorAnimator.ResetTrigger(openTriggerName);
            doorAnimator.ResetTrigger(closeTriggerName);

            doorAnimator.SetTrigger(openTriggerName);
        }
    }

    public void ResetToClosedState()
    {
        if (doorAnimator != null)
        {
            doorAnimator.ResetTrigger(openTriggerName);
            doorAnimator.ResetTrigger(closeTriggerName);

            doorAnimator.Rebind();
        }
    }

    public void ForceClose()
    {
        if (doorAnimator != null)
        {
            doorAnimator.ResetTrigger(openTriggerName);
            doorAnimator.ResetTrigger(closeTriggerName);

            doorAnimator.SetTrigger(closeTriggerName);

            doorAnimator.Update(0f);
        }
    }
}
