/// <summary>
/// The three campaign victory conditions for Project Aegis.
/// All three are checked each week by WinConditionManager.
/// Sandbox mode suppresses all checks per DD-16.
/// </summary>
public enum VictoryType
{
    Financial,    // Valuation >= £500,000,000
    Technology,   // All 17 research nodes Complete
    Market        // Average market share across all branches >= 60%
}