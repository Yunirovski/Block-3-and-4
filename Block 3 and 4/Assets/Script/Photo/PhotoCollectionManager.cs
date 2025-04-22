using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// Manages collections of photos, categorized by animal name.
/// Provides a global singleton to add new photos and automatically updates
/// the on‑screen UI text to show counts per animal category.
/// </summary>
public class PhotoCollectionManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance for easy global access.
    /// </summary>
    public static PhotoCollectionManager Instance;

    // Internal data structure mapping each animal name to its list of photo paths.
    private readonly Dictionary<string, List<string>> photoCollections = new Dictionary<string, List<string>>();

    [Header("UI References")]
    [Tooltip("Text element used to display the photo collection summary in the UI.")]
    [SerializeField] private TMP_Text collectionText;

    private void Awake()
    {
        // Ensure only one instance exists (singleton pattern).
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Adds a new photo path under the specified animal category.
    /// If the category does not yet exist, it will be created.
    /// </summary>
    /// <param name="animalName">The key/category under which to group the photo.</param>
    /// <param name="photoPath">The filesystem path (or identifier) of the captured photo.</param>
    public void AddPhoto(string animalName, string photoPath)
    {
        // Create a new list if this animal hasn't been seen before
        if (!photoCollections.ContainsKey(animalName))
        {
            photoCollections[animalName] = new List<string>();
        }

        // Add the photo path to the appropriate list
        photoCollections[animalName].Add(photoPath);

        // Refresh the on‑screen summary to reflect the new count
        UpdateCollectionText();
    }

    /// <summary>
    /// Regenerates and applies the UI text showing the number of photos per animal.
    /// Uses a StringBuilder for efficient string concatenation.
    /// </summary>
    private void UpdateCollectionText()
    {
        if (collectionText == null)
        {
            Debug.LogWarning("PhotoCollectionManager: Missing reference to 'collectionText'. Cannot update UI.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("Photo Collections:");

        // Append one line per animal category
        foreach (var kvp in photoCollections)
        {
            sb.AppendLine($"{kvp.Key}: {kvp.Value.Count} photos");
        }

        // Apply the built string to the UI text component
        collectionText.text = sb.ToString();
    }
}
