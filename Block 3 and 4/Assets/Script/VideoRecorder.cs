using System.IO;
using UnityEngine;
using TMPro;

public class PhotoCapture : MonoBehaviour
{
    [SerializeField] private TMP_Text debugText; // Show debug message on screen
    [SerializeField] public RenderTexture textureSource; // The image source for screenshot

    private int photoCount = 0; // Number for photo file names
    private bool isPhotoMode = false; // Is photo mode on or off

    private void Update()
    {
        // Press F to turn photo mode on or off
        if (Input.GetKeyDown(KeyCode.F))
        {
            isPhotoMode = !isPhotoMode;
            if (isPhotoMode)
            {
                debugText.text = "Photo mode ON. Press Space to take a photo.";
                Debug.Log("Photo mode activated.");
            }
            else
            {
                debugText.text = "Photo mode OFF. Press F to turn it on.";
                Debug.Log("Photo mode deactivated.");
            }
        }

        // If photo mode is on, press Space to take a photo
        if (isPhotoMode && Input.GetKeyDown(KeyCode.Space))
        {
            TakePhoto();
        }
    }

    /// <summary>
    /// Take a screenshot from the RenderTexture and save it as a PNG image
    /// </summary>
    private void TakePhoto()
    {
        // Create a new image with same size as the RenderTexture
        Texture2D photoTexture = new Texture2D(textureSource.width, textureSource.height, TextureFormat.RGB24, false);

        // Save the current screen, and use our texture as the screen
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = textureSource;

        // Read pixels from the texture
        photoTexture.ReadPixels(new Rect(0, 0, textureSource.width, textureSource.height), 0, 0);
        photoTexture.Apply();

        // Go back to the previous screen
        RenderTexture.active = previous;

        // Change the image to PNG format
        byte[] photoData = photoTexture.EncodeToPNG();

        // Create the file path to save the photo
        string photoFilePath = Path.Combine(Application.persistentDataPath, $"photo_{photoCount:D4}.png");
        File.WriteAllBytes(photoFilePath, photoData);

        // Increase photo number and show success message
        photoCount++;
        debugText.text = $"Success: {photoFilePath}";
        Debug.Log($"Photo saved: {photoFilePath}");
    }
}
