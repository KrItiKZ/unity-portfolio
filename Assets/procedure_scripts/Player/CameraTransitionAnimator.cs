using System.Collections;
using UnityEngine;

public class CameraTransitionAnimator : MonoBehaviour
{
    [Header("Enter Room Animation")]
    public AnimationCurve enterRoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float enterRoomDuration = 1.5f;
    public float enterRoomDistance = 2.5f;
    public float bobFrequency = 2f;
    public float bobAmount = 0.1f;

    private PlayerController playerController;
    private Transform cameraTransform;

    private void Start()
    {
        playerController = GetComponent<PlayerController>();
        cameraTransform = playerController.transform;
    }

    public IEnumerator PlayEnterRoomAnimation()
    {
        
        Vector3 targetLocalPosition = playerController.originalCameraLocalPosition;
        Quaternion targetLocalRotation = playerController.originalCameraLocalRotation;

        
        Vector3 startLocalPosition = targetLocalPosition + Vector3.back * enterRoomDistance;
        Quaternion startLocalRotation = targetLocalRotation;

        float elapsedTime = 0f;

        while (elapsedTime < enterRoomDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = enterRoomCurve.Evaluate(elapsedTime / enterRoomDuration);

            
            Vector3 currentLocalPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, t);

            
            float bob = Mathf.Sin(elapsedTime * bobFrequency) * bobAmount * (1f - t);
            currentLocalPosition.y += bob;

            
            cameraTransform.localPosition = currentLocalPosition;
            cameraTransform.localRotation = Quaternion.Slerp(startLocalRotation, targetLocalRotation, t);

            yield return null;
        }

        
        cameraTransform.localPosition = targetLocalPosition;
        cameraTransform.localRotation = targetLocalRotation;
    }

    
    public IEnumerator PlayEnterRoomAnimationSimple()
    {
        Vector3 targetLocalPosition = playerController.originalCameraLocalPosition;
        Quaternion targetLocalRotation = playerController.originalCameraLocalRotation;

        
        Vector3 startLocalPosition = new Vector3(
            targetLocalPosition.x,
            targetLocalPosition.y,
            targetLocalPosition.z - enterRoomDistance
        );

        float elapsedTime = 0f;

        while (elapsedTime < enterRoomDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = enterRoomCurve.Evaluate(elapsedTime / enterRoomDuration);

            
            float currentZ = Mathf.Lerp(startLocalPosition.z, targetLocalPosition.z, t);
            Vector3 currentLocalPosition = new Vector3(
                targetLocalPosition.x,
                targetLocalPosition.y,
                currentZ
            );

            cameraTransform.localPosition = currentLocalPosition;

            yield return null;
        }

        cameraTransform.localPosition = targetLocalPosition;
        cameraTransform.localRotation = targetLocalRotation;
    }

    public void ResetCameraImmediately()
    {
        if (playerController != null)
        {
            cameraTransform.localPosition = playerController.originalCameraLocalPosition;
            cameraTransform.localRotation = playerController.originalCameraLocalRotation;
        }
    }
}