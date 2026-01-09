using UnityEngine;

/// <summary>
/// ScriptableObject that stores the type effectiveness chart for the damage system.
/// Create via: Assets > Create > Tower Defense > Type Matchup Database
/// 
/// The type chart is a 2D matrix where:
/// - Rows = Attacker element type
/// - Columns = Defender element type
/// - Cell value = Damage multiplier
/// 
/// Example: typeChart[Physical][Magic] = 0.5 means Physical attacks deal
/// half damage to Magic-type defenders.
/// </summary>
[CreateAssetMenu(fileName = "TypeMatchupDatabase", menuName = "Tower Defense/Type Matchup Database")]
public class TypeMatchupDatabase : ScriptableObject
{
    /// <summary>
    /// Represents one row in the type chart (one attacker type vs all defender types).
    /// Array indices correspond to ElementType enum values:
    /// [0] = Physical, [1] = Magic, [2] = Mechanic, [3] = Imaginary
    /// </summary>
    [System.Serializable]
    public class TypeRow
    {
        [Tooltip("Physical, Magic, Mechanic, Imaginary")]
        public float[] multipliers = new float[4] { 1f, 1f, 1f, 1f };
    }

    // ===== PRESET MULTIPLIER VALUES =====
    // Define these once for consistency across the chart
    // Makes it easy to balance - change here affects all matchups using that tier
    [Header("Damage Multipliers")]
    [SerializeField] private float superEffective = 2f;      // Type advantage (2x damage)
    [SerializeField] private float notVeryEffective = 0.5f;  // Type disadvantage (0.5x damage)
    [SerializeField] private float normal = 1f;              // Neutral matchup (1x damage)
    [SerializeField] private float immune = 0f;              // Complete immunity (0 damage)

    // ===== TYPE CHART MATRIX =====
    // 4x4 grid representing all attacker vs defender combinations
    // Row index = attacker ElementType, Column index = defender ElementType
    //
    // Visual representation:
    //                    DEFENDER
    //              Phys  Magic Mech  Imag
    // A  Physical [  1     1     1     1  ]
    // T  Magic    [  1     1     1     1  ]
    // K  Mechanic [  1     1     1     1  ]
    //    Imaginary[  1     1     1     1  ]
    //
    [Header("Type Chart (Attacker vs Defender)")]
    [SerializeField] private TypeRow[] typeChart = new TypeRow[4]
    {
        new TypeRow { multipliers = new float[] { 1f, 1f, 1f, 1f } }, // Physical attacks vs all
        new TypeRow { multipliers = new float[] { 1f, 1f, 1f, 1f } }, // Magic attacks vs all
        new TypeRow { multipliers = new float[] { 1f, 1f, 1f, 1f } }, // Mechanic attacks vs all
        new TypeRow { multipliers = new float[] { 1f, 1f, 1f, 1f } }, // Imaginary attacks vs all
    };

    /// <summary>
    /// Looks up the damage multiplier for a given attacker/defender type combination.
    /// </summary>
    /// <param name="attacker">The attacking element type</param>
    /// <param name="defender">The defending element type</param>
    /// <returns>Damage multiplier (0 = immune, 0.5 = resist, 1 = normal, 2 = super effective)</returns>
    public float GetEffectiveness(ElementType attacker, ElementType defender)
    {
        // Convert enum to array index
        int attackerIndex = (int)attacker;
        int defenderIndex = (int)defender;

        // ===== BOUNDS CHECKING =====
        // Protects against invalid indices (e.g., if Global type is passed)
        // Returns neutral (1.0) for any out-of-bounds access
        if (typeChart == null || 
            attackerIndex >= typeChart.Length || 
            typeChart[attackerIndex].multipliers == null ||
            defenderIndex >= typeChart[attackerIndex].multipliers.Length)
        {
            return 1f;  // Default to neutral if lookup fails
        }

        return typeChart[attackerIndex].multipliers[defenderIndex];
    }

    // ===== EDITOR UTILITY METHODS =====
    // Right-click the ScriptableObject in inspector to access these
    
    /// <summary>
    /// Editor utility: Resets all type matchups to neutral (1.0).
    /// Useful when redesigning the type system from scratch.
    /// </summary>
    [ContextMenu("Reset All to Normal")]
    private void ResetToNormal()
    {
        foreach (var row in typeChart)
        {
            for (int i = 0; i < row.multipliers.Length; i++)
            {
                row.multipliers[i] = normal;
            }
        }
    }

    /// <summary>
    /// Editor utility: Applies a rock-paper-scissors style type chart.
    /// Creates a balanced system where each type has strengths and weaknesses.
    /// 
    /// Example matchups applied:
    /// - Physical: Strong vs Mechanic, Weak vs Magic
    /// - Magic: Strong vs Physical, Weak vs Mechanic  
    /// - Mechanic: Strong vs Magic, Weak vs Physical
    /// - Imaginary: Weak vs itself (self-doubt?)
    /// </summary>
    [ContextMenu("Apply Example Matchups")]
    private void ApplyExampleMatchups()
    {
        // Row = Attacker, Columns = [Physical, Magic, Mechanic, Imaginary]
        typeChart[0].multipliers = new float[] { normal, notVeryEffective, superEffective, normal };  // Physical
        typeChart[1].multipliers = new float[] { superEffective, normal, notVeryEffective, normal };  // Magic
        typeChart[2].multipliers = new float[] { notVeryEffective, superEffective, normal, normal };  // Mechanic
        typeChart[3].multipliers = new float[] { normal, normal, normal, notVeryEffective };          // Imaginary
    }
}