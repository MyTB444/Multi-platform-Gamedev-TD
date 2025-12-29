using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public TowerUpgradeType type;
    public TowerUpgradeType type2;

    public void ApplyEffect()
    {
        TowerUpgradeManager.instance.UnlockUpgrade(type);
        if(type2 != TowerUpgradeType.Null)
        {
            TowerUpgradeManager.instance.UnlockUpgrade(type);
        }
    }
    public void RemoveEffect()
    {
       TowerUpgradeManager.instance.LockUpgrade(type);
        if (type2 != TowerUpgradeType.Null)
        {
            TowerUpgradeManager.instance.LockUpgrade(type);
        }
    }
}
