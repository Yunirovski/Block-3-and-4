using System.IO;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[CreateAssetMenu(menuName = "Items/CameraItem")]
public class CameraItem : BaseItem
{
    public RenderTexture textureSource;                   // 保存照片的 RenderTexture
    [System.NonSerialized] private Camera playerCamera;   // 玩家主摄像机（运行时注入）
    [System.NonSerialized] private TMP_Text debugText;    // 调试文本
    [System.NonSerialized] private TMP_Text detectionText;// 检测文本

    public List<GameObject> detectableObjects;            // 可检测物体
    private int photoCount = 0;
    private bool photoMode = false;

    public void Init(Camera sceneCam) => playerCamera = sceneCam;
    public void InitUI(TMP_Text debug, TMP_Text detect)
    {
        debugText = debug;
        detectionText = detect;
    }

    public override void OnReady()
    {
        // 仅切换 UI 提示，不改 targetTexture，避免主视窗黑屏
        photoMode = true;
        if (debugText) debugText.text = "拍照模式：ON";
    }

    public override void OnUnready()
    {
        photoMode = false;
        if (debugText) debugText.text = "拍照模式：OFF";
    }

    public override void OnUse()
    {
        if (!photoMode || textureSource == null || playerCamera == null) return;

        // ==== 临时绑定 RenderTexture，渲染一帧后立即还原，保证屏幕不黑 ====
        RenderTexture originalRT = playerCamera.targetTexture;
        playerCamera.targetTexture = textureSource;
        playerCamera.Render();
        playerCamera.targetTexture = originalRT;

        // ============================
        playerCamera.Render();

        // 从 RenderTexture 读取像素
        Texture2D tex = new Texture2D(textureSource.width, textureSource.height, TextureFormat.RGB24, false);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = textureSource;
        tex.ReadPixels(new Rect(0, 0, textureSource.width, textureSource.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;

        // 保存 PNG
        byte[] data = tex.EncodeToPNG();
        string path = Path.Combine(Application.persistentDataPath, $"photo_{photoCount:D4}.png");
        File.WriteAllBytes(path, data);
        photoCount++;
        if (debugText) debugText.text = $"照片已保存: {path}";

        // 检测中心物体
        Vector3 center = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
        Ray ray = playerCamera.ScreenPointToRay(center);
        Vector3 detectPoint = ray.origin + ray.direction * 100f;
        if (Physics.Raycast(ray, out RaycastHit hit, 100f)) detectPoint = hit.point;
        Collider[] cols = Physics.OverlapSphere(detectPoint, 2f);
        string result = "未检测到任何物体";
        foreach (var col in cols)
        {
            if (detectableObjects.Contains(col.gameObject))
            {
                result = $"检测到: {col.gameObject.name}";
                break;
            }
        }
        if (detectionText) detectionText.text = result;
    }
}

