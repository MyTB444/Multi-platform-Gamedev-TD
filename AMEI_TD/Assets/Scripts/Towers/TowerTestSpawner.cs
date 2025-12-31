using UnityEngine;

public class TowerTestSpawner : MonoBehaviour
{
    [Header("Tower Prefabs (1-9 keys)")]
    [SerializeField] private GameObject[] towerPrefabs;
    
    [Header("Spawn Settings")]
    [SerializeField] private TowerStandingBase[] towerBases;
    [SerializeField] private int currentBaseIndex = 0;
    
    private void Start()
    {
        Debug.Log($"[TowerTestSpawner] Started! Prefabs: {towerPrefabs?.Length ?? 0}, Bases: {towerBases?.Length ?? 0}");
    }
    
    private void Update()
    {
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SpawnTower(i);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Q))
        {
            CycleBase(-1);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            CycleBase(1);
        }
        
        if (Input.GetKeyDown(KeyCode.X))
        {
            ClearAllTowers();
        }
        
        if (Input.GetKeyDown(KeyCode.G))
        {
            SpawnGuardian();
        }
    }
    
    private void SpawnGuardian()
    {
        if (PlayerCastle.instance != null)
        {
            PlayerCastle.instance.SpawnGuardianTower();
            Debug.Log("Guardian Tower spawned!");
        }
        else
        {
            Debug.LogWarning("No PlayerCastle found!");
        }
    }
    
    private void SpawnTower(int index)
    {
        if (towerPrefabs == null || index >= towerPrefabs.Length || towerPrefabs[index] == null)
        {
            Debug.LogWarning($"No tower prefab at slot {index + 1}");
            return;
        }
        
        if (towerBases == null || towerBases.Length == 0)
        {
            Debug.LogWarning("No tower bases assigned!");
            return;
        }
        
        TowerStandingBase currentBase = towerBases[currentBaseIndex];
        if (currentBase == null) return;
        
        GameObject prefab = towerPrefabs[index];
        TowerBase towerScript = prefab.GetComponent<TowerBase>();
        
        ElementType element = ElementType.Physical;
        if (towerScript != null)
        {
            element = towerScript.GetElementType();
        }
        
        Transform spawnPoint = currentBase.GetSpawnPoint(element);
        Quaternion spawnRotation = currentBase.GetSpawnRotation(element);
        
        Instantiate(prefab, spawnPoint.position, spawnRotation);
        Debug.Log($"Spawned {prefab.name} at {currentBase.name}");
        
        CycleBase(1);
    }
    
    private void CycleBase(int direction)
    {
        if (towerBases == null || towerBases.Length == 0) return;
        
        currentBaseIndex += direction;
        
        if (currentBaseIndex >= towerBases.Length)
            currentBaseIndex = 0;
        if (currentBaseIndex < 0)
            currentBaseIndex = towerBases.Length - 1;
            
        Debug.Log($"Selected base: {towerBases[currentBaseIndex]?.name}");
    }
    
    private void ClearAllTowers()
    {
        TowerBase[] allTowers = FindObjectsOfType<TowerBase>();
        foreach (var tower in allTowers)
        {
            Destroy(tower.gameObject);
        }
        Debug.Log($"Cleared {allTowers.Length} towers");
    }
    
    private void OnGUI()
    {
        GUI.skin.label.fontSize = 14;
        GUI.skin.label.richText = true;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 500));
        
        GUILayout.Label("<b><color=yellow>Tower Test Spawner</color></b>");
        GUILayout.Label("1-9: Spawn tower");
        GUILayout.Label("Q/E: Cycle base");
        GUILayout.Label("X: Clear all towers");
        GUILayout.Label("G: Spawn Guardian");
        
        GUILayout.Space(10);
        
        if (towerBases != null && towerBases.Length > 0 && currentBaseIndex < towerBases.Length)
        {
            string baseName = towerBases[currentBaseIndex] != null ? towerBases[currentBaseIndex].name : "None";
            GUILayout.Label($"<color=green>Current Base: {baseName} ({currentBaseIndex + 1}/{towerBases.Length})</color>");
        }
        
        GUILayout.Space(10);
        GUILayout.Label("<b>Towers:</b>");
        
        if (towerPrefabs != null)
        {
            for (int i = 0; i < towerPrefabs.Length; i++)
            {
                string name = towerPrefabs[i] != null ? towerPrefabs[i].name : "Empty";
                GUILayout.Label($"  {i + 1}: {name}");
            }
        }
        
        GUILayout.EndArea();
    }
}