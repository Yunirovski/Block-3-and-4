// Assets/Scripts/Systems/AnimalDirectoryManager.cs
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// This helps make folders for different animal photos
/// </summary>
public class AnimalDirectoryManager : MonoBehaviour
{
    public static AnimalDirectoryManager Instance { get; private set; }

    [Header("Folder Setup")]
    [Tooltip("List of animal folders to make")]
    public List<string> animalFolders = new List<string>
    {
        "Camel", "Donkey", "Giraffe", "Goat", "Hippo", "Lion", "Pigeon", "Rhino"
    };

    [Tooltip("Folder for photos with no animals")]
    public List<string> otherFolders = new List<string>
    {
        "nothing"
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
    /// Makes all needed folders for animal photos
    /// </summary>
    private void CreateAllDirectories()
    {
        // Make animal folders
        foreach (string folder in animalFolders)
        {
            CreateDirectory(folder);
        }

        // Make "nothing" folder
        foreach (string folder in otherFolders)
        {
            CreateDirectory(folder);
        }

        Debug.Log("AnimalDirectoryManager: All photo folders are ready");
    }

    /// <summary>
    /// Makes one folder if it not here yet
    /// </summary>
    private void CreateDirectory(string folderName)
    {
        string path = Path.Combine(baseDirectory, folderName);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Debug.Log($"Made folder: {path}");
        }
    }

    /// <summary>
    /// Gets the right folder path for an animal type
    /// </summary>
    public string GetDirectoryForAnimal(string animalName)
    {
        // If we know this animal, use its folder
        if (animalFolders.Contains(animalName))
        {
            return Path.Combine(baseDirectory, animalName);
        }

        // If it's a new animal, make a folder for it
        string newPath = Path.Combine(baseDirectory, animalName);
        if (!Directory.Exists(newPath))
        {
            Directory.CreateDirectory(newPath);
            animalFolders.Add(animalName); // Add to our animal list
            Debug.Log($"Made folder for new animal: {animalName}");
        }

        return newPath;
    }

    /// <summary>
    /// Gets the folder path for photos with no animals
    /// </summary>
    public string GetNothingDirectory()
    {
        return Path.Combine(baseDirectory, "nothing");
    }
}