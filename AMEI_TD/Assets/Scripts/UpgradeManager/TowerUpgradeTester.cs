using UnityEngine;
using System.Collections.Generic;

public class TowerUpgradeTester : MonoBehaviour
{
    [Header("Current Selection")]
    [SerializeField] private TowerBase selectedTower;
    
    private TowerBase[] allTowers;
    private int currentTowerIndex = 0;
    
    private List<TowerUpgradeType> validUpgrades = new List<TowerUpgradeType>();
    private int currentUpgradeIndex = 0;
    
    private void Start()
    {
        RefreshTowerList();
    }
    
    private void RefreshTowerList()
    {
        allTowers = FindObjectsOfType<TowerBase>();
        Debug.Log($"Found {allTowers.Length} towers");
    }
    
    private void RefreshValidUpgrades()
    {
        validUpgrades.Clear();
        currentUpgradeIndex = 0;
        
        if (selectedTower == null) return;
        
        // Common upgrades
        validUpgrades.Add(TowerUpgradeType.DamageBoost);
        validUpgrades.Add(TowerUpgradeType.AttackSpeedBoost);
        validUpgrades.Add(TowerUpgradeType.RangeBoost);
        
        // Tower specific
        if (selectedTower is TowerArcher)
        {
            validUpgrades.Add(TowerUpgradeType.PoisonArrows);
            validUpgrades.Add(TowerUpgradeType.FireArrows);
        }
        else if (selectedTower is SpearTower)
        {
            validUpgrades.Add(TowerUpgradeType.BarbedSpear);
            validUpgrades.Add(TowerUpgradeType.ExplosiveTip);
        }
        else if (selectedTower is TowerPyromancer)
        {
            validUpgrades.Add(TowerUpgradeType.BurnChance);
            validUpgrades.Add(TowerUpgradeType.BiggerFireball);
            validUpgrades.Add(TowerUpgradeType.BurnSpread);
        }
        else if (selectedTower is TowerIceMage)
        {
            validUpgrades.Add(TowerUpgradeType.StrongerSlow);
            validUpgrades.Add(TowerUpgradeType.LongerSlow);
            validUpgrades.Add(TowerUpgradeType.Frostbite);
            validUpgrades.Add(TowerUpgradeType.FreezeSolid);
        }
        else if (selectedTower is TowerSpikeTrap)
        {
            validUpgrades.Add(TowerUpgradeType.PoisonSpikes);
            validUpgrades.Add(TowerUpgradeType.BleedingSpikes);
            validUpgrades.Add(TowerUpgradeType.CripplingSpikes);
        }
        else if (selectedTower is BladeTower)
        {
            validUpgrades.Add(TowerUpgradeType.BleedChance);
            validUpgrades.Add(TowerUpgradeType.MoreBlades);
            validUpgrades.Add(TowerUpgradeType.ExtendedReach);
        }
        else if (selectedTower is TowerRockShower)
        {
            validUpgrades.Add(TowerUpgradeType.MoreRocks);
            validUpgrades.Add(TowerUpgradeType.BiggerRocks);
            validUpgrades.Add(TowerUpgradeType.LongerShower);
            validUpgrades.Add(TowerUpgradeType.MeteorStrike);
        }
        else if (selectedTower is TowerPhantomKnight)
        {
            validUpgrades.Add(TowerUpgradeType.MorePhantoms);
            validUpgrades.Add(TowerUpgradeType.CloserSpawn);
            validUpgrades.Add(TowerUpgradeType.SpectralChains);
            validUpgrades.Add(TowerUpgradeType.DoubleSlash);
        }
    }
    
    private TowerUpgradeType GetCurrentUpgrade()
    {
        if (validUpgrades.Count == 0) return TowerUpgradeType.DamageBoost;
        return validUpgrades[currentUpgradeIndex];
    }
    
    private void Update()
    {
        // Press T to cycle through towers
        if (Input.GetKeyDown(KeyCode.T))
        {
            RefreshTowerList();
            if (allTowers.Length > 0)
            {
                currentTowerIndex = (currentTowerIndex + 1) % allTowers.Length;
                selectedTower = allTowers[currentTowerIndex];
                RefreshValidUpgrades();
                Debug.Log($"Selected tower: {selectedTower.name}");
            }
        }
        
        // Press E to unlock upgrade globally
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (TowerUpgradeManager.instance != null && validUpgrades.Count > 0)
            {
                TowerUpgradeManager.instance.UnlockUpgrade(GetCurrentUpgrade());
            }
        }
        
        // Press R to lock upgrade globally
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (TowerUpgradeManager.instance != null && validUpgrades.Count > 0)
            {
                TowerUpgradeManager.instance.LockUpgrade(GetCurrentUpgrade());
            }
        }
        
        // Scroll through valid upgrades with arrow keys
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (validUpgrades.Count > 0)
            {
                currentUpgradeIndex = (currentUpgradeIndex + 1) % validUpgrades.Count;
                Debug.Log($"Selected upgrade: {GetCurrentUpgrade()}");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (validUpgrades.Count > 0)
            {
                currentUpgradeIndex--;
                if (currentUpgradeIndex < 0) currentUpgradeIndex = validUpgrades.Count - 1;
                Debug.Log($"Selected upgrade: {GetCurrentUpgrade()}");
            }
        }
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 350, 400));
        GUILayout.Box("Tower Upgrade Tester (Global)");
        GUILayout.Label($"Selected Tower: {(selectedTower != null ? selectedTower.name : "None")}");
        GUILayout.Label($"Selected Upgrade: {GetCurrentUpgrade()}");
        GUILayout.Space(10);
        GUILayout.Label("T: Cycle through towers");
        GUILayout.Label("Up/Down: Change upgrade");
        GUILayout.Label("E: Unlock upgrade (GLOBAL)");
        GUILayout.Label("R: Lock upgrade (GLOBAL)");
        
        GUILayout.Space(10);
        GUILayout.Box("Available Upgrades:");
        
        for (int i = 0; i < validUpgrades.Count; i++)
        {
            bool isUnlocked = TowerUpgradeManager.instance != null && 
                              TowerUpgradeManager.instance.IsUpgradeUnlocked(validUpgrades[i]);
            string prefix = i == currentUpgradeIndex ? "> " : "  ";
            string status = isUnlocked ? " [UNLOCKED]" : "";
            GUILayout.Label($"{prefix}{validUpgrades[i]}{status}");
        }
        
        GUILayout.EndArea();
    }
}