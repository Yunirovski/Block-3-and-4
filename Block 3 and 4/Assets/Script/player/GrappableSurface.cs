// Assets/Scripts/Player/GrappableSurface.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// �ɹ�ס������� - ���ӵ����Ա���צ��ס��������
/// </summary>
public class GrappableSurface : MonoBehaviour, IGrappable
{
    [Header("��ס����")]
    [Tooltip("�Ƿ���Ա���ס")]
    public bool canBeGrappled = true;

    [Tooltip("����ǿ�ȣ�Խ��Խ�����䣩")]
    [Range(0.1f, 10f)]
    public float surfaceStrength = 1f;

    [Tooltip("��������")]
    public SurfaceType surfaceType = SurfaceType.Rock;

    [Header("��ס������")]
    [Tooltip("Ԥ����Ĺ�ס�㣨Ϊ�����Զ����㣩")]
    public Transform[] predefinedGrapplePoints;

    [Tooltip("��Ե��⾫��")]
    [Range(0.1f, 2f)]
    public float edgeDetectionPrecision = 0.5f;

    [Tooltip("��С��ס�߶�")]
    public float minimumGrappleHeight = 1f;

    [Header("�Ӿ�����")]
    [Tooltip("��צ�ӽ�ʱ�ĸ�������")]
    public Material highlightMaterial;

    [Tooltip("����סʱ����Ч")]
    public GameObject attachedEffect;

    [Header("��Ч")]
    [Tooltip("����סʱ����Ч")]
    public AudioClip attachSound;

    [Tooltip("��צ����ʱ����Ч")]
    public AudioClip detachSound;

    // ����ʱ״̬
    private bool isCurrentlyGrappled = false;
    private Vector3 lastAttachPoint;
    private GameObject currentEffect;
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private AudioSource audioSource;

    [System.Serializable]
    public enum SurfaceType
    {
        Rock,      // ��ʯ - �ǳ��ȹ�
        Metal,     // ���� - �ȹ̵�����������
        Wood,      // ľͷ - �е�ǿ��
        Concrete,  // ������ - �ȹ�
        Ice,       // �� - ���׻���
        Fabric,    // ���� - ���ȶ�
        Glass      // ���� - ��������������
    }

    void Start()
    {
        // ��ȡ��Ⱦ�����
        renderers = GetComponentsInChildren<Renderer>();

        // ����ԭʼ����
        List<Material> origMats = new List<Material>();
        foreach (var renderer in renderers)
        {
            if (renderer != null && renderer.materials != null)
            {
                origMats.AddRange(renderer.materials);
            }
        }
        originalMaterials = origMats.ToArray();

        // ��ȡ�򴴽���ƵԴ
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D��Ч
            audioSource.playOnAwake = false;
        }

        // ���ݱ������͵���Ĭ��ǿ��
        AdjustStrengthBySurfaceType();
    }

    private void AdjustStrengthBySurfaceType()
    {
        switch (surfaceType)
        {
            case SurfaceType.Rock:
                surfaceStrength = Mathf.Max(surfaceStrength, 2f);
                break;
            case SurfaceType.Metal:
                surfaceStrength = Mathf.Max(surfaceStrength, 1.8f);
                break;
            case SurfaceType.Concrete:
                surfaceStrength = Mathf.Max(surfaceStrength, 1.5f);
                break;
            case SurfaceType.Wood:
                surfaceStrength = Mathf.Max(surfaceStrength, 1f);
                break;
            case SurfaceType.Ice:
                surfaceStrength = Mathf.Min(surfaceStrength, 0.5f);
                break;
            case SurfaceType.Fabric:
                surfaceStrength = Mathf.Min(surfaceStrength, 0.3f);
                break;
            case SurfaceType.Glass:
                surfaceStrength = Mathf.Min(surfaceStrength, 0.2f);
                break;
        }
    }

    public bool CanBeGrappled()
    {
        return canBeGrappled && gameObject.activeInHierarchy;
    }

    public Vector3 GetBestGrapplePoint(Vector3 hookPosition)
    {
        // ����ʹ��Ԥ����Ĺ�ס��
        if (predefinedGrapplePoints != null && predefinedGrapplePoints.Length > 0)
        {
            Vector3 bestPoint = predefinedGrapplePoints[0].position;
            float bestDistance = Vector3.Distance(hookPosition, bestPoint);

            foreach (Transform point in predefinedGrapplePoints)
            {
                if (point == null) continue;

                float distance = Vector3.Distance(hookPosition, point.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPoint = point.position;
                }
            }

            return bestPoint;
        }

        // �Զ�Ѱ����ѹ�ס��
        return FindBestEdgePoint(hookPosition);
    }

    private Vector3 FindBestEdgePoint(Vector3 hookPosition)
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return transform.position;

        Vector3 closestPoint = col.ClosestPoint(hookPosition);
        Vector3 boundsCenter = col.bounds.center;
        Vector3 boundsSize = col.bounds.size;

        // �����ҵ���Ե�����
        Vector3[] candidatePoints = {
            boundsCenter + new Vector3(boundsSize.x/2, boundsSize.y/2, boundsSize.z/2),   // ����ǰ
            boundsCenter + new Vector3(-boundsSize.x/2, boundsSize.y/2, boundsSize.z/2),  // ����ǰ
            boundsCenter + new Vector3(boundsSize.x/2, boundsSize.y/2, -boundsSize.z/2),  // ���Ϻ�
            boundsCenter + new Vector3(-boundsSize.x/2, boundsSize.y/2, -boundsSize.z/2), // ���Ϻ�
            boundsCenter + new Vector3(0, boundsSize.y/2, 0),                             // ��������
        };

        Vector3 bestPoint = closestPoint;
        float bestScore = CalculateGrappleScore(closestPoint, hookPosition);

        foreach (Vector3 candidate in candidatePoints)
        {
            // ȷ���������������
            Vector3 surfacePoint = col.ClosestPoint(candidate);
            float score = CalculateGrappleScore(surfacePoint, hookPosition);

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = surfacePoint;
            }
        }

        return bestPoint;
    }

    private float CalculateGrappleScore(Vector3 grapplePoint, Vector3 hookPosition)
    {
        float score = 0f;

        // �߶ȼӷ֣�Խ��Խ�ã�
        score += grapplePoint.y * 0.5f;

        // ����۷֣�ԽԶԽ���ã�
        float distance = Vector3.Distance(grapplePoint, hookPosition);
        score -= distance * 0.1f;

        // ��Ե�ӷ�
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Vector3 centerToPoint = grapplePoint - col.bounds.center;
            float edgeDistance = centerToPoint.magnitude / (col.bounds.size.magnitude * 0.5f);
            score += edgeDistance * 2f; // Խ������ԵԽ��
        }

        return score;
    }

    public void OnGrappleAttach(Vector3 attachPoint)
    {
        isCurrentlyGrappled = true;
        lastAttachPoint = attachPoint;

        // ���Ÿ�����Ч
        if (attachSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attachSound);
        }

        // ����������Ч
        if (attachedEffect != null)
        {
            currentEffect = Instantiate(attachedEffect, attachPoint, Quaternion.identity);
            currentEffect.transform.SetParent(transform);
        }

        // Ӧ�ø�������
        ApplyHighlight(true);

        Debug.Log($"��צ���ŵ� {gameObject.name} �� {surfaceType} ���棬ǿ��: {surfaceStrength}");
    }

    public void OnGrappleDetach()
    {
        isCurrentlyGrappled = false;

        // ����������Ч
        if (detachSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(detachSound);
        }

        // �Ƴ���Ч
        if (currentEffect != null)
        {
            Destroy(currentEffect);
            currentEffect = null;
        }

        // �Ƴ�����
        ApplyHighlight(false);

        Debug.Log($"��צ�� {gameObject.name} ����");
    }

    public float GetSurfaceStrength()
    {
        return surfaceStrength;
    }

    private void ApplyHighlight(bool highlight)
    {
        if (highlightMaterial == null || renderers == null) return;

        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;

            if (highlight)
            {
                // Ӧ�ø�������
                Material[] highlightMats = new Material[renderer.materials.Length];
                for (int i = 0; i < highlightMats.Length; i++)
                {
                    highlightMats[i] = highlightMaterial;
                }
                renderer.materials = highlightMats;
            }
            else if (originalMaterials != null && originalMaterials.Length > 0)
            {
                // �ָ�ԭʼ����
                Material[] originalMats = new Material[renderer.materials.Length];
                int materialIndex = 0;
                for (int i = 0; i < originalMats.Length && materialIndex < originalMaterials.Length; i++)
                {
                    originalMats[i] = originalMaterials[materialIndex];
                    materialIndex++;
                }
                renderer.materials = originalMats;
            }
        }
    }

    public Color GetSurfaceTypeColor()
    {
        switch (surfaceType)
        {
            case SurfaceType.Rock: return Color.gray;
            case SurfaceType.Metal: return Color.cyan;
            case SurfaceType.Wood: return new Color(0.6f, 0.3f, 0.1f);
            case SurfaceType.Concrete: return Color.white;
            case SurfaceType.Ice: return Color.blue;
            case SurfaceType.Fabric: return Color.magenta;
            case SurfaceType.Glass: return Color.clear;
            default: return Color.yellow;
        }
    }

    // ���ӻ�����
    void OnDrawGizmos()
    {
        if (!canBeGrappled) return;

        // ���ƿɹ�ס����
        Gizmos.color = isCurrentlyGrappled ? Color.green : Color.yellow;

        if (predefinedGrapplePoints != null)
        {
            foreach (Transform point in predefinedGrapplePoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.3f);
                }
            }
        }

        // ���Ʊ�������ͼ��
        Gizmos.color = GetSurfaceTypeColor();
        Vector3 center = GetComponent<Collider>()?.bounds.center ?? transform.position;
        Gizmos.DrawWireCube(center, Vector3.one * 0.5f);
    }

    // �༭����������
    [ContextMenu("Auto Setup Grapple Points")]
    private void AutoSetupGrapplePoints()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        // ������ס��ĸ�����
        GameObject pointsParent = new GameObject("GrapplePoints");
        pointsParent.transform.SetParent(transform);
        pointsParent.transform.localPosition = Vector3.zero;

        Vector3 boundsCenter = col.bounds.center;
        Vector3 boundsSize = col.bounds.size;

        // ��������ĸ��ϽǴ�����ס��
        Vector3[] positions = {
            boundsCenter + new Vector3(boundsSize.x/2, boundsSize.y/2, boundsSize.z/2),
            boundsCenter + new Vector3(-boundsSize.x/2, boundsSize.y/2, boundsSize.z/2),
            boundsCenter + new Vector3(boundsSize.x/2, boundsSize.y/2, -boundsSize.z/2),
            boundsCenter + new Vector3(-boundsSize.x/2, boundsSize.y/2, -boundsSize.z/2),
        };

        List<Transform> points = new List<Transform>();

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject point = new GameObject($"GrapplePoint_{i}");
            point.transform.SetParent(pointsParent.transform);
            point.transform.position = positions[i];
            points.Add(point.transform);
        }

        predefinedGrapplePoints = points.ToArray();

        Debug.Log($"Ϊ {gameObject.name} �Զ������� {points.Count} ����ס��");
    }
}