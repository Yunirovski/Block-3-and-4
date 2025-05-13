using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhotoLogUI : MonoBehaviour
{
    /* ---------- Inspector 绑定 ---------- */

    [Header("Animal List")]
    [SerializeField] Transform animalButtonParent;       // ScrollView/Content
    [SerializeField] GameObject animalButtonPrefab;      // Button 预制体

    [Header("Info & Thumbs")]
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descText;
    [SerializeField] Image[] thumbSlots;                 // 8 个 Image+Button

    [Header("Popup")]
    [SerializeField] PhotoPopup popupPrefab;
    [SerializeField] Transform popupRoot;

    /* ---------- 运行时 ---------- */

    string currentAnimalId;

    /* ================= 生命周期 ================= */

    void OnEnable()
    {
        BuildAnimalList();
        PhotoLibrary.Instance.OnPhotoDatabaseChanged += RefreshCurrentAnimal;
    }

    void OnDisable()
    {
        PhotoLibrary.Instance.OnPhotoDatabaseChanged -= RefreshCurrentAnimal;
    }

    /* ================= 左侧动物列表 ================= */

    void BuildAnimalList()
    {
        foreach (Transform c in animalButtonParent) Destroy(c.gameObject);

        foreach (string id in PhotoLibrary.Instance.GetAnimalIds())
        {
            GameObject go = Instantiate(animalButtonPrefab, animalButtonParent);

            // -------- Button 组件：根或子物体均可 --------
            Button uiButton = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>();
            if (uiButton == null)
            {
                Debug.LogError($"AnimalButton prefab 缺少 Button 组件: {animalButtonPrefab.name}");
                continue;
            }

            // -------- 设置文本 --------
            TMP_Text txt = go.GetComponentInChildren<TMP_Text>(true);
            if (txt != null) txt.text = GetDisplayName(id);

            // -------- 绑定点击 --------
            uiButton.onClick.AddListener(() =>
            {
                currentAnimalId = id;
                RefreshAnimal(id);
            });
        }

        // 自动选第一个
        if (animalButtonParent.childCount > 0)
            animalButtonParent.GetChild(0).GetComponent<Button>().onClick.Invoke();
    }

    /* ================= 右侧照片格子 ================= */

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

                int localIndex = i;   // lambda 捕获
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

    /* ================= 弹窗 ================= */

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

    /* ================= 工具 ================= */

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
