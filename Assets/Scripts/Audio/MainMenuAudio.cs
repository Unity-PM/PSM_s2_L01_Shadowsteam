using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
[AddComponentMenu("Audio/Main Menu Audio")]
public class MainMenuAudio : MonoBehaviour
{
    [Header("Song")]
    [SerializeField] private AudioClip song;
    [Range(0f, 1f)] [SerializeField] private float volume = 0.7f;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool playOnEnable = true;

    [Header("Output")]
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioMixerGroup outputMixerGroup;

    [Header("Fade")]
    [SerializeField, Min(0f)] private float fadeInSeconds = 1f;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.clip = song;
        source.outputAudioMixerGroup = outputMixerGroup;
    }

    private void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        Stop();
    }

    public void Play()
    {
        if (source == null || song == null)
            return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        source.clip = song;
        source.loop = loop;

        if (fadeInSeconds <= 0f)
        {
            source.volume = volume;
            source.Play();
            return;
        }

        source.volume = 0f;
        source.Play();
        fadeRoutine = StartCoroutine(FadeIn());
    }

    public void Stop()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (source != null)
            source.Stop();
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeInSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(0f, volume, elapsed / fadeInSeconds);
            yield return null;
        }

        source.volume = volume;
        fadeRoutine = null;
    }

    private void OnValidate()
    {
        if (source != null && Application.isPlaying)
        {
            source.loop = loop;
            source.volume = volume;
        }
    }
}
