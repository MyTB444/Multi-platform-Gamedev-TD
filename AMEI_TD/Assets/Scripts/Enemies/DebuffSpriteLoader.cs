using UnityEngine;

public class DebuffSpriteLoader : MonoBehaviour
{
    [Header("Debuff Sprites")]
    [SerializeField] private Sprite slowSprite;
    [SerializeField] private Sprite freezeSprite;
    [SerializeField] private Sprite burnSprite;
    [SerializeField] private Sprite poisonSprite;
    [SerializeField] private Sprite bleedSprite;
    [SerializeField] private Sprite frostbiteSprite; 
    
    private void Awake()
    {
        // Load all debuff sprites into static registry for UI display
        EnemyDebuffDisplay.LoadSprites(slowSprite, freezeSprite, burnSprite, poisonSprite, bleedSprite, frostbiteSprite);
    }
}