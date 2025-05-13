using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhotoLogUI : MonoBehaviour
{
    /* --------- Inspector �� --------- */

    [Header("Animal List")]
    [SerializeField] Transform animalButtonParent;    // ScrollView/Content
    [SerializeField] GameObject animalButtonPrefab;   // Button Ԥ����

    [Header("Info & Thumbs")]
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descText;
    [SerializeField] Image[] thumbSlots;              // 8 �� Image+Button��˳������

    [Header("Popup")]
    [SerializeField] PhotoPopup popupPrefab;          // �·��ڶ��ݽű����ɵ� prefab
    [SerializeField] Transform popupRoot;             // ������ LogCanvas �¿�����

    /* --------- ����ʱ --------- */

    string currentAnimalId;

    /* ==================== �������� ==================== */

    void OnEnable()
    {
        BuildAnimalList();
        PhotoLibrary.Instance.OnPhotoDatabaseChanged += RefreshCurrentAnimal;
    }

    void OnDisable()
    {
        PhotoLibrary.Instance.OnPhotoDatabaseChanged -= RefreshCurrentAnimal;
    }

    /* ================== ������ද�ﰴť ================== */

    void BuildAnimalList()
    {
        foreach (Transform c in animalButtonParent)
            Destroy(c.gameObject);

        foreach (string id in PhotoLibrary.Instance.GetAnimalIds())
        {
            GameObject btnGO = Instantiate(animalButtonPrefab, animalButtonParent);
            btnGO.GetComponentInChildren<TMP_Text>().text = GetDisplayName(id);

            btnGO.GetComponent<Button>().onClick.AddListener(() =>
            {
                currentAnimalId = id;
                RefreshAnimal(id);
            });
        }

        // �Զ������һ��������У�
        if (animalButtonParent.childCount > 0)
            animalButtonParent.GetChild(0).GetComponent<Button>().onClick.Invoke();
    }

    /* ================== ˢ���Ҳ�����ͼ ================== */

    void RefreshCurrentAnimal()
    {
        if (!string.IsNullOrEmpty(currentAnimalId))
            RefreshAnimal(currentAnimalId);
    }

    void RefreshAnimal(string animalId)
    {
        IReadOnlyList<PhotoLibrary.PhotoEntry> photos =
            PhotoLibrary.Instance.GetPhotos(animalId);

        // ������ͼ
        for (int i = 0; i < thumbSlots.Length; i++)
        {
            Image img = thumbSlots[i];

            if (i < photos.Count)
            {
                var entry = photos[i];
                img.sprite = PhotoLibrary.Instance.GetThumbnail(entry.path);
                img.color = Color.white;

                int localIdx = i;   // lambda ����
                img.GetComponent<Button>().onClick.RemoveAllListeners();
                img.GetComponent<Button>().onClick.AddListener(() =>
                {
                    ShowPopup(animalId, localIdx, entry.path);
                });
            }
            else
            {
                img.sprite = null;
                img.color = new Color(1, 1, 1, 0);   // ͸������
                img.GetComponent<Button>().onClick.RemoveAllListeners();
            }
        }

        // ���� / ����
        nameText.text = GetDisplayName(animalId);
        descText.text = GetDescription(animalId);
    }

    /* ================== �������� ================== */

    void ShowPopup(string animalId, int photoIdx, string path)
    {
        PhotoPopup pop = Instantiate(popupPrefab, popupRoot);
        pop.Init(path,
            onDelete: () =>
            {
                PhotoCollectionManager.Instance.DeletePhoto(animalId, photoIdx);
            },
            onSave: () =>
            {
                string dir = System.IO.Path.Combine(
                    Application.persistentDataPath, "saved_photos");
                System.IO.Directory.CreateDirectory(dir);
                string dst = System.IO.Path.Combine(
                    dir, System.IO.Path.GetFileName(path));
                System.IO.File.Copy(path, dst, overwrite: true);
            });
    }

    /* ================== ���� ================== */

    string GetDisplayName(string id)
    {
        AnimalInfo info = AnimalInfoDB.Lookup(id);
        return info != null ? info.displayName : id;
    }

    string GetDescription(string id)
    {
        AnimalInfo info = AnimalInfoDB.Lookup(id);
        return info != null ? info.description : "";
    }
}
