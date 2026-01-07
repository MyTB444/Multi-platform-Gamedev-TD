using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class TowerUpgradeManager : MonoBehaviour
{
    public static TowerUpgradeManager instance;
    
    private HashSet<TowerUpgradeType> unlockedUpgrades = new HashSet<TowerUpgradeType>();
    private HashSet<SwapType> unlockedSwaps = new HashSet<SwapType>();
    
    [Header("Tower Swap Prefabs - Physical")]
    [SerializeField] private GameObject spearPrefab;
    
    [Header("Tower Swap Prefabs - Magic")]
    [SerializeField] private GameObject iceMagePrefab;
    
    [Header("Tower Swap Prefabs - Mechanic")]
    [SerializeField] private GameObject bladeTowerPrefab;
    
    [Header("Tower Swap Prefabs - Imaginary")]
    [SerializeField] private GameObject phantomKnightPrefab;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void UnlockUpgrade(TowerUpgradeType upgrade)
    {
        if (upgrade == TowerUpgradeType.Null) return;
        if (unlockedUpgrades.Contains(upgrade)) return;
        
        unlockedUpgrades.Add(upgrade);
        
        TowerBase[] allTowers = FindObjectsOfType<TowerBase>();
        foreach (TowerBase tower in allTowers)
        {
            tower.SetUpgrade(upgrade, true);
        }
    }
    
    public void LockUpgrade(TowerUpgradeType upgrade)
    {
        if (!unlockedUpgrades.Contains(upgrade)) return;
        
        unlockedUpgrades.Remove(upgrade);
        
        TowerBase[] allTowers = FindObjectsOfType<TowerBase>();
        foreach (TowerBase tower in allTowers)
        {
            tower.SetUpgrade(upgrade, false);
        }
    }
    
    public bool IsUpgradeUnlocked(TowerUpgradeType upgrade)
    {
        return unlockedUpgrades.Contains(upgrade);
    }
    
    public HashSet<TowerUpgradeType> GetUnlockedUpgrades()
    {
        return unlockedUpgrades;
    }
    
    public void ApplyUnlockedUpgrades(TowerBase tower)
    {
        foreach (TowerUpgradeType upgrade in unlockedUpgrades)
        {
            tower.SetUpgrade(upgrade, true);
        }
    }
    
    public void SwapTowers(SwapType swapType)
    {
        if (swapType == SwapType.Null) return;
    
        unlockedSwaps.Add(swapType);
    
        switch (swapType)
        {
            case SwapType.Spear:
                ReplaceAllTowers<TowerArcher>(spearPrefab);
                break;
            case SwapType.IceMage:
                ReplaceAllTowers<TowerPyromancer>(iceMagePrefab);
                break;
            case SwapType.BladeTower:
                ReplaceAllTowers<TowerSpikeTrap>(bladeTowerPrefab);
                break;
            case SwapType.Knight:
                ReplaceAllTowers<TowerRockShower>(phantomKnightPrefab);
                break;
        }
    }
    
    private void ReplaceAllTowers<T>(GameObject newTowerPrefab) where T : TowerBase
    {
        if (newTowerPrefab == null)
        {
            Debug.LogWarning("New tower prefab is null!");
            return;
        }
    
        T[] towersToReplace = FindObjectsOfType<T>();
        List<SwapData> swapList = new List<SwapData>();
    
        // First, collect all data
        foreach (T oldTower in towersToReplace)
        {
            SwapData data = new SwapData();
            data.position = oldTower.transform.position;
            data.rotation = oldTower.transform.rotation;
            data.tileButton = FindTileButtonForTower(oldTower);
            swapList.Add(data);
        
            Destroy(oldTower.gameObject);
        }
    
        // Then spawn new towers
        foreach (SwapData data in swapList)
        {
            GameObject newTowerObj = Instantiate(newTowerPrefab, data.position, data.rotation);
            TowerBase newTower = newTowerObj.GetComponent<TowerBase>();
        
            if (data.tileButton != null)
            {
                data.tileButton.SetUnit(newTowerObj);
            }
        
            if (newTower != null)
            {
                ApplyUnlockedUpgrades(newTower);
            }
        }
    
        Debug.Log($"Swapped {towersToReplace.Length} towers to {newTowerPrefab.name}");
    }

    private struct SwapData
    {
        public Vector3 position;
        public Quaternion rotation;
        public TileButton tileButton;
    }
    
    private TileButton FindTileButtonForTower(TowerBase tower)
    {
        TileButton[] allTiles = FindObjectsOfType<TileButton>();
        
        foreach (TileButton tile in allTiles)
        {
            if (tile.GetUnit() == tower.gameObject)
            {
                return tile;
            }
        }
        
        return null;
    }
    
    public bool IsSwapUnlocked(SwapType swapType)
    {
        return unlockedSwaps.Contains(swapType);
    }
}