using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;

/// <summary>
/// ��Ƭ������ʾ��������������Ƭ�ﵽ����ʱ��ʾ
/// </summary>
public class FullPageAlertPopup : MonoBehaviour
{
    [Header("UI ����")]
    [Tooltip("��Ƭͼ��")]
    public Image photoImage;

    [Tooltip("��ʾ��Ϣ�ı�")]
    public TMP_Text messageText;

    [Tooltip("���水ť")]
    public Button saveButton;

    [Tooltip("�滻��ť")]
    public Button replaceButton;

    [Tooltip("ȡ����ť")]
    public Button cancelButton;

    // �ڲ�����
    private string animalId;
    private string photoPath;
    private int stars;

    private void Start()
    {
        // ���ð�ť�¼�
        if (saveButton != null)
            saveButton.onClick.AddListener(SavePhoto);

        if (replaceButton != null)
            replaceButton.onClick.AddListener(OpenReplaceSelector);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(Close);
    }

    /// <summary>
    /// ��ʼ������
    /// </summary>
    public void Initialize(string animalId, string photoPath, int stars)
    {
        this.animalId = animalId;
        this.photoPath = photoPath;
        this.stars = stars;

        // ������Ϣ
        if (messageText != null)
        {
            messageText.text = $"{animalId}����Ƭ�Ѵ�����({PhotoLibrary.MaxPerAnimal}��)��\n" +
                               "��ѡ�񣺱��浽�豸���滻������Ƭ��ȡ����";
        }

        // ������Ƭ
        if (photoImage != null)
        {
            StartCoroutine(LoadPhoto(photoPath, photoImage));
        }
    }

    /// <summary>
    /// ������Ƭ���豸
    /// </summary>
    private void SavePhoto()
    {
        if (string.IsNullOrEmpty(photoPath) || !File.Exists(photoPath))
        {
            ShowError("��Ч����Ƭ·��");
            return;
        }

        // ��������Ŀ¼
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string saveFolder = Path.Combine(desktopPath, "AnimalPhotos");

        if (!Directory.Exists(saveFolder))
        {
            try
            {
                Directory.CreateDirectory(saveFolder);
            }
            catch (System.Exception e)
            {
                ShowError($"��������Ŀ¼ʧ��: {e.Message}");
                return;
            }
        }

        // �����ļ���
        string fileName = $"{animalId}_{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}.png";
        string destinationPath = Path.Combine(saveFolder, fileName);

        try
        {
            File.Copy(photoPath, destinationPath);
            Debug.Log($"��Ƭ�ѱ��浽: {destinationPath}");

            // ��ʾ�ɹ���Ϣ���ر�
            if (messageText != null)
            {
                messageText.text = $"��Ƭ�ѱ��浽��\n{destinationPath}";

                // ���ð�ť
                DisableAllButtons();

                // 3���ر�
                Invoke("Close", 3f);
            }
        }
        catch (System.Exception e)
        {
            ShowError($"������Ƭʧ��: {e.Message}");
        }
    }

    /// <summary>
    /// ���滻ѡ����
    /// </summary>
    private void OpenReplaceSelector()
    {
        var selectorPrefab = Resources.Load<GameObject>("Prefabs/ReplacePhotoSelector");
        if (selectorPrefab == null)
        {
            ShowError("�Ҳ����滻ѡ����Ԥ����");
            return;
        }

        var selector = Instantiate(selectorPrefab).GetComponent<ReplacePhotoSelector>();
        if (selector != null)
        {
            selector.Initialize(animalId, photoPath, stars, () => {
                // �滻�ɹ��ص�
                Close();
            });
        }
    }

    /// <summary>
    /// ��ʾ������Ϣ
    /// </summary>
    private void ShowError(string error)
    {
        if (messageText != null)
        {
            messageText.text = $"����: {error}";
            messageText.color = Color.red;
        }

        Debug.LogError(error);
    }

    /// <summary>
    /// �������а�ť
    /// </summary>
    private void DisableAllButtons()
    {
        if (saveButton != null)
            saveButton.interactable = false;

        if (replaceButton != null)
            replaceButton.interactable = false;

        if (cancelButton != null)
            cancelButton.interactable = false;
    }

    /// <summary>
    /// �رյ���
    /// </summary>
    private void Close()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// ������Ƭ
    /// </summary>
    private IEnumerator LoadPhoto(string path, Image targetImage)
    {
        if (string.IsNullOrEmpty(path) || targetImage == null) yield break;

        // ����ͼ��
        targetImage.sprite = null;
        targetImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // �����л�ɫ

        // �����ļ�URL
        string url = "file://" + path;

        // ����ͼ��
        using (UnityEngine.Networking.UnityWebRequest request =
               UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogError($"������Ƭʧ��: {request.error}");
                targetImage.color = new Color(0.8f, 0.2f, 0.2f, 0.5f); // �����ɫ
                yield break;
            }

            // ��������
            Texture2D texture = ((UnityEngine.Networking.DownloadHandlerTexture)request.downloadHandler).texture;
            if (texture != null)
            {
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    Vector2.one * 0.5f
                );

                // ����ͼ��
                targetImage.sprite = sprite;
                targetImage.color = Color.white;
                targetImage.preserveAspect = true;
            }
        }
    }
}