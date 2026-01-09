using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages skill tree progression - unlocking/locking skills, skill loss on enemy reaching castle,
/// and saving/loading progress. Globe nodes protect skills below them from being lost.
/// 
/// Key Concepts:
/// - Skills are unlocked by spending skill points and having prerequisites met
/// - When enemies reach the castle, players can LOSE skills of that enemy's element
/// - Globe nodes act as "checkpoints" - skills unlocked BEFORE the globe are protected
/// - Guardian node protects the ENTIRE tree from skill loss
/// </summary>
public class SkillTreeManager : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent<SkillNode> OnSkillUnlocked;  // Fired when any skill is unlocked
    public UnityEvent<SkillNode> OnSkillLocked;    // Fired when any skill is locked/lost
    public UnityEvent GainedPoints;                 // Fired when player gains skill points
    public UnityEvent OnTreeReset;                  // Fired when entire tree is reset
    
    public static SkillTreeManager instance;
    
    // All currently unlocked skills (fast lookup)
    private HashSet<SkillNode> unlockedSkills = new HashSet<SkillNode>();
    
    // Tracks the ORDER skills were unlocked - critical for globe protection logic
    // Skills unlocked AFTER a globe are "above" it and vulnerable to loss
    private List<SkillNode> unlockOrder = new List<SkillNode>();
    
    // When true, no skills can be lost (Guardian node unlocked)
    private bool treeFullyProtected = false;
    
    private void Start()
    {
        instance = this;
        PlayerPrefs.DeleteKey("SkillTreeSave");
        LoadProgress();
    }
    
    /// <summary>
    /// Checks if a specific skill is currently unlocked.
    /// </summary>
    public bool IsSkillUnlocked(SkillNode skill)
    {
        return unlockedSkills.Contains(skill);
    }
    
    /// <summary>
    /// Checks if a skill CAN be unlocked (not already unlocked, enough points, prereqs met).
    /// </summary>
    public bool CanUnlockSkill(SkillNode skill)
    {
        // Already have it
        if (IsSkillUnlocked(skill))
            return false;

        // Can't afford it
        if (GameManager.instance.GetPoints() < skill.skillPointCost)
            return false;

        // Check all prerequisites are unlocked
        foreach (var prereq in skill.prerequisites)
        {
            if (!IsSkillUnlocked(prereq))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Attempts to unlock a skill. Deducts points, applies effects, and fires events.
    /// </summary>
    /// <returns>True if successfully unlocked</returns>
    public bool TryUnlockSkill(SkillNode skill)
    {
        if (!CanUnlockSkill(skill))
        {
            Debug.Log($"Cannot unlock {skill.skillName}");
            return false;
        }

        // Deduct skill points (negative value to subtract)
        GameManager.instance.UpdateSkillPoints(-skill.skillPointCost);

        // Add to unlocked set
        unlockedSkills.Add(skill);
        
        // Track unlock order for globe protection calculations
        if (!unlockOrder.Contains(skill))
        {
            unlockOrder.Add(skill);
        }

        // Apply the skill's effects (tower upgrades, swaps, spell unlocks, etc.)
        skill.ApplyEffect();
        
        // Guardian node grants full tree protection
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

    /// <summary>
    /// Attempts to voluntarily lock/refund a skill.
    /// Cannot lock: irreversible skills, or skills that other unlocked skills depend on.
    /// </summary>
    public bool TryLockSkill(SkillNode skill)
    {
        if (!IsSkillUnlocked(skill))
            return false;
            
        // Some skills (like swaps) cannot be undone
        if (skill.irreversible)
            return false;

        // Check if any other unlocked skill requires this one as a prerequisite
        foreach (var unlockedSkill in unlockedSkills)
        {
            if (unlockedSkill.prerequisites.Contains(skill))
            {
                Debug.Log($"Cannot lock {skill.skillName} - {unlockedSkill.skillName} depends on it");
                return false;
            }
        }

        // Refund skill points
        GameManager.instance.UpdateSkillPoints(skill.skillPointCost);

        // Remove from tracking
        unlockedSkills.Remove(skill);
        unlockOrder.Remove(skill);

        // Undo the skill's effects
        skill.RemoveEffect();

        OnSkillLocked?.Invoke(skill);
        Debug.Log($"✗ Locked: {skill.skillName}");

        SaveProgress();
        return true;
    }
    
    // ==================== SKILL LOSS SYSTEM ====================
    // When enemies reach the castle, the player loses a skill of that element.
    // This creates risk/reward and encourages diverse tower placement.
    
    /// <summary>
    /// Called when an enemy reaches the player's castle.
    /// Attempts to remove a skill matching the enemy's element type.
    /// </summary>
    /// <param name="enemyElement">The element type of the enemy that got through</param>
    public void OnEnemyReachedCastle(ElementType enemyElement)
    {
        // Guardian provides complete immunity to skill loss
        if (treeFullyProtected)
        {
            Debug.Log("Tree protected by Guardian - no skill loss");
            return;
        }
        
        // Try to find a skill of the matching element to lose
        SkillNode skillToLose = GetSkillToLose(enemyElement);
        
        // If no matching element skills, fall back to Global skills
        if (skillToLose == null && HasNoElementSkills())
        {
            skillToLose = GetSkillToLose(ElementType.Global);
        }
        
        if (skillToLose == null)
        {
            Debug.Log($"No skills to lose");
            return;
        }
        
        // Force remove the skill (no refund, bypasses normal lock restrictions)
        ForceLockSkill(skillToLose);
    }
    
    /// <summary>
    /// Finds the most appropriate skill to lose for a given element.
    /// 
    /// Algorithm:
    /// 1. Get all skills of the matching element (excluding globes/guardian)
    /// 2. Sort by unlock order (most recent first - lose newest skills first)
    /// 3. If element has a globe: only consider skills unlocked AFTER the globe
    /// 4. Only select skills with no dependents (can't orphan other skills)
    /// </summary>
    private SkillNode GetSkillToLose(ElementType element)
    {
        List<SkillNode> elementSkills = new List<SkillNode>();
    
        // Iterate backwards through unlock order (most recent first)
        for (int i = unlockOrder.Count - 1; i >= 0; i--)
        {
            SkillNode skill = unlockOrder[i];
        
            // Filter: must match element
            if (skill.skillElementType != element) continue;
            
            // Filter: guardian and globe nodes are immune to loss
            if (skill.isGuardianNode) continue;
            if (skill.isGlobeNode) continue;
        
            elementSkills.Add(skill);
        }
    
        Debug.Log($"Found {elementSkills.Count} {element} skills that could be lost");
    
        // Check if this element branch has a protective globe
        bool hasGlobe = HasGlobeForElement(element);
        Debug.Log($"Has globe for {element}: {hasGlobe}");
    
        // Find first valid skill to lose
        foreach (SkillNode skill in elementSkills)
        {
            Debug.Log($"Checking skill: {skill.skillName}");
        
            if (hasGlobe)
            {
                // WITH GLOBE: Only skills ABOVE (unlocked after) the globe are vulnerable
                bool isAbove = IsAboveGlobe(skill, element);
                bool hasDependents = HasDependentSkills(skill);
                Debug.Log($"  IsAboveGlobe: {isAbove}, HasDependents: {hasDependents}");
            
                if (isAbove && !hasDependents)
                {
                    return skill;
                }
            }
            else
            {
                // NO GLOBE: Any skill without dependents can be lost
                if (!HasDependentSkills(skill))
                {
                    return skill;
                }
            }
        }
    
        return null;
    }
    
    /// <summary>
    /// Checks if any globe node is unlocked for the specified element.
    /// </summary>
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
    
    /// <summary>
    /// Determines if a skill was unlocked AFTER its element's globe.
    /// Skills "above" the globe (unlocked later) are vulnerable to loss.
    /// Skills "below" the globe (unlocked before) are protected.
    /// 
    /// Uses unlock order index comparison - higher index = unlocked later = above globe.
    /// </summary>
    private bool IsAboveGlobe(SkillNode skill, ElementType element)
    {
        // Find the globe for this element
        SkillNode globe = GetGlobeForElement(element);
        if (globe == null) return false;
    
        // Compare positions in unlock order list
        int globeIndex = unlockOrder.IndexOf(globe);
        int skillIndex = unlockOrder.IndexOf(skill);
    
        // Skill unlocked after globe = above the globe = vulnerable
        return skillIndex > globeIndex;
    }
    
    /// <summary>
    /// Returns the globe node for a specific element, if unlocked.
    /// </summary>
    private SkillNode GetGlobeForElement(ElementType element)
    {
        foreach (SkillNode skill in unlockedSkills)
        {
            if (skill.skillElementType == element && skill.isGlobeNode)
            {
                return skill;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Checks if any other unlocked skill has this skill as a prerequisite.
    /// Skills with dependents cannot be lost (would orphan the dependent).
    /// </summary>
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
    
    /// <summary>
    /// Checks if the player has any element-specific skills that could be lost.
    /// Used to determine if we should fall back to losing Global skills.
    /// </summary>
    private bool HasNoElementSkills()
    {
        foreach (SkillNode skill in unlockedSkills)
        {
            // Skip Global, Guardian, and Globe nodes
            if (skill.skillElementType != ElementType.Global && 
                !skill.isGuardianNode && 
                !skill.isGlobeNode)
            {
                // Found an element skill - check if it can actually be lost
                if (!HasDependentSkills(skill))
                {
                    return false;  // Has losable element skills
                }
            }
        }
        return true;  // No losable element skills
    }
    
    /// <summary>
    /// Forces a skill to be lost (no refund, bypasses irreversible check).
    /// Used by the skill loss system when enemies reach the castle.
    /// </summary>
    private void ForceLockSkill(SkillNode skill)
    {
        if (!unlockedSkills.Contains(skill)) return;
        
        unlockedSkills.Remove(skill);
        unlockOrder.Remove(skill);
        
        // Remove the skill's effects (tower upgrades, etc.)
        skill.RemoveEffect();
        
        OnSkillLocked?.Invoke(skill);
        
        Debug.Log($"✗ Lost skill due to enemy: {skill.skillName}");
        
        SaveProgress();
    }
    
    /// <summary>
    /// Completely resets the skill tree, refunding all points.
    /// </summary>
    public void ResetAllSkills()
    {
        // Refund all spent points
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
    
    /// <summary>
    /// Called by GameManager when player earns skill points.
    /// Fires event so UI can update (buttons may become interactable).
    /// </summary>
    public void PointsGained()
    {
        GainedPoints?.Invoke();
    }

    #region Save/Load

    /// <summary>
    /// Saves current skill tree state to PlayerPrefs as JSON.
    /// </summary>
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

    /// <summary>
    /// Loads skill tree state from PlayerPrefs.
    /// Note: Currently only logs loaded skills - may need to actually restore them.
    /// </summary>
    private void LoadProgress()
    {
        if (!PlayerPrefs.HasKey("SkillTreeSave"))
            return;

        string json = PlayerPrefs.GetString("SkillTreeSave");
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // TODO: Actually restore the skills from saved names
        // Would need a way to look up SkillNode ScriptableObjects by name
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