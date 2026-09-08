using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Singleton day/night clock that drives global 2D lighting.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    [Header("Clock")]
    [Tooltip("In-game hours elapsed per real second (1 = one in-game minute per real second).")]
    public float minutesPerRealSecond = 1f;

    [Range(0f, 24f)]
    public float startHour = 8f;

    [Header("Lighting")]
    public Light2D globalLight;

    readonly LightKeyframe[] _keyframes =
    {
        new LightKeyframe(0f, new Color(0.45f, 0.55f, 0.85f), 0.35f),
        new LightKeyframe(6f, new Color(1f, 0.72f, 0.45f), 0.75f),
        new LightKeyframe(12f, new Color(1f, 0.96f, 0.88f), 1.25f),
        new LightKeyframe(19f, new Color(1f, 0.55f, 0.28f), 0.65f),
        new LightKeyframe(24f, new Color(0.45f, 0.55f, 0.85f), 0.35f),
    };

    public float CurrentHour { get; private set; }
    public bool IsDaytime => CurrentHour >= 6f && CurrentHour < 19f;
    public bool IsNight => !IsDaytime;

    public event Action<float> OnHourChanged;

    int _lastWholeHour = -1;

    struct LightKeyframe
    {
        public float hour;
        public Color color;
        public float intensity;

        public LightKeyframe(float hour, Color color, float intensity)
        {
            this.hour = hour;
            this.color = color;
            this.intensity = intensity;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CurrentHour = startHour;
        _lastWholeHour = GetWholeHour(CurrentHour);

        if (globalLight == null)
            globalLight = FindGlobalLight();
    }

    void Update()
    {
        AdvanceClock();
        UpdateLighting();
        CheckHourChanged();
    }

    void AdvanceClock()
    {
        float hoursPerRealSecond = minutesPerRealSecond / 60f;
        CurrentHour += hoursPerRealSecond * Time.deltaTime;

        if (CurrentHour >= 24f)
            CurrentHour -= 24f;
    }

    void CheckHourChanged()
    {
        int wholeHour = GetWholeHour(CurrentHour);
        if (wholeHour == _lastWholeHour)
            return;

        _lastWholeHour = wholeHour;
        OnHourChanged?.Invoke(CurrentHour);
    }

    void UpdateLighting()
    {
        if (globalLight == null)
            return;

        SampleLightAtHour(CurrentHour, out Color color, out float intensity);
        globalLight.color = color;
        globalLight.intensity = intensity;
    }

    void SampleLightAtHour(float hour, out Color color, out float intensity)
    {
        for (int i = 0; i < _keyframes.Length - 1; i++)
        {
            LightKeyframe a = _keyframes[i];
            LightKeyframe b = _keyframes[i + 1];

            if (hour >= a.hour && hour <= b.hour)
            {
                float t = Mathf.InverseLerp(a.hour, b.hour, hour);
                color = Color.Lerp(a.color, b.color, t);
                intensity = Mathf.Lerp(a.intensity, b.intensity, t);
                return;
            }
        }

        color = _keyframes[0].color;
        intensity = _keyframes[0].intensity;
    }

    static int GetWholeHour(float hour)
    {
        return Mathf.FloorToInt(hour) % 24;
    }

    static Light2D FindGlobalLight()
    {
        Light2D[] lights = FindObjectsByType<Light2D>(FindObjectsInactive.Exclude);
        foreach (Light2D light in lights)
        {
            if (light.lightType == Light2D.LightType.Global)
                return light;
        }

        return lights.Length > 0 ? lights[0] : null;
    }
}
