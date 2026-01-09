using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Central game state manager. Handles player resources (points, health),
/// win/lose conditions, and level-wide configuration like available elements.
/// </summary>
public class GameManager : MonoBehaviour
{
    [SerializeField] private int points;
    [SerializeField] private float healthPoints = 10f;
    public static GameManager instance;
    public WaveManager waveManager;

    private bool gameLost;
    public bool IsGameLost() => gameLost;
    
    [Header("Damage System")]
    [SerializeField] private TypeMatchupDatabase typeMatchupDatabase;

    [Header("Level Element Availability")]
    [SerializeField] private List<ElementType> availableElements = new List<ElementType>();

    private void Awake()
    {
        instance = this;
        
        // Initialize the damage calculation system with type matchup data
        // This must happen before any combat calculations occur
        DamageCalculator.Initialize(typeMatchupDatabase);
    }

    void Start()
    {
        // Initialize UI with starting values
        UIBase.instance.UpdatePointsUI(points);
        waveManager.ActivateWaveManager();
        UIBase.instance.UpdateHpUi(healthPoints);
    }

    /// <summary>
    /// Modifies the player's skill points.
    /// Use negative values to deduct points, positive values to add points.
    /// </summary>
    /// <param name="newPoints">Amount to add (positive) or subtract (negative)</param>
    public void UpdateSkillPoints(int newPoints)
    {
        points += newPoints;
        UIBase.instance.UpdatePointsUI(points);
        
        // Notify skill tree system that points changed (may unlock new abilities)
        SkillTreeManager.instance.PointsGained();
    }

    /// <summary>
    /// Called when the player loses (health reaches 0).
    /// Disables input, shows game over UI, and stops wave progression.
    /// </summary>
    public void LevelFailed()
    {
        InputHandler.instance.DisableInput();
        GameOverOutcomeUI(false);
        gameLost = true;
        StopWaveProgression();
    }

    /// <summary>
    /// Called when the player wins (mega wave completed).
    /// Disables input and shows victory UI.
    /// </summary>
    public void LevelCompleted()
    {
        InputHandler.instance.DisableInput();
        GameOverOutcomeUI(true);
    }
    
    /// <summary>
    /// Shows the appropriate end-game UI based on outcome.
    /// </summary>
    /// <param name="playerWon">True for victory screen, false for defeat screen</param>
    private void GameOverOutcomeUI(bool playerWon)
    {
        UIBase.instance.GameWon(playerWon);
    }

    /// <summary>
    /// Halts all wave and enemy spawning systems.
    /// </summary>
    private void StopWaveProgression()
    {
        StopMakingEnemies();

        if (waveManager != null) waveManager.DeactivateWaveManager();
    }

    /// <summary>
    /// Disables enemy creation on all spawners in the scene.
    /// Called on game over to prevent new enemies from appearing.
    /// </summary>
    public void StopMakingEnemies()
    {
        EnemySpawner[] spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);

        foreach (var spawner in spawners)
        {
            spawner.CanCreateNewEnemies(false);
        }
    }

    public int GetPoints() => points;
    public float GetHealthPoints() => healthPoints;

    /// <summary>
    /// Reduces player's castle health by the specified damage amount.
    /// Triggers game over if health reaches zero.
    /// </summary>
    /// <param name="damage">Amount of damage to apply</param>
    public void TakeDamageHealth(float damage)
    {
        healthPoints -= damage;
        UIBase.instance.UpdateHpUi(healthPoints);

        // Check for death, but only trigger once (gameLost flag)
        if (healthPoints <= 0 && !gameLost)
        {
            healthPoints = 0;
            LevelFailed();
        }
    }

    /// <summary>
    /// Returns the list of element types available in the current level.
    /// Used by towers/skills to determine what elemental options the player has.
    /// </summary>
    public List<ElementType> GetAvailableElements()
    {
        return availableElements;
    }

    /// <summary>
    /// Allows runtime modification of available elements (e.g., for special events).
    /// </summary>
    public void SetAvailableElements(List<ElementType> elements)
    {
        availableElements = elements;
    }
}