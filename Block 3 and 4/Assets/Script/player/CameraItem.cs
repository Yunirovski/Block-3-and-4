using System.IO;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Collections;

[CreateAssetMenu(menuName = "Items/CameraItem")]
public class CameraItem : BaseItem
{
    public RenderTexture textureSource;                  // 拍照用 RT
    [System.NonSerialized] private Camera playerCamera;  // 玩家摄像机
    [System.NonSerialized] private TMP_Text debugText;
    [System.NonSerialized] private TMP_Text detectText;

    public List<GameObject> detectableObjects;           // 可检测物体
    private int photoCount;
    private bool photoMode;

    #region 注入引用
    public void Init(Camera cam) => playerCamera = cam;
    public void InitUI(TMP_Text debug, TMP_Text detect)
    {
        debugText = debug;
        detectText = detect;
    }
    #endregion

    public override void OnReady()
    {
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
        if (!photoMode) return;

        ScreenshotHelper.Instance.StartCoroutine(CaptureFromBackBuffer());
    }

    private IEnumerator CaptureFromBackBuffer()
    {
        yield return new WaitForEndOfFrame();                               // 等整帧渲染

        Texture2D tex = new Texture2D(Screen.width, Screen.height,          // 读取屏幕像素
                                      TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        SaveAndDetect(tex);                                                 // ← 调用现有方法
    }



    private void SaveAndDetect(Texture2D tex)
    {
        // 保存 PNG
        string path = Path.Combine(Application.persistentDataPath, $"photo_{photoCount:D4}.png");
        File.WriteAllBytes(path, tex.EncodeToPNG());
        photoCount++;
        if (debugText) debugText.text = $"照片已保存: {path}";

        // --- 中心检测 ---
        Vector3 center = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
        Ray ray = playerCamera.ScreenPointToRay(center);
        Vector3 p = ray.origin + ray.direction * 100f;
        if (Physics.Raycast(ray, out RaycastHit hit, 100f)) p = hit.point;
        Collider[] cols = Physics.OverlapSphere(p, 2f);
        string result = "未检测到任何物体";
        foreach (var col in cols)
        {
            if (detectableObjects.Contains(col.gameObject))
            {
                result = $"检测到: {col.gameObject.name}";
                break;
            }
        }
        if (detectText) detectText.text = result;
    }
}
