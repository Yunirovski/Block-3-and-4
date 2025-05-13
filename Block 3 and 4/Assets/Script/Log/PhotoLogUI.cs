using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;  // �����һ������

/// <summary>
/// ��������־(Journal)��չʾ������Ƭ��
/// ÿ�������ǩҳ�����һ���������������ʾ������Ķ�����Ƭ��
/// </summary>
public class PhotoLogUI : MonoBehaviour
{
    [Header("����")]
    [Tooltip("�����չʾ������/�������� (polar/savanna/jungle/tutorial)")]
    public string regionKey;

    [Tooltip("������Ϣ���ݿ⣬���ڻ�ȡ�������ƺ�����")]
    public AnimalInfoDB animalInfoDB;

    [Header("UI ����")]
    [Tooltip("���������������ɶ�����Ŀ")]
    public Transform contentParent;

    [Tooltip("������ĿԤ����")]
    public GameObject animalEntryPrefab;

    [Tooltip("��Ƭ����Ԥ����")]
    public PhotoPopup photoPopupPrefab;

    [Header("��ҳ����")]
    [Tooltip("ÿҳ��ʾ����Ƭ����")]
    public int photosPerPage = 4;

    [Tooltip("��ҳ��ť��ָʾ��")]
    public Button prevPageButton;
    public Button nextPageButton;
    public TMP_Text pageIndicatorText;

    // ��ǰ��ʾ�Ķ����б�
    private List<string> displayedAnimals = new List<string>();

    // ��ĿUI�����ֵ�
    private Dictionary<string, GameObject> animalEntries = new Dictionary<string, GameObject>();

    // ÿ�����ﵱǰ��ʾ��ҳ��
    private Dictionary<string, int> currentPages = new Dictionary<string, int>();

    // ��ǰѡ�еĶ���
    private string currentSelectedAnimal;

    private void Start()
    {
        // ����PhotoLibrary�ı���¼�
        if (PhotoLibrary.Instance != null)
        {
            PhotoLibrary.Instance.OnPhotoDatabaseChanged += RefreshDisplay;
        }

        // ����AnimalInfoDB�����δ��ֵ
        if (animalInfoDB == null)
        {
            animalInfoDB = FindObjectOfType<AnimalInfoDB>();
        }

        // ���÷�ҳ��ť
        if (prevPageButton != null)
            prevPageButton.onClick.AddListener(() => ChangePage(false));

        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(() => ChangePage(true));

        // ��ʼˢ��
        RefreshDisplay();
    }

    private void OnEnable()
    {
        // ҳ�漤��ʱˢ����ʾ
        RefreshDisplay();
    }

    /// <summary>
    /// �л�ҳ��
    /// </summary>
    public void ChangePage(bool next)
    {
        if (string.IsNullOrEmpty(currentSelectedAnimal)) return;

        if (!currentPages.ContainsKey(currentSelectedAnimal))
            currentPages[currentSelectedAnimal] = 0;

        int totalPhotos = PhotoLibrary.Instance.GetPhotoCount(currentSelectedAnimal);
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)totalPhotos / photosPerPage));

        int currentPage = currentPages[currentSelectedAnimal];

        if (next)
            currentPages[currentSelectedAnimal] = (currentPage + 1) % totalPages;
        else
            currentPages[currentSelectedAnimal] = (currentPage - 1 + totalPages) % totalPages;

        UpdateAnimalEntry(currentSelectedAnimal);
    }

    /// <summary>
    /// ����ҳ��ָʾ��
    /// </summary>
    private void UpdatePageIndicator(string animalId)
    {
        if (pageIndicatorText == null) return;

        if (!PhotoLibrary.Instance.GetAnimalIds().Contains(animalId))
        {
            pageIndicatorText.text = "����Ƭ";

            if (prevPageButton != null)
                prevPageButton.interactable = false;

            if (nextPageButton != null)
                nextPageButton.interactable = false;

            return;
        }

        int totalPhotos = PhotoLibrary.Instance.GetPhotoCount(animalId);
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)totalPhotos / photosPerPage));
        int currentPage = currentPages.ContainsKey(animalId) ? currentPages[animalId] + 1 : 1;

        pageIndicatorText.text = $"ҳ {currentPage}/{totalPages}";

        // ���°�ť״̬
        if (prevPageButton != null)
            prevPageButton.interactable = totalPages > 1;

        if (nextPageButton != null)
            nextPageButton.interactable = totalPages > 1;
    }

    /// <summary>
    /// ˢ�����������б���ʾ
    /// </summary>
    public void RefreshDisplay()
    {
        // ֻ�е���Ϸ���󼤻�ʱ��ˢ��
        if (!gameObject.activeInHierarchy) return;

        if (PhotoLibrary.Instance == null) return;

        // ��ȡ���ڴ���������ж���ID
        List<string> regionAnimals = new List<string>();
        foreach (string animalId in PhotoLibrary.Instance.GetAnimalIds())
        {
            // ��鶯���Ƿ����ڵ�ǰ����
            if (BelongsToRegion(animalId, regionKey))
            {
                regionAnimals.Add(animalId);
            }
        }

        // ɾ�����ٴ��ڵ���Ŀ
        List<string> toRemove = new List<string>();
        foreach (string displayedAnimal in displayedAnimals)
        {
            if (!regionAnimals.Contains(displayedAnimal) &&
                !IsUnknownAnimalEntry(displayedAnimal))
            {
                toRemove.Add(displayedAnimal);
            }
        }

        foreach (string animal in toRemove)
        {
            RemoveAnimalEntry(animal);
        }

        // �������Ŀ������������Ŀ
        foreach (string animalId in regionAnimals)
        {
            if (!displayedAnimals.Contains(animalId))
            {
                CreateAnimalEntry(animalId);

                // ����Ϊ��ǰѡ�ж������ǵ�һ����
                if (string.IsNullOrEmpty(currentSelectedAnimal))
                {
                    currentSelectedAnimal = animalId;
                }
            }
            else
            {
                UpdateAnimalEntry(animalId);
            }
        }

        // ���δ�����Ķ���
        AddUnknownAnimals();

        // ����ҳ��ָʾ��
        if (!string.IsNullOrEmpty(currentSelectedAnimal))
        {
            UpdatePageIndicator(currentSelectedAnimal);
        }
        else if (pageIndicatorText != null)
        {
            pageIndicatorText.text = "����Ƭ";
        }
    }

    /// <summary>
    /// �ж϶����Ƿ����ڵ�ǰ����
    /// </summary>
    private bool BelongsToRegion(string animalId, string region)
    {
        // ��AnimalInfoDB��ȡ������Ϣ
        if (animalInfoDB != null)
        {
            AnimalInfo info = animalInfoDB.GetAnimalInfo(animalId);
            if (info != null)
            {
                return info.region.ToLower() == region.ToLower();
            }
        }

        // ���û�����ݿ�ƥ�䣬ͨ������Լ���ж�
        // ����: polar_bear, savanna_lion, jungle_parrot
        return animalId.ToLower().StartsWith(region.ToLower() + "_");
    }

    /// <summary>
    /// �ж��Ƿ�Ϊδ�����Ķ�����Ŀ
    /// </summary>
    private bool IsUnknownAnimalEntry(string animalId)
    {
        if (animalInfoDB == null) return false;

        AnimalInfo info = animalInfoDB.GetAnimalInfo(animalId);
        return info != null && info.region.ToLower() == regionKey.ToLower();
    }

    /// <summary>
    /// �����µĶ�����Ŀ
    /// </summary>
    private void CreateAnimalEntry(string animalId)
    {
        if (animalEntryPrefab == null || contentParent == null) return;

        GameObject entryObj = Instantiate(animalEntryPrefab, contentParent);
        entryObj.name = "Entry_" + animalId;

        // ���ö�������
        TMP_Text nameText = entryObj.GetComponentInChildren<TMP_Text>();
        if (nameText != null)
        {
            string displayName = GetDisplayName(animalId);
            nameText.text = displayName;
        }

        // ��ʼ����Ƭ��ʾ����
        Transform photoContainer = entryObj.transform.Find("PhotoContainer");
        if (photoContainer != null)
        {
            // ��ʼ����Ƭ����
            for (int i = 0; i < photosPerPage; i++)
            {
                Transform photoSlot = photoContainer.Find($"PhotoSlot{i + 1}");
                if (photoSlot != null)
                {
                    // ���Ĭ��ͼƬ
                    Image photoImage = photoSlot.GetComponent<Image>();
                    if (photoImage != null)
                    {
                        photoImage.sprite = null;
                        photoImage.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                    }

                    // ���õ����ť
                    Button photoButton = photoSlot.GetComponent<Button>();
                    if (photoButton != null)
                    {
                        photoButton.interactable = false;
                    }
                }
            }
        }

        // �����Ǽ���ʾ
        Transform starsParent = entryObj.transform.Find("Stars");
        if (starsParent != null)
        {
            int maxStars = PhotoLibrary.Instance.GetMaxStars(animalId);
            SetStarsVisibility(starsParent, maxStars);
        }

        // ����ѡ���¼�
        Button selectButton = entryObj.GetComponentInChildren<Button>();
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(() => {
                SelectAnimal(animalId);
            });
        }

        // ��ӵ���Ŀ�ֵ����ʾ�б�
        animalEntries[animalId] = entryObj;
        displayedAnimals.Add(animalId);

        // ��ʼ��ҳ��
        if (!currentPages.ContainsKey(animalId))
        {
            currentPages[animalId] = 0;
        }

        // ������Ƭ
        UpdateAnimalEntry(animalId);
    }

    /// <summary>
    /// ѡ���ﲢ������ʾ
    /// </summary>
    private void SelectAnimal(string animalId)
    {
        currentSelectedAnimal = animalId;
        UpdatePageIndicator(animalId);

        // ����ѡ����Ŀ
        foreach (var entry in animalEntries)
        {
            Image background = entry.Value.GetComponent<Image>();
            if (background != null)
            {
                background.color = entry.Key == animalId
                    ? new Color(0.3f, 0.6f, 0.9f, 0.5f)
                    : new Color(0.2f, 0.2f, 0.2f, 0.5f);
            }
        }
    }

    /// <summary>
    /// ����δ�����Ķ�����Ŀ
    /// </summary>
    private void CreateUnknownAnimalEntry(string animalId)
    {
        if (animalEntryPrefab == null || contentParent == null) return;

        // ����Ƿ��Ѵ���
        if (displayedAnimals.Contains(animalId)) return;

        GameObject entryObj = Instantiate(animalEntryPrefab, contentParent);
        entryObj.name = "Unknown_" + animalId;

        // ����Ϊ�ʺ�
        TMP_Text nameText = entryObj.GetComponentInChildren<TMP_Text>();
        if (nameText != null)
        {
            nameText.text = "???";
        }

        // ��Ƭ������ʾΪ��ɫ����״̬
        Transform photoContainer = entryObj.transform.Find("PhotoContainer");
        if (photoContainer != null)
        {
            Image containerImage = photoContainer.GetComponent<Image>();
            if (containerImage != null)
            {
                containerImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            }

            // ����������Ƭ��
            for (int i = 0; i < photosPerPage; i++)
            {
                Transform photoSlot = photoContainer.Find($"PhotoSlot{i + 1}");
                if (photoSlot != null)
                {
                    photoSlot.gameObject.SetActive(false);
                }
            }

            // �������ͼ��
            GameObject lockIcon = new GameObject("LockIcon");
            lockIcon.transform.SetParent(photoContainer, false);
            Image lockImage = lockIcon.AddComponent<Image>();
            lockImage.sprite = Resources.Load<Sprite>("UI/lock_icon");
            lockImage.rectTransform.sizeDelta = new Vector2(64, 64);
            lockImage.rectTransform.anchoredPosition = Vector2.zero;
        }

        // �����Ǽ�
        Transform starsParent = entryObj.transform.Find("Stars");
        if (starsParent != null)
        {
            starsParent.gameObject.SetActive(false);
        }

        // �������а�ť
        Button[] buttons = entryObj.GetComponentsInChildren<Button>();
        foreach (var button in buttons)
        {
            button.interactable = false;
        }

        // ��ӵ��ֵ���б�
        animalEntries[animalId] = entryObj;
        displayedAnimals.Add(animalId);
    }

    /// <summary>
    /// ���������δ����Ķ�����Ϊ�ʺ���Ŀ
    /// </summary>
    private void AddUnknownAnimals()
    {
        if (animalInfoDB == null) return;

        foreach (AnimalInfo info in animalInfoDB.animalInfos)
        {
            if (info.region.ToLower() == regionKey.ToLower())
            {
                bool hasPhotos = PhotoLibrary.Instance != null &&
                                PhotoLibrary.Instance.GetPhotoCount(info.animalId) > 0;

                if (!hasPhotos && !displayedAnimals.Contains(info.animalId))
                {
                    CreateUnknownAnimalEntry(info.animalId);
                }
            }
        }
    }

    /// <summary>
    /// �������ж�����Ŀ
    /// </summary>
    private void UpdateAnimalEntry(string animalId)
    {
        if (!animalEntries.TryGetValue(animalId, out GameObject entryObj)) return;

        // ��ȡ��Ƭ�б�
        var allPhotos = PhotoLibrary.Instance.GetPhotos(animalId);

        // ��ȡ��ǰҳ
        int currentPage = currentPages.ContainsKey(animalId) ? currentPages[animalId] : 0;
        int startIndex = currentPage * photosPerPage;

        // ������Ƭ��ʾ
        Transform photoContainer = entryObj.transform.Find("PhotoContainer");
        if (photoContainer != null)
        {
            // ����ÿ����Ƭ��
            for (int i = 0; i < photosPerPage; i++)
            {
                int photoIndex = startIndex + i;
                Transform photoSlot = photoContainer.Find($"PhotoSlot{i + 1}");

                if (photoSlot == null) continue;

                Image photoImage = photoSlot.GetComponent<Image>();
                Button photoButton = photoSlot.GetComponent<Button>();

                if (photoIndex < allPhotos.Count)
                {
                    // ����Ƭ����������ͼ
                    photoSlot.gameObject.SetActive(true);

                    // ������Ƭ
                    StartCoroutine(LoadThumbnail(allPhotos[photoIndex].path, photoImage));

                    // ���ð�ť����¼�
                    if (photoButton != null)
                    {
                        int index = photoIndex; // �����ֲ���������Lambda��ʹ��
                        photoButton.onClick.RemoveAllListeners();
                        photoButton.onClick.AddListener(() => {
                            ShowPhotoPopup(animalId, index);
                        });
                        photoButton.interactable = true;
                    }

                    // ������Ƭ�Ǽ�
                    Transform starIndicator = photoSlot.Find("StarIndicator");
                    if (starIndicator != null)
                    {
                        TMP_Text starText = starIndicator.GetComponent<TMP_Text>();
                        if (starText != null)
                        {
                            starText.text = $"{allPhotos[photoIndex].stars}��";
                            starText.gameObject.SetActive(true);
                        }
                    }
                }
                else
                {
                    // ����Ƭ����ʾ�ղ�
                    if (photoImage != null)
                    {
                        photoImage.sprite = null;
                        photoImage.color = new Color(0.2f, 0.2f, 0.2f, 0.3f);
                    }

                    if (photoButton != null)
                    {
                        photoButton.onClick.RemoveAllListeners();
                        photoButton.interactable = false;
                    }

                    // �����Ǽ�
                    Transform starIndicator = photoSlot.Find("StarIndicator");
                    if (starIndicator != null)
                    {
                        starIndicator.gameObject.SetActive(false);
                    }
                }
            }
        }

        // �����Ǽ���ʾ
        Transform starsParent = entryObj.transform.Find("Stars");
        if (starsParent != null)
        {
            int maxStars = PhotoLibrary.Instance.GetMaxStars(animalId);
            SetStarsVisibility(starsParent, maxStars);
        }
    }

    /// <summary>
    /// �����Ǽ���ʾ
    /// </summary>
    private void SetStarsVisibility(Transform starsParent, int count)
    {
        for (int i = 0; i < starsParent.childCount; i++)
        {
            Transform star = starsParent.GetChild(i);
            if (star != null)
            {
                star.gameObject.SetActive(i < count);
            }
        }
    }

    /// <summary>
    /// �Ƴ�������Ŀ
    /// </summary>
    private void RemoveAnimalEntry(string animalId)
    {
        if (animalEntries.TryGetValue(animalId, out GameObject entryObj))
        {
            Destroy(entryObj);
            animalEntries.Remove(animalId);
            displayedAnimals.Remove(animalId);

            // ���ɾ�����ǵ�ǰѡ�еģ�����ѡ��
            if (animalId == currentSelectedAnimal)
            {
                currentSelectedAnimal = displayedAnimals.Count > 0 ? displayedAnimals[0] : "";
                UpdatePageIndicator(currentSelectedAnimal);
            }
        }
    }

    /// <summary>
    /// ��ʾ��Ƭ����
    /// </summary>
    private void ShowPhotoPopup(string animalId, int photoIndex)
    {
        if (photoPopupPrefab == null) return;

        PhotoPopup popup = Instantiate(photoPopupPrefab);
        popup.Initialize(animalId, photoIndex, animalInfoDB);
    }

    /// <summary>
    /// ��ȡ�������ʾ����
    /// </summary>
    private string GetDisplayName(string animalId)
    {
        // ���ȴӶ�����Ϣ���ݿ��ȡ
        if (animalInfoDB != null)
        {
            AnimalInfo info = animalInfoDB.GetAnimalInfo(animalId);
            if (info != null && !string.IsNullOrEmpty(info.displayName))
            {
                return info.displayName;
            }
        }

        // ��ʽ��ID��Ϊ��ѡ����
        string name = animalId.Replace('_', ' ');
        // ����ĸ��д
        if (name.Length > 0)
        {
            name = char.ToUpper(name[0]) + name.Substring(1);
        }
        return name;
    }

    /// <summary>
    /// ������Ƭ����ͼ
    /// </summary>
    private IEnumerator LoadThumbnail(string path, Image targetImage)
    {
        if (string.IsNullOrEmpty(path) || targetImage == null) yield break;

        // ����ͼ��
        targetImage.sprite = null;
        targetImage.color = Color.white;

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
                targetImage.color = new Color(0.8f, 0.2f, 0.2f, 0.5f); // ��ʾ������ɫ
                yield break;
            }

            // ��������
            Texture2D texture = ((UnityEngine.Networking.DownloadHandlerTexture)request.downloadHandler).texture;
            if (texture != null)
            {
                // ��С��������������
                int thumbnailSize = 256;
                TextureScale.Bilinear(texture, thumbnailSize, thumbnailSize);

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    Vector2.one * 0.5f
                );

                // ����ͼ��
                targetImage.sprite = sprite;
                targetImage.preserveAspect = true;
            }
        }
    }
}