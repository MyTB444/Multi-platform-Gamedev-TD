using UnityEngine;

public class PageScroller : MonoBehaviour
{
    [Header("Pages to Scroll Through")]
    public GameObject[] pages;

    [Header("Optional Settings")]
    public bool loopPages = true;

    private int currentIndex = 0;

    void Start()
    {
        ShowPage(currentIndex);
    }

    public void NextPage()
    {
        currentIndex++;
        if (currentIndex >= pages.Length)
            currentIndex = loopPages ? 0 : pages.Length - 1;

        ShowPage(currentIndex);
    }

    public void PreviousPage()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = loopPages ? pages.Length - 1 : 0;

        ShowPage(currentIndex);
    }

    private void ShowPage(int index)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == index);
        }
    }
}
