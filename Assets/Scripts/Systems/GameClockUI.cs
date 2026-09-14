using TMPro;
using UnityEngine;

/// <summary>
/// Displays the in-game clock from DayNightCycle at the top-center of the screen.
/// </summary>
public class GameClockUI : MonoBehaviour
{
    public TMP_Text clockText;

    void Awake()
    {
        if (clockText == null)
            clockText = GetComponent<TMP_Text>();
    }

    void Update()
    {
        if (clockText == null || DayNightCycle.Instance == null)
            return;

        clockText.text = FormatTime(DayNightCycle.Instance.CurrentHour);
    }

    public static string FormatTime(float hour24)
    {
        hour24 = Mathf.Repeat(hour24, 24f);

        int wholeHour = Mathf.FloorToInt(hour24);
        int minutes = Mathf.FloorToInt((hour24 - wholeHour) * 60f);

        bool isPm = wholeHour >= 12;
        int displayHour = wholeHour % 12;
        if (displayHour == 0)
            displayHour = 12;

        return $"{displayHour}:{minutes:00} {(isPm ? "PM" : "AM")}";
    }
}
