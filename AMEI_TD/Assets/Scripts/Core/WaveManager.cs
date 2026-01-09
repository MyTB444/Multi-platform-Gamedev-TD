using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines enemy counts for a single predefined wave (manual mode).
/// </summary>
[Serializable]
public class WaveDetails
{
    public int enemyBasic;
    public int enemyFast;
    public int enemyTank;
    public int enemyInvisible;
    public int enemyReinforced;
    public int enemySummoner;
    public int enemyHexer;
    public int enemyHerald;
    public int enemyAdaptive;
    public int enemySplitter;
    public int enemyGhostwalk;
    public int enemyDecoy;
}

/// <summary>
/// Configuration for the final "Mega Wave" boss wave with enhanced enemy stats.
/// </summary>
[Serializable]
public class MegaWaveDetails
{
    [Header("Mega Wave Enemy Counts")]
    public int enemyBasic;
    public int enemyFast;
    public int enemyTank;
    public int enemyInvisible;
    public int enemyReinforced;
    public int enemySummoner;
    public int enemyHexer;
    public int enemyHerald;
    public int enemyAdaptive;
    public int enemySplitter;
    public int enemyGhostwalk;
    public int enemyDecoy;

    [Header("Mega Wave Settings")]
    public float spawnRateMultiplier = 2f;      // Enemies spawn faster during mega wave
    public float enemyHealthMultiplier = 1.5f;  // Enemies have increased health
}

/// <summary>
/// Defines spawn probability and limits for a single enemy type within a difficulty tier.
/// Used by the weighted random selection algorithm.
/// </summary>
[Serializable]
public class EnemyWeight
{
    [Tooltip("The enemy type to spawn")]
    public EnemyType enemyType;
    
    [Tooltip("Spawn chance weight - higher = more common relative to other types")]
    [Range(1, 100)] public int weight = 10;
    
    [Tooltip("Guaranteed minimum of this type per wave (0 = no guarantee)")]
    public int minCount = 0;
    
    [Tooltip("Maximum allowed per wave (0 = unlimited)")]
    public int maxCount = 0;
}

/// <summary>
/// Defines a difficulty tier for endless mode. Each tier specifies which enemies
/// can spawn, their weights, and how difficulty scales within that tier's wave range.
/// </summary>
[Serializable]
public class DifficultyTier
{
    [Tooltip("Label for this tier (e.g. Early, Mid, Late)")]
    public string tierName = "New Tier";
    
    [Tooltip("First wave number this tier applies to (e.g. 1 = starts at wave 1)")]
    public int startWave = 1;
    
    [Tooltip("Last wave number for this tier. Set to -1 for infinite (final tier)")]
    public int endWave = 10;
    
    [Tooltip("Base number of enemies on the first wave of this tier")]
    public int baseEnemyCount = 5;
    
    [Tooltip("Extra enemies added per wave within this tier (e.g. 2 = +2 enemies each wave)")]
    public int enemiesPerWaveIncrease = 1;
    
    [Tooltip("How fast enemies spawn (1 = normal, 2 = twice as fast, 0.5 = half speed)")]
    public float spawnRateMultiplier = 1f;
    
    [Tooltip("Enemy types that can spawn in this tier with their weights and min/max caps")]
    public List<EnemyWeight> enemyWeights = new List<EnemyWeight>();
}

/// <summary>
/// Maps enemy types to their prefabs and pool sizes for object pooling.
/// </summary>
[Serializable]
public class EnemyPoolConfig
{
    public EnemyType enemyType;
    public GameObject prefab;
    public int poolAmount = 10;
}

/// <summary>
/// Central wave management system. Handles wave progression, enemy generation,
/// and distribution to spawners. Supports both manual wave definitions and
/// procedural endless mode with difficulty tiers.
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager instance;

    [Header("Wave Settings")]
    [SerializeField] private float timeBetweenWaves = 10f;
    [SerializeField] private float waveTimer;
    [SerializeField] private bool useEndlessWaves = true;

    [Header("Manual Waves (if not endless)")]
    [SerializeField] private WaveDetails[] levelWaves;

    [Header("Endless Wave Settings")]
    [SerializeField] private List<DifficultyTier> difficultyTiers = new List<DifficultyTier>();

    [Header("Mega Wave")]
    [SerializeField] private MegaWaveDetails megaWave;
    [SerializeField] private float megaWaveDelay = 3f;
    private bool megaWaveTriggered = false;
    private bool megaWaveActive = false;
    private float megaWaveStartTime;

    [Header("Enemy Prefabs")]
    [SerializeField] private List<EnemyPoolConfig> enemyPrefabs;

    // Fast lookup from EnemyType enum to prefab reference
    private Dictionary<EnemyType, GameObject> enemyPrefabLookup;
    
    // All spawners in the scene - enemies are distributed round-robin across these
    private List<EnemySpawner> enemySpawners;
    
    private int waveIndex;
    private bool waveTimerEnabled;
    
    // Prevents multiple waves from starting simultaneously
    private bool makingNextWave;
    public bool gameBegan;

    // Cached from current difficulty tier for spawners to query
    private float currentSpawnRateMultiplier = 1f;
    
    // First wave uses initial timer value, subsequent waves use timeBetweenWaves
    private bool isFirstWave = true;
    private float initialWaveTimer;

    private void Awake()
    {
        instance = this;
        enemySpawners = new List<EnemySpawner>(FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None));
        enemyPrefabLookup = new Dictionary<EnemyType, GameObject>();
        initialWaveTimer = waveTimer;
    }

    private void Start()
    {
        // Register all enemy prefabs with the object pool for efficient spawning
        foreach (var config in enemyPrefabs)
        {
            if (config.prefab != null)
            {
                enemyPrefabLookup[config.enemyType] = config.prefab;
                ObjectPooling.instance.Register(config.prefab, config.poolAmount);
            }
        }
    }

    private void Update()
    {
        if (gameBegan == false) return;

        HandleWaveTimer();
        HandleMegaWaveDelay();
    }

    public void ActivateWaveManager()
    {
        gameBegan = true;
        EnableWaveTimer(true);
    }

    public void DeactivateWaveManager()
    {
        gameBegan = false;
        waveTimerEnabled = false;
    }

    /// <summary>
    /// Called by spawners/enemies when wave might be complete.
    /// Checks if all enemies defeated and spawners empty before starting next wave.
    /// </summary>
    public void CheckIfWaveCompleted()
    {
        if (gameBegan == false || GameManager.instance.IsGameLost()) return;

        // All conditions must be met: no active enemies, no pending spawns, not already transitioning
        if (AllEnemiesDefeated() == false || AllSpawnersFinishedSpawning() == false || makingNextWave) return;

        makingNextWave = true;
        waveIndex++;

        // Mega wave has special completion handling
        if (megaWaveActive)
        {
            CheckIfMegaWaveCompleted();
            return;
        }

        // In manual mode, check if we've exhausted all predefined waves
        if (!useEndlessWaves && HasNoMoreWaves())
        {
            Debug.Log("All normal waves complete! Summon the Guardian to trigger the final Mega Wave!");
            return;
        }

        EnableWaveTimer(true);
    }

    /// <summary>
    /// Enables/disables the countdown timer between waves.
    /// </summary>
    private void EnableWaveTimer(bool enable)
    {
        if (enable && GameManager.instance.IsGameLost()) return;
        if (waveTimerEnabled == enable) return;

        // First wave can have different timing than subsequent waves
        waveTimer = isFirstWave ? initialWaveTimer : timeBetweenWaves;
        waveTimerEnabled = enable;
    }

    private void HandleWaveTimer()
    {
        if (waveTimerEnabled == false) return;

        if (GameManager.instance.IsGameLost())
        {
            waveTimerEnabled = false;
            return;
        }

        waveTimer -= Time.deltaTime;
        UIBase.instance.UpdateWaveTimerUI(waveTimer);
        if (waveTimer <= 0) StartNewWave();
    }

    private void StartNewWave()
    {
        if (GameManager.instance.IsGameLost()) return;

        isFirstWave = false;
        GiveEnemiesToSpawners();
        EnableWaveTimer(false);
        makingNextWave = false;
    }

    /// <summary>
    /// Generates the enemy list for current wave and distributes them
    /// round-robin across all spawners for balanced lane pressure.
    /// </summary>
    private void GiveEnemiesToSpawners()
    {
        // Generate enemy list based on mode (endless procedural vs manual)
        List<GameObject> newEnemies = useEndlessWaves ? GenerateEndlessWave() : GetNewEnemies();
        int spawnerIndex = 0;

        if (newEnemies == null || newEnemies.Count == 0) return;

        // Randomize spawn order so enemy types aren't grouped together
        ShuffleList(newEnemies);

        // Distribute enemies evenly across spawners (round-robin)
        for (int i = 0; i < newEnemies.Count; i++)
        {
            GameObject enemyToAdd = newEnemies[i];
            EnemySpawner spawnerToReceiveEnemy = enemySpawners[spawnerIndex];

            spawnerToReceiveEnemy.AddEnemy(enemyToAdd);

            spawnerIndex++;

            // Wrap around to first spawner after reaching the end
            if (spawnerIndex >= enemySpawners.Count) spawnerIndex = 0;
        }

        Debug.Log($"Wave {waveIndex + 1} started: {newEnemies.Count} enemies");
    }

    /// <summary>
    /// Generates a wave using the endless mode difficulty tier system.
    /// Enemy count scales based on wave number within the current tier.
    /// </summary>
    private List<GameObject> GenerateEndlessWave()
    {
        DifficultyTier currentTier = GetCurrentTier();
        if (currentTier == null)
        {
            Debug.LogWarning("No difficulty tier found for wave " + (waveIndex + 1));
            return null;
        }

        // Calculate how many waves into this tier we are
        // Example: Wave 15 in a tier that starts at wave 11 = waveInTier of 4
        int waveInTier = (waveIndex + 1) - currentTier.startWave;
        
        // Linear scaling: base count + (waves into tier * increase per wave)
        int enemyCount = currentTier.baseEnemyCount + (waveInTier * currentTier.enemiesPerWaveIncrease);

        // Cache spawn rate for spawners to query
        currentSpawnRateMultiplier = currentTier.spawnRateMultiplier;

        // Generate enemy composition using weighted random selection
        List<GameObject> enemies = GenerateWeightedEnemies(currentTier, enemyCount);

        Debug.Log($"[Endless Wave {waveIndex + 1}] Tier: {currentTier.tierName} | Enemies: {enemyCount} | Spawn Rate: {currentSpawnRateMultiplier}x");

        return enemies;
    }

    /// <summary>
    /// Finds the difficulty tier that contains the current wave number.
    /// Tiers are defined by start/end wave ranges. EndWave of -1 means infinite.
    /// </summary>
    private DifficultyTier GetCurrentTier()
    {
        int currentWave = waveIndex + 1;

        foreach (var tier in difficultyTiers)
        {
            bool afterStart = currentWave >= tier.startWave;
            // EndWave of -1 means this tier extends infinitely
            bool beforeEnd = tier.endWave == -1 || currentWave <= tier.endWave;

            if (afterStart && beforeEnd)
            {
                return tier;
            }
        }

        // Fallback: if wave exceeds all defined tiers, use the last one
        if (difficultyTiers.Count > 0)
        {
            return difficultyTiers[difficultyTiers.Count - 1];
        }

        return null;
    }

    /// <summary>
    /// Generates enemy list using weighted random selection with min/max constraints.
    /// 
    /// Algorithm:
    /// 1. First pass: Add all guaranteed minimums (minCount for each type)
    /// 2. Second pass: Fill remaining slots using weighted random selection
    ///    - Higher weight = higher probability of selection
    ///    - Types at maxCount are excluded from selection
    /// </summary>
    /// <param name="tier">The difficulty tier defining available enemy types and weights</param>
    /// <param name="totalCount">Total number of enemies to generate</param>
    private List<GameObject> GenerateWeightedEnemies(DifficultyTier tier, int totalCount)
    {
        List<GameObject> enemies = new List<GameObject>();
        
        // Track how many of each type we've added (for max cap enforcement)
        Dictionary<EnemyType, int> spawnCounts = new Dictionary<EnemyType, int>();

        // Initialize all counts to 0
        foreach (var weight in tier.enemyWeights)
        {
            spawnCounts[weight.enemyType] = 0;
        }

        // ===== FIRST PASS: Add guaranteed minimums =====
        // These enemies are always included regardless of weight
        foreach (var weight in tier.enemyWeights)
        {
            if (weight.minCount > 0 && enemyPrefabLookup.ContainsKey(weight.enemyType))
            {
                // Don't exceed total count when adding minimums
                int toAdd = Mathf.Min(weight.minCount, totalCount - enemies.Count);
                for (int i = 0; i < toAdd; i++)
                {
                    enemies.Add(enemyPrefabLookup[weight.enemyType]);
                    spawnCounts[weight.enemyType]++;
                }
            }
        }

        // ===== SECOND PASS: Fill remaining with weighted random =====
        int remainingSlots = totalCount - enemies.Count;
        
        // Calculate total weight of all eligible types (excluding those at max)
        int totalWeight = CalculateTotalWeight(tier.enemyWeights, spawnCounts);

        for (int i = 0; i < remainingSlots; i++)
        {
            // Select random enemy type based on weights
            EnemyType? selectedType = SelectWeightedEnemy(tier.enemyWeights, spawnCounts, totalWeight);

            if (selectedType.HasValue && enemyPrefabLookup.ContainsKey(selectedType.Value))
            {
                enemies.Add(enemyPrefabLookup[selectedType.Value]);
                spawnCounts[selectedType.Value]++;

                // If this type just hit its max, recalculate total weight to exclude it
                var weight = tier.enemyWeights.Find(w => w.enemyType == selectedType.Value);
                if (weight != null && weight.maxCount > 0 && spawnCounts[selectedType.Value] >= weight.maxCount)
                {
                    totalWeight = CalculateTotalWeight(tier.enemyWeights, spawnCounts);
                }
            }
        }

        return enemies;
    }

    /// <summary>
    /// Calculates the sum of weights for all enemy types that haven't hit their max cap.
    /// Used as the denominator for weighted random selection probability.
    /// </summary>
    private int CalculateTotalWeight(List<EnemyWeight> weights, Dictionary<EnemyType, int> currentCounts)
    {
        int total = 0;
        foreach (var weight in weights)
        {
            // Skip types without prefabs
            if (!enemyPrefabLookup.ContainsKey(weight.enemyType)) continue;

            // Skip types that have reached their maximum
            if (weight.maxCount > 0 && currentCounts.ContainsKey(weight.enemyType) && currentCounts[weight.enemyType] >= weight.maxCount)
            {
                continue;
            }

            total += weight.weight;
        }
        return total;
    }

    /// <summary>
    /// Selects a random enemy type using weighted probability.
    /// 
    /// Algorithm: Generate random number 0 to totalWeight, then iterate through
    /// types accumulating their weights. When cumulative weight exceeds the roll,
    /// that type is selected. Higher weights = larger "slice" of the range.
    /// 
    /// Example with weights [Basic=50, Fast=30, Tank=20]:
    /// - Roll 0-49 = Basic, Roll 50-79 = Fast, Roll 80-99 = Tank
    /// </summary>
    private EnemyType? SelectWeightedEnemy(List<EnemyWeight> weights, Dictionary<EnemyType, int> currentCounts, int totalWeight)
    {
        if (totalWeight <= 0) return null;

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var weight in weights)
        {
            if (!enemyPrefabLookup.ContainsKey(weight.enemyType)) continue;

            // Skip types at max capacity
            if (weight.maxCount > 0 && currentCounts.ContainsKey(weight.enemyType) && currentCounts[weight.enemyType] >= weight.maxCount)
            {
                continue;
            }

            cumulative += weight.weight;
            
            // If roll falls within this type's weight range, select it
            if (roll < cumulative)
            {
                return weight.enemyType;
            }
        }

        return null;
    }

    /// <summary>
    /// Fisher-Yates shuffle algorithm for randomizing enemy spawn order.
    /// Ensures unbiased random permutation in O(n) time.
    /// 
    /// Works backwards through the list, swapping each element with
    /// a randomly selected element from the remaining unshuffled portion.
    /// </summary>
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            // Pick random index from 0 to i (inclusive)
            int j = UnityEngine.Random.Range(0, i + 1);
            
            // Swap elements at i and j
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    /// <summary>
    /// Gets enemies from predefined wave array (manual mode).
    /// </summary>
    private List<GameObject> GetNewEnemies()
    {
        if (HasNoMoreWaves())
        {
            Debug.Log("No more waves");
            return null;
        }

        List<GameObject> newEnemyList = new List<GameObject>();
        WaveDetails wave = levelWaves[waveIndex];

        // Add each enemy type based on wave definition
        AddEnemiesToList(newEnemyList, EnemyType.Basic, wave.enemyBasic);
        AddEnemiesToList(newEnemyList, EnemyType.Fast, wave.enemyFast);
        AddEnemiesToList(newEnemyList, EnemyType.Tank, wave.enemyTank);
        AddEnemiesToList(newEnemyList, EnemyType.Invisible, wave.enemyInvisible);
        AddEnemiesToList(newEnemyList, EnemyType.Reinforced, wave.enemyReinforced);
        AddEnemiesToList(newEnemyList, EnemyType.Summoner, wave.enemySummoner);
        AddEnemiesToList(newEnemyList, EnemyType.Adaptive, wave.enemyAdaptive);
        AddEnemiesToList(newEnemyList, EnemyType.Splitter, wave.enemySplitter);
        AddEnemiesToList(newEnemyList, EnemyType.Ghostwalk, wave.enemyGhostwalk);
        AddEnemiesToList(newEnemyList, EnemyType.Decoy, wave.enemyDecoy);
        AddEnemiesToList(newEnemyList, EnemyType.Hexer, wave.enemyHexer);
        AddEnemiesToList(newEnemyList, EnemyType.Herald, wave.enemyHerald);

        return newEnemyList;
    }

    /// <summary>
    /// Helper to add multiple enemies of the same type to a list.
    /// </summary>
    private void AddEnemiesToList(List<GameObject> list, EnemyType type, int count)
    {
        if (count <= 0) return;
        if (!enemyPrefabLookup.TryGetValue(type, out GameObject prefab)) return;

        for (int i = 0; i < count; i++)
        {
            list.Add(prefab);
        }
    }

    private bool HasNoMoreWaves() => !useEndlessWaves && waveIndex >= levelWaves.Length;

    /// <summary>
    /// Checks if all spawners have no active enemies remaining.
    /// </summary>
    private bool AllEnemiesDefeated()
    {
        foreach (EnemySpawner spawner in enemySpawners)
        {
            if (spawner.GetActiveEnemies().Count > 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if all spawners have emptied their pending enemy queues.
    /// </summary>
    private bool AllSpawnersFinishedSpawning()
    {
        foreach (EnemySpawner spawner in enemySpawners)
        {
            if (spawner.HasEnemiesToSpawn())
            {
                return false;
            }
        }
        return true;
    }

    // ==================== MEGA WAVE SYSTEM ====================
    // Mega Wave is the final boss wave triggered when Guardian is summoned.
    // It has boosted enemy stats and ends the level when completed.

    /// <summary>
    /// Called when Guardian tower is activated. Starts mega wave countdown.
    /// </summary>
    public void TriggerMegaWave()
    {
        if (megaWaveTriggered) return;

        megaWaveTriggered = true;
        megaWaveStartTime = Time.time;

        // Stop normal wave progression
        EnableWaveTimer(false);

        Debug.Log("Mega Wave triggered! Starting in " + megaWaveDelay + " seconds...");
    }

    /// <summary>
    /// Handles the delay period before mega wave actually starts.
    /// </summary>
    private void HandleMegaWaveDelay()
    {
        if (!megaWaveTriggered || megaWaveActive) return;

        if (Time.time >= megaWaveStartTime + megaWaveDelay)
        {
            StartMegaWave();
        }
    }

    private void StartMegaWave()
    {
        megaWaveActive = true;
        GiveMegaWaveEnemiesToSpawners();
        Debug.Log("MEGA WAVE STARTED!");
    }

    /// <summary>
    /// Distributes mega wave enemies to spawners (same round-robin as normal waves).
    /// </summary>
    private void GiveMegaWaveEnemiesToSpawners()
    {
        if (megaWave == null) return;

        List<GameObject> megaEnemies = GetMegaWaveEnemies();
        int spawnerIndex = 0;

        if (megaEnemies == null || megaEnemies.Count == 0) return;

        ShuffleList(megaEnemies);

        for (int i = 0; i < megaEnemies.Count; i++)
        {
            GameObject enemyToAdd = megaEnemies[i];
            EnemySpawner spawnerToReceiveEnemy = enemySpawners[spawnerIndex];

            spawnerToReceiveEnemy.AddEnemy(enemyToAdd);

            spawnerIndex++;

            if (spawnerIndex >= enemySpawners.Count) spawnerIndex = 0;
        }
    }

    /// <summary>
    /// Builds enemy list from MegaWaveDetails configuration.
    /// </summary>
    private List<GameObject> GetMegaWaveEnemies()
    {
        List<GameObject> newEnemyList = new List<GameObject>();

        AddEnemiesToList(newEnemyList, EnemyType.Basic, megaWave.enemyBasic);
        AddEnemiesToList(newEnemyList, EnemyType.Fast, megaWave.enemyFast);
        AddEnemiesToList(newEnemyList, EnemyType.Tank, megaWave.enemyTank);
        AddEnemiesToList(newEnemyList, EnemyType.Invisible, megaWave.enemyInvisible);
        AddEnemiesToList(newEnemyList, EnemyType.Reinforced, megaWave.enemyReinforced);
        AddEnemiesToList(newEnemyList, EnemyType.Summoner, megaWave.enemySummoner);
        AddEnemiesToList(newEnemyList, EnemyType.Adaptive, megaWave.enemyAdaptive);
        AddEnemiesToList(newEnemyList, EnemyType.Splitter, megaWave.enemySplitter);
        AddEnemiesToList(newEnemyList, EnemyType.Ghostwalk, megaWave.enemyGhostwalk);
        AddEnemiesToList(newEnemyList, EnemyType.Decoy, megaWave.enemyDecoy);
        AddEnemiesToList(newEnemyList, EnemyType.Hexer, megaWave.enemyHexer);
        AddEnemiesToList(newEnemyList, EnemyType.Herald, megaWave.enemyHerald);

        return newEnemyList;
    }

    public void CheckIfMegaWaveCompleted()
    {
        if (!megaWaveActive || GameManager.instance.IsGameLost()) return;

        if (AllEnemiesDefeated() && AllSpawnersFinishedSpawning())
        {
            MegaWaveCompleted();
        }
    }

    /// <summary>
    /// Called when all mega wave enemies are defeated - triggers level victory.
    /// </summary>
    private void MegaWaveCompleted()
    {
        Debug.Log("MEGA WAVE COMPLETED! Level Victory!");
        GameManager.instance.LevelCompleted();
    }

    // ==================== PUBLIC GETTERS ====================
    
    public bool IsMegaWaveActive() => megaWaveActive;
    public bool IsMegaWaveTriggered() => megaWaveTriggered;
    
    // Clamp multipliers to prevent extreme values
    public float GetMegaWaveHealthMultiplier() => megaWave != null ? Mathf.Clamp(megaWave.enemyHealthMultiplier, 0.1f, 10f) : 1f;
    public float GetMegaWaveSpawnRateMultiplier() => megaWave != null ? Mathf.Clamp(megaWave.spawnRateMultiplier, 0.1f, 10f) : 1f;
    
    // Returns appropriate spawn rate based on current wave type
    public float GetCurrentSpawnRateMultiplier() => megaWaveActive ? GetMegaWaveSpawnRateMultiplier() : currentSpawnRateMultiplier;
    
    public int GetCurrentWave() => waveIndex + 1;
    public DifficultyTier GetCurrentDifficultyTier() => GetCurrentTier();
}