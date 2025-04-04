using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

public class PhotoCapture : MonoBehaviour
{
    [SerializeField] private TMP_Text debugText;           // 显示调试信息（例如拍照成功、模式切换等）
    [SerializeField] private TMP_Text detectionText;         // UI 中专门显示检测结果的文本
    [SerializeField] public RenderTexture textureSource;     // 拍照的图像来源
    [SerializeField] private Camera captureCamera;           // 用于射线检测的相机
    [SerializeField] private List<GameObject> detectableObjects; // 待检测物体列表

    private int photoCount = 0;         // 用于生成图片文件名的计数
    private bool isPhotoMode = false;   // 标记是否处于拍照模式

    private void Update()
    {
        // 按 F 键切换拍照模式
        if (Input.GetKeyDown(KeyCode.F))
        {
            isPhotoMode = !isPhotoMode;
            if (isPhotoMode)
            {
                debugText.text = "Photo mode ON. \nPress Space to take a photo. \nPress TAB to toggle mouse cursor.";
                Debug.Log("Photo mode activated.");
            }
            else
            {
                debugText.text = "Photo mode OFF. \nPress F to turn it on. \nPress TAB to toggle mouse cursor.";
                Debug.Log("Photo mode deactivated.");
            }
        }

        // 拍照模式下，按空格拍照并检测屏幕中心区域的物体
        if (isPhotoMode && Input.GetKeyDown(KeyCode.Space))
        {
            string photoFilePath = TakePhoto();
            DetectObjectAtCenter(photoFilePath);
        }
    }

    /// <summary>
    /// 拍摄 RenderTexture 的截图并保存为 PNG 图片
    /// </summary>
    /// <returns>返回图片的保存路径</returns>
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
    /// 通过扩展检测区域检测屏幕中心附近的物体，并更新 UI 显示检测结果
    /// </summary>
    /// <param name="photoFilePath">拍摄的图片路径，可用于传递给对应物体事件</param>
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
        float detectionRadius = 2.0f; // 根据需要调整检测半径
        Collider[] colliders = Physics.OverlapSphere(detectionPoint, detectionRadius);

        bool detected = false;
        foreach (var col in colliders)
        {
            GameObject hitObject = col.gameObject;
            // 判断是否在预设的待检测列表中
            if (detectableObjects.Contains(hitObject))
            {
                // 如果检测到物体，调用其 AnimalEvent 组件
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
