using UnityEngine;

[CreateAssetMenu(fileName = "TypeMatchupDatabase", menuName = "Tower Defense/Type Matchup Database")]
public class TypeMatchupDatabase : ScriptableObject
{
    [System.Serializable]
    public class TypeRow
    {
        [Tooltip("Physical, Magic, Mechanic, Imaginary")]
        public float[] multipliers = new float[4] { 1f, 1f, 1f, 1f };
    }

    [Header("Damage Multipliers")]
    [SerializeField] private float superEffective = 2f;
    [SerializeField] private float notVeryEffective = 0.5f;
    [SerializeField] private float normal = 1f;
    [SerializeField] private float immune = 0f;

    [Header("Type Chart (Attacker vs Defender)")]
    [SerializeField] private TypeRow[] typeChart = new TypeRow[4]
    {
        new TypeRow { multipliers = new float[] { 1f, 1f, 1f, 1f } },
        new TypeRow { multipliers = new float[] { 1f, 1f, 1f, 1f } },
        new TypeRow { multipliers = new float[] { 1f, 1f, 1f, 1f } },
        new TypeRow { multipliers = new float[] { 1f, 1f, 1f, 1f } },
    };

    public float GetEffectiveness(ElementType attacker, ElementType defender)
    {
        int attackerIndex = (int)attacker;
        int defenderIndex = (int)defender;

        if (typeChart == null || 
            attackerIndex >= typeChart.Length || 
            typeChart[attackerIndex].multipliers == null ||
            defenderIndex >= typeChart[attackerIndex].multipliers.Length)
        {
            return 1f;
        }

        return typeChart[attackerIndex].multipliers[defenderIndex];
    }

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

    [ContextMenu("Apply Example Matchups")]
    private void ApplyExampleMatchups()
    {
        typeChart[0].multipliers = new float[] { normal, notVeryEffective, superEffective, normal };
        typeChart[1].multipliers = new float[] { superEffective, normal, notVeryEffective, normal };
        typeChart[2].multipliers = new float[] { notVeryEffective, superEffective, normal, normal };
        typeChart[3].multipliers = new float[] { normal, normal, normal, notVeryEffective };
    }
}