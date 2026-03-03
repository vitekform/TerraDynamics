using UnityEngine;

/// <summary>
/// Tracks in-game seasons based on real elapsed time.
/// 1 real-world second  = 1/3600 in-game day
/// 1 real-world hour    = 1 in-game day
/// 4 seasons cycle over <see cref="daysPerSeason"/> × 4 in-game days.
///
/// Add this MonoBehaviour to a persistent GameObject in your scene.
/// Other systems may read <see cref="CurrentSeason"/> and
/// <see cref="SeasonTemperatureDelta"/> at runtime.
/// </summary>
public class SeasonSystem : MonoBehaviour
{
    public enum Season { Spring, Summer, Autumn, Winter }

    [Tooltip("In-game days per season (default 30 = 30 real hours per season).")]
    public int daysPerSeason = 30;

    // °C offset added to base climate temperature for each season
    private static readonly float[] SeasonTempDelta = { 5f, 15f, 0f, -15f };

    // ── Runtime state ────────────────────────────────────────────────────────

    /// <summary>Total real-time hours elapsed since the world started.</summary>
    private float _elapsedHours;

    /// <summary>Current season.</summary>
    public Season CurrentSeason { get; private set; } = Season.Spring;

    /// <summary>Day index within the current season (0 = first day).</summary>
    public int CurrentSeasonDay { get; private set; }

    /// <summary>Total in-game days elapsed since world start.</summary>
    public int TotalDays { get; private set; }

    /// <summary>Fractional time of day: 0 = midnight, 0.5 = noon, 1 = next midnight.</summary>
    public float TimeOfDay { get; private set; }

    /// <summary>Temperature offset (°C) to add to the climate map for the current season.</summary>
    public float SeasonTemperatureDelta => SeasonTempDelta[(int)CurrentSeason];

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Update()
    {
        // 1 real second = 1/3600 in-game day  →  Time.deltaTime / 3600
        _elapsedHours += Time.deltaTime / 3600f;

        TotalDays     = Mathf.FloorToInt(_elapsedHours);
        TimeOfDay     = _elapsedHours - TotalDays;          // 0–1 fractional day

        int seasonIndex    = (TotalDays / daysPerSeason) % 4;
        CurrentSeason      = (Season)seasonIndex;
        CurrentSeasonDay   = TotalDays % daysPerSeason;
    }
}
