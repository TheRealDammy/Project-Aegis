using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Market panel — branch market share with bar visualization.
/// QA-010 PARTIAL CLOSURE: share percentages and bars render correctly.
/// DEFERRED: chart lines over time (requires historical data tracking).
/// </summary>
public class MarketPanel
{
    private readonly VisualElement _container;
    private readonly MarketManager _marketManager;

    public MarketPanel(VisualElement container, MarketManager marketManager)
    {
        _container = container;
        _marketManager = marketManager;
    }

    public void Build()
    {
        _container.Clear();

        var panel = new VisualElement();
        panel.AddToClassList("market-panel");
        _container.Add(panel);

        AddSectionLabel(panel, "MARKET SHARE BY BRANCH");
        AddLegend(panel);

        foreach (ResearchBranch branch in System.Enum.GetValues(typeof(ResearchBranch)))
            panel.Add(BuildBranchRow(branch));

        panel.Add(BuildSummaryRow());

        Refresh();
    }

    private VisualElement _panelRoot;
    private VisualElement _summaryRow;
    private readonly System.Collections.Generic.List<BranchRowVEs> _branchRows = new();

    private struct BranchRowVEs
    {
        public ResearchBranch Branch;
        public VisualElement PlayerBar;
        public VisualElement RivalBar;
        public Label PctLabel;
    }

    private VisualElement BuildBranchRow(ResearchBranch branch)
    {
        var row = new VisualElement();
        row.AddToClassList("market-branch-row");

        var header = new VisualElement();
        header.AddToClassList("market-branch-header");

        var nameLabel = new Label(branch.ToString().ToUpper());
        nameLabel.AddToClassList("market-branch-name");
        header.Add(nameLabel);

        var pctLabel = new Label("0%");
        pctLabel.AddToClassList("market-branch-pct");
        header.Add(pctLabel);

        row.Add(header);

        var track = new VisualElement();
        track.AddToClassList("market-bar-track");

        var playerBar = new VisualElement();
        playerBar.AddToClassList("market-bar-player");
        playerBar.style.width = Length.Percent(0f);

        var rivalBar = new VisualElement();
        rivalBar.AddToClassList("market-bar-rivals");
        rivalBar.style.width = Length.Percent(0f);

        track.Add(playerBar);
        track.Add(rivalBar);
        row.Add(track);

        _branchRows.Add(new BranchRowVEs
        {
            Branch = branch,
            PlayerBar = playerBar,
            RivalBar = rivalBar,
            PctLabel = pctLabel
        });

        return row;
    }

    private VisualElement BuildSummaryRow()
    {
        _summaryRow = new VisualElement();
        _summaryRow.AddToClassList("market-summary");

        var label = new Label("AVERAGE MARKET SHARE");
        label.AddToClassList("market-summary-label");
        _summaryRow.Add(label);

        var value = new Label("0%");
        value.AddToClassList("market-summary-value");
        value.name = "MarketSummaryValue";
        _summaryRow.Add(value);

        return _summaryRow;
    }

    public void Refresh()
    {
        if (_marketManager == null) return;

        float averageShare = _marketManager.GetAveragePlayerShare();

        foreach (BranchRowVEs row in _branchRows)
        {
            float playerShare = 0f;
            if (_marketManager.PlayerShare.TryGetValue(row.Branch, out float s))
                playerShare = s;

            float playerPct = playerShare * 100f;
            float rivalPct = 100f - playerPct;

            row.PlayerBar.style.width = Length.Percent(playerPct);
            row.RivalBar.style.width = Length.Percent(rivalPct);
            row.PctLabel.text = $"{playerPct:F1}%";
        }

        // Summary
        if (_summaryRow != null)
        {
            var summaryValue = _summaryRow.Q<Label>("MarketSummaryValue");
            if (summaryValue != null)
            {
                summaryValue.text = $"{averageShare * 100f:F1}%";
                summaryValue.RemoveFromClassList("market-victory-close");
                summaryValue.RemoveFromClassList("market-victory-achieved");

                if (averageShare >= AegisConstants.WIN_MARKET_SHARE_THRESHOLD)
                    summaryValue.AddToClassList("market-victory-achieved");
                else if (averageShare >= AegisConstants.WIN_MARKET_SHARE_THRESHOLD * 0.85f)
                    summaryValue.AddToClassList("market-victory-close");
            }
        }
    }

    private static void AddSectionLabel(VisualElement parent, string text)
    {
        var label = new Label(text);
        label.AddToClassList("emp-section-label");
        parent.Add(label);
    }

    private static void AddLegend(VisualElement parent)
    {
        var legend = new VisualElement();
        legend.style.flexDirection = FlexDirection.Row;
        legend.style.marginBottom = 12f;
        legend.style.marginTop = 4f;

        AddLegendItem(legend, "#0A8A8A", "Aegis Systems");
        AddLegendItem(legend, "#2E4060", "Combined rivals");

        parent.Add(legend);
    }

    private static void AddLegendItem(VisualElement parent, string color, string label)
    {
        var item = new VisualElement();
        item.style.flexDirection = FlexDirection.Row;
        item.style.alignItems = Align.Center;
        item.style.marginRight = 16f;

        var swatch = new VisualElement();
        swatch.style.width = 12f;
        swatch.style.height = 12f;
        swatch.style.backgroundColor = new StyleColor(HexToColor(color));
        swatch.style.marginRight = 4f;

        var text = new Label(label);
        text.AddToClassList("cell-text-secondary");

        item.Add(swatch);
        item.Add(text);
        parent.Add(item);
    }

    private static Color HexToColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}