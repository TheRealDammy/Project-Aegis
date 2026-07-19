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

    // — Public Properties ——————————————————————————————————
    public IReadOnlyList<HiringCandidate> HiringPool => _hiringPool;
    public IReadOnlyList<Employee> Employees => _employees;

    // — Private Fields ————————————————————————————————————
    private readonly List<HiringCandidate> _hiringPool = new List<HiringCandidate>();
    private readonly List<Employee> _employees = new List<Employee>();

    // Persisted in save data — must not reset on load.
    // Ensures IDs are unique across save/load cycles.
    private int _employeeIdCounter = 0;

    private string GenerateEmployeeId() =>
        string.Format(AegisConstants.EMPLOYEE_ID_FORMAT, ++_employeeIdCounter);

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
    }

    private void HandleTierChanged(int newTier)
    {
        // QA-005: Tier 1–2 → Phase 1, Tier 3 → Phase 2, Tier 4–5 → Phase 3.
        // Supersedes M3 mapping where Tier 2 was Phase 2.
        _currentPhase = newTier switch
        {
            1 or 2 => 1,
            3 => 2,
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
    /// Finds a roster employee by stable EmployeeId. Returns null if not found.
    /// Always use this method for inter-manager employee resolution.
    /// Name-based lookup is prohibited — names are not unique.
    /// </summary>
    public Employee GetEmployeeById(string employeeId)
    {
        if (string.IsNullOrEmpty(employeeId)) return null;
        foreach (Employee emp in _employees)
            if (emp.EmployeeId == employeeId) return emp;
        return null;
    }

    public void PopulateSaveData(GameSaveData data)
    {
        data.EmployeeIdCounter = _employeeIdCounter;

        data.Employees = new List<EmployeeSaveData>();
        foreach (Employee emp in _employees)
            data.Employees.Add(EmployeeToSaveData(emp));

        data.HiringPool = new List<HiringCandidateSaveData>();
        foreach (HiringCandidate candidate in _hiringPool)
            data.HiringPool.Add(CandidateToSaveData(candidate));
    }

    public void LoadFromSaveData(GameSaveData data)
    {
        _employeeIdCounter = data.EmployeeIdCounter;
        _employees.Clear();
        _hiringPool.Clear();

        Dictionary<string, TraitSO> traitLookup = BuildTraitLookup();

        if (data.Employees != null)
            foreach (EmployeeSaveData d in data.Employees)
                _employees.Add(SaveDataToEmployee(d, traitLookup));

        if (data.HiringPool != null)
            foreach (HiringCandidateSaveData d in data.HiringPool)
                _hiringPool.Add(SaveDataToCandidate(d, traitLookup));

        OnHiringPoolRefreshed?.Invoke(_hiringPool);
        Debug.Log($"[EmployeeManager] Loaded {_employees.Count} employees, " +
                  $"{_hiringPool.Count} candidates. ID counter: {_employeeIdCounter}.");
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
            EmployeeId = GenerateEmployeeId(),   // NEW
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

        // Threshold values derived from cumulative weights in AegisConstants.
        // Phase 1 example: [0,0.60) = Engineer, [0.60,0.85) = Researcher, [0.85,0.95) = Sales, [0.95,1] = Executive
        (float eng, float res, float sal) = _currentPhase switch
        {
            1 => (AegisConstants.ROLE_WEIGHT_P1_ENGINEER,
                  AegisConstants.ROLE_WEIGHT_P1_ENGINEER + AegisConstants.ROLE_WEIGHT_P1_RESEARCHER,
                  AegisConstants.ROLE_WEIGHT_P1_ENGINEER + AegisConstants.ROLE_WEIGHT_P1_RESEARCHER
                                                         + AegisConstants.ROLE_WEIGHT_P1_SALES),
            2 => (AegisConstants.ROLE_WEIGHT_P2_ENGINEER,
                  AegisConstants.ROLE_WEIGHT_P2_ENGINEER + AegisConstants.ROLE_WEIGHT_P2_RESEARCHER,
                  AegisConstants.ROLE_WEIGHT_P2_ENGINEER + AegisConstants.ROLE_WEIGHT_P2_RESEARCHER
                                                         + AegisConstants.ROLE_WEIGHT_P2_SALES),
            _ => (AegisConstants.ROLE_WEIGHT_P3_ENGINEER,
                  AegisConstants.ROLE_WEIGHT_P3_ENGINEER + AegisConstants.ROLE_WEIGHT_P3_RESEARCHER,
                  AegisConstants.ROLE_WEIGHT_P3_ENGINEER + AegisConstants.ROLE_WEIGHT_P3_RESEARCHER
                                                         + AegisConstants.ROLE_WEIGHT_P3_SALES)
        };

        if (roll < eng) return EmployeeRole.Engineer;
        if (roll < res) return EmployeeRole.Researcher;
        if (roll < sal) return EmployeeRole.SalesManager;
        return EmployeeRole.Executive;
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

    // — Save conversion helpers ————————————————————————————

    private EmployeeSaveData EmployeeToSaveData(Employee emp)
    {
        var traitIds = new List<string>();
        foreach (TraitSO trait in emp.Traits)
            if (!string.IsNullOrEmpty(trait.TraitId)) traitIds.Add(trait.TraitId);

        return new EmployeeSaveData
        {
            EmployeeId = emp.EmployeeId,
            Name = emp.Name,
            Role = emp.Role.ToString(),
            Stats = new Dictionary<string, float>(emp.Stats),
            TraitIds = traitIds,
            WeeklySalary = emp.WeeklySalary,
            Assignment = emp.Assignment,
            Happiness = emp.Happiness
        };
    }

    private HiringCandidateSaveData CandidateToSaveData(HiringCandidate c)
    {
        var traitIds = new List<string>();
        foreach (TraitSO trait in c.Traits)
            if (!string.IsNullOrEmpty(trait.TraitId)) traitIds.Add(trait.TraitId);

        return new HiringCandidateSaveData
        {
            EmployeeId = c.EmployeeId,
            Name = c.Name,
            Role = c.Role.ToString(),
            Stats = new Dictionary<string, float>(c.Stats),
            TraitIds = traitIds,
            WeeklySalary = c.WeeklySalary,
            WeeksAvailable = c.WeeksAvailable
        };
    }

    // — Load conversion helpers ————————————————————————————

    private Employee SaveDataToEmployee(EmployeeSaveData d, Dictionary<string, TraitSO> traitLookup)
    {
        return new Employee
        {
            EmployeeId = d.EmployeeId,
            Name = d.Name,
            Role = ParseRole(d.Role),
            Stats = new Dictionary<string, float>(d.Stats ?? new Dictionary<string, float>()),
            Traits = ResolveTrait(d.TraitIds, traitLookup),
            WeeklySalary = d.WeeklySalary,
            Assignment = d.Assignment,
            Happiness = d.Happiness
        };
    }

    private HiringCandidate SaveDataToCandidate(HiringCandidateSaveData d,
                                                 Dictionary<string, TraitSO> traitLookup)
    {
        return new HiringCandidate
        {
            EmployeeId = d.EmployeeId,
            Name = d.Name,
            Role = ParseRole(d.Role),
            Stats = new Dictionary<string, float>(d.Stats ?? new Dictionary<string, float>()),
            Traits = ResolveTrait(d.TraitIds, traitLookup),
            WeeklySalary = d.WeeklySalary,
            WeeksAvailable = d.WeeksAvailable
        };
    }

    private static EmployeeRole ParseRole(string roleString)
    {
        if (Enum.TryParse(roleString, out EmployeeRole role)) return role;
        Debug.LogWarning($"[EmployeeManager] Unknown role string '{roleString}'. Defaulting to Engineer.");
        return EmployeeRole.Engineer;
    }

    private static List<TraitSO> ResolveTrait(List<string> traitIds,
                                               Dictionary<string, TraitSO> lookup)
    {
        var traits = new List<TraitSO>();
        if (traitIds == null) return traits;

        foreach (string id in traitIds)
        {
            if (lookup.TryGetValue(id, out TraitSO trait))
                traits.Add(trait);
            else
                Debug.LogWarning($"[EmployeeManager] TraitId '{id}' not found in available traits. " +
                                 "Trait skipped — check TraitSO.TraitId fields.");
        }
        return traits;
    }

    private Dictionary<string, TraitSO> BuildTraitLookup()
    {
        var lookup = new Dictionary<string, TraitSO>();
        if (_availableTraits == null) return lookup;
        foreach (TraitSO trait in _availableTraits)
        {
            if (trait == null || string.IsNullOrEmpty(trait.TraitId)) continue;
            if (lookup.ContainsKey(trait.TraitId))
            {
                Debug.LogWarning($"[EmployeeManager] Duplicate TraitId '{trait.TraitId}'. " +
                                 "One will be silently skipped. Fix the TraitSO asset.");
                continue;
            }
            lookup[trait.TraitId] = trait;
        }
        return lookup;
    }
}