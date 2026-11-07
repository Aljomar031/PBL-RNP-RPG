using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Text;

public class Login : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    [Header("Error Message")]
    public TMP_Text errorMessage;

    [Header("Supabase Settings")]
    public string supabaseUrl;
    public string supabasePublishableKey;

    public void LogIn()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        errorMessage.text = "";

        if (string.IsNullOrWhiteSpace(email))
        {
            errorMessage.text = "Please enter your email.";
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            errorMessage.text = "Please enter your password.";
            return;
        }

        if (string.IsNullOrWhiteSpace(supabaseUrl) ||
            string.IsNullOrWhiteSpace(supabasePublishableKey))
        {
            errorMessage.text = "Supabase settings are missing.";
            return;
        }

        StartCoroutine(LoginWithSupabase(email, password));
    }

    private IEnumerator LoginWithSupabase(string email, string password)
    {
        string url = supabaseUrl.TrimEnd('/') +
                     "/auth/v1/token?grant_type=password";

        LoginRequest loginData = new LoginRequest
        {
            email = email,
            password = password
        };

        string json = JsonUtility.ToJson(loginData);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", supabasePublishableKey);

            errorMessage.text = "Logging in...";

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                LoginResponse response =
                    JsonUtility.FromJson<LoginResponse>(
                        request.downloadHandler.text);

                if (response == null || response.user == null)
                {
                    errorMessage.text =
                        "Could not read login information.";
                    yield break;
                }

                if (SessionPersistence.Instance == null)
                {
                    errorMessage.text =
                        "Session service is unavailable.";
                    yield break;
                }

                string username =
                    response.user.user_metadata != null &&
                    !string.IsNullOrWhiteSpace(
                        response.user.user_metadata.username)
                        ? response.user.user_metadata.username
                        : response.user.email;

                SessionPersistence.Instance.AcceptLogin(
                    response.user.id,
                    username,
                    response.access_token,
                    response.refresh_token);

                Debug.Log("Login succeeded for " + username);
                errorMessage.text = "Login successful!";

                SceneManager.LoadScene("Main_Menu");
            }
            else
            {
                Debug.LogWarning(
                    "Login failed. HTTP " +
                    request.responseCode + ": " +
                    request.downloadHandler.text);

                errorMessage.text =
                    "Login failed. Check your email and password.";
            }
        }
    }

    [System.Serializable]
    private class LoginRequest
    {
        public string email;
        public string password;
    }

    [System.Serializable]
    private class LoginResponse
    {
        public string access_token;
        public string refresh_token;
        public LoginUser user;
    }

    [System.Serializable]
    private class LoginUser
    {
        public string id;
        public LoginMetadata user_metadata;
        public string email;
    }

    [System.Serializable]
    private class LoginMetadata
    {
        public string username;
    }
}