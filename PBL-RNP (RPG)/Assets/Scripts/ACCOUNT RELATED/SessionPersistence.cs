using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class SessionPersistence : MonoBehaviour
{
    public static SessionPersistence Instance { get; private set; }

    [SerializeField] private Login loginSettings;
    private string supabaseUrl;
    private string supabasePublishableKey;
    [SerializeField] private string loginSceneName = "Login";
    [SerializeField] private string mainMenuSceneName = "Main_Menu";

    private bool busy;

    [Serializable]
    private class RefreshRequest
    {
        public string refresh_token;
    }

    [Serializable]
    private class UserMetadata
    {
        public string username;
    }

    [Serializable]
    private class AuthUser
    {
        public string id;
        public string email;
        public UserMetadata user_metadata;
    }

    [Serializable]
    private class AuthResponse
    {
        public string access_token;
        public string refresh_token;
        public AuthUser user;
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

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(RestoreSession());
#endif
    }

    public void AcceptLogin(
        string userId,
        string username,
        string accessToken,
        string refreshToken)
    {
        AuthSession.UserId = userId;
        AuthSession.Username = username;
        AuthSession.AccessToken = accessToken;
        AuthSession.RefreshToken = refreshToken;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var store = new AndroidJavaClass(
                "com.pblrnp.securestore.SecureTokenStore"))
            {
                store.CallStatic("put", refreshToken);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Could not securely save the login session: " + e.Message);
        }
#endif
    }

    private IEnumerator RestoreSession()
    {
        if (busy) yield break;
        busy = true;

        string refreshToken = ReadRefreshToken();
        if (string.IsNullOrEmpty(refreshToken))
        {
            busy = false;
            yield break;
        }

        var body = new RefreshRequest { refresh_token = refreshToken };
        byte[] bodyBytes =
            Encoding.UTF8.GetBytes(JsonUtility.ToJson(body));

        using (var request = new UnityWebRequest(
            supabaseUrl.TrimEnd('/') +
                "/auth/v1/token?grant_type=refresh_token",
            "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader(
                "Content-Type", "application/json");
            request.SetRequestHeader(
                "apikey", supabasePublishableKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AuthResponse response =
                    JsonUtility.FromJson<AuthResponse>(
                        request.downloadHandler.text);

                if (response != null &&
                    response.user != null &&
                    !string.IsNullOrEmpty(response.access_token) &&
                    !string.IsNullOrEmpty(response.refresh_token))
                {
                    string username =
                        response.user.user_metadata != null
                            ? response.user.user_metadata.username
                            : null;

                    if (string.IsNullOrEmpty(username))
                        username = response.user.email;

                    AcceptLogin(
                        response.user.id,
                        username,
                        response.access_token,
                        response.refresh_token);

                    SceneManager.LoadScene(mainMenuSceneName);
                }
                else
                {
                    ClearLocalSession();
                }
            }
            else if (request.responseCode == 400 ||
                     request.responseCode == 401)
            {
                // Supabase rejected the saved session. Require login again.
                ClearLocalSession();
            }
            // Keep the saved token on network/server errors so startup can retry.
        }

        busy = false;
    }

    // Connect the Main Menu Logout button to this method.
    public void LogOut()
    {
        if (!busy)
            StartCoroutine(LogOutRoutine());
    }

    private IEnumerator LogOutRoutine()
    {
        busy = true;

        if (!string.IsNullOrEmpty(AuthSession.AccessToken))
        {
            using (var request = new UnityWebRequest(
                supabaseUrl.TrimEnd('/') +
                    "/auth/v1/logout?scope=local",
                "POST"))
            {
                request.downloadHandler =
                    new DownloadHandlerBuffer();

                request.SetRequestHeader(
                    "apikey", supabasePublishableKey);
                request.SetRequestHeader(
                    "Authorization",
                    "Bearer " + AuthSession.AccessToken);

                yield return request.SendWebRequest();
            }
        }

        // Clear this device's session even if it is offline.
        ClearLocalSession();

        busy = false;
        SceneManager.LoadScene(loginSceneName);
    }

    private string ReadRefreshToken()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var store = new AndroidJavaClass(
                "com.pblrnp.securestore.SecureTokenStore"))
            {
                return store.CallStatic<string>("get");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "Could not read the saved login session: " + e.Message);
        }
#endif
        return null;
    }

    private void ClearLocalSession()
    {
        AuthSession.UserId = null;
        AuthSession.Username = null;
        AuthSession.AccessToken = null;
        AuthSession.RefreshToken = null;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var store = new AndroidJavaClass(
                "com.pblrnp.securestore.SecureTokenStore"))
            {
                store.CallStatic("clear");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "Could not clear saved login session: " + e.Message);
        }
#endif
    }
}