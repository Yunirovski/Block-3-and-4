using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ��Ƭ�滻ѡ����������ѡ��Ҫ�滻����Ƭ
/// </summary>
public class ReplacePhotoSelector : MonoBehaviour
{
    [Header("UI ����")]
    [Tooltip("��Ƭ��������")]
    public Transform photoGrid;

    [Tooltip("��Ƭ��ĿԤ����")]
    public GameObject photoItemPrefab;

    [Tooltip("�����ı�")]
    public TMP_Text titleText;

    [Tooltip("ȡ����ť")]
    public Button cancelButton;

    // �ڲ�����
    private string animalId;
    private string newPhotoPath;
    private int newPhotoStars;
    private System.Action onReplaceComplete;

    private void Start()
    {
        // ����ȡ����ť
        if (cancelButton != null)
            cancelButton.onClick.AddListener(Close);
    }

    /// <summary>
    /// ��ʼ��ѡ����
    /// </summary>
    public void Initialize(string animalId, string photoPath, int stars, System.Action onCompleteCallback)
    {
        this.animalId = animalId;
        this.newPhotoPath = photoPath;
        this.newPhotoStars = stars;
        this.onReplaceComplete = onCompleteCallback;

        // ���ñ���
        if (titleText != null)
        {
            titleText.text = $"ѡ��Ҫ�滻��{animalId}��Ƭ";
        }

        // ����������Ƭ
        LoadExistingPhotos();
    }

    /// <summary>
    /// ����������Ƭ
    /// </summary>
    private void LoadExistingPhotos()
    {
        if (PhotoLibrary.Instance == null || photoGrid == null) return;

        var photos = PhotoLibrary.Instance.GetPhotos(animalId);

        // �������
        foreach (Transform child in photoGrid)
        {
            Destroy(child.gameObject);
        }

        // ������Ƭ
        for (int i = 0; i < photos.Count; i++)
        {
            int photoIndex = i; // ���ر�������Lambda

            GameObject item = Instantiate(photoItemPrefab, photoGrid);
            if (item == null) continue;

            // ������Ƭ
            Image photoImage = item.GetComponentInChildren<Image>();
            if (photoImage != null)
            {
                StartCoroutine(LoadThumbnail(photos[i].path, photoImage));
            }

            // �����Ǽ�
            Transform starsContainer = item.transform.Find("Stars");
            if (starsContainer != null)
            {
                SetStarsVisibility(starsContainer, photos[i].stars);
            }

            // ���������ı�
            TMP_Text indexText = item.GetComponentInChildren<TMP_Text>();
            if (indexText != null)
            {
                indexText.text = $"#{photoIndex + 1} - {photos[i].stars}��";
            }

            // ����ѡ��ť
            Button selectButton = item.GetComponent<Button>();
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(() => {
                    ReplacePhoto(photoIndex);
                });
            }
        }
    }

    /// <summary>
    /// �滻��Ƭ
    /// </summary>
    private void ReplacePhoto(int indexToReplace)
    {
        if (PhotoLibrary.Instance == null) return;

        // ��ʾȷ�϶Ի���
        ConfirmationDialog dialog = Instantiate(Resources.Load<GameObject>("Prefabs/ConfirmationDialog"))
            .GetComponent<ConfirmationDialog>();

        if (dialog != null)
        {
            dialog.Initialize(
                "ȷ���滻",
                $"ȷ��Ҫ���������{newPhotoStars}����Ƭ�滻��{indexToReplace + 1}����Ƭ��",
                () => {
                    // ִ���滻
                    PerformReplace(indexToReplace);
                },
                null // ȡ�������κ���
            );
        }
        else
        {
            // ���û�жԻ���Ԥ���壬ֱ���滻
            PerformReplace(indexToReplace);
        }
    }

    /// <summary>
    /// ִ���滻����
    /// </summary>
    private void PerformReplace(int indexToReplace)
    {
        if (PhotoLibrary.Instance.ReplacePhoto(animalId, indexToReplace, newPhotoPath, newPhotoStars))
        {
            Debug.Log($"�ѽ�{animalId}�ĵ�{indexToReplace + 1}����Ƭ�滻Ϊ����Ƭ");

            // �ص�֪ͨ
            onReplaceComplete?.Invoke();

            // �ر�ѡ����
            Close();
        }
        else
        {
            Debug.LogError($"�滻��Ƭʧ��: {animalId} ���� {indexToReplace}");
        }
    }

    /// <summary>
    /// �����Ǽ���ʾ
    /// </summary>
    private void SetStarsVisibility(Transform starsParent, int count)
    {
        if (starsParent == null) return;

        for (int i = 0; i < starsParent.childCount; i++)
        {
            if (starsParent.GetChild(i) != null)
                starsParent.GetChild(i).gameObject.SetActive(i < count);
        }
    }

    /// <summary>
    /// ��������ͼ
    /// </summary>
    private IEnumerator LoadThumbnail(string path, Image targetImage)
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
                Debug.LogError($"������Ƭ����ͼʧ��: {request.error}");
                targetImage.color = new Color(0.8f, 0.2f, 0.2f, 0.5f); // �����ɫ
                yield break;
            }

            // ��������
            Texture2D texture = ((UnityEngine.Networking.DownloadHandlerTexture)request.downloadHandler).texture;
            if (texture != null)
            {
                // ��С����
                int thumbnailSize = 128;
                TextureScale.Bilinear(texture, thumbnailSize, thumbnailSize);

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
    /// �ر�ѡ����
    /// </summary>
    private void Close()
    {
        Destroy(gameObject);
    }
}