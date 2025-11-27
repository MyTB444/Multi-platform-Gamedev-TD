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
    public GameObject tileInnerCorner2;
    public GameObject tileOuterCorner;
    public GameObject tileOuterCorner2;

    [Header("Sideways")]
    public GameObject tileSidewayRoad;
    public GameObject tileSidewayRoad2;

    [Header("Water")]
    public GameObject tileRiver;
    public GameObject tilePond;
    public GameObject tileRiverBridge;
    public GameObject tileRiverDoubleBridge;

    [Header("Props - Objects")]
    public GameObject propBarrel;
    public GameObject propCrate;
    public GameObject propLadder;

    [Header("Props - Grass")]
    public GameObject propGrass1;
    public GameObject propGrass2;

    [Header("Props - Apple Trees")]
    public GameObject propAppleTreeShort;
    public GameObject propAppleTreeTall;

    [Header("Props - Trees")]
    public GameObject propTreeShort;
    public GameObject propTreeTall;

    [Header("Props - Pine Trees")]
    public GameObject propPineShort;
    public GameObject propPineShort2;
    public GameObject propPineTall;
    public GameObject propPineTall2;
}