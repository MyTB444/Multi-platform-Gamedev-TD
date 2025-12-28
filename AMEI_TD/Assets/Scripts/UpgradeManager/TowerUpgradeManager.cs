using UnityEngine;
using System.Collections.Generic;

public class TowerUpgradeManager : MonoBehaviour
{
    public static TowerUpgradeManager instance;
    
    private HashSet<TowerUpgradeType> unlockedUpgrades = new HashSet<TowerUpgradeType>();
    
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
        if (unlockedUpgrades.Contains(upgrade)) return;
        
        unlockedUpgrades.Add(upgrade);
        
        // Apply to all existing towers
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
        
        // Remove from all existing towers
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
    
    // Called by newly placed towers to get their upgrades
    public void ApplyUnlockedUpgrades(TowerBase tower)
    {
        foreach (TowerUpgradeType upgrade in unlockedUpgrades)
        {
            tower.SetUpgrade(upgrade, true);
        }
    }
}