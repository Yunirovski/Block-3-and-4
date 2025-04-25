using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simple food-response state machine for animals.
/// Animals transition through Idle ¡ú Approaching ¡ú Eating ¡ú Grace states
/// based on their food preferences and temperament.
/// </summary>
public class AnimalFoodResponse : MonoBehaviour
{
    [Header("Food Interaction Settings")]
    [Tooltip("List of FoodTypes this animal will respond to.")]
    public List<FoodType> preferences;

    [Tooltip("Animal temperament: controls approach behavior.")]
    public Temperament temperament;

    [Tooltip("Radius within which the animal can detect food.")]
    public float detectionRadius = 20f;

    [Tooltip("Safe distance threshold for Fearful/Hostile temperaments.")]
    public float safeDistance = 15f;

    [Tooltip("Duration (seconds) the animal spends eating.")]
    public float eatDuration = 5f;

    [Tooltip("Duration (seconds) the animal remains calm after eating.")]
    public float graceDuration = 10f;

    // Internal state machine states
    private enum State { Idle, Approaching, Eating, Grace }
    private State state = State.Idle;

    // The current food target the animal is moving toward
    private Transform targetFood;

    // Timer used for Eating and Grace durations
    private float timer;

    private void Update()
    {
        switch (state)
        {
            case State.Idle:
                DetectAndReact();
                break;

            case State.Approaching:
                MoveTowardsFood();
                break;

            case State.Eating:
                // Count down eating time; then enter Grace state
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    state = State.Grace;
                    timer = graceDuration;
                }
                break;

            case State.Grace:
                // After grace period, return to Idle
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    state = State.Idle;
                }
                break;
        }
    }

    /// <summary>
    /// Scans for nearby FoodWorld objects and reacts based on preference and temperament.
    /// </summary>
    private void DetectAndReact()
    {
        // Find all colliders within detectionRadius
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var col in hits)
        {
            var foodWorld = col.GetComponent<FoodWorld>();
            if (foodWorld == null)
                continue;

            // Skip if this food type is not in the preference list
            if (!preferences.Contains(foodWorld.foodType))
                continue;

            // Compute distance between animal and player camera
            float distToPlayer = Vector3.Distance(
                transform.position,
                Camera.main.transform.position);

            // Decide if the animal should approach, based on temperament
            switch (temperament)
            {
                case Temperament.Neutral:
                    StartApproach(foodWorld.transform);
                    return;

                case Temperament.Fearful:
                    // Only approach if the player is far enough away
                    if (distToPlayer > safeDistance)
                        StartApproach(foodWorld.transform);
                    return;

                case Temperament.Hostile:
                    // Only approach if the player is within a certain range
                    if (distToPlayer <= safeDistance)
                        StartApproach(foodWorld.transform);
                    return;
            }
        }
    }

    /// <summary>
    /// Initializes approach toward the specified food transform.
    /// </summary>
    /// <param name="food">Transform of the FoodWorld object to approach.</param>
    private void StartApproach(Transform food)
    {
        targetFood = food;
        state = State.Approaching;
    }

    /// <summary>
    /// Moves the animal toward the target food without using NavMesh.
    /// Destroys the food and begins Eating once close enough.
    /// </summary>
    private void MoveTowardsFood()
    {
        if (targetFood == null)
        {
            // If food was destroyed or lost, return to Idle
            state = State.Idle;
            return;
        }

        // Simple linear movement toward the food
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetFood.position,
            3f * Time.deltaTime);

        // Check arrival at food (<1 meter)
        if (Vector3.Distance(transform.position, targetFood.position) < 1f)
        {
            // Begin Eating state
            state = State.Eating;
            timer = eatDuration;

            // Remove food object from scene
            Destroy(targetFood.gameObject);
            targetFood = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize detection radius in the editor for debugging
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}

/// <summary>
/// Defines animal temperaments affecting food-approach behavior.
/// </summary>
public enum Temperament
{
    /// <summary>No bias: always approach preferred food when detected.</summary>
    Neutral,

    /// <summary>Timid: only approach if the player is far away.</summary>
    Fearful,

    /// <summary>Aggressive: only approach when the player is close.</summary>
    Hostile
}
