using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhotoPopup : MonoBehaviour
{
    [SerializeField] RawImage preview;   // ��Prefab���� RawImage ���
    [SerializeField] Button deleteBtn;
    [SerializeField] Button saveBtn;
    [SerializeField] Button closeBtn;
    [SerializeField] TMP_Text infoText;

    /// <param name="imgPath">PNG �ļ�����·��</param>
    /// <param name="onDelete">��� Delete �ص�</param>
    /// <param name="onSave">��� Save �ص�</param>
    public void Init(string imgPath, System.Action onDelete, System.Action onSave)
    {
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
