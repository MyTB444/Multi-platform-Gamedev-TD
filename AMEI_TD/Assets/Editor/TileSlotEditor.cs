using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomEditor(typeof(TileSlot)), CanEditMultipleObjects]
public class TileSlotEditor : Editor
{
    private GUIStyle centredStyle;
    private GUIStyle propInfoStyle;
    
    private static int selectedThemeIndex = 0;
    private static readonly string[] themeNames = { "Field", "Snow", "Desert", "Dungeon" };
    
    private TileSetHolder GetSelectedHolder()
    {
        TileSetHolder.TileTheme selectedTheme = (TileSetHolder.TileTheme)selectedThemeIndex;
        
        var holders = FindObjectsByType<TileSetHolder>(FindObjectsSortMode.None);
        var matchingHolder = holders.FirstOrDefault(h => h.theme == selectedTheme);
        
        if (matchingHolder == null && holders.Length > 0)
        {
            Debug.LogWarning($"No TileSetHolder found for theme '{selectedTheme}'. Using first available.");
            return holders[0];
        }
        
        return matchingHolder;
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        base.OnInspectorGUI();

        centredStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 16
        };
        
        propInfoStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Italic,
            fontSize = 12
        };

        float oneButtonWidth = (EditorGUIUtility.currentViewWidth - 25);
        float twoButtonWidth = (EditorGUIUtility.currentViewWidth - 25) / 2;
        
        // ==================== THEME SELECTION ====================
        GUILayout.Space(5);
        EditorGUILayout.LabelField("Theme", centredStyle);
        selectedThemeIndex = GUILayout.Toolbar(selectedThemeIndex, themeNames);
        
        TileSetHolder holder = GetSelectedHolder();
        
        if (holder == null)
        {
            EditorGUILayout.HelpBox($"No TileSetHolder found for '{themeNames[selectedThemeIndex]}' theme.\nCreate a GameObject with TileSetHolder component and set its theme.", MessageType.Warning);
            return;
        }
        
        GUILayout.Space(10);
        
        // Position and Rotation Section
        GUILayout.Label("Position and Rotation", centredStyle);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Rotate Left", GUILayout.Width(twoButtonWidth)))
        {
            foreach (var targetTile in targets)
            {
                ((TileSlot)targetTile).RotateTile(-1);
            }
        }
        
        if (GUILayout.Button("Rotate Right", GUILayout.Width(twoButtonWidth)))
        {
            foreach (var targetTile in targets)
            {
                ((TileSlot)targetTile).RotateTile(1);
            }
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("- .1f on the Y", GUILayout.Width(twoButtonWidth)))
        {
            foreach (var targetTile in targets)
            {
                ((TileSlot)targetTile).AdjustY(-1);
            }
        }
        
        if (GUILayout.Button("+ .1f on the Y", GUILayout.Width(twoButtonWidth)))
        {
            foreach (var targetTile in targets)
            {
                ((TileSlot)targetTile).AdjustY(1);
            }
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("- .5f on the Y", GUILayout.Width(twoButtonWidth)))
        {
            foreach (var targetTile in targets)
            {
                ((TileSlot)targetTile).AdjustY(-5);
            }
        }
        
        if (GUILayout.Button("+ .5f on the Y", GUILayout.Width(twoButtonWidth)))
        {
            foreach (var targetTile in targets)
            {
                ((TileSlot)targetTile).AdjustY(5);
            }
        }
        GUILayout.EndHorizontal();
        
        // Field Tiles Section
        GUILayout.Label("Field Tiles", centredStyle);

        if (GUILayout.Button("Field", GUILayout.Width(oneButtonWidth)))
        {
            foreach (var targetTile in targets)
            {
                TileSlot slot = (TileSlot)targetTile;
                int x = Mathf.RoundToInt(slot.transform.position.x / 2f);
                int z = Mathf.RoundToInt(slot.transform.position.z / 2f);
                bool useLight = (x + z) % 2 == 0;
                GameObject newTile = useLight ? holder.tileFieldLight : holder.tileFieldDark;
                slot.SwitchTile(newTile);
        
                // Reset Y to 0
                Vector3 pos = slot.transform.position;
                slot.transform.position = new Vector3(pos.x, 0f, pos.z);
            }
        }
        
        // Roads Section
        GUILayout.Label("Roads", centredStyle);

        if (GUILayout.Button("Road", GUILayout.Width(oneButtonWidth)))
        {
            foreach (var targetTile in targets)
            {
                TileSlot slot = (TileSlot)targetTile;
                slot.SwitchTile(holder.tileRoad);
                Vector3 pos = slot.transform.position;
                slot.transform.position = new Vector3(pos.x, holder.roadYOffset, pos.z);
            }
        }
        
        // Corners Section
        GUILayout.Label("Corners", centredStyle);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Inner Corner", GUILayout.Width(twoButtonWidth)))
        {
            foreach (var targetTile in targets)
            {
                TileSlot slot = (TileSlot)targetTile;
                slot.SwitchTile(holder.tileInnerCorner);
                Vector3 pos = slot.transform.position;
                slot.transform.position = new Vector3(pos.x, holder.roadYOffset, pos.z);
            }
        }

        if (GUILayout.Button("Outer Corner", GUILayout.Width(twoButtonWidth)))
        {
            foreach (var targetTile in targets)
            {
                TileSlot slot = (TileSlot)targetTile;
                slot.SwitchTile(holder.tileOuterCorner);
                Vector3 pos = slot.transform.position;
                slot.transform.position = new Vector3(pos.x, holder.roadYOffset, pos.z);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Inner Corner 2", GUILayout.Width(twoButtonWidth)))
        {
            foreach (var targetTile in targets)
            {
                TileSlot slot = (TileSlot)targetTile;
                slot.SwitchTile(holder.tileInnerCorner2);
                Vector3 pos = slot.transform.position;
                slot.transform.position = new Vector3(pos.x, holder.roadYOffset, pos.z);
            }
        }

        if (GUILayout.Button("Outer Corner 2", GUILayout.Width(twoButtonWidth)))
        {
            foreach (var targetTile in targets)
            {
                TileSlot slot = (TileSlot)targetTile;
                slot.SwitchTile(holder.tileOuterCorner2);
                Vector3 pos = slot.transform.position;
                slot.transform.position = new Vector3(pos.x, holder.roadYOffset, pos.z);
            }
        }
        GUILayout.EndHorizontal();

        // Sideways Section
        GUILayout.Label("Sideways", centredStyle);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Sideway Road1", GUILayout.Width(twoButtonWidth)))
        {
            foreach (var targetTile in targets)
            {
                TileSlot slot = (TileSlot)targetTile;
                slot.SwitchTile(holder.tileSidewayRoad);
                Vector3 pos = slot.transform.position;
                slot.transform.position = new Vector3(pos.x, holder.roadYOffset, pos.z);
            }
        }

        if (GUILayout.Button("Sideway Road2", GUILayout.Width(twoButtonWidth)))
        {
            foreach (var targetTile in targets)
            {
                TileSlot slot = (TileSlot)targetTile;
                slot.SwitchTile(holder.tileSidewayRoad2);
                Vector3 pos = slot.transform.position;
                slot.transform.position = new Vector3(pos.x, holder.roadYOffset, pos.z);
            }
        }
        GUILayout.EndHorizontal();
        
        // Water Section
        GUILayout.Label("Water", centredStyle);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("River", GUILayout.Width(twoButtonWidth)))
        {
            foreach (var targetTile in targets)
            {
                TileSlot slot = (TileSlot)targetTile;
                slot.SwitchTile(holder.tileRiver);
                Vector3 pos = slot.transform.position;
                slot.transform.position = new Vector3(pos.x, 0f, pos.z);
            }
        }

        if (GUILayout.Button("Pond", GUILayout.Width(twoButtonWidth)))
        {
            foreach (var targetTile in targets)
            {
                TileSlot slot = (TileSlot)targetTile;
                slot.SwitchTile(holder.tilePond);
                Vector3 pos = slot.transform.position;
                slot.transform.position = new Vector3(pos.x, 0f, pos.z);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("River Bridge", GUILayout.Width(twoButtonWidth)))
        {
            foreach (var targetTile in targets)
            {
                TileSlot slot = (TileSlot)targetTile;
                slot.SwitchTile(holder.tileRiverBridge);
                Vector3 pos = slot.transform.position;
                slot.transform.position = new Vector3(pos.x, 0f, pos.z);
            }
        }

        if (GUILayout.Button("River Double Bridge", GUILayout.Width(twoButtonWidth)))
        {
            foreach (var targetTile in targets)
            {
                TileSlot slot = (TileSlot)targetTile;
                slot.SwitchTile(holder.tileRiverDoubleBridge);
                Vector3 pos = slot.transform.position;
                slot.transform.position = new Vector3(pos.x, 0f, pos.z);
            }
        }
        GUILayout.EndHorizontal();
        
        // ==================== DYNAMIC PROPS SECTION ====================
        GUILayout.Space(10);
        GUILayout.Label("Props", centredStyle);
        
        TileSlot primarySlot = (TileSlot)target;
        int propCount = primarySlot.GetPropCount();
        GUILayout.Label(propCount > 0 ? $"Props on tile: {propCount}" : "No props placed", propInfoStyle);
        
        // Dynamically generate prop buttons from categories
        if (holder.propCategories != null && holder.propCategories.Count > 0)
        {
            foreach (var category in holder.propCategories)
            {
                if (category.props == null || category.props.Count == 0) continue;
                
                GUILayout.Label(category.categoryName, EditorStyles.boldLabel);
                
                // Calculate button width based on prop count (max 3 per row)
                int propsPerRow = Mathf.Min(category.props.Count, 3);
                float buttonWidth = (EditorGUIUtility.currentViewWidth - 25) / propsPerRow;
                
                int propIndex = 0;
                while (propIndex < category.props.Count)
                {
                    GUILayout.BeginHorizontal();
                    
                    // Draw up to 3 buttons per row
                    for (int i = 0; i < 3 && propIndex < category.props.Count; i++)
                    {
                        var prop = category.props[propIndex];
                        if (GUILayout.Button(prop.displayName, GUILayout.Width(buttonWidth)))
                        {
                            AddPropToTargets(prop.prefab);
                        }
                        propIndex++;
                    }
                    
                    GUILayout.EndHorizontal();
                }
            }
        }
        else
        {
            GUILayout.Label("No prop categories defined for this theme", propInfoStyle);
        }
        
        // Remove All Props Button
        GUILayout.Space(5);
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Remove All Props", GUILayout.Width(oneButtonWidth)))
        {
            foreach (var targetTile in targets)
            {
                ((TileSlot)targetTile).RemoveAllProps();
            }
        }
        GUI.backgroundColor = originalColor;
    }
    
    private void AddPropToTargets(GameObject propPrefab)
    {
        if (propPrefab == null) return;
        
        foreach (var targetTile in targets)
        {
            ((TileSlot)targetTile).AddProp(propPrefab);
        }
    }
}