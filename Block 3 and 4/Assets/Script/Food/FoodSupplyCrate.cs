// Assets/Scripts/Food/FoodSupplyCrate.cs
using UnityEngine;

/// <summary>
/// Simple Food Supply Crate: Gives food, disappears, respawns after cooldown
/// </summary>
public class FoodSupplyCrate : MonoBehaviour
{
    [Header("Settings")]
    public int foodAmount = 3;
    public float cooldownTime = 10f;
    public AudioClip collectSound;

    private bool isAvailable = true;
    private AudioSource audioSource;
    private Renderer crateRenderer;
    private Collider crateCollider;

    void Start()
    {
        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Get components
        crateRenderer = GetComponent<Renderer>();
        crateCollider = GetComponent<Collider>();

        // Setup trigger
        if (crateCollider == null)
        {
            crateCollider = gameObject.AddComponent<BoxCollider>();
        }
        crateCollider.isTrigger = true;

        Debug.Log($"Food Crate Ready: +{foodAmount} food, {cooldownTime}s cooldown");
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isAvailable) return;

        // Check if player
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>())
        {
            CollectFood();
        }
    }

    void CollectFood()
    {
        if (ConsumableManager.Instance == null) return;

        // Add food
        int oldFood = ConsumableManager.Instance.Food;
        ConsumableManager.Instance.AddFood(foodAmount);
        int newFood = ConsumableManager.Instance.Food;

        if (newFood > oldFood)
        {
            // Success - play sound and hide crate
            PlaySound();
            HideCrate();
            ShowMessage($"Collected {foodAmount} Food!");

            // Start respawn timer
            Invoke(nameof(ShowCrate), cooldownTime);
        }
        else
        {
            ShowMessage("Food Inventory Full!");
        }
    }

    void PlaySound()
    {
        if (collectSound && audioSource)
        {
            audioSource.PlayOneShot(collectSound);
        }
    }

    void HideCrate()
    {
        isAvailable = false;
        crateRenderer.enabled = false;
        crateCollider.enabled = false;
        Debug.Log("Food crate collected - hidden for " + cooldownTime + "s");
    }

    void ShowCrate()
    {
        isAvailable = true;
        crateRenderer.enabled = true;
        crateCollider.enabled = true;
        Debug.Log("Food crate respawned!");
    }

    void ShowMessage(string msg)
    {
        if (UIManager.Instance)
        {
            UIManager.Instance.UpdateCameraDebugText(msg);
        }
        Debug.Log("Food Crate: " + msg);
    }

    // Visual debug
    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col)
        {
            Gizmos.color = isAvailable ? Color.green : Color.red;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider box)
                Gizmos.DrawWireCube(box.center, box.size);
            else if (col is SphereCollider sphere)
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
        }
    }
}