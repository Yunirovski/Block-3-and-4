// Assets/Scripts/Items/DartGunItem.cs
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/DartGunItem")]
public class DartGunItem : BaseItem
{
    [Header("����ǹ����")]
    [Tooltip("������Զ���� (m)")]
    public float maxDistance = 20f;
    [Tooltip("�������ȴʱ�� (s)")]
    public float cooldown = 3f;
    [Tooltip("���ж������Գ���ʱ�� (s)")]
    public float stunDuration = 10f;

    [Header("���߿��ӻ�")]
    [Tooltip("���߲��ʣ����� LineRenderer��")]
    public Material rayMaterial;
    [Tooltip("���߿��")]
    public float rayWidth = 0.02f;
    [Tooltip("������ʾʱ�� (s)")]
    public float rayDuration = 0.1f;

    // ���� ����ʱ״̬ ���� 
    float nextReadyTime = 0f;
    Camera cam;
    Transform playerRoot;

    public override void OnSelect(GameObject model)
    {
        // �������������Ҹ��ڵ����ں�������
        cam = Camera.main;
        if (cam == null)
            Debug.LogError("DartGunItem: �Ҳ��������");

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerRoot = player.transform;
    }

    public override void OnUse()
    {
        Debug.Log("DartGunItem: ���Է��䣡");  // �������

        if (cam == null) return;

        // ��ȴ���
        if (Time.time < nextReadyTime)
        {
            Debug.Log($"DartGunItem: ��ȴ�� {(nextReadyTime - Time.time):F1}s");
            return;
        }

        Vector3 origin = cam.transform.position;
        Vector3 dir = cam.transform.forward;

        // �� RaycastAll �ռ�������ײ��������������
        var hits = Physics.RaycastAll(origin, dir, maxDistance)
                          .OrderBy(h => h.distance);

        RaycastHit? validHit = null;
        foreach (var h in hits)
        {
            // ��������̫�������������������������Ҳ㼶����ײ��
            if (h.distance < 0.1f) continue;
            if (playerRoot != null && h.collider.transform.IsChildOf(playerRoot)) continue;

            validHit = h;
            break;
        }

        Vector3 rayEnd;
        if (validHit.HasValue)
        {
            var hit = validHit.Value;
            rayEnd = hit.point;

            // ������ж������ѣ
            var animal = hit.collider.GetComponent<AnimalBehavior>();
            if (animal != null)
            {
                animal.Stun(stunDuration);
                Debug.Log($"DartGunItem: ���� {animal.gameObject.name}������ {stunDuration}s");
            }
            else
            {
                Debug.Log("DartGunItem: δ�����κο���ѣ�Ķ���");
            }
        }
        else
        {
            // δ���У��������뷽����ʾ����
            rayEnd = origin + dir * maxDistance;
            Debug.Log("DartGunItem: �������Ŀ��");
        }

        // �������ߣ����ӻ���
        if (rayMaterial != null)
            ShowLine(origin, rayEnd);
        else
            Debug.DrawLine(origin, rayEnd, Color.cyan, rayDuration);

        // ��¼��ȴ��֪ͨ UI
        nextReadyTime = Time.time + cooldown;
        InventorySystemEvents.OnItemCooldownStart?.Invoke(this, cooldown);
    }

    void ShowLine(Vector3 start, Vector3 end)
    {
        GameObject go = new GameObject("DartRay");
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.material = rayMaterial;
        lr.startWidth = rayWidth;
        lr.endWidth = rayWidth;
        lr.useWorldSpace = true;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        Destroy(go, rayDuration);
    }
}
