// Assets/Scripts/Food/FoodWorld.cs
using UnityEngine;

/// <summary>
/// Food in the world script
/// Takes care of food life, talking to animals, etc
/// </summary>
public class FoodWorld : MonoBehaviour
{
    [Header("Food Properties")]
    [Tooltip("What kind of food this is")]
    public FoodType foodType = FoodType.Apple;

    [Tooltip("How long food stays (seconds), -1 means forever")]
    public float lifetime = 300f; // 5 minutes

    [Header("Visual Effects")]
    [Tooltip("Effect when food gets eaten")]
    public GameObject eatEffect;

    [Tooltip("Effect when food goes away")]
    public GameObject disappearEffect;

    [Header("Audio")]
    [Tooltip("Sound when food hits ground")]
    public AudioClip landSound;

    [Tooltip("Sound when food gets eaten")]
    public AudioClip eatSound;

    // What's happening inside
    private float timeRemaining;
    private bool hasLanded = false;
    private bool isBeingEaten = false;
    private AudioSource audioSource;

    void Start()
    {
        timeRemaining = lifetime;

        // Make sound player
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.playOnAwake = false;
        }

        Debug.Log($"FoodWorld: Food {foodType} made, will last {lifetime} seconds");
    }

    void Update()
    {
        // Count down how long food stays
        if (lifetime > 0 && timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0)
            {
                DestroyFood(false);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if food hit the ground
        if (!hasLanded && collision.gameObject.CompareTag("Ground"))
        {
            hasLanded = true;
            PlayLandSound();
            Debug.Log($"FoodWorld: Food {foodType} hit ground");
        }

        // Check if food hit an animal
        AnimalBehavior animal = collision.gameObject.GetComponent<AnimalBehavior>();
        if (animal != null && !isBeingEaten)
        {
            // Animal found the food
            OnAnimalFound(animal);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // If collider is set as trigger, also handle animal talk
        AnimalBehavior animal = other.GetComponent<AnimalBehavior>();
        if (animal != null && !isBeingEaten)
        {
            OnAnimalFound(animal);
        }
    }

    /// <summary>
    /// Called when animal finds food
    /// </summary>
    private void OnAnimalFound(AnimalBehavior animal)
    {
        Debug.Log($"FoodWorld: Animal {animal.name} found food {foodType}");

        // Here we can add animal reaction to food
        // Like: animal walks to food, changes what it does, etc

        // If we want food to be eaten right away, call BeEaten()
        // BeEaten(animal);
    }

    /// <summary>
    /// Food gets eaten by animal
    /// </summary>
    public void BeEaten(AnimalBehavior eater = null)
    {
        if (isBeingEaten) return;

        isBeingEaten = true;

        Debug.Log($"FoodWorld: Food {foodType} got eaten by {(eater != null ? eater.name : "some animal")}");

        // Play eating sound
        if (eatSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(eatSound);
        }

        // Make eating effect
        if (eatEffect != null)
        {
            Instantiate(eatEffect, transform.position, transform.rotation);
        }

        // Wait a bit then destroy (let sound finish)
        Invoke(nameof(DestroyFoodImmediate), 0.1f);
    }

    /// <summary>
    /// Make food go away
    /// </summary>
    /// <param name="wasEaten">Did it get eaten</param>
    private void DestroyFood(bool wasEaten)
    {
        if (isBeingEaten) return;

        Debug.Log($"FoodWorld: Food {foodType} goes away because {(wasEaten ? "got eaten" : "time ran out")}");

        // If not eaten, make disappear effect
        if (!wasEaten && disappearEffect != null)
        {
            Instantiate(disappearEffect, transform.position, transform.rotation);
        }

        DestroyFoodImmediate();
    }

    /// <summary>
    /// Delete food object right now
    /// </summary>
    private void DestroyFoodImmediate()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Play sound when food hits ground
    /// </summary>
    private void PlayLandSound()
    {
        if (landSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(landSound);
        }
    }

    /// <summary>
    /// Get how much time food has left
    /// </summary>
    public float GetRemainingLifetime()
    {
        return timeRemaining;
    }

    /// <summary>
    /// Change what kind of food this is
    /// </summary>
    public void SetFoodType(FoodType newType)
    {
        foodType = newType;
    }

    /// <summary>
    /// Check if food is still good
    /// </summary>
    public bool IsValid()
    {
        return !isBeingEaten && (lifetime <= 0 || timeRemaining > 0);
    }

    // Show debug info
    void OnDrawGizmosSelected()
    {
        // Draw how far food can affect things (optional)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
}