using UnityEngine;

/// <summary>
/// Represents a piece of food in the game world.  
/// Attach this script to a Food prefab to enable automatic lifetime management
/// and trigger-based detection by animals.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class FoodWorld : MonoBehaviour
{
    /// <summary>
    /// The category of this food item, as configured on the FoodItem prefab.
    /// Determines how different animals react to or consume it.
    /// </summary>
    [Tooltip("Type of this food, set on the FoodItem prefab")]
    public FoodType foodType;

    /// <summary>
    /// Maximum duration (in seconds) this food remains in the scene before being removed.
    /// Prevents clutter and controls spawn lifecycle.
    /// </summary>
    [Tooltip("How long (in seconds) this food lasts in the scene")]
    public float lifetime = 300f;

    // Tracks elapsed time since this instance was spawned.
    private float timer;

    /// <summary>
    /// Unity callback invoked on the first frame this component is active.
    /// Configures the SphereCollider for trigger-based detection by animal AI.
    /// </summary>
    private void Start()
    {
        // Ensure we have a SphereCollider set as a trigger for overlap events.
        var collider = GetComponent<SphereCollider>();
        collider.isTrigger = true;  // Animals can detect without physical collision
        collider.radius = 1f;       // Adjust detection radius as needed for gameplay
    }

    /// <summary>
    /// Unity callback invoked once per frame.
    /// Increments the timer and destroys the GameObject when its lifetime expires.
    /// </summary>
    private void Update()
    {
        timer += Time.deltaTime;           // Accumulate elapsed time
        if (timer >= lifetime)
        {
            // Automatically remove this food object to free resources
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// (Optional) Called by animal AI when entering this food¡¯s trigger zone.
    /// </summary>
    /// <param name="other">Collider of the entering object (e.g., an animal).</param>
    private void OnTriggerEnter(Collider other)
    {
        // Example: You might notify the animal AI system here:
        // var animal = other.GetComponent<AnimalController>();
        // if (animal != null) animal.ConsumeFood(this);
    }
}
