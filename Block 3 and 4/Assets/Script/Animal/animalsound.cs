﻿using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AnimalSound : MonoBehaviour
{
    [Header("Sound Settings")]
    [Tooltip("List of possible animal sounds")]
    public AudioClip[] animalSounds;

    [Tooltip("Minimum time between sounds (seconds)")]
    public float minInterval = 7f;

    [Tooltip("Maximum time between sounds (seconds)")]
    public float maxInterval = 15f;

    [Range(0f, 1f)]
    [Tooltip("Volume of the sound (0 = silent, 1 = full volume)")]
    public float soundVolume = 1.0f;

    [Header("Trigger Settings")]
    [Tooltip("Distance at which the player triggers the sound")]
    public float triggerDistance = 25f;

    [Tooltip("Tag used to identify the player object")]
    public string playerTag = "Player";

    private AudioSource audioSource;
    private Transform player;
    private float nextPlayTime = 0f;

    void Start()
    {
        // Setup AudioSource
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 40f; // 👈 allow sound to be heard farther away
        audioSource.volume = soundVolume;
        audioSource.playOnAwake = false;

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("AnimalSound: Player object not found. Make sure the player has the correct tag.");
        }

        ScheduleNextSound();
    }

    void Update()
    {
        if (player == null || animalSounds.Length == 0) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= triggerDistance && Time.time >= nextPlayTime && !audioSource.isPlaying)
        {
            PlayRandomSound();
            ScheduleNextSound();
        }
    }

    private void PlayRandomSound()
    {
        int index = Random.Range(0, animalSounds.Length);
        AudioClip clip = animalSounds[index];

        audioSource.volume = soundVolume; // Make sure volume is applied every time
        audioSource.PlayOneShot(clip);
        Debug.Log($"AnimalSound: Playing sound - {clip.name} at volume {soundVolume}");
    }

    private void ScheduleNextSound()
    {
        float interval = Random.Range(minInterval, maxInterval);
        nextPlayTime = Time.time + interval;
    }
}
