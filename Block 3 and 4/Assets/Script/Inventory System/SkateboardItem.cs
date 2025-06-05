// Assets/Scripts/Items/SkateboardItem.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Items/SkateboardItem")]
public class SkateboardItem : BaseItem
{
    [Header("Skateboard Settings")]
    public float forwardSpeed = 30f;
    public float turnSpeed = 40f;
    public float deceleration = 0.95f;
    public float minSpeed = 0.5f;

    [Header("Sound")]
    public AudioClip skateboardSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    private bool _isSkating;
    private player_move2 _mover;
    private CharacterController _cc;
    private GameObject _model;
    private AudioSource _audioSource;

    private float _currentSpeed;
    private Vector3 _velocity;

    public override void OnSelect(GameObject model)
    {
        _model = model;

        // Apply transform from base class
        ApplyHoldTransform(model.transform);

        _mover = Object.FindObjectOfType<player_move2>();
        if (_mover == null)
        {
            Debug.LogError("Cannot find player_move2");
            return;
        }

        _cc = _mover.GetComponent<CharacterController>();

        // Setup audio
        _audioSource = _mover.GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = _mover.gameObject.AddComponent<AudioSource>();
        }

        _isSkating = false;
        _currentSpeed = 0f;
        _velocity = Vector3.zero;

        UIManager.Instance.UpdateCameraDebugText("Skateboard ready - Hold left click to skate");
    }

    public override void OnDeselect()
    {
        StopSkating();
    }

    public override void HandleUpdate()
    {
        if (_mover == null || _cc == null) return;

        bool holdingClick = Input.GetMouseButton(0);

        if (holdingClick && !_isSkating)
        {
            StartSkating();
        }
        else if (!holdingClick && _isSkating)
        {
            StopSkating();
        }

        if (_isSkating)
        {
            HandleSkateboardMovement();
        }
    }

    private void StartSkating()
    {
        _isSkating = true;
        _mover.enabled = false; // Turn off normal walking
        _currentSpeed = forwardSpeed;

        // Play skateboard sound
        if (skateboardSound != null && _audioSource != null)
        {
            _audioSource.clip = skateboardSound;
            _audioSource.loop = true;
            _audioSource.volume = soundVolume;
            _audioSource.Play();
        }

        UIManager.Instance.UpdateCameraDebugText("Skating - A/D to turn, can't go backwards");
    }

    private void StopSkating()
    {
        if (!_isSkating) return;

        _isSkating = false;
        _mover.enabled = true; // Turn on normal walking
        _currentSpeed = 0f;
        _velocity = Vector3.zero;

        // Stop skateboard sound
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }

        UIManager.Instance.UpdateCameraDebugText("Stopped skating");
    }

    private void HandleSkateboardMovement()
    {
        // Get input
        float horizontalInput = Input.GetAxis("Horizontal"); // A/D keys
        float verticalInput = Input.GetAxis("Vertical");     // W/S keys

        // Only W key can maintain speed, S key does nothing (can't go backwards)
        if (verticalInput > 0.1f)
        {
            _currentSpeed = forwardSpeed; // Push with W key
        }
        else
        {
            _currentSpeed *= deceleration; // Slow down naturally
        }

        // Stop if too slow
        if (_currentSpeed < minSpeed)
        {
            StopSkating();
            return;
        }

        // A/D keys turn the player (not strafe)
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            float turnAmount = horizontalInput * turnSpeed * Time.deltaTime;
            _mover.transform.Rotate(0, turnAmount, 0);
        }

        // Always move forward in the direction player is facing
        Vector3 forwardDirection = _mover.transform.forward;
        _velocity = new Vector3(forwardDirection.x * _currentSpeed, _velocity.y, forwardDirection.z * _currentSpeed);

        // Handle gravity
        if (_cc.isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f; // Stay on ground
        }
        else
        {
            _velocity.y += _mover.gravity * Time.deltaTime; // Apply gravity
        }

        // Move the player
        _cc.Move(_velocity * Time.deltaTime);

        // Update sound volume based on speed
        if (_audioSource != null && _audioSource.isPlaying)
        {
            float speedRatio = _currentSpeed / forwardSpeed;
            _audioSource.volume = soundVolume * speedRatio;
        }

        UIManager.Instance.UpdateCameraDebugText($"Skating - Speed: {_currentSpeed:F1} - A/D to turn");
    }
}