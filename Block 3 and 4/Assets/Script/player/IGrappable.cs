// Assets/Scripts/Player/IGrappable.cs
using UnityEngine;

/// <summary>
/// �ɹ�ס����ӿ�
/// </summary>
public interface IGrappable
{
    /// <summary>
    /// �Ƿ���Ա���ס
    /// </summary>
    bool CanBeGrappled();

    /// <summary>
    /// ��ȡ��ѹ�ס��
    /// </summary>
    Vector3 GetBestGrapplePoint(Vector3 hookPosition);

    /// <summary>
    /// ��צ����ʱ����
    /// </summary>
    void OnGrappleAttach(Vector3 attachPoint);

    /// <summary>
    /// ��צ����ʱ����
    /// </summary>
    void OnGrappleDetach();

    /// <summary>
    /// ��ȡ����ǿ�ȣ�Ӱ�칳צ�Ƿ�����䣩
    /// </summary>
    float GetSurfaceStrength();
}