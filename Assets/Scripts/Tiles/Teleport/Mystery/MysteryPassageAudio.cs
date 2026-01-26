using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MysteryPassageAudio : MonoBehaviour
{
    public static MysteryPassageAudio Instance { get; private set; }

    [Tooltip("Globalna muzyka odtwarzana podczas aktywnych tajemniczych wejść.")]
    public AudioClip musicClip;

    private AudioSource audioSource;
    private int activeEntrancesWithMusic = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
    }

    public void RegisterEntranceActivation()
    {
        if (musicClip == null)
            return;

        activeEntrancesWithMusic++;

        if (activeEntrancesWithMusic == 1)
        {
            audioSource.clip = musicClip;
            audioSource.Play();
        }
    }

    public void RegisterEntranceDeactivation()
    {
        if (musicClip == null)
            return;

        activeEntrancesWithMusic = Mathf.Max(0, activeEntrancesWithMusic - 1);

        if (activeEntrancesWithMusic == 0)
        {
            audioSource.Stop();
        }
    }
}
