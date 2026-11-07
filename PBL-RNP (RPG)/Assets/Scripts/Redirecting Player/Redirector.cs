using UnityEngine;
using UnityEngine.SceneManagement;

public class Redirector : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;
    [SerializeField] private string spawnPointName;
    [SerializeField] private string playerTag = "Player";

    private static string nextSpawnPointName;
    private bool isLoading;

    // When the Player enters this trigger, remember the arrival point
    // and load the destination scene.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isLoading || !other.CompareTag(playerTag))
            return;

        isLoading = true;
        nextSpawnPointName = spawnPointName;
        SceneManager.LoadScene(sceneToLoad);
    }

    // The Player uses this after the destination scene loads.
    public static string TakeSpawnPointName()
    {
        string result = nextSpawnPointName;
        nextSpawnPointName = null;
        return result;
    }

    // Keeps the existing method available for any Inspector button
    // that already calls it.
    public void GoToScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}