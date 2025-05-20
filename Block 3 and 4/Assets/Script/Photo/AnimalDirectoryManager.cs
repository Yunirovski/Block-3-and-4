// Assets/Scripts/Systems/AnimalDirectoryManager.cs
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Manages photo directory structure for different animal types
/// </summary>
public class AnimalDirectoryManager : MonoBehaviour
{
    public static AnimalDirectoryManager Instance { get; private set; }

    [Header("Directory Configuration")]
    [Tooltip("List of animal type folders to create")]
    public List<string> animalFolders = new List<string>
    {
        "Bear", "Deer", "Fox", "Rabbit", "Wolf", "Penguin", "Eagle", "Turtle"
    };

    [Tooltip("Additional test folders")]
    public List<string> testFolders = new List<string>
    {
        "test1", "test2"
    };

    private string baseDirectory;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Initialize base directory
        baseDirectory = Application.persistentDataPath;
        CreateAllDirectories();
    }

    /// <summary>
    /// Creates all required directories for animal photos
    /// </summary>
    private void CreateAllDirectories()
    {
        // Create animal type directories
        foreach (string folder in animalFolders)
        {
            CreateDirectory(folder);
        }

        // Create test directories
        foreach (string folder in testFolders)
        {
            CreateDirectory(folder);
        }

        Debug.Log("AnimalDirectoryManager: All photo directories initialized");
    }

    /// <summary>
    /// Creates a single directory if it doesn't exist
    /// </summary>
    private void CreateDirectory(string folderName)
    {
        string path = Path.Combine(baseDirectory, folderName);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Debug.Log($"Created directory: {path}");
        }
    }

    /// <summary>
    /// Gets the appropriate directory path for an animal type
    /// </summary>
    public string GetDirectoryForAnimal(string animalName)
    {
        // If the animal type is known, use its directory
        if (animalFolders.Contains(animalName))
        {
            return Path.Combine(baseDirectory, animalName);
        }

        // If it's a new animal type, create a directory for it
        string newPath = Path.Combine(baseDirectory, animalName);
        if (!Directory.Exists(newPath))
        {
            Directory.CreateDirectory(newPath);
            animalFolders.Add(animalName); // Add to known animals
            Debug.Log($"Created directory for new animal type: {animalName}");
        }

        return newPath;
    }

    /// <summary>
    /// Gets a random test directory path
    /// </summary>
    public string GetRandomTestDirectory()
    {
        string testFolder = testFolders[Random.Range(0, testFolders.Count)];
        return Path.Combine(baseDirectory, testFolder);
    }
}