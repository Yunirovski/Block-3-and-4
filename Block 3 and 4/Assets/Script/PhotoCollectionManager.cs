using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PhotoCollectionManager : MonoBehaviour
{
    public static PhotoCollectionManager Instance;

    // 字典：Key 为动物名称，Value 为该动物对应的照片文件路径列表
    private Dictionary<string, List<string>> photoCollections = new Dictionary<string, List<string>>();

    [SerializeField] private TMP_Text collectionText; // 用于在 UI 中显示归类信息

    private void Awake()
    {
        // 实现单例模式，方便其他脚本调用
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 添加照片到指定动物的集合中
    /// </summary>
    /// <param name="animalName">动物名称（分类依据）</param>
    /// <param name="photoPath">照片文件的路径</param>
    public void AddPhoto(string animalName, string photoPath)
    {
        if (!photoCollections.ContainsKey(animalName))
        {
            photoCollections[animalName] = new List<string>();
        }
        photoCollections[animalName].Add(photoPath);
        UpdateCollectionText();
    }

    /// <summary>
    /// 更新 UI 显示，每个动物的照片数量
    /// </summary>
    private void UpdateCollectionText()
    {
        string text = "Photo Collections:\n";
        foreach (var kvp in photoCollections)
        {
            text += $"{kvp.Key}: {kvp.Value.Count} photos\n";
        }
        collectionText.text = text;
    }
}