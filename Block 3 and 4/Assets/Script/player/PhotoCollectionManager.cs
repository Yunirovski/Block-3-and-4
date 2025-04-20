using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PhotoCollectionManager : MonoBehaviour
{
    public static PhotoCollectionManager Instance;

    // �ֵ䣺Key Ϊ�������ƣ�Value Ϊ�ö����Ӧ����Ƭ�ļ�·���б�
    private Dictionary<string, List<string>> photoCollections = new Dictionary<string, List<string>>();

    [SerializeField] private TMP_Text collectionText; // ������ UI ����ʾ������Ϣ

    private void Awake()
    {
        // ʵ�ֵ���ģʽ�����������ű�����
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
    /// �����Ƭ��ָ������ļ�����
    /// </summary>
    /// <param name="animalName">�������ƣ��������ݣ�</param>
    /// <param name="photoPath">��Ƭ�ļ���·��</param>
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
    /// ���� UI ��ʾ��ÿ���������Ƭ����
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