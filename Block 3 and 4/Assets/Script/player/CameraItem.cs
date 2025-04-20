using System.IO;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[CreateAssetMenu(menuName = "Items/CameraItem")]
public class CameraItem : BaseItem
{
    public RenderTexture textureSource;         // 截图使用的 RenderTexture
    // 场景摄像机引用，不可在 ScriptableObject Inspector 中直接设置
    [System.NonSerialized]
    private Camera captureCamera;

    /// <summary>
    /// 在初始化时由 MonoBehaviour 传入摄像机引用
    /// </summary>
    public void Init(Camera sceneCamera)
    {
        captureCamera = sceneCamera;
    }               // 用于中心点检测的相机
    public TMP_Text debugText;                 // 显示调试信息（如拍照保存路径）
    public TMP_Text detectionText;             // 显示检测结果（检测到的物体名称）
    public List<GameObject> detectableObjects; // 可检测的物体列表

    private int photoCount = 0;                // 照片文件计数
    private bool photoMode = false;            // 是否进入拍照模式

    public override void OnSelect(GameObject model)
    {
        // 选中时可在此位置做额外处理，如挂载到相机下
    }

    public override void OnReady()
    {
        photoMode = true;
        debugText.text = "拍照模式：ON";
    }

    public override void OnUnready()
    {
        photoMode = false;
        debugText.text = "拍照模式：OFF";
    }

    public override void OnUse()
    {
        if (!photoMode) return;
        // 使用传入的摄像机或主摄像机
        Camera cam = captureCamera != null ? captureCamera : Camera.main;

        // ===== 拍照并保存 =====
        {
            if (!photoMode) return;

            // ===== 拍照并保存 =====
            Texture2D tex = new Texture2D(textureSource.width, textureSource.height, TextureFormat.RGB24, false);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = textureSource;
            tex.ReadPixels(new Rect(0, 0, textureSource.width, textureSource.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            byte[] data = tex.EncodeToPNG();
            string path = Path.Combine(Application.persistentDataPath, $"photo_{photoCount:D4}.png");
            File.WriteAllBytes(path, data);
            photoCount++;
            debugText.text = $"照片已保存: {path}";
            Debug.Log($"照片已保存: {path}");

            // ===== 中心区域检测 =====
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
            Ray ray = captureCamera.ScreenPointToRay(screenCenter);
            Vector3 detectPoint = ray.origin + ray.direction * 100f;
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                detectPoint = hit.point;
            }
            Collider[] cols = Physics.OverlapSphere(detectPoint, 2f);
            bool found = false;
            foreach (var col in cols)
            {
                if (detectableObjects.Contains(col.gameObject))
                {
                    string name = col.gameObject.name;
                    detectionText.text = $"检测到: {name}";
                    Debug.Log("检测到物体: " + name);
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                detectionText.text = "未检测到任何物体";
            }
        }
    }
}
