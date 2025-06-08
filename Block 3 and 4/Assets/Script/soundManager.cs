using System.Runtime.CompilerServices;
using UnityEngine;

public enum SoundType
{
    VOICEOVER
}

[RequireComponent(typeof(AudioSource))]
public class soundManager : MonoBehaviour
{

    [SerializeField] private AudioClip[] soundList;
    private static soundManager instance;
    private AudioSource audioSource;


    private void Awake(){
        
    }

    private void Start(){
        audioSource = GetComponent<AudioSource>();
    }

    public static void PlaySound(SoundType sound, float volume = 1){

      // instance.audioSource.PlayOneShot(instance.soundList(int)sound, volume)
    }
}
