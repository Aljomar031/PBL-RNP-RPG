using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuParent; // Resume, Save/Load, Settings, etc.
    public GameObject saveLoadPanel;    // The copied panel
    public SaveSlotMenu saveSlotMenu;   // SaveSlotMenu on the copied panel

    public void Pause()
    {
        pauseMenuParent.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        pauseMenuParent.SetActive(false);
        Time.timeScale = 1f;
    }

    public void SaveNLoad()
    {
        pauseMenuParent.SetActive(false);
        saveLoadPanel.SetActive(true);
        saveSlotMenu.ResetSelection();

        if (SupabaseCloudSave.Instance != null)
            SupabaseCloudSave.Instance.RefreshSlots();
    }

    public void CloseSaveNLoad()
    {
        Resume();
        saveLoadPanel.SetActive(false);
        saveSlotMenu.ResetSelection();
    }

    public void Settings()
    {
        // No functionality yet.
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main_Menu");
    }
}