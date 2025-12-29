using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CircleNode : MonoBehaviour
{
    public Button button;
    public GameObject leftNode;
    public GameObject rightNode;
    public GameObject nextNode;
    [SerializeField] private NodeButton leftNodeSkill;
    [SerializeField] private NodeButton rightNodeSkill;
    private SkillNode lefty;
    private SkillNode righty;
    void Start()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
        leftNodeSkill = leftNode.GetComponentInChildren<NodeButton>();
        rightNodeSkill = rightNode.GetComponentInChildren<NodeButton>();
        lefty = leftNodeSkill.GetSkill();
        righty = rightNodeSkill.GetSkill();
    }

    // Update is called once per frame
    void Update()
    {
        CheckNodes();
        
    }
    private void  CheckNodes()
    {
        if (SkillTreeManager.instance.IsSkillUnlocked(righty))
        {
            SkillChosen(rightNode, leftNode);
        }
        if (SkillTreeManager.instance.IsSkillUnlocked(lefty))
        {
            SkillChosen(leftNode, rightNode);
        }

    }
    private void SkillChosen(GameObject objectChosen, GameObject discarded)
    {
        objectChosen.transform.position = this.gameObject.transform.position;
        Destroy(this.gameObject);
        Destroy(discarded);
        nextNode.SetActive(true);
    }
    private void OnButtonClick()
    {
        ButtonEnabler();
    }
    private void ButtonEnabler()
    {
        if (!rightNode.activeSelf)
        {
            rightNode.SetActive(true);
            leftNode.SetActive(true);
        }
        else{
            rightNode.SetActive(false);
            leftNode.SetActive(false);            
        }
    }
}
