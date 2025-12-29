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

    [Header("Effect")]
    public SkillEffectType effectType;
    public float effectValue;
    public string skillTypeName;
    public enum SkillEffectType
    {
        AddFlat,
        AddPercent,
        UnlockSkill,
        UnlockTower
    }
    public void ApplyEffect()
    {
    }
    public void RemoveEffect()
    {
    }
}