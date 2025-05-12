using UnityEngine;

[CreateAssetMenu(menuName = "Items/GrappleItem")]
public class GrappleItem : BaseItem
{
    [Header("×¥¹³²ÎÊý")]
    public float maxDistance = 20f;
    public float pullSpeed = 5f;

    [Header("¹³×¦¿ÉÊÓ»¯")]
    [Tooltip("×¥¹³Ä£ÐÍ Prefab£¬ÓÉÃÀÊõÌá¹©")]
    public GameObject hookPrefab;
    [Tooltip("¹³×¦·ÉÐÐËÙ¶È (m/s)")]
    public float hookTravelSpeed = 50f;
    [Tooltip("ÉþË÷²ÄÖÊ£¬ÓÃÓÚ LineRenderer")]
    public Material ropeMaterial;

    [Header("ÒôÐ§")]
    [Tooltip("×¥¹³¿ªÇ®ÒôÐ§")]
    public AudioClip grappleFireSound;
    [Tooltip("ÒôÐ§ÒôÁ¿")]
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    // ÔËÐÐÊ±»º´æ
    Camera _cam;
    GrappleController _grappler;
    AudioSource _audioSource;

    public override void OnSelect(GameObject model)
    {
        _cam = Camera.main;
        if (_cam == null) { Debug.LogError("ÕÒ²»µ½Ö÷Ïà»ú"); return; }

        // ¼ÙÉè GrappleController ¹ÒÔÚÏà»úµÄ¸¸¶ÔÏóÉÏ£¨Íæ¼ÒÉíÉÏ£©
        _grappler = _cam.GetComponentInParent<GrappleController>();
        if (_grappler == null)
        {
            Debug.LogError("Íæ¼ÒÎïÌåÉÏÈ±ÉÙ GrappleController ×é¼þ");
            return;
        }

        // ´´½¨ÒôÆµÔ´£¬Èç¹û²»´æÔÚ
        _audioSource = _cam.GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = _cam.gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0f; // È«¾ÖÒôÐ§
        }

        // ×¢Èë¹³×¦¿ÉÊÓ»¯×ÊÔ´
        _grappler.InitializeHook(hookPrefab, hookTravelSpeed, ropeMaterial);
    }

    public override void OnUse()
    {
        if (_grappler == null || _cam == null) return;

        // ²¥·Å¿ªÇ®ÒôÐ§
        if (grappleFireSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(grappleFireSound, soundVolume);
        }

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
                Debug.Log("ÃüÖÐÄ¿±ê·Ç¾²Ì¬£¬²»¿É¸½×Å");
            }
        }
        else
        {
            Debug.Log("Éä³ÌÄÚÎ´ÃüÖÐÈÎºÎ±íÃæ");
        }
    }
}