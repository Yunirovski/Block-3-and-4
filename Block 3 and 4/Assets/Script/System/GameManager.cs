// Assets/Scripts/Systems/GameManager.cs
using UnityEngine;

/// <summary>
/// 负责管理游戏系统的初始化顺序，确保单例的正确加载。
/// 将此脚本放在场景中最先加载的对象上。
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();

                if (_instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                    Debug.Log("GameManager: 自动创建实例");
                }
            }
            return _instance;
        }
    }

    [Header("Manager Prefabs")]
    [Tooltip("进度管理器预制体")]
    public GameObject progressionManagerPrefab;
    [Tooltip("照片检测器预制体")]
    public GameObject photoDetectorPrefab;
    [Tooltip("照片收集管理器预制体")]
    public GameObject photoCollectionManagerPrefab;

    [Header("Debug")]
    [Tooltip("输出详细日志")]
    public bool verboseLogging = true;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.Log($"GameManager: 发现重复实例，禁用此组件 {gameObject.name}");
            this.enabled = false;
            return;
        }

        _instance = this;

        if (verboseLogging) Debug.Log("GameManager: 初始化开始");

        // 确保不被销毁
        DontDestroyOnLoad(gameObject);

        // 初始化关键管理器
        InitializeManagers();
    }

    private void InitializeManagers()
    {
        // 按依赖顺序初始化

        // 1. ProgressionManager
        InitializeManager<ProgressionManager>(progressionManagerPrefab, "ProgressionManager");

        // 2. PhotoDetector
        InitializeManager<PhotoDetector>(photoDetectorPrefab, "PhotoDetector");

        // 3. PhotoCollectionManager
        InitializeManager<PhotoCollectionManager>(photoCollectionManagerPrefab, "PhotoCollectionManager");

        if (verboseLogging) Debug.Log("GameManager: 所有管理器初始化完成");
    }

    private T InitializeManager<T>(GameObject prefab, string managerName) where T : Component
    {
        // 检查是否已存在
        T manager = FindObjectOfType<T>();

        if (manager != null)
        {
            if (verboseLogging) Debug.Log($"GameManager: {managerName}已存在，跳过创建");
            return manager;
        }

        // 创建新实例
        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab);
            instance.name = managerName;
            manager = instance.GetComponent<T>();

            if (manager == null)
            {
                manager = instance.AddComponent<T>();
            }

            if (verboseLogging) Debug.Log($"GameManager: 使用预制体创建了{managerName}实例");
        }
        else
        {
            // 如果没有预制体，直接创建空对象并添加组件
            GameObject newObject = new GameObject(managerName);
            manager = newObject.AddComponent<T>();
            if (verboseLogging) Debug.Log($"GameManager: 创建了空的{managerName}实例");
        }

        return manager;
    }

    // 用于在运行时检查和恢复单例
    private void Update()
    {
        // 检查关键单例是否存在，如果不存在则恢复
        if (ProgressionManager.Instance == null)
        {
            Debug.LogWarning("GameManager: ProgressionManager实例丢失，正在恢复");
            InitializeManager<ProgressionManager>(progressionManagerPrefab, "ProgressionManager");
        }

        if (PhotoDetector.Instance == null)
        {
            Debug.LogWarning("GameManager: PhotoDetector实例丢失，正在恢复");
            InitializeManager<PhotoDetector>(photoDetectorPrefab, "PhotoDetector");
        }

        if (PhotoCollectionManager.Instance == null)
        {
            Debug.LogWarning("GameManager: PhotoCollectionManager实例丢失，正在恢复");
            InitializeManager<PhotoCollectionManager>(photoCollectionManagerPrefab, "PhotoCollectionManager");
        }
    }
}