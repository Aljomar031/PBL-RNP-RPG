using UnityEngine;

public class QuestItem : InteractableBase
{
    [Header("Quest Item Information")]
    [SerializeField] private string questID;
    [SerializeField] private string itemID;

    private void Start()
    {
        RefreshCollectedState();
    }

    public override void Interact()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("QuestManager was not found.");
            return;
        }

        bool collected = QuestManager.Instance.CollectQuestItem(
            questID,
            itemID
        );

        if (collected) // If the item was successfully collected, deactivate it
        {
            gameObject.SetActive(false);
        }
    }

    public void RefreshCollectedState() // Check if the item has already been collected and deactivate it if so
    {
        if (QuestManager.Instance == null)
            return;

        if (QuestManager.Instance.HasCollectedQuestItem(questID, itemID))
            gameObject.SetActive(false);
    }
}