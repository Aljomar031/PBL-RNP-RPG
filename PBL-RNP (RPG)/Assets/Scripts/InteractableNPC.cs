
using UnityEngine;

public enum RequiredQuestStatus
{
    Any,
    NotStarted,
    Active,
    Completed
}

public enum RequiredItemStatus
{
    Any,
    NotCollected,
    Collected
}

public enum DialogueQuestAction
{
    None,
    StartQuest,
    SetQuestStage,
    CompleteQuest
}

public class InteractableNPC : InteractableBase
{
    [System.Serializable]
    public class DialogueSection
    {
        [Header("Section Information")]
        public string sectionID = "001";
        public string sectionTitle = "First Meeting";

        [TextArea(2, 5)]
        public string[] dialogueLines;

        [Header("Quest Conditions")]
        public string conditionQuestID;

        public RequiredQuestStatus requiredStatus =
            RequiredQuestStatus.Any;

        public string conditionQuestStage;

        [Header("Item Conditions")]
        public string conditionItemQuestID;
        public string conditionItemID;

        public RequiredItemStatus requiredItemStatus =
            RequiredItemStatus.Any;

        [Header("Action After Dialogue Finishes")]
        public DialogueQuestAction questAction =
            DialogueQuestAction.None;

        public string actionQuestID;
        public string actionQuestStage;
    }

    [Header("NPC Information")]
    [SerializeField] private string npcName = "Villager";

    [Header("Dialogue Sections")]
    [SerializeField] private DialogueSection[] dialogueSections;

    public string NPCName => npcName;

    public override void Interact()
    {
        DialogueSection section = GetAvailableDialogueSection();

        if (section == null)
        {
            Debug.Log("No available dialogue section for " + npcName);
            return;
        }

        if (section.dialogueLines == null ||
            section.dialogueLines.Length == 0)
        {
            Debug.LogWarning("Dialogue section has no lines.");
            return;
        }

        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("DialogueManager was not found.");
            return;
        }

        DialogueManager.Instance.StartDialogue(this, section);
    }

    public DialogueSection GetAvailableDialogueSection()
    {
        if (dialogueSections == null) return null;

        foreach (DialogueSection section in dialogueSections)
        {
            if (section == null) continue;

            if (IsSectionAvailable(section))
                return section;
        }

        return null;
    }

    private bool IsSectionAvailable(DialogueSection section)
    {
        if (section.requiredStatus != RequiredQuestStatus.Any)
        {
            if (string.IsNullOrWhiteSpace(section.conditionQuestID))
                return false;

            if (QuestManager.Instance == null)
                return false;

            QuestManager.QuestStatus status =
                QuestManager.Instance.GetQuestStatus(
                    section.conditionQuestID
                );

            switch (section.requiredStatus)
            {
                case RequiredQuestStatus.NotStarted:
                    if (status != QuestManager.QuestStatus.NotStarted)
                        return false;
                    break;

                case RequiredQuestStatus.Active:
                    if (status != QuestManager.QuestStatus.Active)
                        return false;
                    break;

                case RequiredQuestStatus.Completed:
                    if (status != QuestManager.QuestStatus.Completed)
                        return false;
                    break;
            }
        }

        // If a quest stage is specified, it must match.
        if (!string.IsNullOrWhiteSpace(section.conditionQuestStage))
        {
            if (QuestManager.Instance == null ||
                string.IsNullOrWhiteSpace(section.conditionQuestID))
            {
                return false;
            }

            string currentStage =
                QuestManager.Instance.GetQuestStage(
                    section.conditionQuestID
                );

            if (currentStage != section.conditionQuestStage)
                return false;
        }

        if (section.requiredItemStatus != RequiredItemStatus.Any)
        {
            if (string.IsNullOrWhiteSpace(section.conditionItemQuestID) ||
                string.IsNullOrWhiteSpace(section.conditionItemID))
            {
                return false;
            }

            if (QuestManager.Instance == null)
                return false;

            bool collected = QuestManager.Instance.HasCollectedQuestItem(
                section.conditionItemQuestID,
                section.conditionItemID
            );

            if (section.requiredItemStatus == RequiredItemStatus.Collected
                && !collected)
            {
                return false;
            }

            if (section.requiredItemStatus == RequiredItemStatus.NotCollected
                && collected)
            {
                return false;
            }
        }

        return true;
    }

    public string[] GetDialogueLines(DialogueSection section)
    {
        return section != null ? section.dialogueLines : null;
    }

    public void ApplyDialogueAction(DialogueSection section)
    {
        if (section == null || QuestManager.Instance == null) return;

        switch (section.questAction)
        {
            case DialogueQuestAction.StartQuest:
                QuestManager.Instance.StartQuest(
                    section.actionQuestID,
                    section.actionQuestStage
                );
                break;

            case DialogueQuestAction.SetQuestStage:
                QuestManager.Instance.SetQuestStage(
                    section.actionQuestID,
                    section.actionQuestStage
                );
                break;

            case DialogueQuestAction.CompleteQuest:
                QuestManager.Instance.CompleteQuest(
                    section.actionQuestID
                );
                break;
        }
    }
}
