using UnityEngine;

public class TileSetHolder : MonoBehaviour
{
    [Header("Field Tiles (Checkered)")]
    public GameObject tileFieldLight;
    public GameObject tileFieldDark;

    [Header("Roads")]
    public GameObject tileRoad;

    [Header("Corners")]
    public GameObject tileInnerCorner;
    public GameObject tileOuterCorner;

    [Header("Sideways")]
    public GameObject tileSidewayRoad;

    [Header("Water")]
    public GameObject tileRiver;
    public GameObject tilePond;
    public GameObject tileRiverBridge;
    public GameObject tileRiverDoubleBridge;
}