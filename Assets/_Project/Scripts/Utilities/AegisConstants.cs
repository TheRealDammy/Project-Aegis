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
}