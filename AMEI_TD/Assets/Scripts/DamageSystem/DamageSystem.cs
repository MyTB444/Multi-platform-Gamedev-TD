using UnityEngine;

public enum ElementType
{
    Physical,
    Magic,
    Mechanic,
    Imaginary
}

[System.Serializable]
public struct DamageInfo
{
    public float amount;
    public ElementType elementType;

    public DamageInfo(float amount, ElementType elementType)
    {
        this.amount = amount;
        this.elementType = elementType;
    }
}

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
        result.finalDamage = attackInfo.amount * multiplier;

        return result;
    }
}