using UnityEngine;

public enum ElementType
{
    Physical,
    Magic,
    Mechanic,
    Imaginary,
    Global
}

[System.Serializable]
public struct DamageInfo
{
    public float amount;
    public ElementType elementType;
    public bool isDoT;

    public DamageInfo(float amount, ElementType elementType, bool isDoT = false)
    {
        this.amount = amount;
        this.elementType = elementType;
        this.isDoT = isDoT;
    }
}

/// <summary>
/// Calculates damage with element type effectiveness (super effective, not very effective, immune).
/// </summary>
public static class DamageCalculator
{
    private static TypeMatchupDatabase _database;

    public static void Initialize(TypeMatchupDatabase database)
    {
        _database = database;
        Debug.Log("DamageCalculator initialized with database: " + (database != null));
    }

    public struct DamageResult
    {
        public float finalDamage;
        public bool wasSuperEffective;
        public bool wasNotVeryEffective;
        public bool wasImmune;
    }

    public static DamageResult Calculate(DamageInfo attackInfo, ElementType defenderType)
    {
        DamageResult result = new DamageResult();

        float multiplier = _database != null 
            ? _database.GetEffectiveness(attackInfo.elementType, defenderType) 
            : 1f;

        result.wasImmune = multiplier <= 0f;
        result.wasNotVeryEffective = multiplier < 1f && multiplier > 0f;
        result.wasSuperEffective = multiplier > 1f;
    
        // Round to whole number, minimum 1 damage unless immune
        if (result.wasImmune)
        {
            result.finalDamage = 0f;
        }
        else
        {
            result.finalDamage = Mathf.Max(1f, Mathf.Round(attackInfo.amount * multiplier));
        }

        return result;
    }
}