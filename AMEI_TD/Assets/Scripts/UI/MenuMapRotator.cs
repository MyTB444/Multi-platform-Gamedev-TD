using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuMapRotator : MonoBehaviour
{
    [Header("Cubes")]
    public List<GameObject> cubePrefabs;         
    public Transform cubeSlot;                  

    [Header("UI Buttons")]
    public Button leftButton;
    public Button rightButton;
    public Button selectButton;

    private int currentIndex = 0;
    private GameObject currentCubeInstance;

    void Start()
    {
        leftButton.onClick.AddListener(RotateLeft);
        rightButton.onClick.AddListener(RotateRight);
        selectButton.onClick.AddListener(SelectCube);

        ShowCube(currentIndex);
    }

    void ShowCube(int index)
    {
        // Destroy existing cube
        if (currentCubeInstance != null)
            Destroy(currentCubeInstance);

        // Instantiate new cube at the center
        currentCubeInstance = Instantiate(cubePrefabs[index], cubeSlot.position, cubeSlot.rotation, cubeSlot);
    }

    void RotateLeft()
    {
        currentIndex = (currentIndex - 1 + cubePrefabs.Count) % cubePrefabs.Count;
        ShowCube(currentIndex);
    }

    void RotateRight()
    {
        currentIndex = (currentIndex + 1) % cubePrefabs.Count;
        ShowCube(currentIndex);
    }

    void SelectCube()
    {
        Debug.Log($"Selected cube index: {currentIndex}");
        //TODO
    }
    public int ReturnIndex()
    {
        return currentIndex;
    }
}
