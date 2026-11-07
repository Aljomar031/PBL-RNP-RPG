using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSlotMenu : MonoBehaviour
{
    [Header("Assign slot buttons in order: 1 through 6")]
    [SerializeField] private Button[] slotButtons = new Button[6];

    [Header("Assign each slot's date TMP text in the same order")]
    [SerializeField] private TMP_Text[] slotDateTexts = new TMP_Text[6];

    [Header("The overlay's single Load button")]
    [SerializeField] private Button loadButton;

    [Header("Optional: assign for the in-game pause menu")]
    [SerializeField] private Button saveButton;

    private SaveDateFormatter dateFormatter;
    private readonly bool[] occupiedSlots = new bool[6];
    private int selectedSlotIndex = -1;

    private void Awake()
    {
        dateFormatter = GetComponent<SaveDateFormatter>();

        if (dateFormatter == null)
            Debug.LogError(
                "SaveDateFormatter must be on the same object as SaveSlotMenu.");

        if (saveButton != null)
            saveButton.onClick.AddListener(SaveSelectedSlot);

        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slotIndex = i;

            if (slotButtons[i] != null)
                slotButtons[i].onClick.AddListener(
                    () => SelectSlot(slotIndex));
        }

        if (loadButton != null)
            loadButton.onClick.AddListener(LoadSelectedSlot);

        UpdateActionButtons();
    }

    private void OnEnable()
    {
        // Refresh whenever this panel becomes active.
        if (SupabaseCloudSave.Instance != null)
            SupabaseCloudSave.Instance.RefreshSlots();
    }

    private void SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotButtons.Length)
            return;

        selectedSlotIndex = slotIndex;
        UpdateActionButtons();
    }

    private void LoadSelectedSlot()
    {
        if (SelectedSlotNumber == 0 ||
            !occupiedSlots[selectedSlotIndex])
            return;

        if (SupabaseCloudSave.Instance == null)
        {
            Debug.LogError("SupabaseCloudSave was not found.");
            return;
        }

        SupabaseCloudSave.Instance.LoadSlot(SelectedSlotNumber);
        Time.timeScale = 1f; // Resume the game if it was paused when the save/load menu was opened.
    }

    private void SaveSelectedSlot()
    {
        if (SelectedSlotNumber == 0)
            return;

        if (SupabaseCloudSave.Instance == null)
        {
            Debug.LogError("SupabaseCloudSave was not found.");
            return;
        }

        SupabaseCloudSave.Instance.SaveSlot(SelectedSlotNumber);
    }

    private void UpdateActionButtons()
    {
        bool hasSelection = SelectedSlotNumber != 0;
        bool selectedSlotIsOccupied =
            hasSelection && occupiedSlots[selectedSlotIndex];

        if (loadButton != null)
            loadButton.interactable = selectedSlotIsOccupied;

        if (saveButton != null)
            saveButton.interactable = hasSelection;
    }

    // Called by SupabaseCloudSave after it fetches the slot list.
    public void SetSlotData(
        int slotNumber,
        bool isOccupied,
        string savedAt)
    {
        int index = slotNumber - 1;

        if (index < 0 || index >= occupiedSlots.Length)
            return;

        occupiedSlots[index] = isOccupied;

        if (slotDateTexts != null &&
            index < slotDateTexts.Length &&
            slotDateTexts[index] != null)
        {
            if (!isOccupied)
            {
                slotDateTexts[index].text = "Empty";
            }
            else if (dateFormatter != null)
            {
                slotDateTexts[index].text =
                    dateFormatter.FormatDate(savedAt);
            }
            else
            {
                slotDateTexts[index].text = savedAt;
            }
        }

        UpdateActionButtons();
    }

    public void ResetSelection()
    {
        selectedSlotIndex = -1;
        UpdateActionButtons();
    }

    public int SelectedSlotNumber =>
        selectedSlotIndex < 0 ? 0 : selectedSlotIndex + 1;
}