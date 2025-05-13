using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhotoPopup : MonoBehaviour
{
    [SerializeField] RawImage preview;   // 注意：RawImage！
    [SerializeField] Button deleteBtn;
    [SerializeField] Button saveBtn;
    [SerializeField] Button closeBtn;
    [SerializeField] TMP_Text infoText;

    /// <summary>
    /// 初始化弹窗
    /// </summary>
    /// <param name="imgPath">照片完整路径</param>
    /// <param name="onDelete">点击 Delete 回调</param>
    /// <param name="onSave">点击 Save 回调</param>
    public void Init(string imgPath, System.Action onDelete, System.Action onSave)
    {
        // 生成缩略图并显示
        Sprite sprite = PhotoLibrary.Instance.GetThumbnail(imgPath, 512);
        if (sprite != null)
            preview.texture = sprite.texture;

        infoText.text = System.IO.Path.GetFileName(imgPath);

        deleteBtn.onClick.AddListener(() =>
        {
            onDelete?.Invoke();
            Destroy(gameObject);
        });

        saveBtn.onClick.AddListener(() => onSave?.Invoke());

        closeBtn.onClick.AddListener(() => Destroy(gameObject));
    }
}
