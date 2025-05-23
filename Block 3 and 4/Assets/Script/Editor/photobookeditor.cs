using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 照片书编辑器工具 - 帮助设计师在编辑器中可视化地摆放照片位置
/// 放在 Editor 文件夹中
/// </summary>
[CustomEditor(typeof(EnhancedPhotoBookController))]
public class PhotoBookEditor : Editor
{
    private EnhancedPhotoBookController controller;
    private bool showPhotoPositions = true;
    private int selectedAnimalIndex = 0;
    private string[] animalNames;

    void OnEnable()
    {
        controller = (EnhancedPhotoBookController)target;
        UpdateAnimalNames();
    }

    void UpdateAnimalNames()
    {
        List<string> names = new List<string>();
        foreach (var page in controller.animalPages)
        {
            names.Add(page.animalName);
        }
        animalNames = names.ToArray();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("照片位置编辑器", EditorStyles.boldLabel);

        showPhotoPositions = EditorGUILayout.Foldout(showPhotoPositions, "照片位置设置");

        if (showPhotoPositions)
        {
            EditorGUI.indentLevel++;

            // 选择要编辑的动物
            selectedAnimalIndex = EditorGUILayout.Popup("选择动物", selectedAnimalIndex, animalNames);

            if (selectedAnimalIndex >= 0 && selectedAnimalIndex < controller.animalPages.Count)
            {
                var animalPage = controller.animalPages[selectedAnimalIndex];

                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"{animalPage.animalName} 的照片位置", EditorStyles.boldLabel);

                // 显示和编辑每个照片槽位的位置
                for (int i = 0; i < animalPage.photoSlots.Count; i++)
                {
                    if (animalPage.photoSlots[i] != null)
                    {
                        RectTransform rt = animalPage.photoSlots[i].GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"照片 {i + 1}:");

                            Vector2 newPos = EditorGUILayout.Vector2Field("", rt.anchoredPosition);
                            if (newPos != rt.anchoredPosition)
                            {
                                Undo.RecordObject(rt, "修改照片位置");
                                rt.anchoredPosition = newPos;
                                EditorUtility.SetDirty(rt);
                            }

                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }

                EditorGUILayout.Space();

                // 添加快速布局按钮
                EditorGUILayout.LabelField("快速布局", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("水平排列"))
                {
                    ArrangePhotosHorizontally(animalPage);
                }
                if (GUILayout.Button("垂直排列"))
                {
                    ArrangePhotosVertically(animalPage);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("网格布局"))
                {
                    ArrangePhotosInGrid(animalPage);
                }
                if (GUILayout.Button("圆形布局"))
                {
                    ArrangePhotosInCircle(animalPage);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // 工具按钮
        EditorGUILayout.LabelField("工具", EditorStyles.boldLabel);

        if (GUILayout.Button("创建所有照片槽位"))
        {
            CreateAllPhotoSlots();
        }

        if (GUILayout.Button("刷新动物列表"))
        {
            UpdateAnimalNames();
        }
    }

    void ArrangePhotosHorizontally(EnhancedPhotoBookController.AnimalPage page)
    {
        float spacing = 250f;
        float startX = -(page.photoSlots.Count - 1) * spacing / 2f;

        for (int i = 0; i < page.photoSlots.Count; i++)
        {
            if (page.photoSlots[i] != null)
            {
                RectTransform rt = page.photoSlots[i].GetComponent<RectTransform>();
                if (rt != null)
                {
                    Undo.RecordObject(rt, "水平排列照片");
                    rt.anchoredPosition = new Vector2(startX + i * spacing, 0);
                    EditorUtility.SetDirty(rt);
                }
            }
        }
    }

    void ArrangePhotosVertically(EnhancedPhotoBookController.AnimalPage page)
    {
        float spacing = 180f;
        float startY = (page.photoSlots.Count - 1) * spacing / 2f;

        for (int i = 0; i < page.photoSlots.Count; i++)
        {
            if (page.photoSlots[i] != null)
            {
                RectTransform rt = page.photoSlots[i].GetComponent<RectTransform>();
                if (rt != null)
                {
                    Undo.RecordObject(rt, "垂直排列照片");
                    rt.anchoredPosition = new Vector2(0, startY - i * spacing);
                    EditorUtility.SetDirty(rt);
                }
            }
        }
    }

    void ArrangePhotosInGrid(EnhancedPhotoBookController.AnimalPage page)
    {
        int cols = 3;
        float spacingX = 250f;
        float spacingY = 180f;

        for (int i = 0; i < page.photoSlots.Count; i++)
        {
            if (page.photoSlots[i] != null)
            {
                RectTransform rt = page.photoSlots[i].GetComponent<RectTransform>();
                if (rt != null)
                {
                    int row = i / cols;
                    int col = i % cols;

                    float x = (col - 1) * spacingX;
                    float y = -row * spacingY + 100f;

                    Undo.RecordObject(rt, "网格排列照片");
                    rt.anchoredPosition = new Vector2(x, y);
                    EditorUtility.SetDirty(rt);
                }
            }
        }
    }

    void ArrangePhotosInCircle(EnhancedPhotoBookController.AnimalPage page)
    {
        float radius = 200f;
        float angleStep = 360f / page.photoSlots.Count;

        for (int i = 0; i < page.photoSlots.Count; i++)
        {
            if (page.photoSlots[i] != null)
            {
                RectTransform rt = page.photoSlots[i].GetComponent<RectTransform>();
                if (rt != null)
                {
                    float angle = i * angleStep * Mathf.Deg2Rad;
                    float x = Mathf.Cos(angle) * radius;
                    float y = Mathf.Sin(angle) * radius;

                    Undo.RecordObject(rt, "圆形排列照片");
                    rt.anchoredPosition = new Vector2(x, y);
                    EditorUtility.SetDirty(rt);
                }
            }
        }
    }

    void CreateAllPhotoSlots()
    {
        controller.InitializeAnimalPages();
        EditorUtility.SetDirty(controller);
    }
}

/// <summary>
/// 照片槽位预制体创建工具
/// </summary>
public class PhotoSlotCreator : EditorWindow
{
    private Sprite borderSprite;
    private Color borderColor = Color.white;
    private Vector2 slotSize = new Vector2(200, 150);

    [MenuItem("Tools/Photo Book/创建照片槽位预制体")]
    static void CreateWindow()
    {
        PhotoSlotCreator window = GetWindow<PhotoSlotCreator>("照片槽位创建器");
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("照片槽位预制体设置", EditorStyles.boldLabel);

        borderSprite = (Sprite)EditorGUILayout.ObjectField("边框图片", borderSprite, typeof(Sprite), false);
        borderColor = EditorGUILayout.ColorField("边框颜色", borderColor);
        slotSize = EditorGUILayout.Vector2Field("槽位大小", slotSize);

        EditorGUILayout.Space();

        if (GUILayout.Button("创建预制体"))
        {
            CreatePhotoSlotPrefab();
        }
    }

    void CreatePhotoSlotPrefab()
    {
        // 创建根对象
        GameObject slotRoot = new GameObject("PhotoSlot");
        RectTransform rootRT = slotRoot.AddComponent<RectTransform>();
        rootRT.sizeDelta = slotSize;

        // 创建照片显示对象
        GameObject photoObj = new GameObject("Photo");
        photoObj.transform.SetParent(slotRoot.transform, false);

        RectTransform photoRT = photoObj.AddComponent<RectTransform>();
        photoRT.anchorMin = Vector2.zero;
        photoRT.anchorMax = Vector2.one;
        photoRT.sizeDelta = Vector2.zero;
        photoRT.anchoredPosition = Vector2.zero;

        Image photoImage = photoObj.AddComponent<Image>();
        photoImage.preserveAspect = true;

        // 创建边框对象
        if (borderSprite != null)
        {
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(slotRoot.transform, false);

            RectTransform borderRT = borderObj.AddComponent<RectTransform>();
            borderRT.anchorMin = Vector2.zero;
            borderRT.anchorMax = Vector2.one;
            borderRT.sizeDelta = new Vector2(10, 10); // 边框稍微大一点
            borderRT.anchoredPosition = Vector2.zero;

            Image borderImage = borderObj.AddComponent<Image>();
            borderImage.sprite = borderSprite;
            borderImage.color = borderColor;
            borderImage.type = Image.Type.Sliced;
        }

        // 保存为预制体
        string path = EditorUtility.SaveFilePanelInProject(
            "保存照片槽位预制体",
            "PhotoSlot",
            "prefab",
            "选择保存位置"
        );

        if (!string.IsNullOrEmpty(path))
        {
            PrefabUtility.SaveAsPrefabAsset(slotRoot, path);
            DestroyImmediate(slotRoot);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("成功", "照片槽位预制体创建成功！", "确定");
        }
        else
        {
            DestroyImmediate(slotRoot);
        }
    }
}