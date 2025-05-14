using UnityEngine;
using System;  // ��Ӵ�����֧��Action

[RequireComponent(typeof(CharacterController))]
public class GrappleController : MonoBehaviour
{
    [Header("ץ���ƶ�����")]
    [Tooltip("��������ƶ����ٶ�(m/s)")]
    public float defaultPullSpeed = 5f;
    [Tooltip("����Ŀ���ֹͣ����(m)")]
    public float stopDistance = 1f;

    [Header("��צ���ӻ����ⲿ�ɸ�ֵ��")]
    [Tooltip("ץ��ʵ��ģ��Prefab����UI����������")]
    public GameObject hookPrefab;
    [Tooltip("��צ�����ٶ�(m/s)")]
    public float hookTravelSpeed = 50f;
    [Tooltip("�������ʣ�����LineRenderer")]
    public Material ropeMaterial;

    // �ڲ�״̬
    CharacterController _cc;
    GameObject _currentHook;
    LineRenderer _ropeRenderer;
    bool _hookFlying;
    bool _isGrappling;
    Vector3 _hookStartPoint;
    Vector3 _hookTargetPoint;
    Vector3 _grapplePoint;
    float _pullSpeed;

    // ���һ���¼�����ץ������Ŀ���ʱ����
    public event Action onGrappleComplete;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        // ���� ��צ�׳��׶� ���� //
        if (_hookFlying)
        {
            // ��צ��Ŀ�����
            _currentHook.transform.position = Vector3.MoveTowards(
                _currentHook.transform.position,
                _hookTargetPoint,
                hookTravelSpeed * Time.deltaTime);

            // �����������˵�
            _ropeRenderer.SetPosition(0, _hookStartPoint);
            _ropeRenderer.SetPosition(1, _currentHook.transform.position);

            // ����Ƿ񵽴�Ŀ���л��������׶�
            if (Vector3.Distance(_currentHook.transform.position, _hookTargetPoint) < 0.1f)
            {
                _hookFlying = false;
                _isGrappling = true;

                // ����ץ������Ŀ����¼�
                onGrappleComplete?.Invoke();
            }
            return;
        }

        // ���� ������ҽ׶� ���� //
        if (_isGrappling)
        {
            Vector3 delta = _grapplePoint - transform.position;
            delta.y = 0; // ֻˮƽ��
            float dist = delta.magnitude;

            // �����㹻��ʱ����ץ��
            if (dist <= stopDistance)
            {
                EndGrapple();
                return;
            }

            // �ƶ����
            Vector3 move = delta.normalized * _pullSpeed * Time.deltaTime;
            _cc.Move(move);

            // ����������㣨����Ҹ߶ȣ�
            _ropeRenderer.SetPosition(0, transform.position + Vector3.up * _cc.height * 0.5f);
            // �յ㱣�ֹ�צλ��
            _ropeRenderer.SetPosition(1, _currentHook.transform.position);
        }
    }

    /// <summary>
    /// �ⲿ���ã����ù�צģ�͡��ٶȺ���������
    /// </summary>
    public void InitializeHook(GameObject hookPrefab, float hookSpeed, Material ropeMat)
    {
        this.hookPrefab = hookPrefab;
        this.hookTravelSpeed = hookSpeed;
        this.ropeMaterial = ropeMat;
    }

    /// <summary>
    /// �ⲿ���ã���ʼһ��ץ���߼�
    /// </summary>
    public void StartGrapple(Vector3 hitPoint, float pullSpeed)
    {
        _grapplePoint = hitPoint;
        _pullSpeed = pullSpeed > 0f ? pullSpeed : defaultPullSpeed;

        // ���㹳צ�����"��"λ�÷�����������
        _hookStartPoint = transform.position + Vector3.up * (_cc.height * 0.5f);

        // ʵ������צģ��
        if (hookPrefab != null)
        {
            _currentHook = Instantiate(hookPrefab, _hookStartPoint, Quaternion.identity);
        }
        else
        {
            // ��ûָ��ģ�ͣ���һ��С�����
            _currentHook = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _currentHook.transform.position = _hookStartPoint;
            _currentHook.transform.localScale = Vector3.one * 0.2f;
        }

        // ��������
        _ropeRenderer = _currentHook.AddComponent<LineRenderer>();
        _ropeRenderer.positionCount = 2;
        _ropeRenderer.material = ropeMaterial;
        _ropeRenderer.startWidth = 0.05f;
        _ropeRenderer.endWidth = 0.05f;

        // ��ʼ�׳��׶�
        _hookTargetPoint = hitPoint;
        _hookFlying = true;
        _isGrappling = false;
    }

    /// <summary>
    /// �ⲿ�ڲ����ɵ��ã�����ץ������
    /// </summary>
    public void EndGrapple()
    {
        _hookFlying = false;
        _isGrappling = false;
        if (_currentHook != null) Destroy(_currentHook);
        if (_ropeRenderer != null) Destroy(_ropeRenderer);

        // ����ץ������¼������������������
        // ע�⣺���������Ҫ�������������������жϣ�ȡ���ھ�������
        onGrappleComplete?.Invoke();
    }
}