using UnityEngine;
using UnityEngine.Networking; // Sends HTTP requests to Supabase
using TMPro;
using System.Collections; // Provides interfaces and types for working with collections and iteration.
using System.Text.RegularExpressions;

public class Registration : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField usernameInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;

    [Header("Error Message")]
    public TMP_Text errorMessage;

    [Header("Supabase Settings")]
    public string supabaseUrl;
    public string supabasePublishableKey;

    // Represents the data sent to Supabase during registration.
    [System.Serializable]
    private class SignupRequest
    {
        public string email;
        public string password;
        public SignupMetadata data;
    }

    // Holds additional user information. for database display name and username.
    [System.Serializable]
    private class SignupMetadata
    {
        public string username;
        public string display_name;
    }

    public void Register()
    {
        // Get the values from the input fields
        string username = usernameInput.text.Trim();
        string email = emailInput.text.Trim();
        string password = passwordInput.text;
        string confirmPassword = confirmPasswordInput.text;

        // Clear previous error
        errorMessage.text = "";

        // Username validation
        if (username.Length < 3 || username.Length > 16)
        {
            errorMessage.text = "Username must be 3-16 characters.";
            return;
        }

        // Username can only contain letters, numbers, and underscore
        if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
        {
            errorMessage.text = "Username can only contain letters, numbers, and _.";
            return;
        }

        // Email validation
        if (string.IsNullOrWhiteSpace(email))
        {
            errorMessage.text = "Please enter your email.";
            return;
        }

        if (!IsValidEmail(email))
        {
            errorMessage.text = "Please enter a valid email address.";
            return;
        }

        // Password validation
        if (password.Length < 8)
        {
            errorMessage.text = "Password must be at least 8 characters.";
            return;
        }

        // Confirm password
        if (password != confirmPassword)
        {
            errorMessage.text = "Passwords do not match.";
            return;
        }

        // Check whether the Supabase settings are configured.
        if (string.IsNullOrWhiteSpace(supabaseUrl) ||
            string.IsNullOrWhiteSpace(supabasePublishableKey))
        {
            errorMessage.text = "Supabase settings are missing.";
            return;
        }

        // Send the validated information to Supabase.
        StartCoroutine(RegisterWithSupabase(username, email, password));
    }

    private IEnumerator RegisterWithSupabase(
    string username, string email, string password)
    {
        // Create the registration data.
        SignupRequest signupData = new SignupRequest
        {
            email = email,
            password = password,
            data = new SignupMetadata
            {
                username = username,
                display_name = username
            }
        };

        // Convert the registration data into JSON.
        string json = JsonUtility.ToJson(signupData);

        // Supabase Auth signup 
        string url = supabaseUrl.TrimEnd('/') + "/auth/v1/signup";

        // HTTP POST request.
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            // Attach the JSON body to the request.
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            // Tell Supabase that we are sending JSON.
            request.SetRequestHeader("Content-Type", "application/json");

            // Identify the project using its publishable key.
            request.SetRequestHeader("apikey", supabasePublishableKey);

            // Send the request and wait for Supabase's response.
            errorMessage.text = "Creating account...";

            yield return request.SendWebRequest();

            // Check whether the request succeeded.
            if (request.result == UnityWebRequest.Result.Success)
            {
                errorMessage.text = "Registration request succeeded! Check your email for verification.";

                Debug.Log("Supabase signup request succeeded.");
            }
            else
            {
                Debug.LogWarning(
                    "Signup failed. HTTP " +
                    request.responseCode + ": " +
                    request.downloadHandler.text);

                errorMessage.text = "Registration failed. Please check your details and try again.";
            }
        }
    }

    private bool IsValidEmail(string email)
    {
        return Regex.IsMatch(
            email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"
        );
    }
}