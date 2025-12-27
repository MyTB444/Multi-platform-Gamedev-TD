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

        EditorGUILayout.LabelField("Quick Reference Values", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(superEffective);
        EditorGUILayout.PropertyField(notVeryEffective);
        EditorGUILayout.PropertyField(normal);
        EditorGUILayout.PropertyField(immune);

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Type Matchup Chart", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Attacker (row) vs Defender (column)", EditorStyles.miniLabel);

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("", GUILayout.Width(80));
        foreach (string typeName in typeNames)
        {
            EditorGUILayout.LabelField(typeName, EditorStyles.boldLabel, GUILayout.Width(65));
        }
        EditorGUILayout.EndHorizontal();

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

                Color originalColor = GUI.backgroundColor;
                
                if (value > 1f)
                    GUI.backgroundColor = superColor;
                else if (value < 1f && value > 0f)
                    GUI.backgroundColor = weakColor;
                else if (value <= 0f)
                    GUI.backgroundColor = immuneColor;

                cell.floatValue = EditorGUILayout.FloatField(value, GUILayout.Width(65));
                GUI.backgroundColor = originalColor;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(15);

        EditorGUILayout.LabelField("Legend", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        DrawLegendItem(superColor, "Super Effective (>1)");
        DrawLegendItem(weakColor, "Not Very (<1)");
        DrawLegendItem(immuneColor, "Immune (0)");
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
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