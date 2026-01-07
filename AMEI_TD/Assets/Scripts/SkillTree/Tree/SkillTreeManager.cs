using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class SkillTreeManager : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent<SkillNode> OnSkillUnlocked;
    public UnityEvent<SkillNode> OnSkillLocked;
    public UnityEvent GainedPoints;
    public UnityEvent OnTreeReset;
    
    public static SkillTreeManager instance;
    
    private HashSet<SkillNode> unlockedSkills = new HashSet<SkillNode>();
    private List<SkillNode> unlockOrder = new List<SkillNode>();
    private bool treeFullyProtected = false;
    
    private void Start()
    {
        instance = this;
        LoadProgress();
    }
    
    public bool IsSkillUnlocked(SkillNode skill)
    {
        return unlockedSkills.Contains(skill);
    }
    
    public bool CanUnlockSkill(SkillNode skill)
    {
        if (IsSkillUnlocked(skill))
            return false;

        if (GameManager.instance.GetPoints() < skill.skillPointCost)
            return false;

        foreach (var prereq in skill.prerequisites)
        {
            if (!IsSkillUnlocked(prereq))
                return false;
        }

        return true;
    }

    public bool TryUnlockSkill(SkillNode skill)
    {
        if (!CanUnlockSkill(skill))
        {
            Debug.Log($"Cannot unlock {skill.skillName}");
            return false;
        }

        GameManager.instance.UpdateSkillPoints(-skill.skillPointCost);

        unlockedSkills.Add(skill);
        
        if (!unlockOrder.Contains(skill))
        {
            unlockOrder.Add(skill);
        }

        skill.ApplyEffect();
        
        if (skill.isGuardianNode)
        {
            treeFullyProtected = true;
            Debug.Log("Guardian unlocked! Entire skill tree is now protected!");
        }

        OnSkillUnlocked?.Invoke(skill);

        Debug.Log($"✓ Unlocked: {skill.skillName}");

        SaveProgress();
        return true;
    }

    public bool TryLockSkill(SkillNode skill)
    {
        if (!IsSkillUnlocked(skill))
            return false;
        if (skill.irreversible)
            return false;

        foreach (var unlockedSkill in unlockedSkills)
        {
            if (unlockedSkill.prerequisites.Contains(skill))
            {
                Debug.Log($"Cannot lock {skill.skillName} - {unlockedSkill.skillName} depends on it");
                return false;
            }
        }

        GameManager.instance.UpdateSkillPoints(skill.skillPointCost);

        unlockedSkills.Remove(skill);
        unlockOrder.Remove(skill);

        skill.RemoveEffect();

        OnSkillLocked?.Invoke(skill);
        Debug.Log($"✗ Locked: {skill.skillName}");

        SaveProgress();
        return true;
    }
    
    public void OnEnemyReachedCastle(ElementType enemyElement)
    {
        if (treeFullyProtected)
        {
            Debug.Log("Tree protected by Guardian - no skill loss");
            return;
        }
        
        SkillNode skillToLose = GetSkillToLose(enemyElement);
        
        if (skillToLose == null && HasNoElementSkills())
        {
            skillToLose = GetSkillToLose(ElementType.Global);
        }
        
        if (skillToLose == null)
        {
            Debug.Log($"No skills to lose");
            return;
        }
        
        ForceLockSkill(skillToLose);
    }
    
    private SkillNode GetSkillToLose(ElementType element)
    {
        List<SkillNode> elementSkills = new List<SkillNode>();
        
        for (int i = unlockOrder.Count - 1; i >= 0; i--)
        {
            SkillNode skill = unlockOrder[i];
            
            if (skill.skillElementType != element) continue;
            if (skill.isGuardianNode) continue;
            if (skill.isGlobeNode) continue;
            
            elementSkills.Add(skill);
        }
        
        bool hasGlobe = HasGlobeForElement(element);
        
        foreach (SkillNode skill in elementSkills)
        {
            if (hasGlobe)
            {
                if (IsAboveGlobe(skill, element))
                {
                    if (!HasDependentSkills(skill))
                    {
                        return skill;
                    }
                }
            }
            else
            {
                if (!HasDependentSkills(skill))
                {
                    return skill;
                }
            }
        }
        
        return null;
    }
    
    private bool HasGlobeForElement(ElementType element)
    {
        foreach (SkillNode skill in unlockedSkills)
        {
            if (skill.skillElementType == element && skill.isGlobeNode)
            {
                return true;
            }
        }
        return false;
    }
    
    private bool IsAboveGlobe(SkillNode skill, ElementType element)
    {
        foreach (SkillNode prereq in skill.prerequisites)
        {
            if (prereq.isGlobeNode && prereq.skillElementType == element)
            {
                return true;
            }
            
            if (IsAboveGlobe(prereq, element))
            {
                return true;
            }
        }
        return false;
    }
    
    private bool HasDependentSkills(SkillNode skill)
    {
        foreach (SkillNode unlockedSkill in unlockedSkills)
        {
            if (unlockedSkill.prerequisites.Contains(skill))
            {
                return true;
            }
        }
        return false;
    }
    
    private bool HasNoElementSkills()
    {
        foreach (SkillNode skill in unlockedSkills)
        {
            if (skill.skillElementType != ElementType.Global && 
                !skill.isGuardianNode && 
                !skill.isGlobeNode)
            {
                if (!HasDependentSkills(skill))
                {
                    return false;
                }
            }
        }
        return true;
    }
    
    private void ForceLockSkill(SkillNode skill)
    {
        if (!unlockedSkills.Contains(skill)) return;
        
        unlockedSkills.Remove(skill);
        unlockOrder.Remove(skill);
        
        skill.RemoveEffect();
        
        OnSkillLocked?.Invoke(skill);
        
        Debug.Log($"✗ Lost skill due to enemy: {skill.skillName}");
        
        SaveProgress();
    }
    
    public void ResetAllSkills()
    {
        foreach (var skill in unlockedSkills)
        {
            GameManager.instance.UpdateSkillPoints(skill.skillPointCost);
        }

        unlockedSkills.Clear();
        unlockOrder.Clear();
        treeFullyProtected = false;
        
        OnTreeReset?.Invoke();

        Debug.Log("All skills reset!");

        SaveProgress();
    }
    
    public void PointsGained()
    {
        GainedPoints?.Invoke();
    }

    #region Save/Load

    private void SaveProgress()
    {
        SaveData data = new SaveData
        {
            unlockedSkillNames = unlockedSkills.Select(s => s.name).ToList(),
            unlockOrderNames = unlockOrder.Select(s => s.name).ToList(),
            treeProtected = treeFullyProtected
        };

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("SkillTreeSave", json);
        PlayerPrefs.Save();
    }

    private void LoadProgress()
    {
        if (!PlayerPrefs.HasKey("SkillTreeSave"))
            return;

        string json = PlayerPrefs.GetString("SkillTreeSave");
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        foreach (string skillName in data.unlockedSkillNames)
        {
            Debug.Log($"Loaded skill: {skillName}");
        }
        
        treeFullyProtected = data.treeProtected;
    }

    [System.Serializable]
    private class SaveData
    {
        public List<string> unlockedSkillNames;
        public List<string> unlockOrderNames;
        public bool treeProtected;
    }

    #endregion
}