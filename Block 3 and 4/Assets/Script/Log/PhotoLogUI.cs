using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhotoLogUI : MonoBehaviour
{
    /* --------- Inspector 绑定 --------- */

    [Header("Animal List")]
    [SerializeField] Transform animalButtonParent;    // ScrollView/Content
    [SerializeField] GameObject animalButtonPrefab;   // Button 预制体

    [Header("Info & Thumbs")]
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descText;
    [SerializeField] Image[] thumbSlots;              // 8 个 Image+Button，顺序随意

    [Header("Popup")]
    [SerializeField] PhotoPopup popupPrefab;          // 下方第二份脚本生成的 prefab
    [SerializeField] Transform popupRoot;             // 建议在 LogCanvas 下空物体

    /* --------- 运行时 --------- */

    string currentAnimalId;

    /* ==================== 生命周期 ==================== */

    void OnEnable()
    {
        BuildAnimalList();
        PhotoLibrary.Instance.OnPhotoDatabaseChanged += RefreshCurrentAnimal;
    }

    void OnDisable()
    {
        PhotoLibrary.Instance.OnPhotoDatabaseChanged -= RefreshCurrentAnimal;
    }

    /* ================== 构建左侧动物按钮 ================== */

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

        // 自动点击第一个（如果有）
        if (animalButtonParent.childCount > 0)
            animalButtonParent.GetChild(0).GetComponent<Button>().onClick.Invoke();
    }

    /* ================== 刷新右侧缩略图 ================== */

    void RefreshCurrentAnimal()
    {
        if (!string.IsNullOrEmpty(currentAnimalId))
            RefreshAnimal(currentAnimalId);
    }

    void RefreshAnimal(string animalId)
    {
        IReadOnlyList<PhotoLibrary.PhotoEntry> photos =
            PhotoLibrary.Instance.GetPhotos(animalId);

        // 填缩略图
        for (int i = 0; i < thumbSlots.Length; i++)
        {
            Image img = thumbSlots[i];

            if (i < photos.Count)
            {
                var entry = photos[i];
                img.sprite = PhotoLibrary.Instance.GetThumbnail(entry.path);
                img.color = Color.white;

                int localIdx = i;   // lambda 捕获
                img.GetComponent<Button>().onClick.RemoveAllListeners();
                img.GetComponent<Button>().onClick.AddListener(() =>
                {
                    ShowPopup(animalId, localIdx, entry.path);
                });
            }
            else
            {
                img.sprite = null;
                img.color = new Color(1, 1, 1, 0);   // 透明隐藏
                img.GetComponent<Button>().onClick.RemoveAllListeners();
            }
        }

        // 名称 / 描述
        nameText.text = GetDisplayName(animalId);
        descText.text = GetDescription(animalId);
    }

    /* ================== 弹出窗口 ================== */

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

    /* ================== 工具 ================== */

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
