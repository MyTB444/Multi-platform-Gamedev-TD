using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Handles a branching point in the skill tree where player must choose between two paths.
/// Represented as a circle button that expands to show left/right skill options.
/// 
/// Behavior:
/// 1. Click circle → shows left and right skill options
/// 2. Player unlocks one of the skills
/// 3. Chosen skill moves to circle's position, other option is destroyed
/// 4. Next node in the tree becomes active
/// 
/// This creates mutually exclusive skill paths (e.g., choose between two tower upgrades).
/// </summary>
public class CircleNode : MonoBehaviour
{
    [Header("References")]
    public Button button;              // The circle button itself
    public GameObject leftNode;        // Left skill option container
    public GameObject rightNode;       // Right skill option container
    public GameObject nextNode;        // Next node to activate after choice is made
    
    [SerializeField] private NodeButton leftNodeSkill;   // NodeButton component of left option
    [SerializeField] private NodeButton rightNodeSkill;  // NodeButton component of right option
    
    // Cached SkillNode references for checking unlock status
    private SkillNode lefty;
    private SkillNode righty;
    
    void Start()
    {
        // Auto-find button if not assigned
        if (button == null)
            button = GetComponent<Button>();
            
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
        
        // Get NodeButton components from child objects
        leftNodeSkill = leftNode.GetComponentInChildren<NodeButton>();
        rightNodeSkill = rightNode.GetComponentInChildren<NodeButton>();
        
        // Cache the SkillNode references for faster checking in Update
        lefty = leftNodeSkill.GetSkill();
        righty = rightNodeSkill.GetSkill();
    }

    void Update()
    {
        // Continuously check if either skill has been unlocked
        CheckNodes();
    }
    
    /// <summary>
    /// Checks if either the left or right skill has been unlocked.
    /// When one is chosen, collapses the branch and advances the tree.
    /// </summary>
    private void CheckNodes()
    {
        // Check if right skill was chosen
        if (SkillTreeManager.instance.IsSkillUnlocked(righty))
        {
            SkillChosen(rightNode, leftNode);
        }
        
        // Check if left skill was chosen
        if (SkillTreeManager.instance.IsSkillUnlocked(lefty))
        {
            SkillChosen(leftNode, rightNode);
        }
    }
    
    /// <summary>
    /// Called when player makes their choice. Collapses the branch:
    /// 1. Moves chosen skill to this circle's position
    /// 2. Destroys the circle node (no longer needed)
    /// 3. Destroys the unchosen option
    /// 4. Activates the next node in the tree
    /// </summary>
    /// <param name="objectChosen">The skill node the player selected</param>
    /// <param name="discarded">The skill node that was not selected</param>
    private void SkillChosen(GameObject objectChosen, GameObject discarded)
    {
        // Move chosen skill to where the circle was (cleaner visual layout)
        objectChosen.transform.position = this.gameObject.transform.position;
        
        // Clean up - destroy circle and unchosen option
        Destroy(this.gameObject);
        Destroy(discarded);
        
        // Reveal the next node in the tree progression
        nextNode.SetActive(true);
    }
    
    /// <summary>
    /// Button click handler - toggles visibility of the two options.
    /// </summary>
    private void OnButtonClick()
    {
        ButtonEnabler();
    }
    
    /// <summary>
    /// Toggles the left/right skill options on and off.
    /// Click once to expand, click again to collapse.
    /// </summary>
    private void ButtonEnabler()
    {
        if (!rightNode.activeSelf)
        {
            // Currently hidden → show both options
            rightNode.SetActive(true);
            leftNode.SetActive(true);
        }
        else
        {
            // Currently shown → hide both options
            rightNode.SetActive(false);
            leftNode.SetActive(false);            
        }
    }
}