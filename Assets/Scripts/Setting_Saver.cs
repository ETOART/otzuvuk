using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[AddComponentMenu("Settings/Setting Saver")]
public class Setting_Saver : MonoBehaviour
{
    [Tooltip("File name inside StreamingAssets where settings are saved/loaded.")]
    [SerializeField] private string fileName = "sound_distances.json";

    private string FilePath => Path.Combine(Application.streamingAssetsPath, fileName);

    [Serializable]
    private class FloatListContainer
    {
        public List<float> values = new List<float>();
    }

    /// <summary>
    /// Saves a list of normalized values (0..1) into a JSON file in StreamingAssets.
    /// </summary>
    public void Save(List<float> values)
    {
        try
        {
            var container = new FloatListContainer { values = values ?? new List<float>() };
            var json = JsonUtility.ToJson(container, prettyPrint: true);

            // Ensure directory exists
            if (!Directory.Exists(Application.streamingAssetsPath))
                Directory.CreateDirectory(Application.streamingAssetsPath);

            File.WriteAllText(FilePath, json);
            Debug.Log($"Setting_Saver: Saved {container.values.Count} values to {FilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Setting_Saver: Failed to save settings to {FilePath}: {ex}");
        }
    }

    /// <summary>
    /// Loads list of normalized values (0..1) from JSON file in StreamingAssets.
    /// Returns null if file not found or loading failed.
    /// </summary>
    public List<float> Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                Debug.Log($"Setting_Saver: File not found at {FilePath}");
                return null;
            }

            var json = File.ReadAllText(FilePath);
            var container = JsonUtility.FromJson<FloatListContainer>(json);
            return container?.values;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Setting_Saver: Failed to load settings from {FilePath}: {ex}");
            return null;
        }
    }
}
