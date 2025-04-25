using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// �̵�ϵͳ���� B �򿪣���ͣ��Ϸ���г� catalog �е���Ʒ��
/// ֧�ֹ��� Consumable������/ʳ��� Equipment��ע�ᵽ��3ѭ������
/// ������ʱ��� CurrencyManager �Ļص���
/// </summary>
public class ShopManager : MonoBehaviour
{
    // ���� ����ʵ�� ���� 
    public static ShopManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject shopRoot;        // ShopPanel ����Start ʱ���� inactive
    public Transform contentHolder;    // ScrollView/Viewport/Content
    public GameObject itemEntryPrefab; // Button+Text ��Ԥ��
    public TMP_Text quotaText;         // ��ʾ��ǰ����

    [Header("Catalog Data")]
    public ShopItemData[] catalog;     // �� Inspector �������� ShopItemData

    private void Awake()
    {
        // ������ʼ��
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        BuildUI();
        UpdateQuotaLabel();

        // ���� CurrencyManager �ص�
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged += UpdateQuotaLabel;
    }

    private void OnDestroy()
    {
        // ����¼�
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged -= UpdateQuotaLabel;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
            ToggleShop();
    }

    /// <summary>
    /// ���������б�İ�ť��Ŀ��ִֻ��һ�Ρ�
    /// </summary>
    private void BuildUI()
    {
        foreach (var data in catalog)
        {
            GameObject go = Instantiate(itemEntryPrefab, contentHolder);
            var entry = go.GetComponent<ShopEntryUI>();
            entry.Init(data);
        }
    }

    /// <summary>
    /// ��/�ر��̵���壬����ͣ/�ָ���Ϸʱ�������״̬��
    /// ��ʱˢ��������Ŀ��״̬��
    /// </summary>
    private void ToggleShop()
    {
        bool active = !shopRoot.activeSelf;
        shopRoot.SetActive(active);
        Time.timeScale = active ? 0f : 1f;
        Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = active;

        if (active)
        {
            // ��ʱ����ˢ�¿ɹ���״̬
            foreach (Transform t in contentHolder)
            {
                var entry = t.GetComponent<ShopEntryUI>();
                if (entry != null)
                    entry.Refresh();
            }
        }
    }

    /// <summary>
    /// ���¶������� �ı���ʾ��
    /// </summary>
    private void UpdateQuotaLabel()
    {
        if (quotaText != null)
            quotaText.text = $"Quota��: {CurrencyManager.Instance.QuotaStar}";
    }

    /// <summary>
    /// �� ShopEntryUI ���õĹ������߼���
    /// </summary>
    public void TryBuy(ShopItemData data)
    {
        if (CurrencyManager.Instance.TotalStar < data.unlockNeedTotal)
        {
            Debug.Log($"δ�����ղء����� ({data.unlockNeedTotal})");
            return;
        }
        if (!CurrencyManager.Instance.TrySpendStars(data.priceQuota))
        {
            Debug.Log("���ﲻ�㣡");
            return;
        }

        switch (data.type)
        {
            case ShopItemType.Consumable:
                if (data.itemName.Contains("Film"))
                    ConsumableManager.Instance.AddFilm(data.amount);
                else
                    ConsumableManager.Instance.AddFood(data.amount);
                break;

            case ShopItemType.Equipment:
                InventoryCycler.RegisterItem(data.linkedItem);
                Debug.Log($"�ѹ���װ�� {data.itemName}������ Q/E �л���");
                break;
        }
    }
}
