using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Manages the employee roster and the weekly hiring pool.
/// M1: Full hiring pool implementation per OQ-03.
/// M2+: Assignment system, weekly productivity, fire/promote logic.
/// </summary>
public class EmployeeManager : MonoBehaviour
{
    // — Static Events ——————————————————————————————————————
    public static event Action<IReadOnlyList<HiringCandidate>> OnHiringPoolRefreshed;
    public static event Action<Employee> OnEmployeeHired;
    public static event Action<Employee> OnEmployeeFired;

    // — Serialized Fields ——————————————————————————————————
    /// <summary>
    /// All TraitSOs available for candidate generation. Assign in Inspector
    /// from Assets/_Project/Data/Employees/. Empty = no traits generated (valid for Phase 1 testing).
    /// </summary>
    [SerializeField] private TraitSO[] _availableTraits;
    [SerializeField] private bool _autoHireForTesting = false;

    // — Public Properties ——————————————————————————————————
    public IReadOnlyList<HiringCandidate> HiringPool => _hiringPool;
    public IReadOnlyList<Employee> Employees => _employees;

    // — Private Fields ————————————————————————————————————
    private readonly List<HiringCandidate> _hiringPool = new List<HiringCandidate>();
    private readonly List<Employee> _employees = new List<Employee>();

    // Phase is hardcoded to 1 at M1 — wired to ReputationManager.OnTierChanged in M2.
    private int _currentPhase = 1;

    // — Static Name Tables ————————————————————————————————
    // International, role-neutral names. Expand post-launch for flavour.
    private static readonly string[] _firstNames =
    {
        "Alex", "Jordan", "Morgan", "Casey", "Riley", "Taylor", "Jamie", "Avery",
        "Reece", "Drew", "Blake", "Quinn", "Sam", "Charlie", "Hayden", "Robin",
        "Skyler", "Cameron", "Dakota", "Jessie"
    };

    private static readonly string[] _lastNames =
    {
        "Chen", "Williams", "Okafor", "Martinez", "Singh", "Nakamura", "Petrov",
        "Nielsen", "Ali", "Kowalski", "Ibrahim", "Santos", "Osei", "Fischer",
        "Park", "Walsh", "Nguyen", "Okonkwo", "Lindberg", "Moreau"
    };

    // — Unity Lifecycle ————————————————————————————————————
    private void OnEnable()
    {
        TimeManager.OnWeekTick += HandleWeekTick;
        ReputationManager.OnTierChanged += HandleTierChanged; // QA-004
    }

    private void OnDisable()
    {
        TimeManager.OnWeekTick -= HandleWeekTick;
        ReputationManager.OnTierChanged -= HandleTierChanged;
    }

    private void Start()
    {
        FillPool();
        Debug.Log($"[EmployeeManager] Pool initialised: {_hiringPool.Count} candidates.");

        if (_autoHireForTesting)
            AutoHireForTesting();
    }

    /// <summary>
    /// DEV ONLY — auto-hires one Researcher and one Engineer for testing without
    /// the EMP panel. Enable via Inspector checkbox. Remove when EMP panel is built.
    /// </summary>
    private void AutoHireForTesting()
    {
        bool hiredResearcher = false;
        bool hiredEngineer = false;

        // Iterate a copy — HireCandidate modifies the pool.
        var snapshot = new System.Collections.Generic.List<HiringCandidate>(_hiringPool);
        foreach (HiringCandidate candidate in snapshot)
        {
            if (!hiredResearcher && candidate.Role == EmployeeRole.Researcher)
            {
                HireCandidate(candidate);
                hiredResearcher = true;
                Debug.Log($"[DEV] Auto-hired Researcher: {candidate.Name}");
            }
            else if (!hiredEngineer && candidate.Role == EmployeeRole.Engineer)
            {
                HireCandidate(candidate);
                hiredEngineer = true;
                Debug.Log($"[DEV] Auto-hired Engineer: {candidate.Name}");
            }

            if (hiredResearcher && hiredEngineer) break;
        }

        if (!hiredResearcher)
            Debug.LogWarning("[DEV] No Researcher in initial pool — re-enter Play mode to retry.");
        if (!hiredEngineer)
            Debug.LogWarning("[DEV] No Engineer in initial pool — re-enter Play mode to retry.");
    }

    private void HandleTierChanged(int newTier)
    {
        // QA-004 closed. Phase now driven by reputation tier.
        // Tier 1 = Phase 1, Tier 2–3 = Phase 2, Tier 4–5 = Phase 3.
        _currentPhase = newTier switch
        {
            1 => 1,
            2 or 3 => 2,
            4 or 5 => 3,
            _ => 1
        };
        Debug.Log($"[EmployeeManager] Phase updated to {_currentPhase} (Reputation Tier {newTier}).");
    }

    // — Public Methods —————————————————————————————————————

    /// <summary>
    /// Hires a candidate from the pool, adds them to the roster,
    /// and immediately replaces them in the pool with a new candidate.
    /// Returns false if the candidate is no longer in the pool.
    /// </summary>
    public bool HireCandidate(HiringCandidate candidate)
    {
        if (!_hiringPool.Contains(candidate))
        {
            Debug.LogWarning($"[EmployeeManager] Cannot hire '{candidate.Name}' — no longer in pool.");
            return false;
        }

        Employee newEmployee = candidate.ToEmployee();
        _employees.Add(newEmployee);
        _hiringPool.Remove(candidate);

        // Immediate replacement keeps pool at target size after a hire.
        _hiringPool.Add(GenerateCandidate());

        OnEmployeeHired?.Invoke(newEmployee);
        Debug.Log($"[EmployeeManager] Hired: {newEmployee.Name} ({newEmployee.Role}) @ £{newEmployee.WeeklySalary}/wk.");
        return true;
    }

    /// <summary>Removes an employee from the roster.</summary>
    public bool FireEmployee(Employee employee)
    {
        if (!_employees.Contains(employee))
        {
            Debug.LogWarning($"[EmployeeManager] Cannot fire '{employee.Name}' — not on roster.");
            return false;
        }

        _employees.Remove(employee);
        OnEmployeeFired?.Invoke(employee);
        Debug.Log($"[EmployeeManager] Fired: {employee.Name}.");
        return true;
    }

    /// <summary>
    /// Finds an active roster employee by exact name match.
    /// Returns null if not found. Name-based lookup is a temporary M3 solution —
    /// replace with stable employee ID at save/load implementation (M4).
    /// </summary>
    public Employee GetEmployeeByName(string name)
    {
        foreach (Employee emp in _employees)
        {
            if (emp.Name == name)
                return emp;
        }
        return null;
    }

    // — Private Methods ————————————————————————————————————

    private void HandleWeekTick()
    {
        TickHiringPool();
    }

    private void TickHiringPool()
    {
        // Decrement availability. Iterate backwards to safely remove during loop.
        for (int i = _hiringPool.Count - 1; i >= 0; i--)
        {
            _hiringPool[i].WeeksAvailable--;

            if (_hiringPool[i].WeeksAvailable <= 0)
            {
                _hiringPool.RemoveAt(i);
                _hiringPool.Add(GenerateCandidate());
            }
        }

        // Top up if below target — covers phase transitions and edge cases.
        FillPool();

        OnHiringPoolRefreshed?.Invoke(_hiringPool);
    }

    private void FillPool()
    {
        int target = GetTargetPoolSize();
        while (_hiringPool.Count < target)
            _hiringPool.Add(GenerateCandidate());
    }

    private int GetTargetPoolSize()
    {
        return _currentPhase switch
        {
            1 => AegisConstants.HIRING_POOL_SIZE_PHASE_1,
            2 => AegisConstants.HIRING_POOL_SIZE_PHASE_2,
            3 => AegisConstants.HIRING_POOL_SIZE_PHASE_3,
            _ => AegisConstants.HIRING_POOL_SIZE_PHASE_1
        };
    }

    private HiringCandidate GenerateCandidate()
    {
        EmployeeRole role = RollRole();
        Dictionary<string, float> stats = GenerateStats(role);
        List<TraitSO> traits = RollTraits();
        float salary = CalculateSalary(role, traits);
        string name = GenerateName();

        return new HiringCandidate
        {
            Name = name,
            Role = role,
            Stats = stats,
            Traits = traits,
            WeeklySalary = salary,
            WeeksAvailable = AegisConstants.CANDIDATE_WEEKS_AVAILABLE
        };
    }

    /// <summary>
    /// Phase-weighted role selection. Weights favour Engineers at Phase 1
    /// and broaden toward Executives at Phase 3.
    /// REVIEW: confirm weights with System Designer before M2 balance pass.
    /// </summary>
    private EmployeeRole RollRole()
    {
        float roll = Random.value;

        if (_currentPhase == 1)
        {
            if (roll < 0.60f) return EmployeeRole.Engineer;
            if (roll < 0.85f) return EmployeeRole.Researcher;
            if (roll < 0.95f) return EmployeeRole.SalesManager;
            return EmployeeRole.Executive;
        }
        if (_currentPhase == 2)
        {
            if (roll < 0.40f) return EmployeeRole.Engineer;
            if (roll < 0.70f) return EmployeeRole.Researcher;
            if (roll < 0.90f) return EmployeeRole.SalesManager;
            return EmployeeRole.Executive;
        }
        // Phase 3
        {
            if (roll < 0.30f) return EmployeeRole.Engineer;
            if (roll < 0.55f) return EmployeeRole.Researcher;
            if (roll < 0.80f) return EmployeeRole.SalesManager;
            return EmployeeRole.Executive;
        }
    }

    private Dictionary<string, float> GenerateStats(EmployeeRole role)
    {
        (float min, float max) = GetStatRange();
        var stats = new Dictionary<string, float>();

        foreach (string statName in GetStatNamesForRole(role))
            stats[statName] = Random.Range(min, max);

        return stats;
    }

    private (float min, float max) GetStatRange()
    {
        return _currentPhase switch
        {
            1 => (AegisConstants.STAT_RANGE_PHASE1_MIN, AegisConstants.STAT_RANGE_PHASE1_MAX),
            2 => (AegisConstants.STAT_RANGE_PHASE2_MIN, AegisConstants.STAT_RANGE_PHASE2_MAX),
            3 => (AegisConstants.STAT_RANGE_PHASE3_MIN, AegisConstants.STAT_RANGE_PHASE3_MAX),
            _ => (AegisConstants.STAT_RANGE_PHASE1_MIN, AegisConstants.STAT_RANGE_PHASE1_MAX)
        };
    }

    private static string[] GetStatNamesForRole(EmployeeRole role)
    {
        return role switch
        {
            EmployeeRole.Engineer => new[] { AegisConstants.STAT_EFFICIENCY, AegisConstants.STAT_INTELLIGENCE, AegisConstants.STAT_CREATIVITY },
            EmployeeRole.Researcher => new[] { AegisConstants.STAT_RESEARCH_SPEED, AegisConstants.STAT_INNOVATION },
            EmployeeRole.SalesManager => new[] { AegisConstants.STAT_NEGOTIATION, AegisConstants.STAT_NETWORKING },
            EmployeeRole.Executive => new[] { AegisConstants.STAT_LEADERSHIP, AegisConstants.STAT_STRATEGY },
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// Draws traits without replacement using a partial Fisher-Yates shuffle.
    /// 40% chance of 0 traits, 45% of 1, 15% of 2 — per OQ-03.
    /// </summary>
    private List<TraitSO> RollTraits()
    {
        var result = new List<TraitSO>();

        if (_availableTraits == null || _availableTraits.Length == 0)
            return result;

        float roll = Random.value;
        int count;

        if (roll < AegisConstants.TRAIT_PROB_ZERO) count = 0;
        else if (roll < AegisConstants.TRAIT_PROB_ONE_CUMULATIVE) count = 1;
        else count = 2;

        if (count == 0) return result;

        // Partial shuffle — swap each selected index with a random remaining index.
        var pool = new List<TraitSO>(_availableTraits);
        int draws = Mathf.Min(count, pool.Count);

        for (int i = 0; i < draws; i++)
        {
            int j = Random.Range(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
            result.Add(pool[i]);
        }

        return result;
    }

    private float CalculateSalary(EmployeeRole role, List<TraitSO> traits)
    {
        float baseSalary = GetBaseSalary(role);
        float multiplier = 1f;

        foreach (TraitSO trait in traits)
            multiplier *= trait.SalaryMultiplier;

        return baseSalary * multiplier;
    }

    private static float GetBaseSalary(EmployeeRole role)
    {
        return role switch
        {
            EmployeeRole.Engineer => AegisConstants.SALARY_ENGINEER,
            EmployeeRole.Researcher => AegisConstants.SALARY_RESEARCHER,
            EmployeeRole.SalesManager => AegisConstants.SALARY_SALES_MANAGER,
            EmployeeRole.Executive => AegisConstants.SALARY_EXECUTIVE,
            _ => AegisConstants.SALARY_ENGINEER
        };
    }

    private static string GenerateName()
    {
        string first = _firstNames[Random.Range(0, _firstNames.Length)];
        string last = _lastNames[Random.Range(0, _lastNames.Length)];
        return $"{first} {last}";
    }
}