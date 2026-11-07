using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public int version = 1;
    public string sceneName;
    public PlayerPositionSaveData playerPosition;
    public QuestProgressSaveData quests;
}

[Serializable]
public class PlayerPositionSaveData
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class QuestProgressSaveData
{
    public List<string> activeQuestIds = new List<string>();
    public List<string> completedQuestIds = new List<string>();
    public List<QuestStageSaveData> stages =
        new List<QuestStageSaveData>();
    public List<CollectedQuestItemSaveData> collectedItems =
        new List<CollectedQuestItemSaveData>();
}

[Serializable]
public class QuestStageSaveData
{
    public string questId;
    public string stageId;
}

[Serializable]
public class CollectedQuestItemSaveData
{
    public string questId;
    public string itemId;
}