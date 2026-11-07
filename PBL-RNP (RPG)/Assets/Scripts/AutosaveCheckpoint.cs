using UnityEngine;

public class AutosaveCheckpoint : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (SupabaseCloudSave.Instance != null)
            SupabaseCloudSave.Instance.RequestAutoSave();
    }
}