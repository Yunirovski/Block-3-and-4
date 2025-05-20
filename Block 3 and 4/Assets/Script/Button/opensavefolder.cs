// This script opens the photo folder when button is clicked
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System;
using UnityEngine.UI;
using TMPro;

public class OpenPhotoFolder : MonoBehaviour
{
    // Optional: Text component to show the path or errors
    public TextMeshProUGUI debugText;

    /// <summary>
    /// Opens the photo folder in file explorer
    /// </summary>
    public void OpenFolder()
    {
        try
        {
            // Get the path where photos are saved
            string path = Application.persistentDataPath;

            // Show path in debug console
            UnityEngine.Debug.Log("Trying to open: " + path);

            // Display path in UI if text component assigned
            if (debugText != null)
            {
                debugText.text = "Path: " + path;
            }

            // Make sure the path exists
            if (Directory.Exists(path))
            {
                // Try different methods to open the folder
                bool success = false;

                // Method 1: Using Process.Start
                try
                {
                    if (Application.platform == RuntimePlatform.WindowsEditor ||
                        Application.platform == RuntimePlatform.WindowsPlayer)
                    {
                        // On Windows, use explorer.exe
                        Process.Start("explorer.exe", "\"" + path.Replace("/", "\\") + "\"");
                        success = true;
                    }
                    else if (Application.platform == RuntimePlatform.OSXEditor ||
                             Application.platform == RuntimePlatform.OSXPlayer)
                    {
                        // On Mac, use open command
                        Process.Start("open", "\"" + path + "\"");
                        success = true;
                    }
                    else if (Application.platform == RuntimePlatform.LinuxEditor ||
                             Application.platform == RuntimePlatform.LinuxPlayer)
                    {
                        // On Linux, use xdg-open
                        Process.Start("xdg-open", "\"" + path + "\"");
                        success = true;
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("Method 1 failed: " + e.Message);
                    if (debugText != null)
                    {
                        debugText.text += "\nMethod 1 failed";
                    }
                }

                // Method 2: Using Application.OpenURL
                if (!success)
                {
                    try
                    {
                        Application.OpenURL("file://" + path);
                        success = true;
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError("Method 2 failed: " + e.Message);
                        if (debugText != null)
                        {
                            debugText.text += "\nMethod 2 failed";
                        }
                    }
                }

                // Method 3: Using System.Diagnostics.Process with ProcessStartInfo
                if (!success)
                {
                    try
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            Arguments = path.Replace("/", "\\"),
                            FileName = "explorer.exe"
                        };
                        Process.Start(startInfo);
                        success = true;
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError("Method 3 failed: " + e.Message);
                        if (debugText != null)
                        {
                            debugText.text += "\nMethod 3 failed";
                        }
                    }
                }

                // Copy path to clipboard as last resort
                if (!success)
                {
                    GUIUtility.systemCopyBuffer = path;
                    UnityEngine.Debug.Log("Path copied to clipboard: " + path);
                    if (debugText != null)
                    {
                        debugText.text += "\nPath copied to clipboard";
                    }
                }
            }
            else
            {
                // Show error if folder not found
                UnityEngine.Debug.LogError("Cannot find folder: " + path);
                if (debugText != null)
                {
                    debugText.text += "\nCannot find folder";
                }
            }
        }
        catch (Exception e)
        {
            // Catch any errors
            UnityEngine.Debug.LogError("Error: " + e.Message);
            if (debugText != null)
            {
                debugText.text = "Error: " + e.Message;
            }
        }
    }
}