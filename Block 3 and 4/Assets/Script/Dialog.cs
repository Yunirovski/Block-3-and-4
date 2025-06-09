using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Dialog : MonoBehaviour
{
    [Header("Dialog Components")]
    public TextMeshProUGUI textComponent;
    public string[] lines;

    [Header("Dialog Settings")]
    [Tooltip("Speed for typing each character (smaller = faster)")]
    public float txtSpeed = 0.045f; // Made faster than 0.075

    [Tooltip("If true, player cannot move during dialog")]
    public bool disablePlayerMovement = true;

    [Header("Audio Settings")]
    [Tooltip("Audio clips for each dialog line (should match lines array)")]
    public AudioClip[] dialogAudioClips = new AudioClip[15];

    [Tooltip("Volume for dialog audio")]
    [Range(0f, 1f)]
    public float audioVolume = 0.8f;

    // Private variables
    private int index = 0;
    private bool isTyping = false;
    private AudioSource audioSource;
    private player_move2 playerMovement;
    private bool wasPlayerMovementEnabled = true;

    void Start()
    {
        // Initialize components
        SetupAudioSource();
        FindPlayerMovement();

        // Start dialog
        textComponent.text = string.Empty;
        StartDialog();
    }

    void Update()
    {
        HandleInput();
    }

    /// <summary>
    /// Setup audio source component
    /// </summary>
    void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.volume = audioVolume;
        audioSource.playOnAwake = false;
    }

    /// <summary>
    /// Find and store reference to player movement script
    /// </summary>
    void FindPlayerMovement()
    {
        if (disablePlayerMovement)
        {
            playerMovement = FindObjectOfType<player_move2>();
            if (playerMovement != null)
            {
                wasPlayerMovementEnabled = playerMovement.enabled;
                playerMovement.enabled = false; // Disable player movement when dialog starts
                Debug.Log("Player movement disabled for dialog");
            }
        }
    }

    /// <summary>
    /// Handle all input for dialog progression
    /// </summary>
    void HandleInput()
    {
        // Check for Enter key (skip to end of current line)
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (isTyping)
            {
                // Skip typing and show full line immediately
                StopAllCoroutines();
                textComponent.text = lines[index];
                isTyping = false;
                Debug.Log("Dialog line skipped");
            }
            return;
        }

        // Check for any other key (continue to next line)
        if (Input.GetKeyDown(KeyCode.T) && !isTyping)
        {
            NextLine();
        }
    }

    /// <summary>
    /// Start the dialog system
    /// </summary>
    void StartDialog()
    {
        index = 0;
        PlayCurrentLineAudio();
        StartCoroutine(TypeLine());
    }

    /// <summary>
    /// Type out the current line character by character
    /// </summary>
    IEnumerator TypeLine()
    {
        isTyping = true;
        textComponent.text = string.Empty;

        foreach (char c in lines[index].ToCharArray())
        {
            textComponent.text += c;
            yield return new WaitForSeconds(txtSpeed);
        }

        isTyping = false;
    }

    /// <summary>
    /// Move to the next dialog line
    /// </summary>
    void NextLine()
    {
        Debug.Log("Moving to next dialog line");

        if (index < lines.Length - 1)
        {
            // Stop current audio before moving to next line
            StopCurrentAudio();

            // Move to next line
            index++;
            PlayCurrentLineAudio();
            StartCoroutine(TypeLine());
        }
        else
        {
            // Dialog finished
            EndDialog();
        }
    }

    /// <summary>
    /// Play audio for the current dialog line
    /// </summary>
    void PlayCurrentLineAudio()
    {
        if (audioSource != null && index < dialogAudioClips.Length && dialogAudioClips[index] != null)
        {
            audioSource.clip = dialogAudioClips[index];
            audioSource.Play();
            Debug.Log($"Playing audio for dialog line {index + 1}");
        }
    }

    /// <summary>
    /// Stop the currently playing audio
    /// </summary>
    void StopCurrentAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("Stopped current dialog audio");
        }
    }

    /// <summary>
    /// End the dialog and restore player movement
    /// </summary>
    void EndDialog()
    {
        Debug.Log("Dialog ended");

        // Stop any playing audio
        StopCurrentAudio();

        // Restore player movement if it was disabled
        if (disablePlayerMovement && playerMovement != null)
        {
            playerMovement.enabled = wasPlayerMovementEnabled;
            Debug.Log("Player movement restored");
        }

        // Hide dialog
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Force close dialog (can be called externally)
    /// </summary>
    public void ForceCloseDialog()
    {
        StopAllCoroutines();
        EndDialog();
    }

    /// <summary>
    /// Get current dialog progress (for UI or other systems)
    /// </summary>
    public float GetDialogProgress()
    {
        return (float)(index + 1) / lines.Length;
    }

    /// <summary>
    /// Check if dialog is currently active
    /// </summary>
    public bool IsDialogActive()
    {
        return gameObject.activeInHierarchy;
    }

    // Called when dialog object is disabled
    void OnDisable()
    {
        // Make sure player movement is restored if dialog is force-closed
        if (disablePlayerMovement && playerMovement != null)
        {
            playerMovement.enabled = wasPlayerMovementEnabled;
        }

        // Stop any playing audio
        StopCurrentAudio();
    }

    void OnDestroy()
    {
        // Cleanup when object is destroyed
        StopCurrentAudio();

        if (disablePlayerMovement && playerMovement != null)
        {
            playerMovement.enabled = wasPlayerMovementEnabled;
        }
    }
}