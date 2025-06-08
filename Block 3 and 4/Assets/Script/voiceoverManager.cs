using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class voiceoverManager : MonoBehaviour
{
    public AudioClip[] audioClips;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        
    }

    /// Plays the audio clip at the given index.

    public void PlayClip(int index)
    {
        if (audioClips == null || audioClips.Length == 0)
        {
            Debug.LogWarning("No audio clips assigned.");
            return;
        }

        if (index < 0 || index >= audioClips.Length)
        {
            Debug.LogWarning("Audio clip index out of range.");
            return;
        }

        audioSource.clip = audioClips[index];
        audioSource.Play();
    }

    public void StopAudio()
    {
        audioSource.Stop();
    }

}
