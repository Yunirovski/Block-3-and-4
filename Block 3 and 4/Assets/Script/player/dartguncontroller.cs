// Assets/Scripts/Player/DartGunController.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls dart gun firing mechanics and dart projectile behavior
/// </summary>
public class DartGunController : MonoBehaviour
{
    [Header("Dart Configuration (Set by DartGunItem)")]
    [HideInInspector] public GameObject dartPrefab;
    [HideInInspector] public float dartSpeed = 30f;
    [HideInInspector] public float maxRange = 50f;
    [HideInInspector] public float accuracy = 0.5f;
    [HideInInspector] public float movementPenalty = 1f;
    [HideInInspector] public float stunDuration = 30f;
    [HideInInspector] public GameObject impactEffect;
    [HideInInspector] public GameObject muzzleFlashEffect;
    [HideInInspector] public AudioClip fireSound;
    [HideInInspector] public AudioClip hitSound;
    [HideInInspector] public float soundVolume = 0.8f;

    [Header("Physics Settings")]
    [Tooltip("Dart gravity effect")]
    public float dartGravity = 9.8f;
    [Tooltip("Dart drag coefficient")]
    public float dartDrag = 0.1f;

    // Component references
    private AudioSource audioSource;
    private List<GameObject> activeDarts = new List<GameObject>();

    // Performance optimization
    private int maxActiveDarts = 3;

    public void Initialize()
    {
        // Get audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }

        Debug.Log("DartGunController initialized");
    }

    public bool FireDart(Vector3 firePoint, Vector3 fireDirection)
    {
        if (dartPrefab == null)
        {
            Debug.LogError("DartGun: No dart prefab assigned");
            return false;
        }

        // Create dart projectile
        GameObject dart = CreateDart(firePoint, fireDirection);
        if (dart == null)
        {
            return false;
        }

        // Manage active darts count
        ManageActiveDarts();

        Debug.Log($"Dart fired from {firePoint} in direction {fireDirection}");
        return true;
    }

    private GameObject CreateDart(Vector3 position, Vector3 direction)
    {
        try
        {
            GameObject dart = Instantiate(dartPrefab, position, Quaternion.LookRotation(direction));

            // Add dart behavior component
            DartProjectile dartBehavior = dart.GetComponent<DartProjectile>();
            if (dartBehavior == null)
            {
                dartBehavior = dart.AddComponent<DartProjectile>();
            }

            // Configure dart
            dartBehavior.Initialize(direction * dartSpeed, maxRange, stunDuration, this);

            // Add to active darts list
            activeDarts.Add(dart);

            return dart;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"DartGun: Failed to create dart - {e.Message}");
            return null;
        }
    }

    private void ManageActiveDarts()
    {
        // Remove null references (destroyed darts)
        activeDarts.RemoveAll(dart => dart == null);

        // Limit number of active darts
        while (activeDarts.Count > maxActiveDarts)
        {
            if (activeDarts[0] != null)
            {
                Destroy(activeDarts[0]);
            }
            activeDarts.RemoveAt(0);
        }
    }

    public void OnDartHit(Vector3 hitPosition, Collider hitCollider, GameObject dart)
    {
        // Create impact effect
        if (impactEffect != null)
        {
            GameObject effect = Instantiate(impactEffect, hitPosition, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // Play hit sound
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound, soundVolume);
        }

        // Check if hit an animal
        AnimalBehavior animal = hitCollider.GetComponent<AnimalBehavior>();
        if (animal != null)
        {
            StunAnimal(animal);
            Debug.Log($"Dart hit animal: {animal.name}");
        }
        else
        {
            Debug.Log($"Dart hit: {hitCollider.name}");
        }

        // Remove from active darts list
        activeDarts.Remove(dart);
    }

    private void StunAnimal(AnimalBehavior animal)
    {
        // Apply stun effect
        animal.Stun(stunDuration);

        // Apply 90-degree rotation effect
        StartCoroutine(RotateAnimal(animal.transform));
    }

    private System.Collections.IEnumerator RotateAnimal(Transform animalTransform)
    {
        if (animalTransform == null) yield break;

        // Store original rotation
        Quaternion originalRotation = animalTransform.rotation;

        // Calculate 90-degree rotation
        Vector3 rotationAxis = Random.Range(0f, 1f) > 0.5f ? Vector3.forward : Vector3.right;
        Quaternion targetRotation = originalRotation * Quaternion.AngleAxis(90f, rotationAxis);

        // Rotate over time
        float rotationTime = 0.5f;
        float elapsed = 0f;

        while (elapsed < rotationTime && animalTransform != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / rotationTime;

            animalTransform.rotation = Quaternion.Slerp(originalRotation, targetRotation, progress);
            yield return null;
        }

        // Ensure final rotation is set
        if (animalTransform != null)
        {
            animalTransform.rotation = targetRotation;
        }

        // Wait for stun duration
        yield return new WaitForSeconds(stunDuration - rotationTime);

        // Restore original rotation
        if (animalTransform != null)
        {
            elapsed = 0f;
            Quaternion currentRotation = animalTransform.rotation;

            while (elapsed < rotationTime && animalTransform != null)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / rotationTime;

                animalTransform.rotation = Quaternion.Slerp(currentRotation, originalRotation, progress);
                yield return null;
            }

            // Ensure original rotation is restored
            if (animalTransform != null)
            {
                animalTransform.rotation = originalRotation;
            }
        }
    }

    public void Cleanup()
    {
        // Destroy all active darts
        foreach (GameObject dart in activeDarts)
        {
            if (dart != null)
            {
                Destroy(dart);
            }
        }
        activeDarts.Clear();

        Debug.Log("DartGunController cleaned up");
    }

    void OnDestroy()
    {
        Cleanup();
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (Application.isPlaying && activeDarts.Count > 0)
        {
            Gizmos.color = Color.red;
            foreach (GameObject dart in activeDarts)
            {
                if (dart != null)
                {
                    Gizmos.DrawWireSphere(dart.transform.position, 0.1f);
                }
            }
        }
    }
}