using UnityEngine;

public class PlayerSpawnOnLoad : MonoBehaviour
{
    // Move the Player to the arrival marker after the scene loads.
    private void Start()
    {
        string spawnPointName = Redirector.TakeSpawnPointName();

        // No saved destination means this wasn't a doorway transition.
        if (string.IsNullOrWhiteSpace(spawnPointName))
            return;

        GameObject spawnPoint = GameObject.Find(spawnPointName);

        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point not found: " + spawnPointName);
            return;
        }

        transform.position = spawnPoint.transform.position;

        Rigidbody2D body = GetComponent<Rigidbody2D>();

        if (body != null)
            body.linearVelocity = Vector2.zero;
    }
}