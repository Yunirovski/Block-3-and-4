// Assets/Scripts/Items/DartGunItem.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Items/DartGunItem")]
public class DartGunItem : BaseItem
{
    [Header("Dart Settings")]
    [Tooltip("Dart projectile prefab")]
    public GameObject dartPrefab;
    [Tooltip("Dart firing speed")]
    public float dartSpeed = 30f;
    [Tooltip("Maximum effective range")]
    public float maxRange = 50f;
    [Tooltip("Fire rate cooldown")]
    public float fireCooldown = 1f;

    [Header("Accuracy")]
    [Tooltip("Accuracy spread (0 = perfect accuracy)")]
    [Range(0f, 5f)] public float accuracy = 0.5f;
    [Tooltip("Movement accuracy penalty")]
    [Range(0f, 2f)] public float movementPenalty = 1f;

    [Header("Dart Effects")]
    [Tooltip("Stun duration for animals")]
    public float stunDuration = 30f;
    [Tooltip("Dart impact effect")]
    public GameObject impactEffect;
    [Tooltip("Muzzle flash effect")]
    public GameObject muzzleFlashEffect;

    [Header("Audio")]
    [Tooltip("Dart gun fire sound")]
    public AudioClip fireSound;
    [Tooltip("Dart hit sound")]
    public AudioClip hitSound;
    [Tooltip("Empty chamber sound")]
    public AudioClip emptySound;
    [Tooltip("Reload sound")]
    public AudioClip reloadSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    [Header("Ammo System")]
    [Tooltip("Enable ammo limitation")]
    public bool useAmmoSystem = true;
    [Tooltip("Darts per reload")]
    public int magazineSize = 6;
    [Tooltip("Reload time")]
    public float reloadTime = 2f;

    // Runtime variables
    private Camera _camera;
    private DartGunController _controller;
    private AudioSource _audioSource;
    private float _nextFireTime = 0f;
    private int _currentAmmo;
    private bool _isReloading = false;

    public override void OnSelect(GameObject model)
    {
        _camera = Camera.main;
        if (_camera == null)
        {
            Debug.LogError("DartGun: Main camera not found");
            return;
        }

        // Get or create controller
        _controller = _camera.GetComponentInParent<DartGunController>();
        if (_controller == null)
        {
            GameObject player = _camera.transform.parent?.gameObject;
            if (player != null)
            {
                _controller = player.AddComponent<DartGunController>();
                Debug.Log("DartGun: Added DartGunController component");
            }
            else
            {
                Debug.LogError("DartGun: Cannot add controller component");
                return;
            }
        }

        // Setup audio
        _audioSource = _camera.GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = _camera.gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0f;
        }

        // Configure controller
        ConfigureController();

        // Initialize ammo
        if (useAmmoSystem)
        {
            _currentAmmo = magazineSize;
        }

        _controller.Initialize();
        UIManager.Instance?.UpdateCameraDebugText("Dart Gun ready - Left click to fire, R to reload");
    }

    private void ConfigureController()
    {
        _controller.dartPrefab = dartPrefab;
        _controller.dartSpeed = dartSpeed;
        _controller.maxRange = maxRange;
        _controller.accuracy = accuracy;
        _controller.movementPenalty = movementPenalty;
        _controller.stunDuration = stunDuration;
        _controller.impactEffect = impactEffect;
        _controller.muzzleFlashEffect = muzzleFlashEffect;
        _controller.fireSound = fireSound;
        _controller.hitSound = hitSound;
        _controller.soundVolume = soundVolume;
    }

    public override void OnUse()
    {
        if (_controller == null || _camera == null)
        {
            UIManager.Instance?.UpdateCameraDebugText("Dart Gun not ready");
            return;
        }

        if (_isReloading)
        {
            UIManager.Instance?.UpdateCameraDebugText("Reloading...");
            return;
        }

        if (Time.time < _nextFireTime)
        {
            float cooldown = _nextFireTime - Time.time;
            UIManager.Instance?.UpdateCameraDebugText($"Cooldown: {cooldown:F1}s");
            return;
        }

        if (useAmmoSystem && _currentAmmo <= 0)
        {
            PlaySound(emptySound);
            UIManager.Instance?.UpdateCameraDebugText("No ammo! Press R to reload");
            return;
        }

        // Fire dart
        Vector3 firePoint = _camera.transform.position;
        Vector3 fireDirection = CalculateFireDirection();

        bool success = _controller.FireDart(firePoint, fireDirection);

        if (success)
        {
            _nextFireTime = Time.time + fireCooldown;

            if (useAmmoSystem)
            {
                _currentAmmo--;
            }

            PlaySound(fireSound);
            CreateMuzzleFlash();

            UIManager.Instance?.UpdateCameraDebugText(useAmmoSystem ?
                $"Fired! Ammo: {_currentAmmo}/{magazineSize}" : "Fired!");
        }
    }

    public override void HandleUpdate()
    {
        if (_controller == null) return;

        // Handle reload
        if (useAmmoSystem && Input.GetKeyDown(KeyCode.R) && !_isReloading && _currentAmmo < magazineSize)
        {
            StartReload();
        }

        // Update status display
        UpdateStatusDisplay();
    }

    private void StartReload()
    {
        _isReloading = true;
        PlaySound(reloadSound);
        UIManager.Instance?.StartItemCooldown(this, reloadTime);

        // Schedule reload completion
        if (_controller != null)
        {
            _controller.StartCoroutine(CompleteReload());
        }
    }

    private System.Collections.IEnumerator CompleteReload()
    {
        yield return new WaitForSeconds(reloadTime);

        _currentAmmo = magazineSize;
        _isReloading = false;

        UIManager.Instance?.UpdateCameraDebugText($"Reloaded! Ammo: {_currentAmmo}/{magazineSize}");
    }

    private Vector3 CalculateFireDirection()
    {
        // Base direction from camera
        Vector3 direction = _camera.transform.forward;

        // Add accuracy spread
        float currentAccuracy = accuracy;

        // Increase spread if moving
        if (IsPlayerMoving())
        {
            currentAccuracy += movementPenalty;
        }

        if (currentAccuracy > 0f)
        {
            Vector3 spread = new Vector3(
                Random.Range(-currentAccuracy, currentAccuracy),
                Random.Range(-currentAccuracy, currentAccuracy),
                0f
            );

            direction = (_camera.transform.forward + _camera.transform.TransformDirection(spread * 0.01f)).normalized;
        }

        return direction;
    }

    private bool IsPlayerMoving()
    {
        // Check if player is moving
        float inputMagnitude = Mathf.Abs(Input.GetAxis("Horizontal")) + Mathf.Abs(Input.GetAxis("Vertical"));
        return inputMagnitude > 0.1f;
    }

    private void CreateMuzzleFlash()
    {
        if (muzzleFlashEffect != null && _camera != null)
        {
            Vector3 muzzlePosition = _camera.transform.position + _camera.transform.forward * 0.5f;
            GameObject flash = Instantiate(muzzleFlashEffect, muzzlePosition, _camera.transform.rotation);
            Destroy(flash, 0.2f);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    private void UpdateStatusDisplay()
    {
        if (UIManager.Instance == null) return;

        string status = "";

        if (_isReloading)
        {
            status = "Reloading...";
        }
        else if (Time.time < _nextFireTime)
        {
            float cooldown = _nextFireTime - Time.time;
            status = $"Cooldown: {cooldown:F1}s";
        }
        else if (useAmmoSystem)
        {
            if (_currentAmmo <= 0)
            {
                status = "No ammo! Press R to reload";
            }
            else
            {
                status = $"Ready - Ammo: {_currentAmmo}/{magazineSize} (R to reload)";
            }
        }
        else
        {
            status = "Ready to fire";
        }

        UIManager.Instance.UpdateCameraDebugText(status);
    }

    public override void OnDeselect()
    {
        if (_controller != null)
        {
            _controller.Cleanup();
        }
    }

    public override void OnUnready()
    {
        OnDeselect();
    }

    // Public accessors for external systems
    public bool IsReady()
    {
        return !_isReloading && Time.time >= _nextFireTime && (!useAmmoSystem || _currentAmmo > 0);
    }

    public int GetCurrentAmmo()
    {
        return useAmmoSystem ? _currentAmmo : -1; // -1 means unlimited
    }

    public int GetMaxAmmo()
    {
        return useAmmoSystem ? magazineSize : -1;
    }
}