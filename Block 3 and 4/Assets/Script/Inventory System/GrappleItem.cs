using UnityEngine;

[CreateAssetMenu(menuName = "Items/GrappleItem")]
public class GrappleItem : BaseItem
{
    [Header("ץ������")]
    public float maxDistance = 20f;
    public float pullSpeed = 5f;

    [Header("��צ���ӻ�")]
    [Tooltip("ץ��ģ�� Prefab���������ṩ")]
    public GameObject hookPrefab;
    [Tooltip("��צ�����ٶ� (m/s)")]
    public float hookTravelSpeed = 50f;
    [Tooltip("�������ʣ����� LineRenderer")]
    public Material ropeMaterial;

    // ����ʱ����
    Camera _cam;
    GrappleController _grappler;

    public override void OnSelect(GameObject model)
    {
        _cam = Camera.main;
        if (_cam == null) { Debug.LogError("�Ҳ��������"); return; }

        // ���� GrappleController ��������ĸ������ϣ�������ϣ�
        _grappler = _cam.GetComponentInParent<GrappleController>();
        if (_grappler == null)
        {
            Debug.LogError("���������ȱ�� GrappleController ���");
            return;
        }

        // ע�빳צ���ӻ���Դ
        _grappler.InitializeHook(hookPrefab, hookTravelSpeed, ropeMaterial);
    }

    public override void OnUse()
    {
        if (_grappler == null || _cam == null) return;

        Ray ray = _cam.ScreenPointToRay(
            new Vector3(Screen.width / 2f, Screen.height / 2f)
        );
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            if (hit.collider.gameObject.isStatic)
            {
                _grappler.StartGrapple(hit.point, pullSpeed);
            }
            else
            {
                Debug.Log("����Ŀ��Ǿ�̬�����ɸ���");
            }
        }
        else
        {
            Debug.Log("�����δ�����κα���");
        }
    }
}
