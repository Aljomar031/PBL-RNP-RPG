using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityPipeline.Microsoft.CodeAnalysis;

using Stopwatch = System.Diagnostics.Stopwatch;

public class QuestDijkstraHint : MonoBehaviour
{
    // Connects a Unity scene name to the name shown to the player.
    [Serializable]
    private class Location
    {
        public string sceneName;
        public string displayName;
    }

    // Connects two scenes and gives that connection a travel cost.
    [Serializable]
    private class Connection
    {
        public string fromScene;
        public string toScene;
        public int cost = 1;
        public bool twoWay = true;
    }

    // Stores the objective and destination for one quest or stage.
    [Serializable]
    private class QuestObjective
    {
        public string questID;

        // Leave blank if this entry should work for any stage.
        public string stageID;

        public string questTitle;

        [TextArea]
        public string objective;

        public string destinationScene;
    }

    [Header("Quest panel")]
    [SerializeField] private Button questPanelButton;
    [SerializeField] private TMP_Text questPanelText;

    [Header("Locations and scene connections")]
    [SerializeField] private Location[] locations;
    [SerializeField] private Connection[] connections;

    [Header("Quest destinations, in priority order")]
    [SerializeField] private QuestObjective[] questObjectives;

    // Remembers what the panel currently represents.
    private QuestObjective currentObjective;
    private string currentScene;
    private bool showingRoute;
    private float refreshTimer;

    // Connects the button and displays the current objective.
    private void OnEnable()
    {
        if (questPanelButton != null)
            questPanelButton.onClick.AddListener(TogglePanel);

        CheckForQuestOrSceneChange();
    }

    // Removes the button listener when this object is disabled.
    private void OnDisable()
    {
        if (questPanelButton != null)
            questPanelButton.onClick.RemoveListener(TogglePanel);
    }

    // Checks periodically for quest, stage, or scene changes.
    private void Update()
    {
        refreshTimer += Time.unscaledDeltaTime;

        if (refreshTimer >= 0.25f)
        {
            refreshTimer = 0f;
            CheckForQuestOrSceneChange();
        }
    }

    // Refreshes the panel when the active quest or scene changes.
    private void CheckForQuestOrSceneChange()
    {
        QuestObjective foundObjective = FindActiveObjective();
        string sceneName = SceneManager.GetActiveScene().name;

        if (foundObjective != currentObjective ||
            sceneName != currentScene)
        {
            currentObjective = foundObjective;
            currentScene = sceneName;

            // Show the objective again when the quest or scene changes.
            showingRoute = false;
            UpdatePanelText();
        }
    }

    // Finds the first active quest entry that matches its quest and stage.
    private QuestObjective FindActiveObjective()
    {
        if (QuestManager.Instance == null || questObjectives == null)
            return null;

        foreach (QuestObjective objective in questObjectives)
        {
            if (objective == null ||
                string.IsNullOrWhiteSpace(objective.questID))
            {
                continue;
            }

            if (QuestManager.Instance.GetQuestStatus(objective.questID) !=
                QuestManager.QuestStatus.Active)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(objective.stageID) &&
                QuestManager.Instance.GetQuestStage(objective.questID) !=
                objective.stageID)
            {
                continue;
            }

            return objective;
        }

        return null;
    }

    // Switches the panel between the objective and its route.
    private void TogglePanel()
    {
        CheckForQuestOrSceneChange();

        if (currentObjective == null)
            return;

        showingRoute = !showingRoute;
        UpdatePanelText();
    }

    // Builds the text shown on the quest panel.
    private void UpdatePanelText()
    {
        if (questPanelButton != null)
            questPanelButton.interactable = currentObjective != null;

        if (questPanelText == null)
            return;

        if (currentObjective == null)
        {
            questPanelText.text = "No active quest.";
            return;
        }

        if (!showingRoute)
        {
            questPanelText.text =
                currentObjective.questTitle +
                "\nObjective: " + currentObjective.objective +
                "\n(Tap for route)";
            return;
        }

        if (currentScene == currentObjective.destinationScene)
        {
            questPanelText.text =
                "You're at " + GetLocationName(currentScene) +
                ". Follow the objective here." +
                "\n(Tap to return)";
            return;
        }

        if (FindRoute(
                currentScene,
                currentObjective.destinationScene,
                out List<string> route))
        {
            questPanelText.text =
                "Route: " + string.Join(" → ", route.ToArray()) +
                "\n(Tap to return)";
        }
        else
        {
            questPanelText.text =
                "No route is set from " +
                GetLocationName(currentScene) +
                " to " +
                GetLocationName(currentObjective.destinationScene) +
                "\n(Tap to return)";
        }
    }

    // Uses Dijkstra's algorithm to find the lowest-cost scene route.
    private bool FindRoute(
        string start,
        string destination,
        out List<string> route)
    {
        Stopwatch timer = Stopwatch.StartNew();

        try {
            route = new List<string>();

            if (connections == null || connections.Length == 0)
                return false;

            List<string> allScenes = GetAllSceneNames();

            if (!allScenes.Contains(start) ||
                !allScenes.Contains(destination))
            {
                return false;
            }

            // Stores the best known cost and the previous scene on each route.
            Dictionary<string, int> distance =
                new Dictionary<string, int>();

            Dictionary<string, string> previousScene =
                new Dictionary<string, string>();

            // Scenes in this list still need to be checked.
            List<string> unvisited = new List<string>();

            // Initially, all scenes are unreachable.
            foreach (string scene in allScenes)
            {
                distance[scene] = int.MaxValue;
                unvisited.Add(scene);
            }

            // The route starts at cost zero.
            distance[start] = 0;

            while (unvisited.Count > 0)
            {
                // Choose the unvisited scene with the lowest known cost.
                string closestScene =
                    FindClosestScene(unvisited, distance);

                // Stop if no remaining scene can be reached.
                if (closestScene == null ||
                    distance[closestScene] == int.MaxValue)
                {
                    break;
                }

                // The cheapest route to the destination is now known.
                if (closestScene == destination)
                    break;

                unvisited.Remove(closestScene);

                // Check each connection from this scene.
                foreach (Connection connection in connections)
                {
                    if (connection == null || connection.cost < 0)
                        continue;

                    // Check the forward direction.
                    if (connection.fromScene == closestScene)
                    {
                        UpdateDistance(
                            closestScene,
                            connection.toScene,
                            connection.cost,
                            distance,
                            previousScene);
                    }

                    // Check the reverse direction if travel is two-way.
                    if (connection.twoWay &&
                        connection.toScene == closestScene)
                    {
                        UpdateDistance(
                            closestScene,
                            connection.fromScene,
                            connection.cost,
                            distance,
                            previousScene);
                    }
                }
            }

            // An infinite distance means no route was found.
            if (distance[destination] == int.MaxValue)
                return false;

            // Follow the previous-scene records backwards from the destination.
            string sceneToAdd = destination;
            route.Add(GetLocationName(sceneToAdd));

            while (sceneToAdd != start)
            {
                if (!previousScene.TryGetValue(
                        sceneToAdd,
                        out string previous))
                {
                    return false;
                }

                sceneToAdd = previous;
                route.Add(GetLocationName(sceneToAdd));
            }

            // Put the route in travel order: start first, destination last.
            route.Reverse();
            return true;
        }
        finally
        {
            timer.Stop();
            Debug.Log("Route calc took " + timer.Elapsed.TotalMilliseconds.ToString("F4") + " ms.");
        }


    }

    // Collects unique scene names from the location and connection lists.
    private List<string> GetAllSceneNames()
    {
        List<string> sceneNames = new List<string>();

        if (locations != null)
        {
            foreach (Location location in locations)
            {
                if (location != null &&
                    !string.IsNullOrWhiteSpace(location.sceneName) &&
                    !sceneNames.Contains(location.sceneName))
                {
                    sceneNames.Add(location.sceneName);
                }
            }
        }

        if (connections != null)
        {
            foreach (Connection connection in connections)
            {
                if (connection == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(connection.fromScene) &&
                    !sceneNames.Contains(connection.fromScene))
                {
                    sceneNames.Add(connection.fromScene);
                }

                if (!string.IsNullOrWhiteSpace(connection.toScene) &&
                    !sceneNames.Contains(connection.toScene))
                {
                    sceneNames.Add(connection.toScene);
                }
            }
        }

        return sceneNames;
    }

    // Returns the unvisited scene with the smallest known cost.
    private string FindClosestScene(
        List<string> unvisited,
        Dictionary<string, int> distance)
    {
        string closestScene = null;
        int shortestDistance = int.MaxValue;

        foreach (string scene in unvisited)
        {
            if (distance[scene] < shortestDistance)
            {
                shortestDistance = distance[scene];
                closestScene = scene;
            }
        }

        return closestScene;
    }

    // Records a new route if it costs less than the current best route.
    private void UpdateDistance(
        string fromScene,
        string toScene,
        int cost,
        Dictionary<string, int> distance,
        Dictionary<string, string> previousScene)
    {
        if (!distance.ContainsKey(toScene) ||
            distance[fromScene] == int.MaxValue)
        {
            return;
        }

        int newDistance = distance[fromScene] + cost;

        if (newDistance < distance[toScene])
        {
            distance[toScene] = newDistance;
            previousScene[toScene] = fromScene;
        }
    }

    // Converts an internal scene name into a player friendly location name.
    private string GetLocationName(string sceneName)
    {
        if (locations != null)
        {
            foreach (Location location in locations)
            {
                if (location != null &&
                    location.sceneName == sceneName &&
                    !string.IsNullOrWhiteSpace(location.displayName))
                {
                    return location.displayName;
                }
            }
        }

        // Use the scene name if no display name was assigned.
        return sceneName;
    }
}