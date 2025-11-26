using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using TreeEditor;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SkillDescriptionUpdater : MonoBehaviour
{
    public static SkillDescriptionUpdater instance;
    [SerializeField] private TextMeshProUGUI[] texts;
    private string[] savedTexts;
    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        texts = GetComponentsInChildren<TextMeshProUGUI>();
        savedTexts[0] = texts[0].text;
        savedTexts[1] = texts[1].text;
        savedTexts[2] = texts[2].text;

    }
    public void UpdateTexts(SkillNode node)
    {
        texts[0].text = node.skillName;
        texts[1].text = node.skillCost;
        texts[2].text = node.description;
    }
}
