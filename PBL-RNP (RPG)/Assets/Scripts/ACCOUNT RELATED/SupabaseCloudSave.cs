using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class SupabaseCloudSave : MonoBehaviour
{
    public static SupabaseCloudSave Instance { get; private set; }

    [Header("Supabase")]
    [SerializeField] private Login loginSettings;
    private string supabaseUrl;
    private string supabasePublishableKey;
    [SerializeField] private string tableName = "game_saves";

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    [Header("Autosave")]
    [SerializeField] private float autoSaveDebounceSeconds = 1f;

    private Coroutine autoSaveCoroutine;
    private bool suppressSceneAutoSave;
    private bool busy;

    [Serializable]
    private class SaveRow
    {
        public string user_id;
        public int slot_number;
        public string saved_at;
        public string scene_name;
        public int save_version;
        public GameSaveData save_data;
    }

    [Serializable]
    private class SaveRowArray
    {
        public SaveRow[] items;
    }

    [Serializable]
    private class SlotRow
    {
        public int slot_number;
        public string saved_at;
    }

    [Serializable]
    private class SlotRowArray
    {
        public SlotRow[] items;
    }

    private void Awake()
    {
        if (loginSettings == null)
        {
            Debug.LogError("Assign the Login component to Login Settings.");
            return;
        }

        supabaseUrl = loginSettings.supabaseUrl;
        supabasePublishableKey = loginSettings.supabasePublishableKey;
        loginSettings = null; // The manager no longer needs the scene's Login object.

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public void RefreshSlots()
    {
        if (!busy)
            StartCoroutine(RefreshSlotsRoutine());
    }

    public void SaveSlot(int slotNumber)
    {
        if (!busy)
            StartCoroutine(SaveSlotRoutine(slotNumber));
    }

    public void LoadSlot(int slotNumber)
    {
        if (!busy)
            StartCoroutine(LoadSlotRoutine(slotNumber));
    }

    private bool HasSession()
    {
        if (string.IsNullOrWhiteSpace(supabaseUrl) ||
            string.IsNullOrWhiteSpace(supabasePublishableKey))
        {
            Debug.LogError("Supabase URL or publishable key is missing.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(AuthSession.UserId) ||
            string.IsNullOrWhiteSpace(AuthSession.AccessToken))
        {
            Debug.LogError("There is no logged-in Supabase session.");
            return false;
        }

        return true;
    }

    private string TableUrl =>
        supabaseUrl.TrimEnd('/') + "/rest/v1/" + tableName;

    private void AddHeaders(UnityWebRequest request)
    {
        request.SetRequestHeader("apikey", supabasePublishableKey);
        request.SetRequestHeader(
            "Authorization",
            "Bearer " + AuthSession.AccessToken);
        request.SetRequestHeader("Content-Type", "application/json");
    }

    private IEnumerator RefreshSlotsRoutine()
    {
        if (!HasSession())
            yield break;

        busy = true;

        string url = TableUrl +
            "?select=slot_number,saved_at&user_id=eq." +
            Uri.EscapeDataString(AuthSession.UserId);

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            AddHeaders(request);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "Could not get save slots. HTTP " +
                    request.responseCode + ": " +
                    request.downloadHandler.text);

                busy = false;
                yield break;
            }

            string wrappedJson =
                "{\"items\":" + request.downloadHandler.text + "}";

            SlotRowArray response =
                JsonUtility.FromJson<SlotRowArray>(wrappedJson);

            SaveSlotMenu menu = FindFirstObjectByType<SaveSlotMenu>();

            if (menu != null)
            {
                for (int slot = 1; slot <= 6; slot++)
                    menu.SetSlotData(slot, false, null);

                if (response != null && response.items != null)
                {
                    foreach (SlotRow row in response.items)
                    {
                        if (row != null)
                            menu.SetSlotData(
                                row.slot_number,
                                true,
                                row.saved_at);
                    }
                }
            }
            else
            {
                Debug.LogWarning(
                    "No active SaveSlotMenu was found to display the slots.");
            }
        }

        busy = false;
    }

    private IEnumerator SaveSlotRoutine(int slotNumber)
    {
        if (slotNumber < 1 || slotNumber > 6)
        {
            Debug.LogError("Slot number must be from 1 to 6.");
            yield break;
        }

        if (!HasSession())
            yield break;

        if (QuestManager.Instance == null)
        {
            Debug.LogError("QuestManager was not found.");
            yield break;
        }

        GameObject player;

        try
        {
            player = GameObject.FindGameObjectWithTag(playerTag);
        }
        catch (UnityException)
        {
            Debug.LogError(
                "The Player tag does not exist. Add it in Unity's Tags settings.");
            yield break;
        }

        if (player == null)
        {
            Debug.LogError(
                "Could not find a GameObject with tag: " + playerTag);
            yield break;
        }

        busy = true;
        bool saveSucceeded = false;

        Vector3 position = player.transform.position;

        GameSaveData data = new GameSaveData
        {
            version = 1,
            sceneName = SceneManager.GetActiveScene().name,
            playerPosition = new PlayerPositionSaveData
            {
                x = position.x,
                y = position.y,
                z = position.z
            },
            quests = QuestManager.Instance.ExportSaveData()
        };

        SaveRow row = new SaveRow
        {
            user_id = AuthSession.UserId,
            slot_number = slotNumber,
            saved_at = DateTimeOffset.UtcNow.ToString("o"),
            scene_name = data.sceneName,
            save_version = data.version,
            save_data = data
        };

        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(row));
        string url = TableUrl + "?on_conflict=user_id,slot_number";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            AddHeaders(request);
            request.SetRequestHeader(
                "Prefer",
                "resolution=merge-duplicates,return=minimal");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Saved to slot " + slotNumber + ".");
                saveSucceeded = true;
            }
            else
            {
                Debug.LogError(
                    "Save failed. HTTP " + request.responseCode +
                    ": " + request.downloadHandler.text);
            }
        }

        busy = false;

        // Fetch the saved timestamp and update the currently visible menu.
        if (saveSucceeded)
            yield return RefreshSlotsRoutine();
    }

    private IEnumerator LoadSlotRoutine(int slotNumber)
    {
        if (slotNumber < 1 || slotNumber > 6)
        {
            Debug.LogError("Slot number must be from 1 to 6.");
            yield break;
        }

        if (!HasSession())
            yield break;

        busy = true;

        string url = TableUrl +
            "?select=scene_name,save_version,save_data" +
            "&user_id=eq." +
            Uri.EscapeDataString(AuthSession.UserId) +
            "&slot_number=eq." + slotNumber +
            "&limit=1";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            AddHeaders(request);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "Load failed. HTTP " + request.responseCode +
                    ": " + request.downloadHandler.text);

                busy = false;
                yield break;
            }

            string wrappedJson =
                "{\"items\":" + request.downloadHandler.text + "}";

            SaveRowArray response =
                JsonUtility.FromJson<SaveRowArray>(wrappedJson);

            if (response == null ||
                response.items == null ||
                response.items.Length == 0 ||
                response.items[0].save_data == null)
            {
                Debug.LogWarning("That slot has no save data.");
                busy = false;
                yield break;
            }

            SaveRow loadedRow = response.items[0];

            if (loadedRow.save_version != 1 ||
                loadedRow.save_data.version != 1)
            {
                Debug.LogError("This save uses an unsupported version.");
                busy = false;
                yield break;
            }

            if (string.IsNullOrWhiteSpace(loadedRow.scene_name))
            {
                Debug.LogError("The save does not contain a scene name.");
                busy = false;
                yield break;
            }

            suppressSceneAutoSave = true;

            AsyncOperation operation =
                SceneManager.LoadSceneAsync(loadedRow.scene_name);

            if (operation == null)
            {
                Debug.LogError(
                    "Could not load scene: " + loadedRow.scene_name);

                suppressSceneAutoSave = false;
                busy = false;
                yield break;
            }

            yield return operation;
            yield return null;

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.ImportSaveData(
                    loadedRow.save_data.quests);
            }

            if (loadedRow.save_data.playerPosition != null)
            {
                GameObject loadedPlayer =
                    GameObject.FindGameObjectWithTag(playerTag);

                if (loadedPlayer != null)
                {
                    PlayerPositionSaveData savedPosition =
                        loadedRow.save_data.playerPosition;

                    loadedPlayer.transform.position = new Vector3(
                        savedPosition.x,
                        savedPosition.y,
                        savedPosition.z);

                    Physics2D.SyncTransforms();
                }
                else
                {
                    Debug.LogError(
                        "Loaded the scene but couldn't find the Player-tagged object.");
                }
            }

            QuestItem[] questItems =
                FindObjectsByType<QuestItem>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (QuestItem item in questItems)
            {
                if (item != null && item.gameObject.scene.IsValid())
                    item.RefreshCollectedState();
            }

            suppressSceneAutoSave = false;
        }

        busy = false;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (suppressSceneAutoSave)
            return;

        // If the scene is a menu or login/register/settings, we don't want to auto-save.
        if (scene.name == "Main_Menu" ||
            scene.name == "Login" ||
            scene.name == "Register" ||
            scene.name == "Settings")
        {
            if (autoSaveCoroutine != null)
            {
                StopCoroutine(autoSaveCoroutine);
                autoSaveCoroutine = null;
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(AuthSession.UserId) ||
            string.IsNullOrWhiteSpace(AuthSession.AccessToken))
        {
            return;
        }

        RequestAutoSave();
    }

    public void RequestAutoSave()
    {
        if (suppressSceneAutoSave)
            return;

        if (autoSaveCoroutine != null)
            StopCoroutine(autoSaveCoroutine);

        autoSaveCoroutine = StartCoroutine(AutoSaveAfterDelay());
    }

    private IEnumerator AutoSaveAfterDelay()
    {
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, autoSaveDebounceSeconds));

        while (busy)
            yield return null;

        autoSaveCoroutine = null;
        yield return SaveSlotRoutine(1);
    }
}