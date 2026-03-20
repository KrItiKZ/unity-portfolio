using UnityEngine;

public class FakePassageTrigger : MonoBehaviour
{
    private bool passed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (passed || !other.CompareTag("Player")) return;
        passed = true;

        Debug.Log("✅ Игрок прошёл сквозь фейк — теперь в настоящей комнате");
        
    }
}