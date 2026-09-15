using System.Collections;
using UnityEngine;

/// <summary>
/// Persistent background music singleton. Fades in on Start using unscaled time.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Track")]
    public AudioClip mainTheme;

    [Header("Fade")]
    [Range(0f, 1f)]
    public float targetVolume = 0.5f;
    public float fadeDurationSeconds = 3f;

    AudioSource _source;
    Coroutine _fadeRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _source = GetComponent<AudioSource>();
        if (_source == null)
            _source = gameObject.AddComponent<AudioSource>();

        _source.playOnAwake = false;
        _source.loop = true;
        _source.volume = 0f;
        _source.spatialBlend = 0f;
    }

    void Start()
    {
        if (Instance != this)
            return;

        if (mainTheme != null)
            _source.clip = mainTheme;

        if (_source.clip == null)
        {
            Debug.LogWarning("MusicManager: no AudioClip assigned — silence until you set Main Theme.");
            return;
        }

        _source.volume = 0f;
        if (!_source.isPlaying)
            _source.Play();

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeIn());
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    IEnumerator FadeIn()
    {
        float duration = Mathf.Max(0.01f, fadeDurationSeconds);
        float target = Mathf.Clamp01(targetVolume);
        float elapsed = 0f;

        _source.volume = 0f;
        while (elapsed < duration)
        {
            // Unscaled so fade continues even if timeScale is 0.
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _source.volume = Mathf.Lerp(0f, target, t);
            yield return null;
        }

        _source.volume = target;
        _fadeRoutine = null;
    }
}
