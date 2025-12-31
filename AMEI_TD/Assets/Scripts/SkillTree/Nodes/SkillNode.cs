using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SwapType
{
    Null,
    Spear,
    Knight,
    IceMage,
    BladeTower
}

public enum SpellEnableType
{
    Null,
    Physical,
    Magical,
    Mechanical,
    Imaginary
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "Skill Tree/Skill Node")]
public class SkillNode : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;
    public string skillCost;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;

    public bool irreversible;

    [Header("Requirements")]
    public int skillPointCost;
    public List<SkillNode> prerequisites = new List<SkillNode>();

    [Header("Functionalities")]
    public TowerUpgradeType function;
    public TowerUpgradeType function2;
    public SwapType swapType;
    public SpellEnableType spellEnableType;
    public event Action<SwapType> EventRaised;

    public void ApplyEffect()
    {
        TowerUpgradeManager.instance.UnlockUpgrade(function);
        if(function2 != TowerUpgradeType.Null)
        {
            TowerUpgradeManager.instance.UnlockUpgrade(function);
        }
        if(swapType != SwapType.Null)
        Raise(swapType);
        if(spellEnableType != SpellEnableType.Null)
        EnableSpell(spellEnableType);
        
    }
    public void RemoveEffect()
    {
       TowerUpgradeManager.instance.LockUpgrade(function);
        if (function2 != TowerUpgradeType.Null)
        {
            TowerUpgradeManager.instance.LockUpgrade(function);
        }
    }
    public void Raise(SwapType type)
    {
        EventRaised.Invoke(type);
    }
    public void EnableSpell(SpellEnableType type)
    {
        SpellFunctionality.instance.EnableButton(type);
    }
}
