using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

public class PhotoCapture : MonoBehaviour
{
    [SerializeField] private TMP_Text debugText;           // Show debug info (e.g., photo success, mode change)
    [SerializeField] private TMP_Text detectionText;       // UI text for detection results
    [SerializeField] public RenderTexture textureSource;   // Image source for the photo
    [SerializeField] private Camera captureCamera;         // Camera for raycasting
    [SerializeField] private List<GameObject> detectableObjects; // List of objects to detect

    private int photoCount = 0;         // Counter for photo filenames
    private bool isPhotoMode = false;   // Is photo mode on or off

    private void Update()
    {
        // Press F to turn photo mode on/off
        if (Input.GetKeyDown(KeyCode.F))
        {
            isPhotoMode = !isPhotoMode;
            if (isPhotoMode)
            {
                debugText.text = "Photo mode ON. \nPress Left Mouse Button to take a photo. \nPress TAB to toggle mouse cursor.";
                Debug.Log("Photo mode activated.");
            }
            else
            {
                debugText.text = "Photo mode OFF. \nPress F to turn it on. \nPress TAB to toggle mouse cursor.";
                Debug.Log("Photo mode deactivated.");
            }
        }

        // In photo mode, click left mouse to take a photo and check the center of the screen for objects
        if (isPhotoMode && Input.GetMouseButtonDown(0)) // 0 is left mouse button
        {
            string photoFilePath = TakePhoto();
            DetectObjectAtCenter(photoFilePath);
        }
    }

    /// <summary>
    /// Take a screenshot of the RenderTexture and save it as a PNG
    /// </summary>
    /// <returns>Return the path where the photo is saved</returns>
    private string TakePhoto()
    {
        Texture2D photoTexture = new Texture2D(textureSource.width, textureSource.height, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = textureSource;
        photoTexture.ReadPixels(new Rect(0, 0, textureSource.width, textureSource.height), 0, 0);
        photoTexture.Apply();
        RenderTexture.active = previous;

        byte[] photoData = photoTexture.EncodeToPNG();
        string photoFilePath = Path.Combine(Application.persistentDataPath, $"photo_{photoCount:D4}.png");
        File.WriteAllBytes(photoFilePath, photoData);
        photoCount++;

        debugText.text = $"Photo saved: {photoFilePath}";
        Debug.Log($"Photo saved: {photoFilePath}");
        return photoFilePath;
    }

    /// <summary>
    /// Check the center of the screen for objects and detect them
    /// </summary>
    /// <param name="photoFilePath">Path of the photo, can be used for object events</param>
    private void DetectObjectAtCenter(string photoFilePath)
    {
        // 从屏幕中心发射射线获取检测点
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
        Ray ray = captureCamera.ScreenPointToRay(screenCenter);
        float maxDistance = 100f;
        Vector3 detectionPoint = ray.origin + ray.direction * maxDistance;

        // 如果射线碰撞到了物体，则以碰撞点作为检测中心
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            detectionPoint = hit.point;
        }

        // 在检测中心周围使用 OverlapSphere 进行区域检测
        float detectionRadius = 2.0f; // 根据实际需要调整检测半径
        Collider[] colliders = Physics.OverlapSphere(detectionPoint, detectionRadius);

        bool detected = false;
        foreach (var col in colliders)
        {
            GameObject hitObject = col.gameObject;
            // 判断该物体是否在预设的待检测列表中
            if (detectableObjects.Contains(hitObject))
            {
                AnimalEvent animalEvent = hitObject.GetComponent<AnimalEvent>();
                if (animalEvent != null)
                {
                    animalEvent.TriggerEvent(photoFilePath);
                    detectionText.text = "Detected object: " + hitObject.name;
                }
                detected = true;
                break;
            }
        }

        if (!detected)
        {
            Debug.Log("No detectable object in detection area.");
            detectionText.text = "No detectable object in detection area.";
        }
    }

}
