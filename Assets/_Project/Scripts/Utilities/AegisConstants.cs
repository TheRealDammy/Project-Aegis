/// <summary>
/// Project-wide constants. No magic strings or numbers anywhere else in the codebase.
/// Add to this file as new systems require shared values.
/// </summary>
public static class AegisConstants
{
    // — Scene Names ———————————————————————————————————————————
    public const string SCENE_BOOT = "Boot";
    public const string SCENE_MAIN_MENU = "MainMenu";
    public const string SCENE_GAME = "Game";

    // — Panel Names ———————————————————————————————————————————
    // Must match the text displayed in the content area and logged on tab click.
    public const string PANEL_OVERVIEW = "Overview";
    public const string PANEL_RESEARCH = "Research";
    public const string PANEL_EMPLOYEES = "Employees";
    public const string PANEL_CONTRACTS = "Contracts";
    public const string PANEL_MARKET = "Market";
    public const string PANEL_WORLD = "World";

    // — HUD Element Names ————————————————————————————————————
    // Must exactly match 'name' attributes in GameHud.uxml.
    // If names drift between UXML and these constants, Q<>() calls return null silently.
    public const string HUD_COMPANY_LABEL = "CompanyNameLabel";
    public const string HUD_CASH_LABEL = "CashLabel";
    public const string HUD_WEEK_LABEL = "WeekLabel";
    public const string HUD_ACTIVE_PANEL_LABEL = "ActivePanelLabel";
    public const string HUD_NAV_OVERVIEW = "NavOverview";
    public const string HUD_NAV_RESEARCH = "NavResearch";
    public const string HUD_NAV_EMPLOYEES = "NavEmployees";
    public const string HUD_NAV_CONTRACTS = "NavContracts";
    public const string HUD_NAV_MARKET = "NavMarket";
    public const string HUD_NAV_WORLD = "NavWorld";
    public const string HUD_PAUSE_BUTTON = "PauseButton";
    public const string HUD_SPEED1_BUTTON = "Speed1Button";
    public const string HUD_SPEED2_BUTTON = "Speed2Button";
    public const string HUD_SPEED4_BUTTON = "Speed4Button";

    // — Time Simulation ——————————————————————————————————————
    public const float TICK_INTERVAL_1X = 2.0f;
    public const float TICK_INTERVAL_2X = 1.0f;
    public const float TICK_INTERVAL_4X = 0.5f;

    // — Contract Risk Formula (OQ-02) ————————————————————————
    public const float BASE_CONTRACT_CHANCE = 50f;
    public const float TEAM_BONUS_MULTIPLIER = 0.6f;
    public const float COMPLEXITY_PENALTY_PER_TIER = 8f;
    public const float MAX_BUDGET_BONUS = 20f;
    public const float MIN_CONTRACT_CHANCE = 5f;
    public const float MAX_CONTRACT_CHANCE = 95f;

    // — Hiring Pool (OQ-03) ——————————————————————————————————
    public const int HIRING_POOL_SIZE_PHASE_1 = 5;
    public const int HIRING_POOL_SIZE_PHASE_2 = 6;
    public const int HIRING_POOL_SIZE_PHASE_3 = 7;
    public const int CANDIDATE_WEEKS_AVAILABLE = 2;

    // Stat generation ranges per phase
    public const float STAT_RANGE_PHASE1_MIN = 25f;
    public const float STAT_RANGE_PHASE1_MAX = 65f;
    public const float STAT_RANGE_PHASE2_MIN = 40f;
    public const float STAT_RANGE_PHASE2_MAX = 78f;
    public const float STAT_RANGE_PHASE3_MIN = 55f;
    public const float STAT_RANGE_PHASE3_MAX = 90f;

    // Trait count probability thresholds (cumulative)
    public const float TRAIT_PROB_ZERO = 0.40f;  // 0–0.40  → 0 traits
    public const float TRAIT_PROB_ONE_CUMULATIVE = 0.85f;  // 0.40–0.85 → 1 trait
                                                           // 0.85–1.0  → 2 traits

    // Base weekly salaries (GBP)
    public const float SALARY_ENGINEER = 1800f;
    public const float SALARY_RESEARCHER = 2000f;
    public const float SALARY_SALES_MANAGER = 2500f;
    public const float SALARY_EXECUTIVE = 4000f;

    // — Stat Names (Dictionary keys in Employee.Stats) ————————
    // Use these constants everywhere — never hardcode the strings.
    public const string STAT_EFFICIENCY = "Efficiency";
    public const string STAT_INTELLIGENCE = "Intelligence";
    public const string STAT_CREATIVITY = "Creativity";
    public const string STAT_RESEARCH_SPEED = "ResearchSpeed";
    public const string STAT_INNOVATION = "Innovation";
    public const string STAT_NEGOTIATION = "Negotiation";
    public const string STAT_NETWORKING = "Networking";
    public const string STAT_LEADERSHIP = "Leadership";
    public const string STAT_STRATEGY = "Strategy";
}