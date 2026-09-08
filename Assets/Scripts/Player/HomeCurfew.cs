using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Enforces a home curfew at night: warning UI, then soft return at hard curfew hour.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class HomeCurfew : MonoBehaviour
{
    [Header("Home")]
    public Transform homePosition;
    public float curfewRadius = 3f;

    [Header("Curfew")]
    public float hardCurfewHour = 0f;
    public float fadeDuration = 0.75f;
    public float pulseSpeed = 4f;

    [Header("UI")]
    public TMP_Text warningText;
    public CanvasGroup fadePanel;

    Rigidbody2D _rb;
    bool _curfewEnforcedTonight;
    bool _wasNight;
    Coroutine _fadeRoutine;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        if (warningText != null)
            warningText.gameObject.SetActive(false);

        if (fadePanel != null)
        {
            fadePanel.alpha = 0f;
            fadePanel.blocksRaycasts = false;
        }
    }

    void Update()
    {
        DayNightCycle cycle = DayNightCycle.Instance;
        if (cycle == null || homePosition == null)
            return;

        if (!cycle.IsNight)
        {
            _curfewEnforcedTonight = false;
            _wasNight = false;
            SetWarningVisible(false);
            return;
        }

        if (!_wasNight)
            _wasNight = true;

        float distance = Vector2.Distance(transform.position, homePosition.position);
        bool awayFromHome = distance > curfewRadius;

        if (awayFromHome && !_curfewEnforcedTonight)
            UpdateWarningPulse();
        else
            SetWarningVisible(false);

        if (!_curfewEnforcedTonight && awayFromHome && HasReachedHardCurfew(cycle.CurrentHour))
            _fadeRoutine = StartCoroutine(EnforceCurfewRoutine());
    }

    bool HasReachedHardCurfew(float currentHour)
    {
        int wholeHour = Mathf.FloorToInt(Mathf.Repeat(currentHour, 24f));
        int curfewWholeHour = Mathf.FloorToInt(Mathf.Repeat(hardCurfewHour, 24f));
        return wholeHour == curfewWholeHour;
    }

    void UpdateWarningPulse()
    {
        if (warningText == null)
            return;

        warningText.gameObject.SetActive(true);
        warningText.text = "Head home before midnight!";

        float pulse = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed));
        Color c = warningText.color;
        c.a = pulse;
        warningText.color = c;
    }

    void SetWarningVisible(bool visible)
    {
        if (warningText == null)
            return;

        warningText.gameObject.SetActive(visible);
    }

    IEnumerator EnforceCurfewRoutine()
    {
        _curfewEnforcedTonight = true;
        SetWarningVisible(false);

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        yield return FadePanel(0f, 1f);

        TeleportHome();

        yield return new WaitForSeconds(0.35f);
        yield return FadePanel(1f, 0f);
    }

    void TeleportHome()
    {
        Vector3 home = homePosition.position;
        home.z = transform.position.z;

        if (_rb != null)
        {
            _rb.position = home;
            _rb.linearVelocity = Vector2.zero;
        }
        else
        {
            transform.position = home;
        }
    }

    IEnumerator FadePanel(float from, float to)
    {
        if (fadePanel == null)
            yield break;

        fadePanel.blocksRaycasts = to > from;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeDuration <= 0f ? 1f : elapsed / fadeDuration;
            fadePanel.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        fadePanel.alpha = to;
        fadePanel.blocksRaycasts = to > 0.01f;
    }
}
