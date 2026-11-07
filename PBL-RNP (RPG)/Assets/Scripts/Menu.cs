
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Menu : MonoBehaviour
{
    public GameObject menuButtons;
    public GameObject exitConfirmationPanel;
    public GameObject startConfirmationPanel;
    public GameObject loadSavePanel;
    private SaveSlotMenu saveSlotMenu;

    private void Awake()
    {
        saveSlotMenu = GetComponent<SaveSlotMenu>();
    }

    [Header("Welcome Message")]
    public TMP_Text welcomeText;

    private void Start()
    {

        // Display the logged-in user's username.
        if (welcomeText != null)
        {
            string username = AuthSession.Username;

            if (string.IsNullOrWhiteSpace(username))
                username = "Player";

            welcomeText.text = "Welcome, " + username + "!";
        }
    }

    public void BtnStart() // New game
    {
        // Clear progress left in the persistent QuestManager, if one exists.
        if (QuestManager.Instance != null)
            QuestManager.Instance.ResetProgress();

        SceneManager.LoadScene("SampleScene");
    }

    // Start confirmation panel methods for new game
    public void ShowStartConfirmantion()
    {
        menuButtons.SetActive(false);
        startConfirmationPanel.SetActive(true);
    }

    public void HideStartConfirmantion()
    {
        menuButtons.SetActive(true);
        startConfirmationPanel.SetActive(false);
    }

    public void BtnContinue() // Continue game
    {
        if (SupabaseCloudSave.Instance == null)
        {
            Debug.LogError("SupabaseCloudSave was not found.");
            return;
        }

        SupabaseCloudSave.Instance.LoadSlot(1);
    }

    // Load save panel methods
    public void BtnLoadSave() // Load save
    {
        saveSlotMenu.ResetSelection();
        menuButtons.SetActive(false);
        loadSavePanel.SetActive(true);

        // Refresh the save slots when the load save panel is opened
        if (SupabaseCloudSave.Instance != null)
            SupabaseCloudSave.Instance.RefreshSlots();
    }

    public void BtnCloseLoadSave() // Close Load save
    {
        saveSlotMenu.ResetSelection();
        menuButtons.SetActive(true);
        loadSavePanel.SetActive(false);
    }

    public void BtnSettings() // Settings
    {
        SceneManager.LoadScene("Settings");
    }

    public void BtnBackToMain()
    {
        SceneManager.LoadScene("Main_Menu");
    }

    // Exit confirmation panel methods
    public void BtnShowExitConfirmation()
    {
        menuButtons.SetActive(false);
        exitConfirmationPanel.SetActive(true);
    }

    public void BtnHideExitConfirmation()
    {
        menuButtons.SetActive(true);
        exitConfirmationPanel.SetActive(false);
    }
    public void BtnExit()
    {
        Application.Quit();
    }

    public void AccountSwitch()
    {
        if (SessionPersistence.Instance != null)
        {
            SessionPersistence.Instance.LogOut();
        }
        else
        {
            Debug.LogError("SessionPersistence.Instance was not found.");
        }
    }
}
