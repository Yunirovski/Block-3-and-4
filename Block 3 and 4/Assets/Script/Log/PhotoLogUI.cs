using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhotoLogUI : MonoBehaviour
{
    /* ---------- Inspector �� ---------- */

    [Header("Animal List")]
    [SerializeField] Transform animalButtonParent;       // ScrollView/Content
    [SerializeField] GameObject animalButtonPrefab;      // Button Ԥ����

    [Header("Info & Thumbs")]
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descText;
    [SerializeField] Image[] thumbSlots;                 // 8 �� Image+Button

    [Header("Popup")]
    [SerializeField] PhotoPopup popupPrefab;
    [SerializeField] Transform popupRoot;

    /* ---------- ����ʱ ---------- */

    string currentAnimalId;

    /* ================= �������� ================= */

    void OnEnable()
    {
        BuildAnimalList();
        PhotoLibrary.Instance.OnPhotoDatabaseChanged += RefreshCurrentAnimal;
    }

    void OnDisable()
    {
        PhotoLibrary.Instance.OnPhotoDatabaseChanged -= RefreshCurrentAnimal;
    }

    /* ================= ��ද���б� ================= */

    void BuildAnimalList()
    {
        foreach (Transform c in animalButtonParent) Destroy(c.gameObject);

        foreach (string id in PhotoLibrary.Instance.GetAnimalIds())
        {
            GameObject go = Instantiate(animalButtonPrefab, animalButtonParent);

            // -------- Button ������������������ --------
            Button uiButton = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>();
            if (uiButton == null)
            {
                Debug.LogError($"AnimalButton prefab ȱ�� Button ���: {animalButtonPrefab.name}");
                continue;
            }

            // -------- �����ı� --------
            TMP_Text txt = go.GetComponentInChildren<TMP_Text>(true);
            if (txt != null) txt.text = GetDisplayName(id);

            // -------- �󶨵�� --------
            uiButton.onClick.AddListener(() =>
            {
                currentAnimalId = id;
                RefreshAnimal(id);
            });
        }

        // �Զ�ѡ��һ��
        if (animalButtonParent.childCount > 0)
            animalButtonParent.GetChild(0).GetComponent<Button>().onClick.Invoke();
    }

    /* ================= �Ҳ���Ƭ���� ================= */

    void RefreshCurrentAnimal()
    {
        if (!string.IsNullOrEmpty(currentAnimalId))
            RefreshAnimal(currentAnimalId);
    }

    void RefreshAnimal(string animalId)
    {
        IReadOnlyList<PhotoLibrary.PhotoEntry> list =
            PhotoLibrary.Instance.GetPhotos(animalId);

        for (int i = 0; i < thumbSlots.Length; i++)
        {
            Image img = thumbSlots[i];

            if (i < list.Count)
            {
                var entry = list[i];
                img.sprite = PhotoLibrary.Instance.GetThumbnail(entry.path);
                img.color = Color.white;

                int localIndex = i;   // lambda ����
                img.GetComponent<Button>().onClick.RemoveAllListeners();
                img.GetComponent<Button>().onClick.AddListener(() =>
                {
                    ShowPopup(animalId, localIndex, entry.path);
                });
            }
            else
            {
                img.sprite = null;
                img.color = new Color(1, 1, 1, 0);
                img.GetComponent<Button>().onClick.RemoveAllListeners();
            }
        }

        nameText.text = GetDisplayName(animalId);
        descText.text = GetDescription(animalId);
    }

    /* ================= ���� ================= */

    void ShowPopup(string animalId, int photoIdx, string path)
    {
        PhotoPopup pop = Instantiate(popupPrefab, popupRoot);
        pop.Init(path,
            onDelete: () => PhotoCollectionManager.Instance.DeletePhoto(animalId, photoIdx),
            onSave: () =>
            {
                string dir = System.IO.Path.Combine(Application.persistentDataPath, "saved_photos");
                System.IO.Directory.CreateDirectory(dir);
                string dst = System.IO.Path.Combine(dir, System.IO.Path.GetFileName(path));
                System.IO.File.Copy(path, dst, overwrite: true);
            });
    }

    /* ================= ���� ================= */

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
