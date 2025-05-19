using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ��Ƭ���鵯������ʾ�����������Ƭ����Ϣ
/// </summary>
public class PhotoPopup : MonoBehaviour
{
    [Header("UI ����")]
    [Tooltip("�����")]
    public GameObject popupPanel;

    [Tooltip("���������ı�")]
    public TMP_Text animalNameText;

    [Tooltip("���������ı�")]
    public TMP_Text descriptionText;

    [Tooltip("��Ƭ��ʾͼ��")]
    public Image photoImage;

    [Tooltip("�Ǽ�ͼ������")]
    public Image[] starImages;

    [Tooltip("��ҳ��ť - ��һ��")]
    public Button prevButton;

    [Tooltip("��ҳ��ť - ��һ��")]
    public Button nextButton;

    [Tooltip("��Ƭ�����ı� (���� 2/5)")]
    public TMP_Text indexText;

    [Tooltip("�رհ�ť")]
    public Button closeButton;

    [Header("����ť")]
    public Button deleteButton;
    public Button saveButton;

    // ��Ƭ����
    private string animalId;
    private List<PhotoLibrary.PhotoEntry> photos = new List<PhotoLibrary.PhotoEntry>();
    private int currentPhotoIndex = 0;

    private void Start()
    {
        // ���ð�ť�¼�
        if (prevButton != null)
            prevButton.onClick.AddListener(ShowPreviousPhoto);

        if (nextButton != null)
            nextButton.onClick.AddListener(ShowNextPhoto);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (deleteButton != null)
            deleteButton.onClick.AddListener(DeleteCurrentPhoto);

        if (saveButton != null)
            saveButton.onClick.AddListener(SaveCurrentPhoto);
    }

    /// <summary>
    /// ��ʼ������
    /// </summary>
    public void Initialize(string animalId, int initialPhotoIndex, AnimalInfoDB infoDatabase)
    {
        this.animalId = animalId;

        // ��ȡ��Ƭ�б�
        if (PhotoLibrary.Instance != null)
        {
            photos = new List<PhotoLibrary.PhotoEntry>(PhotoLibrary.Instance.GetPhotos(animalId));
        }

        // ���ó�ʼ��Ƭ����
        currentPhotoIndex = Mathf.Clamp(initialPhotoIndex, 0, photos.Count - 1);

        // ���ö�����Ϣ
        if (animalNameText != null)
        {
            animalNameText.text = GetDisplayName(animalId, infoDatabase);
        }

        if (descriptionText != null && infoDatabase != null)
        {
            AnimalInfo info = infoDatabase.GetAnimalInfo(animalId);
            descriptionText.text = info != null ? info.description : "û�п��õ�������Ϣ��";
        }

        // ��ʾ��Ƭ
        UpdatePhotoDisplay();
    }

    /// <summary>
    /// ��ȡ������ʾ����
    /// </summary>
    private string GetDisplayName(string animalId, AnimalInfoDB infoDatabase)
    {
        // �����ݿ��ȡ
        if (infoDatabase != null)
        {
            AnimalInfo info = infoDatabase.GetAnimalInfo(animalId);
            if (info != null && !string.IsNullOrEmpty(info.displayName))
            {
                return info.displayName;
            }
        }

        // ��ʽ��IDΪ��ʾ����
        string name = animalId.Replace('_', ' ');
        if (name.Length > 0)
        {
            name = char.ToUpper(name[0]) + name.Substring(1);
        }
        return name;
    }

    /// <summary>
    /// ��ʾ��һ����Ƭ
    /// </summary>
    public void ShowPreviousPhoto()
    {
        if (photos.Count == 0) return;

        currentPhotoIndex--;
        if (currentPhotoIndex < 0)
            currentPhotoIndex = photos.Count - 1;

        UpdatePhotoDisplay();
    }

    /// <summary>
    /// ��ʾ��һ����Ƭ
    /// </summary>
    public void ShowNextPhoto()
    {
        if (photos.Count == 0) return;

        currentPhotoIndex++;
        if (currentPhotoIndex >= photos.Count)
            currentPhotoIndex = 0;

        UpdatePhotoDisplay();
    }

    /// <summary>
    /// ɾ����ǰ��Ƭ
    /// </summary>
    public void DeleteCurrentPhoto()
    {
        if (photos.Count == 0) return;

        // ȷ��ɾ���Ի���
        ConfirmationDialog dialog = Instantiate(Resources.Load<GameObject>("Prefabs/ConfirmationDialog"))
            .GetComponent<ConfirmationDialog>();

        if (dialog != null)
        {
            dialog.Initialize(
                "ȷ��ɾ��",
                "ȷ��Ҫɾ��������Ƭ�𣿴˲����޷�������",
                () => {
                    // ȷ��ɾ��
                    PerformDelete();
                },
                null // ȡ������
            );
        }
        else
        {
            // ���û�жԻ���Ԥ���壬ֱ��ɾ��
            PerformDelete();
        }
    }

    /// <summary>
    /// ִ��ɾ������
    /// </summary>
    private void PerformDelete()
    {
        if (PhotoLibrary.Instance == null || photos.Count == 0) return;

        // ִ��ɾ��
        if (PhotoLibrary.Instance.DeletePhoto(animalId, currentPhotoIndex))
        {
            // ���±�������
            photos.RemoveAt(currentPhotoIndex);

            // ��������
            if (currentPhotoIndex >= photos.Count && photos.Count > 0)
                currentPhotoIndex = photos.Count - 1;

            // ���û����Ƭ�˾͹رյ���
            if (photos.Count == 0)
            {
                Close();
            }
            else
            {
                // ������ʾ
                UpdatePhotoDisplay();
            }
        }
    }

    /// <summary>
    /// ���浱ǰ��Ƭ���豸
    /// </summary>
    public void SaveCurrentPhoto()
    {
        if (photos.Count == 0) return;

        string sourcePath = photos[currentPhotoIndex].path;
        if (!File.Exists(sourcePath))
        {
            Debug.LogError($"�Ҳ�����Ƭ�ļ�: {sourcePath}");

            if (descriptionText != null)
            {
                string originalText = descriptionText.text;
                descriptionText.text = "�����Ҳ�����Ƭ�ļ�";
                StartCoroutine(RestoreTextAfterDelay(descriptionText, originalText, 3f));
            }

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
                Debug.LogError($"��������Ŀ¼ʧ��: {e.Message}");

                if (descriptionText != null)
                {
                    string originalText = descriptionText.text;
                    descriptionText.text = "�����޷���������Ŀ¼";
                    StartCoroutine(RestoreTextAfterDelay(descriptionText, originalText, 3f));
                }

                return;
            }
        }

        // �����ļ���
        string fileName = $"{animalId}_{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}.png";
        string destinationPath = Path.Combine(saveFolder, fileName);

        try
        {
            File.Copy(sourcePath, destinationPath);
            Debug.Log($"��Ƭ�ѱ��浽: {destinationPath}");

            // ��ʾ�ɹ���Ϣ
            if (descriptionText != null)
            {
                string originalText = descriptionText.text;
                descriptionText.text = $"��Ƭ�ѱ��浽��\n{destinationPath}";
                StartCoroutine(RestoreTextAfterDelay(descriptionText, originalText, 3f));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"������Ƭʧ��: {e.Message}");

            if (descriptionText != null)
            {
                string originalText = descriptionText.text;
                descriptionText.text = $"����ʧ��: {e.Message}";
                StartCoroutine(RestoreTextAfterDelay(descriptionText, originalText, 3f));
            }
        }
    }

    /// <summary>
    /// �ӳٺ�ָ��ı�
    /// </summary>
    private IEnumerator RestoreTextAfterDelay(TMP_Text textComponent, string originalText, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (textComponent != null)
            textComponent.text = originalText;
    }

    /// <summary>
    /// ������Ƭ��ʾ
    /// </summary>
    private void UpdatePhotoDisplay()
    {
        if (photos.Count == 0)
        {
            if (photoImage != null)
                photoImage.gameObject.SetActive(false);

            if (indexText != null)
                indexText.text = "����Ƭ";

            if (prevButton != null)
                prevButton.interactable = false;

            if (nextButton != null)
                nextButton.interactable = false;

            if (deleteButton != null)
                deleteButton.interactable = false;

            if (saveButton != null)
                saveButton.interactable = false;

            // ������������
            if (starImages != null)
            {
                foreach (var star in starImages)
                {
                    if (star != null) star.gameObject.SetActive(false);
                }
            }

            return;
        }

        // ����UIԪ��
        if (photoImage != null)
            photoImage.gameObject.SetActive(true);

        if (prevButton != null)
            prevButton.interactable = photos.Count > 1;

        if (nextButton != null)
            nextButton.interactable = photos.Count > 1;

        if (deleteButton != null)
            deleteButton.interactable = true;

        if (saveButton != null)
            saveButton.interactable = true;

        // ��ȡ��ǰ��Ƭ
        PhotoLibrary.PhotoEntry currentPhoto = photos[currentPhotoIndex];

        // ��ʾ������Ϣ
        if (indexText != null)
            indexText.text = $"{currentPhotoIndex + 1}/{photos.Count}";

        // ��ʾ�Ǽ�
        if (starImages != null)
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                    starImages[i].gameObject.SetActive(i < currentPhoto.stars);
            }
        }

        // ������Ƭ
        if (photoImage != null)
        {
            StartCoroutine(LoadPhoto(currentPhoto.path, photoImage));
        }
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

    /// <summary>
    /// �رյ���
    /// </summary>
    public void Close()
    {
        Destroy(gameObject);
    }
}