
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    private string file = "player.txt"; // File name to save the data
    public PlayerData data; // PlayerData object to hold the player's data

    public void Save()
    { 
        // the function that saves the data by writing/ overwring the text file content
        string json = JsonUtility.ToJson(data); // Convert the PlayerData object to JSON format
        writeToFile(file, json);
    }

    public void Load()
    {
        // reads the data written on the player.txt file and loads it to populate the game data
        data = new PlayerData(); // Create a new instance of PlayerData
        string json = ReadFromFile(file); // Read the JSON data from the file
        JsonUtility.FromJsonOverwrite(json, data); // Populate the PlayerData object with the JSON data
    }

    private void writeToFile(string fileName, string json)
    {
        // writes the data using this function
        string path = GetFilePath(fileName);
        FileStream fileStream = new FileStream(path, FileMode.Create); // Create a new file or overwrite the existing one

        using (StreamWriter writer = new StreamWriter(fileStream))
        {
            writer.Write(json); // Write the content to the file
        }
    }

    private string ReadFromFile(string fileName) 
    { 
        // read the data from the file
        string path = GetFilePath(fileName);

        if (File.Exists(path))
        {
            using (StreamReader reader = new StreamReader(path))
            {
                string json = reader.ReadToEnd(); // Read the entire content of the file
                return json;
            }
        }
        else
        {
            Debug.LogWarning("File not found: " + path);
        }
        return "";

    }

    private string GetFilePath(string fileName)
    {
        // sets the file path on where the text json file will be saved
        return Application.persistentDataPath + "/" + fileName; // Use Application.persistentDataPath for a platform-independent path
    }
}
