using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TileSlot)), CanEditMultipleObjects]
public class TileSlotEditor : Editor
{
    private GUIStyle centredStyle;
    private GUIStyle propInfoStyle;
    
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
        float threeButtonWidth = (EditorGUIUtility.currentViewWidth - 25) / 3;
        
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
        
        // Field Tiles Section
        GUILayout.Label("Field Tiles", centredStyle);
        
        if (GUILayout.Button("Field", GUILayout.Width(oneButtonWidth)))
        {
            TileSetHolder holder = FindFirstObjectByType<TileSetHolder>();
    
            foreach (var targetTile in targets)
            {
                TileSlot slot = (TileSlot)targetTile;
        
                // Divide by tileSpacing (2) to get actual grid coordinates
                int x = Mathf.RoundToInt(slot.transform.position.x / 2f);
                int z = Mathf.RoundToInt(slot.transform.position.z / 2f);
                bool useLight = (x + z) % 2 == 0;
        
                GameObject newTile = useLight ? holder.tileFieldLight : holder.tileFieldDark;
                slot.SwitchTile(newTile);
            }
        }
        
        // Roads Section
        GUILayout.Label("Roads", centredStyle);
        
        if (GUILayout.Button("Road", GUILayout.Width(oneButtonWidth)))
        {
            GameObject newTile = FindFirstObjectByType<TileSetHolder>().tileRoad;
            foreach (var targetTile in targets)
            {
                ((TileSlot)targetTile).SwitchTile(newTile);
            }
        }
        
        // Corners Section
        GUILayout.Label("Corners", centredStyle);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Inner Corner", GUILayout.Width(twoButtonWidth)))
        {
            GameObject newTile = FindFirstObjectByType<TileSetHolder>().tileInnerCorner;
            foreach (var targetTile in targets)
            {
                ((TileSlot)targetTile).SwitchTile(newTile);
            }
        }
        
        if (GUILayout.Button("Outer Corner", GUILayout.Width(twoButtonWidth)))
        {
            GameObject newTile = FindFirstObjectByType<TileSetHolder>().tileOuterCorner;
            foreach (var targetTile in targets)
            {
                ((TileSlot)targetTile).SwitchTile(newTile);
            }
        }
        
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Inner Corner 2", GUILayout.Width(twoButtonWidth)))
        {
            GameObject newTile = FindFirstObjectByType<TileSetHolder>().tileInnerCorner2;
            foreach (var targetTile in targets)
            {
                ((TileSlot)targetTile).SwitchTile(newTile);
            }
        }
        
        if (GUILayout.Button("Outer Corner 2", GUILayout.Width(twoButtonWidth)))
        {
            GameObject newTile = FindFirstObjectByType<TileSetHolder>().tileOuterCorner2;
            foreach (var targetTile in targets)
            {
                ((TileSlot)targetTile).SwitchTile(newTile);
            }
        }
        GUILayout.EndHorizontal();
        
        // Sideways Section
        GUILayout.Label("Sideways", centredStyle);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Sideway Road1", GUILayout.Width(twoButtonWidth)))
        {
            GameObject newTile = FindFirstObjectByType<TileSetHolder>().tileSidewayRoad;
            foreach (var targetTile in targets)
            {
                ((TileSlot)targetTile).SwitchTile(newTile);
            }
        }
        
        if (GUILayout.Button("Sideway Road2", GUILayout.Width(twoButtonWidth)))
        {
            GameObject newTile = FindFirstObjectByType<TileSetHolder>().tileSidewayRoad2;
            foreach (var targetTile in targets)
            {
                ((TileSlot)targetTile).SwitchTile(newTile);
            }
        }
        GUILayout.EndHorizontal();
        
        // Water Section
        GUILayout.Label("Water", centredStyle);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("River", GUILayout.Width(twoButtonWidth)))
        {
            GameObject newTile = FindFirstObjectByType<TileSetHolder>().tileRiver;
            foreach (var targetTile in targets)
            {
                ((TileSlot)targetTile).SwitchTile(newTile);
            }
        }
        
        if (GUILayout.Button("Pond", GUILayout.Width(twoButtonWidth)))
        {
            GameObject newTile = FindFirstObjectByType<TileSetHolder>().tilePond;
            foreach (var targetTile in targets)
            {
                ((TileSlot)targetTile).SwitchTile(newTile);
            }
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("River Bridge", GUILayout.Width(twoButtonWidth)))
        {
            GameObject newTile = FindFirstObjectByType<TileSetHolder>().tileRiverBridge;
            foreach (var targetTile in targets)
            {
                ((TileSlot)targetTile).SwitchTile(newTile);
            }
        }
        
        if (GUILayout.Button("River Double Bridge", GUILayout.Width(twoButtonWidth)))
        {
            GameObject newTile = FindFirstObjectByType<TileSetHolder>().tileRiverDoubleBridge;
            foreach (var targetTile in targets)
            {
                ((TileSlot)targetTile).SwitchTile(newTile);
            }
        }
        GUILayout.EndHorizontal();
        
        // ==================== PROPS SECTION ====================
        GUILayout.Space(10);
        GUILayout.Label("Props", centredStyle);
        
        // Show prop count info
        TileSlot primarySlot = (TileSlot)target;
        int propCount = primarySlot.GetPropCount();
        GUILayout.Label(propCount > 0 ? $"Props on tile: {propCount}" : "No props placed", propInfoStyle);
        
        TileSetHolder holder2 = FindFirstObjectByType<TileSetHolder>();
        
        // Objects
        GUILayout.Label("Objects", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Barrel", GUILayout.Width(threeButtonWidth)))
        {
            AddPropToTargets(holder2.propBarrel);
        }
        if (GUILayout.Button("Crate", GUILayout.Width(threeButtonWidth)))
        {
            AddPropToTargets(holder2.propCrate);
        }
        if (GUILayout.Button("Ladder", GUILayout.Width(threeButtonWidth)))
        {
            AddPropToTargets(holder2.propLadder);
        }
        GUILayout.EndHorizontal();
        
        // Grass
        GUILayout.Label("Grass", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Grass 1", GUILayout.Width(twoButtonWidth)))
        {
            AddPropToTargets(holder2.propGrass1);
        }
        if (GUILayout.Button("Grass 2", GUILayout.Width(twoButtonWidth)))
        {
            AddPropToTargets(holder2.propGrass2);
        }
        GUILayout.EndHorizontal();
        
        // Apple Trees
        GUILayout.Label("Apple Trees", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Apple Short", GUILayout.Width(twoButtonWidth)))
        {
            AddPropToTargets(holder2.propAppleTreeShort);
        }
        if (GUILayout.Button("Apple Tall", GUILayout.Width(twoButtonWidth)))
        {
            AddPropToTargets(holder2.propAppleTreeTall);
        }
        GUILayout.EndHorizontal();
        
        // Regular Trees
        GUILayout.Label("Trees", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Tree Short", GUILayout.Width(twoButtonWidth)))
        {
            AddPropToTargets(holder2.propTreeShort);
        }
        if (GUILayout.Button("Tree Tall", GUILayout.Width(twoButtonWidth)))
        {
            AddPropToTargets(holder2.propTreeTall);
        }
        GUILayout.EndHorizontal();
        
        // Pine Trees
        GUILayout.Label("Pine Trees", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Pine Short", GUILayout.Width(twoButtonWidth)))
        {
            AddPropToTargets(holder2.propPineShort);
        }
        if (GUILayout.Button("Pine Short 2", GUILayout.Width(twoButtonWidth)))
        {
            AddPropToTargets(holder2.propPineShort2);
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Pine Tall", GUILayout.Width(twoButtonWidth)))
        {
            AddPropToTargets(holder2.propPineTall);
        }
        if (GUILayout.Button("Pine Tall 2", GUILayout.Width(twoButtonWidth)))
        {
            AddPropToTargets(holder2.propPineTall2);
        }
        GUILayout.EndHorizontal();
        
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