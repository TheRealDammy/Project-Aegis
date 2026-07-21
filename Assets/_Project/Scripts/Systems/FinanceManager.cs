using System;
using UnityEngine;

/// <summary>
/// Tracks company cash balance. Deducts weekly salaries, credits contract rewards.
/// M2: salary deductions only. Contract rewards wired at M3 with contract delivery.
/// </summary>
public class FinanceManager : MonoBehaviour
{
    // — Static Events ——————————————————————————————————————
    /// <summary>Fires whenever cash balance changes, with the new balance.</summary>
    public static event Action<float> OnCashChanged;

    // — Public Properties ——————————————————————————————————
    public float CashBalance { get; private set; }
    /// <summary>
    /// Average weekly revenue over the game's history.
    /// Used by WinConditionManager for the financial valuation formula.
    /// Updated at the start of each tick before salary deductions.
    /// </summary>
    public float WeeklyRevenueAverage { get; private set; }

    // — Serialized Fields ——————————————————————————————————
    [SerializeField] private EmployeeManager _employeeManager;

    // - Private Fields ————————————————————————————————————
    private float _cumulativeRevenue = 0f;
    private int _weekCount = 0;

    // — Unity Lifecycle ————————————————————————————————————
    private void Start()
    {
        CashBalance = AegisConstants.STARTING_CASH;
        OnCashChanged?.Invoke(CashBalance);
        Debug.Log($"[FinanceManager] Starting cash: £{CashBalance:N0}");
    }

    private void OnEnable()
    {
        TimeManager.OnWeekTick += HandleWeekTick;
        ContractManager.OnContractCompleted += HandleContractCompleted;
        ContractManager.OnContractFailed += HandleContractFailed;
    }

    private void OnDisable()
    {
        TimeManager.OnWeekTick -= HandleWeekTick;
        ContractManager.OnContractCompleted -= HandleContractCompleted;
        ContractManager.OnContractFailed -= HandleContractFailed;
    }

    // — Public Methods —————————————————————————————————————

    /// <summary>Credits cash. Use for contract rewards (M3+).</summary>
    public void AddRevenue(float amount)
    {
        if (amount < 0f)
        {
            Debug.LogWarning("[FinanceManager] AddRevenue called with negative amount.");
            return;
        }
        CashBalance += amount;
        _cumulativeRevenue += amount;   // NEW — feeds weekly average
        OnCashChanged?.Invoke(CashBalance);
    }

    /// <summary>Deducts cash. Use for contract costs, facility payments.</summary>
    public void DeductCost(float amount)
    {
        CashBalance -= amount;
        // Cash can go negative — bankruptcy detection is M4 game logic.
        OnCashChanged?.Invoke(CashBalance);
    }

    public void PopulateSaveData(GameSaveData data)
    {
        data.CashBalance = CashBalance;
        data.FinanceCumulativeRevenue = _cumulativeRevenue;
        data.FinanceWeekCount = _weekCount;
    }

    public void LoadFromSaveData(GameSaveData data)
    {
        CashBalance = data.CashBalance;
        OnCashChanged?.Invoke(CashBalance);
        _cumulativeRevenue = data.FinanceCumulativeRevenue;
        _weekCount = data.FinanceWeekCount;
        WeeklyRevenueAverage = _weekCount > 0 ? _cumulativeRevenue / _weekCount : 0f;
        Debug.Log($"[FinanceManager] Loaded cash: £{CashBalance:N0}.");
    }

    // — Private Methods ————————————————————————————————————
    private void HandleWeekTick()
    {
        // Update average before deducting costs for this week.
        _weekCount++;
        WeeklyRevenueAverage = _weekCount > 0 ? _cumulativeRevenue / _weekCount : 0f;
        DeductWeeklySalaries();
    }

    private void DeductWeeklySalaries()
    {
        if (_employeeManager == null) return;

        float totalSalaries = 0f;
        foreach (Employee emp in _employeeManager.Employees)
            totalSalaries += emp.WeeklySalary;

        if (totalSalaries <= 0f) return;

        DeductCost(totalSalaries);
        Debug.Log($"[FinanceManager] Weekly salaries deducted: £{totalSalaries:N0}. " +
                  $"Balance: £{CashBalance:N0}");
    }

    private void HandleContractCompleted(Contract contract)
    {
        AddRevenue(contract.BaseRewardGBP);
        Debug.Log($"[FinanceManager] Revenue received: £{contract.BaseRewardGBP:N0}.");
    }

    private void HandleContractFailed(Contract contract)
    {
        float penalty = contract.BaseCostGBP * AegisConstants.CONTRACT_FAILURE_PENALTY_RATIO;
        if (penalty > 0f) DeductCost(penalty);
    }
}