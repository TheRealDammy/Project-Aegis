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

    // — Serialized Fields ——————————————————————————————————
    [SerializeField] private EmployeeManager _employeeManager;

    // — Unity Lifecycle ————————————————————————————————————
    private void Start()
    {
        CashBalance = AegisConstants.STARTING_CASH;
        OnCashChanged?.Invoke(CashBalance);
        Debug.Log($"[FinanceManager] Starting cash: £{CashBalance:N0}");
    }

    private void OnEnable() => TimeManager.OnWeekTick += HandleWeekTick;
    private void OnDisable() => TimeManager.OnWeekTick -= HandleWeekTick;

    // — Public Methods —————————————————————————————————————

    /// <summary>Credits cash. Use for contract rewards (M3+).</summary>
    public void AddRevenue(float amount)
    {
        if (amount < 0f)
        {
            Debug.LogWarning("[FinanceManager] AddRevenue called with negative amount. Use DeductCost.");
            return;
        }

        CashBalance += amount;
        OnCashChanged?.Invoke(CashBalance);
    }

    /// <summary>Deducts cash. Use for contract costs, facility payments.</summary>
    public void DeductCost(float amount)
    {
        CashBalance -= amount;
        // Cash can go negative — bankruptcy detection is M4 game logic.
        OnCashChanged?.Invoke(CashBalance);
    }

    // — Private Methods ————————————————————————————————————
    private void HandleWeekTick()
    {
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
}