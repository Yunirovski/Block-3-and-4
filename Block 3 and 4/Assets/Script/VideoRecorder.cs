using System.IO;
using UnityEngine;
using TMPro;

public class PhotoCapture : MonoBehaviour
{
    [SerializeField] private TMP_Text debugText; // 用于显示调试信息
    [SerializeField] public RenderTexture textureSource; // 截图源

    private int photoCount = 0; // 用于给保存的图片命名
    private bool isPhotoMode = false; // 是否处于拍照模式

    private void Update()
    {
        // 按 F 键切换拍照模式
        if (Input.GetKeyDown(KeyCode.F))
        {
            isPhotoMode = !isPhotoMode;
            if (isPhotoMode)
            {
                debugText.text = "Enable photo mode, press space to take a photo";
                Debug.Log("Photo mode activated.");
            }
            else
            {
                debugText.text = "Disable photo mode, press F to enable photo mode";
                Debug.Log("Photo mode deactivated.");
            }
        }

        // 当处于拍照模式时，按空格键进行拍照
        if (isPhotoMode && Input.GetKeyDown(KeyCode.Space))
        {
            TakePhoto();
        }
    }

    /// <summary>
    /// 捕捉当前 RenderTexture 的内容并保存为 PNG 图片
    /// </summary>
    private void TakePhoto()
    {
        // 根据 RenderTexture 大小创建 Texture2D
        Texture2D photoTexture = new Texture2D(textureSource.width, textureSource.height, TextureFormat.RGB24, false);

        // 保存当前 RenderTexture，并将目标设置为截图源
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = textureSource;

        // 读取像素数据
        photoTexture.ReadPixels(new Rect(0, 0, textureSource.width, textureSource.height), 0, 0);
        photoTexture.Apply();

        // 恢复之前的 RenderTexture
        RenderTexture.active = previous;

        // 将图片编码为 PNG 格式的字节数组
        byte[] photoData = photoTexture.EncodeToPNG();

        // 构建保存路径，例如：persistentDataPath/photo_0000.png
        string photoFilePath = Path.Combine(Application.persistentDataPath, $"photo_{photoCount:D4}.png");
        File.WriteAllBytes(photoFilePath, photoData);

        // 更新图片计数和调试信息
        photoCount++;
        debugText.text = $"Sucess：{photoFilePath}";
        Debug.Log($"Photo saved: {photoFilePath}");
    }
}