using System;
using System.Collections.Generic;
using UnityEngine;

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
    public float spawnRateMultiplier = 2f;
    public float enemyHealthMultiplier = 1.5f;
}

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

[Serializable]
public class EnemyPoolConfig
{
    public EnemyType enemyType;
    public GameObject prefab;
    public int poolAmount = 10;
}

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

    private Dictionary<EnemyType, GameObject> enemyPrefabLookup;
    private List<EnemySpawner> enemySpawners;
    private int waveIndex;
    private bool waveTimerEnabled;
    private bool makingNextWave;
    public bool gameBegan;

    private float currentSpawnRateMultiplier = 1f;
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
        // Register enemy prefabs with pool
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

    public void CheckIfWaveCompleted()
    {
        if (gameBegan == false || GameManager.instance.IsGameLost()) return;

        if (AllEnemiesDefeated() == false || AllSpawnersFinishedSpawning() == false || makingNextWave) return;

        makingNextWave = true;
        waveIndex++;

        // If mega wave is active, check if completed
        if (megaWaveActive)
        {
            CheckIfMegaWaveCompleted();
            return;
        }

        // Endless mode never runs out of waves - wait for guardian
        if (!useEndlessWaves && HasNoMoreWaves())
        {
            Debug.Log("All normal waves complete! Summon the Guardian to trigger the final Mega Wave!");
            return;
        }

        EnableWaveTimer(true);
    }

    private void EnableWaveTimer(bool enable)
    {
        if (enable && GameManager.instance.IsGameLost()) return;

        if (waveTimerEnabled == enable) return;

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

    private void GiveEnemiesToSpawners()
    {
        List<GameObject> newEnemies = useEndlessWaves ? GenerateEndlessWave() : GetNewEnemies();
        int spawnerIndex = 0;

        if (newEnemies == null || newEnemies.Count == 0) return;

        // Shuffle for variety
        ShuffleList(newEnemies);

        for (int i = 0; i < newEnemies.Count; i++)
        {
            GameObject enemyToAdd = newEnemies[i];
            EnemySpawner spawnerToReceiveEnemy = enemySpawners[spawnerIndex];

            spawnerToReceiveEnemy.AddEnemy(enemyToAdd);

            spawnerIndex++;

            if (spawnerIndex >= enemySpawners.Count) spawnerIndex = 0;
        }

        Debug.Log($"Wave {waveIndex + 1} started: {newEnemies.Count} enemies");
    }

    private List<GameObject> GenerateEndlessWave()
    {
        DifficultyTier currentTier = GetCurrentTier();
        if (currentTier == null)
        {
            Debug.LogWarning("No difficulty tier found for wave " + (waveIndex + 1));
            return null;
        }

        // Calculate enemy count for this wave
        int waveInTier = (waveIndex + 1) - currentTier.startWave;
        int enemyCount = currentTier.baseEnemyCount + (waveInTier * currentTier.enemiesPerWaveIncrease);

        // Store spawn rate for spawners
        currentSpawnRateMultiplier = currentTier.spawnRateMultiplier;

        // Generate enemy list with weights and min/max
        List<GameObject> enemies = GenerateWeightedEnemies(currentTier, enemyCount);

        Debug.Log($"[Endless Wave {waveIndex + 1}] Tier: {currentTier.tierName} | Enemies: {enemyCount} | Spawn Rate: {currentSpawnRateMultiplier}x");

        return enemies;
    }

    private DifficultyTier GetCurrentTier()
    {
        int currentWave = waveIndex + 1;

        foreach (var tier in difficultyTiers)
        {
            bool afterStart = currentWave >= tier.startWave;
            bool beforeEnd = tier.endWave == -1 || currentWave <= tier.endWave;

            if (afterStart && beforeEnd)
            {
                return tier;
            }
        }

        // Fallback to last tier if beyond all defined tiers
        if (difficultyTiers.Count > 0)
        {
            return difficultyTiers[difficultyTiers.Count - 1];
        }

        return null;
    }

    private List<GameObject> GenerateWeightedEnemies(DifficultyTier tier, int totalCount)
    {
        List<GameObject> enemies = new List<GameObject>();
        Dictionary<EnemyType, int> spawnCounts = new Dictionary<EnemyType, int>();

        // Initialize counts
        foreach (var weight in tier.enemyWeights)
        {
            spawnCounts[weight.enemyType] = 0;
        }

        // First pass: Add guaranteed minimums
        foreach (var weight in tier.enemyWeights)
        {
            if (weight.minCount > 0 && enemyPrefabLookup.ContainsKey(weight.enemyType))
            {
                int toAdd = Mathf.Min(weight.minCount, totalCount - enemies.Count);
                for (int i = 0; i < toAdd; i++)
                {
                    enemies.Add(enemyPrefabLookup[weight.enemyType]);
                    spawnCounts[weight.enemyType]++;
                }
            }
        }

        // Second pass: Fill remaining with weighted random
        int remainingSlots = totalCount - enemies.Count;
        int totalWeight = CalculateTotalWeight(tier.enemyWeights, spawnCounts);

        for (int i = 0; i < remainingSlots; i++)
        {
            EnemyType? selectedType = SelectWeightedEnemy(tier.enemyWeights, spawnCounts, totalWeight);

            if (selectedType.HasValue && enemyPrefabLookup.ContainsKey(selectedType.Value))
            {
                enemies.Add(enemyPrefabLookup[selectedType.Value]);
                spawnCounts[selectedType.Value]++;

                // Recalculate weight if we hit a max
                var weight = tier.enemyWeights.Find(w => w.enemyType == selectedType.Value);
                if (weight != null && weight.maxCount > 0 && spawnCounts[selectedType.Value] >= weight.maxCount)
                {
                    totalWeight = CalculateTotalWeight(tier.enemyWeights, spawnCounts);
                }
            }
        }

        return enemies;
    }

    private int CalculateTotalWeight(List<EnemyWeight> weights, Dictionary<EnemyType, int> currentCounts)
    {
        int total = 0;
        foreach (var weight in weights)
        {
            if (!enemyPrefabLookup.ContainsKey(weight.enemyType)) continue;

            // Skip if at max
            if (weight.maxCount > 0 && currentCounts.ContainsKey(weight.enemyType) && currentCounts[weight.enemyType] >= weight.maxCount)
            {
                continue;
            }

            total += weight.weight;
        }
        return total;
    }

    private EnemyType? SelectWeightedEnemy(List<EnemyWeight> weights, Dictionary<EnemyType, int> currentCounts, int totalWeight)
    {
        if (totalWeight <= 0) return null;

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var weight in weights)
        {
            if (!enemyPrefabLookup.ContainsKey(weight.enemyType)) continue;

            // Skip if at max
            if (weight.maxCount > 0 && currentCounts.ContainsKey(weight.enemyType) && currentCounts[weight.enemyType] >= weight.maxCount)
            {
                continue;
            }

            cumulative += weight.weight;
            if (roll < cumulative)
            {
                return weight.enemyType;
            }
        }

        return null;
    }

    // Fisher-Yates shuffle for random enemy spawn order
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private List<GameObject> GetNewEnemies()
    {
        if (HasNoMoreWaves())
        {
            Debug.Log("No more waves");
            return null;
        }

        List<GameObject> newEnemyList = new List<GameObject>();
        WaveDetails wave = levelWaves[waveIndex];

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

    public void TriggerMegaWave()
    {
        if (megaWaveTriggered) return;

        megaWaveTriggered = true;
        megaWaveStartTime = Time.time;

        EnableWaveTimer(false);

        Debug.Log("Mega Wave triggered! Starting in " + megaWaveDelay + " seconds...");
    }

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

    private void MegaWaveCompleted()
    {
        Debug.Log("MEGA WAVE COMPLETED! Level Victory!");
        GameManager.instance.LevelCompleted();
    }
    
    public bool IsMegaWaveActive() => megaWaveActive;
    public bool IsMegaWaveTriggered() => megaWaveTriggered;
    public float GetMegaWaveHealthMultiplier() => megaWave != null ? Mathf.Clamp(megaWave.enemyHealthMultiplier, 0.1f, 10f) : 1f;
    public float GetMegaWaveSpawnRateMultiplier() => megaWave != null ? Mathf.Clamp(megaWave.spawnRateMultiplier, 0.1f, 10f) : 1f;
    public float GetCurrentSpawnRateMultiplier() => megaWaveActive ? GetMegaWaveSpawnRateMultiplier() : currentSpawnRateMultiplier;
    public int GetCurrentWave() => waveIndex + 1;
    public DifficultyTier GetCurrentDifficultyTier() => GetCurrentTier();
}