using UnityEngine;

/// <summary>
/// Defines the element types in the game's damage system.
/// Used for towers, enemies, and type matchup calculations.
/// </summary>
public enum ElementType
{
    Physical,   // Index 0 - Standard physical damage
    Magic,      // Index 1 - Magical/arcane damage
    Mechanic,   // Index 2 - Technology/mechanical damage
    Imaginary,  // Index 3 - Psychic/illusion damage
    Global      // Index 4 - Special type, typically for effects that ignore type matchups
}

/// <summary>
/// Data container for a single damage instance.
/// Packages all information needed for damage calculation.
/// </summary>
[System.Serializable]
public struct DamageInfo
{
    public float amount;           // Base damage before multipliers
    public ElementType elementType; // Element of the attack (determines effectiveness)
    public bool isDoT;             // True if this is Damage over Time (affects UI display)

    public DamageInfo(float amount, ElementType elementType, bool isDoT = false)
    {
        this.amount = amount;
        this.elementType = elementType;
        this.isDoT = isDoT;
    }
}

/// <summary>
/// Static utility class for calculating damage with elemental type effectiveness.
/// Implements a Pokemon-style type matchup system where certain elements deal
/// bonus/reduced damage against others.
/// 
/// Must call Initialize() with a TypeMatchupDatabase before use.
/// </summary>
public static class DamageCalculator
{
    // Reference to the ScriptableObject containing the type matchup chart
    // Set via Initialize() - typically called in GameManager.Awake()
    private static TypeMatchupDatabase _database;

    /// <summary>
    /// Initializes the damage system with type matchup data.
    /// Must be called before any damage calculations occur.
    /// </summary>
    /// <param name="database">ScriptableObject containing the type effectiveness chart</param>
    public static void Initialize(TypeMatchupDatabase database)
    {
        _database = database;
        Debug.Log("DamageCalculator initialized with database: " + (database != null));
    }

    /// <summary>
    /// Contains the results of a damage calculation including
    /// final damage and effectiveness flags for UI/feedback.
    /// </summary>
    public struct DamageResult
    {
        public float finalDamage;          // Actual damage to apply after multipliers
        public bool wasSuperEffective;     // True if multiplier > 1.0 (type advantage)
        public bool wasNotVeryEffective;   // True if 0 < multiplier < 1.0 (type disadvantage)
        public bool wasImmune;             // True if multiplier = 0 (complete immunity)
    }

    /// <summary>
    /// Calculates final damage based on attacker element vs defender element.
    /// 
    /// Type Effectiveness System:
    /// - Super Effective (multiplier > 1.0): Attacker has advantage, deals bonus damage
    /// - Normal (multiplier = 1.0): No type interaction
    /// - Not Very Effective (0 < multiplier < 1.0): Defender resists, reduced damage
    /// - Immune (multiplier = 0): Defender immune, no damage dealt
    /// </summary>
    /// <param name="attackInfo">The incoming attack's damage and element type</param>
    /// <param name="defenderType">The defender's element type</param>
    /// <returns>DamageResult containing final damage and effectiveness flags</returns>
    public static DamageResult Calculate(DamageInfo attackInfo, ElementType defenderType)
    {
        DamageResult result = new DamageResult();

        // ===== GET TYPE MULTIPLIER =====
        // Query the type chart for attacker vs defender effectiveness
        // Falls back to 1.0 (neutral) if database not initialized
        float multiplier = _database != null 
            ? _database.GetEffectiveness(attackInfo.elementType, defenderType) 
            : 1f;

        // ===== DETERMINE EFFECTIVENESS CATEGORY =====
        // These flags are mutually exclusive and used for UI feedback
        // Priority order matters: check immune first, then not very, then super
        result.wasImmune = multiplier <= 0f;
        result.wasNotVeryEffective = multiplier < 1f && multiplier > 0f;
        result.wasSuperEffective = multiplier > 1f;
    
        // ===== CALCULATE FINAL DAMAGE =====
        // Immune: Always 0 damage
        // Otherwise: Base * Multiplier, rounded, minimum 1 damage
        // The minimum 1 ensures chip damage is always possible (unless immune)
        if (result.wasImmune)
        {
            result.finalDamage = 0f;
        }
        else
        {
            // Round to whole number for cleaner display
            // Mathf.Max ensures at least 1 damage even with tiny multipliers
            result.finalDamage = Mathf.Max(1f, Mathf.Round(attackInfo.amount * multiplier));
        }

        return result;
    }
}