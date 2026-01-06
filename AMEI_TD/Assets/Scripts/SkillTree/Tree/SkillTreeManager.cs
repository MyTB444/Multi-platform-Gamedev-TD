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
    // Track unlocked skills
    private HashSet<SkillNode> unlockedSkills = new HashSet<SkillNode>();
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
        // Already unlocked
        if (IsSkillUnlocked(skill))
            return false;

        // Not enough points
        if (GameManager.instance.GetPoints() < skill.skillPointCost)
            return false;

        // Check prerequisites
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

        // Spend points
        GameManager.instance.UpdateSkillPoints(-skill.skillPointCost);

        // Mark as unlocked
        unlockedSkills.Add(skill);

        // Apply effect
        skill.ApplyEffect();

        // Fire events
        OnSkillUnlocked?.Invoke(skill);

        Debug.Log($"✓ Unlocked: {skill.skillName}");

        SaveProgress();
        return true;
    }

    public bool TryLockSkill(SkillNode skill)
    {
        if (!IsSkillUnlocked(skill))
            return false;
        if(skill.irreversible)
            return false;

        // Check if any unlocked skills depend on this
        foreach (var unlockedSkill in unlockedSkills)
        {
            if (unlockedSkill.prerequisites.Contains(skill))
            {
                Debug.Log($"Cannot lock {skill.skillName} - {unlockedSkill.skillName} depends on it");
                return false;
            }
        }

        // Refund points
        GameManager.instance.UpdateSkillPoints(skill.skillPointCost);

        // Remove from unlocked
        unlockedSkills.Remove(skill);

        skill.RemoveEffect();

        // Fire events
        OnSkillLocked?.Invoke(skill);
        Debug.Log($"✗ Locked: {skill.skillName}");

        SaveProgress();
        return true;
    }
    public void ResetAllSkills()
    {
        // Refund all points
        foreach (var skill in unlockedSkills)
        {
            GameManager.instance.UpdateSkillPoints(skill.skillPointCost);
        }

        // Clear everything
        unlockedSkills.Clear();
        OnTreeReset?.Invoke();

        Debug.Log("All skills reset!");

        SaveProgress();
    }

    #region Save/Load

    private void SaveProgress()
    {
        SaveData data = new SaveData
        {
            unlockedSkillNames = unlockedSkills.Select(s => s.name).ToList()
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
    }
    public void PointsGained()
    {
        GainedPoints?.Invoke();

    }

    [System.Serializable]
    private class SaveData
    {
        public List<string> unlockedSkillNames;
    }

    #endregion
}