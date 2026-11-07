
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    public enum QuestStatus
    {
        NotStarted,
        Active,
        Completed
    }

    private HashSet<string> activeQuests = new HashSet<string>();
    private HashSet<string> completedQuests = new HashSet<string>();
    private HashSet<string> collectedQuestItems = new HashSet<string>();

    private Dictionary<string, string> questStages =
        new Dictionary<string, string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public QuestStatus GetQuestStatus(string questID)
    {
        if (completedQuests.Contains(questID))
            return QuestStatus.Completed;

        if (activeQuests.Contains(questID))
            return QuestStatus.Active;

        return QuestStatus.NotStarted;
    }

    public void StartQuest(string questID, string initialStage = "")
    {
        if (string.IsNullOrWhiteSpace(questID))
            return;

        if (GetQuestStatus(questID) != QuestStatus.NotStarted)
            return;

        activeQuests.Add(questID);

        if (!string.IsNullOrWhiteSpace(initialStage))
            questStages[questID] = initialStage;

        Debug.Log("Quest started: " + questID);
        RequestAutoSave();
    }

    public void SetQuestStage(string questID, string newStage)
    {
        if (!IsQuestActive(questID))
            return;

        if (string.IsNullOrWhiteSpace(newStage))
            return;

        questStages[questID] = newStage;

        Debug.Log("Quest " + questID + " stage: " + newStage);
        RequestAutoSave();
    }

    public string GetQuestStage(string questID)
    {
        if (questStages.TryGetValue(questID, out string stage))
            return stage;

        return "";
    }

    public void CompleteQuest(string questID)
    {
        if (string.IsNullOrWhiteSpace(questID))
            return;

        if (!activeQuests.Contains(questID))
        {
            Debug.LogWarning("Quest is not active: " + questID);
            return;
        }

        activeQuests.Remove(questID);
        completedQuests.Add(questID);
        questStages.Remove(questID);

        Debug.Log("Quest completed: " + questID);
        RequestAutoSave();
    }

    public bool CollectQuestItem(string questID, string itemID)
    {
        if (string.IsNullOrWhiteSpace(questID) ||
            string.IsNullOrWhiteSpace(itemID))
        {
            return false;
        }

        if (!IsQuestActive(questID))
        {
            Debug.Log("Start quest " + questID +
                      " before collecting this item.");
            return false;
        }

        string key = GetItemKey(questID, itemID);

        if (collectedQuestItems.Contains(key))
            return false;

        collectedQuestItems.Add(key);

        Debug.Log("Collected quest item: " + itemID);
        RequestAutoSave();

        return true;
    }

    public bool HasCollectedQuestItem(string questID, string itemID)
    {
        if (string.IsNullOrWhiteSpace(questID) ||
            string.IsNullOrWhiteSpace(itemID))
        {
            return false;
        }

        return collectedQuestItems.Contains(GetItemKey(questID, itemID));
    }

    private string GetItemKey(string questID, string itemID)
    {
        return questID + "::" + itemID;
    }

    public bool IsQuestActive(string questID)
    {
        return GetQuestStatus(questID) == QuestStatus.Active;
    }

    public bool IsQuestCompleted(string questID)
    {
        return GetQuestStatus(questID) == QuestStatus.Completed;
    }



    // Save and Load Methods
    public QuestProgressSaveData ExportSaveData()
    {
        QuestProgressSaveData data = new QuestProgressSaveData();

        data.activeQuestIds.AddRange(activeQuests);
        data.completedQuestIds.AddRange(completedQuests);

        foreach (KeyValuePair<string, string> stage in questStages)
        {
            data.stages.Add(new QuestStageSaveData
            {
                questId = stage.Key,
                stageId = stage.Value
            });
        }

        foreach (string key in collectedQuestItems)
        {
            int separatorIndex = key.IndexOf("::");

            if (separatorIndex < 0)
                continue;

            data.collectedItems.Add(new CollectedQuestItemSaveData
            {
                questId = key.Substring(0, separatorIndex),
                itemId = key.Substring(separatorIndex + 2)
            });
        }

        return data;
    }

    public void ImportSaveData(QuestProgressSaveData data)
    {
        if (data == null)
            return;

        activeQuests.Clear();
        completedQuests.Clear();
        questStages.Clear();
        collectedQuestItems.Clear();

        if (data.activeQuestIds != null)
        {
            foreach (string questId in data.activeQuestIds)
            {
                if (!string.IsNullOrWhiteSpace(questId))
                    activeQuests.Add(questId);
            }
        }

        if (data.completedQuestIds != null)
        {
            foreach (string questId in data.completedQuestIds)
            {
                if (!string.IsNullOrWhiteSpace(questId))
                    completedQuests.Add(questId);
            }
        }

        if (data.stages != null)
        {
            foreach (QuestStageSaveData stage in data.stages)
            {
                if (stage != null &&
                    !string.IsNullOrWhiteSpace(stage.questId) &&
                    !string.IsNullOrWhiteSpace(stage.stageId))
                {
                    questStages[stage.questId] = stage.stageId;
                }
            }
        }
        
        if (data.collectedItems != null)
        {
            foreach (CollectedQuestItemSaveData item in data.collectedItems)
            {
                if (item != null &&
                    !string.IsNullOrWhiteSpace(item.questId) &&
                    !string.IsNullOrWhiteSpace(item.itemId))
                {
                    collectedQuestItems.Add(
                        GetItemKey(item.questId, item.itemId));
                }
            }
        }
    }

    private void RequestAutoSave()
    {
        if (SupabaseCloudSave.Instance != null)
            SupabaseCloudSave.Instance.RequestAutoSave();
    }

    public void ResetProgress()
    {
        activeQuests.Clear();
        completedQuests.Clear();
        questStages.Clear();
        collectedQuestItems.Clear();
    }
}
