using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

public class PhotoCapture : MonoBehaviour
{
    [SerializeField] private TMP_Text debugText;          // 用于显示调试信息
    [SerializeField] public RenderTexture textureSource;    // 截图的图像来源
    [SerializeField] private Camera captureCamera;          // 用于发射射线检测（可以在 Inspector 指定）
    [SerializeField] private List<GameObject> detectableObjects; // 预设的待检测物体列表

    private int photoCount = 0;         // 拍照计数，用于生成文件名
    private bool isPhotoMode = false;   // 是否处于拍照模式

    private void Update()
    {
        // 按 F 键切换拍照模式
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

        // 在拍照模式下，按空格拍照并检测屏幕中心的物体
        if (isPhotoMode && Input.GetKeyDown(KeyCode.Space))
        {
            string photoFilePath = TakePhoto();   // 拍照并保存图片
            DetectObjectAtCenter(photoFilePath);    // 检测屏幕中心的物体
        }
    }

    /// <summary>
    /// 拍摄 RenderTexture 的截图，并保存为 PNG 图片
    /// </summary>
    /// <returns>图片的保存路径</returns>
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
    /// 扩大检测区域：先用射线得到目标点，再在目标点周围进行 OverlapSphere 检测
    /// </summary>
    /// <param name="photoFilePath">拍摄的图片路径，可用于事件处理</param>
    private void DetectObjectAtCenter(string photoFilePath)
    {
        // 计算屏幕中心点（确保使用的是正确的相机分辨率）
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
        Ray ray = captureCamera.ScreenPointToRay(screenCenter);

        float maxDistance = 100f; // 射线最大检测距离
        Vector3 detectionPoint = ray.origin + ray.direction * maxDistance;

        // 尝试用射线检测获取目标点，如果射线碰撞到了某物，则使用该碰撞点
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            detectionPoint = hit.point;
        }

        // 定义检测区域的半径（根据需要进行调整）
        float detectionRadius = 2.0f;

        // 在目标点周围检测所有碰撞体
        Collider[] colliders = Physics.OverlapSphere(detectionPoint, detectionRadius);

        bool detected = false;
        foreach (var col in colliders)
        {
            GameObject hitObject = col.gameObject;
            // 检查这个物体是否在预设的检测列表中
            if (detectableObjects.Contains(hitObject))
            {
                Debug.Log("Detected object: " + hitObject.name);

                // 尝试获取该物体上的事件处理组件
                AnimalEvent animalEvent = hitObject.GetComponent<AnimalEvent>();
                if (animalEvent != null)
                {
                    animalEvent.OnDetected(photoFilePath);
                    debugText.text += "\nDetected: " + hitObject.name;
                }
                detected = true;
            }
        }

        if (!detected)
        {
            Debug.Log("No detectable object in detection area.");
            debugText.text += "\nNo detectable object in detection area.";
        }
    }
}