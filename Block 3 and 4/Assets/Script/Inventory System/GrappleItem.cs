using UnityEngine;

[CreateAssetMenu(menuName = "Items/GrappleItem")]
public class GrappleItem : BaseItem
{
    [Header("Grapple Params")]
    public float maxDistance = 30f;   // ���������
    public float pullSpeed = 15f;   // �����ٶ�
    public float cooldown = 4f;    // ��ȴ

    private Transform player;         // ��� Transform
    private float nextReadyTime;

    public override void OnSelect(GameObject model)
    {
        player = Camera.main.transform.root;     // ��ȡ��Ҹ�
    }

    public override void OnUse()
    {
        // ��ȴ���
        if (Time.time < nextReadyTime) return;

        // ���ߣ�ע�� origin �� position�����Ĳ����� maxDistance(float)
        if (Physics.Raycast(Camera.main.transform.position,
                            Camera.main.transform.forward,
                            out RaycastHit hit,
                            maxDistance))
        {
            // ��������ϼ�һ�� GrappleMover���������������Ŀ���
            var mover = player.gameObject.AddComponent<GrappleMover>();
            mover.Init(hit.point, pullSpeed);

            // ��ȴ��ʱ & ֪ͨ HUD
            nextReadyTime = Time.time + cooldown;
            InventorySystemEvents.OnItemCooldownStart?.Invoke(this, cooldown);
        }
    }
}

/// <summary>
/// ����ʱ��������ϵ�С���������ҳ�Ŀ����������ִ���Զ������Լ�
/// </summary>
public class GrappleMover : MonoBehaviour
{
    private Vector3 target;
    private float speed;

    public void Init(Vector3 point, float pullSpeed)
    {
        target = point;
        speed = pullSpeed;
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 1f)
            Destroy(this);     // ���㼴����
    }
}
