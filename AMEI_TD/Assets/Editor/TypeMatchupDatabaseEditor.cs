using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TypeMatchupDatabase))]
public class TypeMatchupDatabaseEditor : Editor
{
    private SerializedProperty typeChart;
    private SerializedProperty superEffective;
    private SerializedProperty notVeryEffective;
    private SerializedProperty normal;
    private SerializedProperty immune;

    private readonly string[] typeNames = { "Physical", "Magic", "Mechanic", "Imaginary" };
    private readonly string[] effectivenessOptions = { "Immune", "Not Very Effective", "Normal", "Super Effective" };
    
    private readonly Color superColor = new Color(0.5f, 1f, 0.5f);
    private readonly Color weakColor = new Color(1f, 0.7f, 0.7f);
    private readonly Color immuneColor = new Color(0.5f, 0.5f, 0.5f);

    private void OnEnable()
    {
        typeChart = serializedObject.FindProperty("typeChart");
        superEffective = serializedObject.FindProperty("superEffective");
        notVeryEffective = serializedObject.FindProperty("notVeryEffective");
        normal = serializedObject.FindProperty("normal");
        immune = serializedObject.FindProperty("immune");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Damage Multipliers", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("These values are applied when you select an option from the dropdowns below.", MessageType.Info);
        EditorGUILayout.PropertyField(superEffective);
        EditorGUILayout.PropertyField(notVeryEffective);
        EditorGUILayout.PropertyField(normal);
        EditorGUILayout.PropertyField(immune);

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Type Matchup Chart", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Attacker (row) vs Defender (column)", EditorStyles.miniLabel);

        EditorGUILayout.Space(10);

        // Header row
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("", GUILayout.Width(80));
        foreach (string typeName in typeNames)
        {
            EditorGUILayout.LabelField(typeName, EditorStyles.boldLabel, GUILayout.Width(100));
        }
        EditorGUILayout.EndHorizontal();

        // Data rows
        for (int attacker = 0; attacker < 4; attacker++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(typeNames[attacker], EditorStyles.boldLabel, GUILayout.Width(80));

            SerializedProperty row = typeChart.GetArrayElementAtIndex(attacker);
            SerializedProperty multipliers = row.FindPropertyRelative("multipliers");

            for (int defender = 0; defender < 4; defender++)
            {
                SerializedProperty cell = multipliers.GetArrayElementAtIndex(defender);
                float value = cell.floatValue;

                // Convert float to dropdown index
                int selectedIndex = FloatToIndex(value);

                // Color based on effectiveness
                Color originalColor = GUI.backgroundColor;
                GUI.backgroundColor = GetColorForIndex(selectedIndex);

                int newIndex = EditorGUILayout.Popup(selectedIndex, effectivenessOptions, GUILayout.Width(100));

                GUI.backgroundColor = originalColor;

                // Convert back to float
                if (newIndex != selectedIndex)
                {
                    cell.floatValue = IndexToFloat(newIndex);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(15);

        // Legend
        EditorGUILayout.LabelField("Legend", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        DrawLegendItem(superColor, "Super Effective");
        DrawLegendItem(weakColor, "Not Very Effective");
        DrawLegendItem(immuneColor, "Immune");
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }

    private int FloatToIndex(float value)
    {
        if (value <= 0f) return 0;                    // Immune
        if (value < 1f) return 1;                     // Not Very Effective
        if (Mathf.Approximately(value, 1f)) return 2; // Normal
        return 3;                                      // Super Effective
    }

    private float IndexToFloat(int index)
    {
        return index switch
        {
            0 => immune.floatValue,
            1 => notVeryEffective.floatValue,
            2 => normal.floatValue,
            3 => superEffective.floatValue,
            _ => 1f
        };
    }

    private Color GetColorForIndex(int index)
    {
        return index switch
        {
            0 => immuneColor,
            1 => weakColor,
            3 => superColor,
            _ => Color.white
        };
    }

    private void DrawLegendItem(Color color, string label)
    {
        Color original = GUI.backgroundColor;
        GUI.backgroundColor = color;
        GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(20));
        GUI.backgroundColor = original;
        EditorGUILayout.LabelField(label, GUILayout.Width(110));
    }
}