using UnityEngine;
using System.Collections;

public class DoorSoundTrigger : MonoBehaviour
{
    public Door door;
    public float triggerDelay = 2f;
    public float fadeOutDuration = 1f;

    private AudioSource audioSource;
    private Coroutine soundCoroutine;
    private bool isPlayerInTrigger = false;
    private bool isEnabled = true;
    private Transform playerTransform;
    private float originalVolume;

    private void Start()
    {
        if (door != null)
        {
            audioSource = door.AudioSource;
            if (audioSource != null)
            {
                originalVolume = audioSource.volume;
            }
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isEnabled || door == null || door.doorOpened) return;

        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = true;

            if (soundCoroutine != null)
                StopCoroutine(soundCoroutine);
            soundCoroutine = StartCoroutine(PlayDoorSound());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false;

            if (soundCoroutine != null)
                StopCoroutine(soundCoroutine);
            soundCoroutine = StartCoroutine(FadeOutSound());
        }
    }

    private IEnumerator PlayDoorSound()
    {
        yield return new WaitForSeconds(triggerDelay);

        if (!isEnabled || !isPlayerInTrigger || door == null || door.doorOpened)
        {
            yield break;
        }

        door.PlayDoorSound();

        yield return new WaitForSeconds(0.1f);

        if (audioSource == null || !audioSource.isPlaying)
        {
            yield break;
        }

        float fadeInTimer = 0f;
        while (fadeInTimer < fadeOutDuration && isPlayerInTrigger && audioSource != null && audioSource.isPlaying)
        {
            fadeInTimer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, originalVolume, fadeInTimer / fadeOutDuration);
            yield return null;
        }

        if (audioSource != null)
        {
            audioSource.volume = originalVolume;
        }

        while (isPlayerInTrigger && audioSource != null && audioSource.isPlaying)
        {
            yield return null;
        }
    }

    private IEnumerator FadeOutSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            float startVolume = audioSource.volume;
            float timer = 0f;

            while (timer < fadeOutDuration && audioSource != null && audioSource.isPlaying)
            {
                timer += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeOutDuration);
                yield return null;
            }

            if (audioSource != null)
            {
                audioSource.volume = 0f;
                audioSource.Stop();
            }
        }
    }

    public void DisableTrigger()
    {
        isEnabled = false;
        isPlayerInTrigger = false;

        if (soundCoroutine != null)
        {
            StopCoroutine(soundCoroutine);
            soundCoroutine = null;
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            audioSource.volume = originalVolume;
        }
    }

    public void EnableTrigger()
    {
        isEnabled = true;

        if (audioSource != null)
        {
            audioSource.volume = originalVolume;
        }
    }
}