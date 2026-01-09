using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player spell abilities - Fire (path burning), Magic (area damage), Mechanic (bomb),
/// and Imaginary (reveals invisible enemies). Each spell type has its own cooldown and VFX.
/// </summary>
public class SpellAbility : MonoBehaviour
{
    // ==================== State & References ====================
    private SelectedPath selectedPath;
    private List<GameObject> flames = new();
    private GameObject magicAreas;
    public static SpellAbility instance;
    
    // Cooldown flags - prevent spell re-activation during cooldown
    private bool stopFire = false;
    private bool stopMagic = false;
    private bool stopMechanic = false;
    private bool stopImaginary = false;
    
    private Vector3 currentMousePosition;
    
    // Tracks placed magic areas to prevent duplicate placements at same position
    private Dictionary<Vector3, Vector3> mousePositionDictionary = new();

    public enum SpellType { Physical, Magic, Mechanic, Imaginary, None }
    
    private EnemyBase enemyBaseGameObjectRef;
    
    // Pre-allocated array for OverlapSphereNonAlloc to avoid GC allocations
    private Collider[] colliders = new Collider[800];
   
    [Header("VFX Prefabs")]
    [SerializeField] private GameObject flamesPrefab;
    [SerializeField] private GameObject magicAreaPrefab;
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private GameObject imaginaryVFXPrefab;
    [SerializeField] private GameObject tinyFlamesVisualPrefab;
    [SerializeField] private GameObject magicVisualPrefab;
    [SerializeField] private GameObject mechanicVisualPrefab;
    [SerializeField] private GameObject tinyFlamesPrefab;

    [Header("Pool Settings")]
    [SerializeField] private int flamesPoolSize = 20;
    [SerializeField] private int magicAreaPoolSize = 5;
    [SerializeField] private int bombPoolSize = 3;
    [SerializeField] private int imaginaryVFXPoolSize = 1;
    [SerializeField] private int tinyFlamesVFXPoolSize = 1;
    [SerializeField] private int magicVFXPoolSize = 1;
    [SerializeField] private int mechanicVFXPoolSize = 1;

    [Header("Cool Downs")]
    [SerializeField] private float fireSpellCoolDown;
    [SerializeField] private float magicSpellCoolDown;
    [SerializeField] private float mechanicSpellCoolDown;
    [SerializeField] private float imaginarySpellCoolDown;
    [SerializeField] private float imaginaryVFXCoolDown;

    // Stores original particle color values for fade-out lerping
    private List<float> originalColorfloat = new();
    private bool IsSpellActivated = false;
    
    // Visual indicator that follows the mouse cursor when spell is ready
    private GameObject vfxVisualVisualGameObject;
    private bool isHoveringOnPotentialPaths = false;
    
    private void Awake()
    {
        Debug.Log("done");
        instance = this;
    }

    private void Start()
    {
        // Register all VFX prefabs with the object pool for efficient instantiation/recycling
        if (flamesPrefab != null)
        {
            ObjectPooling.instance.Register(flamesPrefab, flamesPoolSize);
        }
        if (magicAreaPrefab != null)
        {
            ObjectPooling.instance.Register(magicAreaPrefab, magicAreaPoolSize);
        }
        if (bombPrefab != null)
        {
            ObjectPooling.instance.Register(bombPrefab, bombPoolSize);
        }
        if (imaginaryVFXPrefab != null)
        {
            ObjectPooling.instance.Register(imaginaryVFXPrefab, imaginaryVFXPoolSize);
        }
        if (tinyFlamesVisualPrefab != null)
        {
            ObjectPooling.instance.Register(tinyFlamesVisualPrefab, tinyFlamesVFXPoolSize);
        }
        if (magicVisualPrefab != null)
        {
            ObjectPooling.instance.Register(magicVisualPrefab, magicVFXPoolSize);
        }
        if (tinyFlamesPrefab != null)
        {
            ObjectPooling.instance.Register(tinyFlamesPrefab, 20);
        }
    }

    private void OnEnable()
    {
        // Reset all spell states when component is enabled (e.g., scene reload)
        Debug.Log("123");
        FireSpellActivated = false;
        MagicSpellActivated = false;
        MechanicSpellActivated = false;
        ImaginarySpellActivated = false;    
        IsSpellActivated = false;
        currenSpellType = SpellType.None;
    }

    private void Update()
    {
        #region FireSpell
        // Fire spell: Burns along a selected path, damaging enemies over time
        if (currenSpellType == SpellType.Physical)
        {
            Debug.Log("12");

            // Update visual indicator position to follow mouse
            if (vfxVisualVisualGameObject != null)
            {
                VisualiseSpellsPrerfab(vfxVisualVisualGameObject);
            }
            
            // Activate fire spell on mouse click
            if (Input.GetMouseButtonDown(0))
            { 
                FireSpellActivated = true; 
            }
            
            if (FireSpellActivated)
            {
                FireSpell();
            }
        }
        #endregion

        #region MagicSpell
        // Magic spell: Places up to 3 area damage zones at clicked positions
        if (currenSpellType == SpellType.Magic)
        {
            if (vfxVisualVisualGameObject != null)
            {
                Debug.Log("hi");
                VisualiseSpellsPrerfab(vfxVisualVisualGameObject);
            }
            
            if (Input.GetMouseButtonDown(0))
            { 
                MagicSpellActivated = true; 
            }
            
            if (MagicSpellActivated)
            {
                MagicSpell();
            }
        }
        #endregion

        #region MechanicSpell
        // Mechanic spell: Click an enemy to lift them, then drop as explosive bomb
        if (currenSpellType == SpellType.Mechanic && !stopMechanic)
        {
            MechanicSpellActivated = true;
            
            if (vfxVisualVisualGameObject != null)
            {
                Debug.Log("hi");
                // Pass true for isMechanicSpell to show visual even when not hovering on paths
                VisualiseSpellsPrerfab(vfxVisualVisualGameObject, true);
            }
            
            if (Input.GetMouseButton(0))
            {
                EnableMechanicDamage();
            }
        }
        #endregion

        #region ImaginarySpell
        // Imaginary spell: Reveals all invisible enemies on the battlefield
        if (ImaginarySpellActivated && !stopImaginary)
        {
            IsSpellActivated = true;
            
            // Find all active enemies in the scene
            EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Debug.Log($"[Imaginary Spell] Found {enemies.Length} enemies");

            if (enemies != null)
            {
                if (enemies.Length > 0)
                {
                    foreach (EnemyBase enemy in enemies)
                    {
                        if (enemy != null)
                        {
                            Debug.Log($"[Imaginary Spell] Enemy: {enemy.name} | isInvisible: {enemy.isInvisible}");

                            // Only apply reveal effect to invisible enemies
                            if (enemy.isInvisible)
                            {
                                // Spawn reveal VFX on the enemy
                                GameObject vfx = ObjectPooling.instance.Get(imaginaryVFXPrefab);
                                if (vfx != null)
                                {
                                    SkinnedMeshRenderer[] skinnedMesh = enemy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                                    if (skinnedMesh != null)
                                    {
                                        foreach (SkinnedMeshRenderer o in skinnedMesh)
                                        {
                                            if (o != null)
                                            {
                                                // Parent VFX to enemy so it follows them
                                                vfx.transform.parent = enemy.vfxContainer.gameObject.transform;
                                                // Position randomly within enemy mesh bounds for visual variety
                                                vfx.transform.localPosition = GiveRandomPointOnEnemyMesh(o.localBounds) - enemy.vfxContainer.gameObject.transform.localPosition;
                                                vfx.transform.rotation = Quaternion.identity;
                                                vfx.SetActive(true);
                                            }
                                        }
                                    }
                                }

                                Debug.Log($"[Imaginary Spell] Revealed {enemy.name}");

                                // Start cooldown - parameters are inverted: VFX duration first, then spell cooldown
                                StartCoroutine(CoolDownSpells(imaginaryVFXCoolDown, enemy, vfx, imaginarySpellCoolDown));
                            }
                        }
                    }
                }
            }
            ImaginarySpellActivated = false;
        }
        #endregion
    }

    #region FireSpell

    /// <summary>
    /// Spawns flame VFX along the selected path. Number of flames scales with path size.
    /// </summary>
    public void FireSpell()
    {
        if (selectedPath != null)
        {
            // Return the visual indicator to pool since spell is now being cast
            if (vfxVisualVisualGameObject != null)
            {
                ObjectPooling.instance.Return(vfxVisualVisualGameObject);
            }

            IsSpellActivated = true;
            if (!stopFire)
            {
                // Calculate flame count based on path bounds magnitude (larger paths = more flames)
                // Multiplied by 7 to ensure good coverage
                for (int i = 0; i <= (int)selectedPath.FlameArea.bounds.size.magnitude * 7; i++)
                {
                    GameObject flame = ObjectPooling.instance.Get(flamesPrefab);
                    if (flame != null)
                    {
                        // Position flame randomly within path bounds
                        flame.transform.position = GiveRandomPointOnMesh(selectedPath.FlameArea.bounds);
                        flame.transform.rotation = Quaternion.identity;
                        flame.SetActive(true);
                        flames.Add(flame);
                    }

                    // Once all flames are placed, start cooldown
                    if (i >= ((int)selectedPath.FlameArea.bounds.size.magnitude) - 1)
                    {
                        Debug.Log("Here");
                        stopFire = true;
                        CanSelectPaths = false;
                        StartCoroutine(selectedPath.ChangePathMatToOriginalColor());
                        StartCoroutine(CoolDownSpells(fireSpellCoolDown));
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns a random point within enemy mesh bounds for VFX positioning.
    /// Biased towards upper portion of enemy for better visual effect.
    /// </summary>
    private Vector3 GiveRandomPointOnEnemyMesh(Bounds bounds)
    {
        return new Vector3(
            bounds.center.x, 
            Random.Range(bounds.extents.y, bounds.extents.y / 3f),  // Upper third of mesh
            Random.Range(-bounds.extents.z / 10f, bounds.extents.z / 8f)  // Slight z variation
        );
    }

    /// <summary>
    /// Returns a random point along the path bounds for flame positioning.
    /// Direction depends on path orientation (X-axis vs Z-axis aligned).
    /// </summary>
    private Vector3 GiveRandomPointOnMesh(Bounds bounds)
    {
        if (selectedPath.isOnAxisProperty)
        {
            // Path runs along X-axis: randomize X position, keep Y and Z at center
            return new Vector3(
                Random.Range(bounds.min.x + 0.1f, bounds.max.x + 0.1f), 
                bounds.center.y, 
                bounds.center.z
            );
        }
        // Path runs along Z-axis: randomize Z position, keep X and Y at center
        return new Vector3(
            bounds.center.x, 
            bounds.center.y, 
            Random.Range(bounds.min.z + 0.1f, bounds.max.z + 0.1f)
        );
    }

    #endregion

    #region MagicSpell

    /// <summary>
    /// Places magic area damage zones at clicked positions. Player can place up to 3 zones
    /// before the spell completes and goes on cooldown.
    /// </summary>
    public void MagicSpell()
    {
        if (selectedPath != null)
        {
            if (!stopMagic)
            {
                IsSpellActivated = true;
                
                for (int i = 0; i <= 3; i++)
                {
                    // Only place if this position hasn't been used and is valid
                    if (!mousePositionDictionary.ContainsKey(currentMousePosition) && currentMousePosition != Vector3.zero)
                    {
                        magicAreas = ObjectPooling.instance.Get(magicAreaPrefab);
                        if (magicAreas != null)
                        {
                            // Track this position to prevent duplicate placements
                            mousePositionDictionary.Add(currentMousePosition, magicAreas.transform.position);
                            magicAreas.transform.localPosition = currentMousePosition;
                            magicAreas.transform.rotation = Quaternion.Euler(-90, 0, 0);  // Lay flat on ground
                            magicAreas.SetActive(true);
                            CanSelectPaths = true;
                        }
                    }
                    
                    // Reset path selection if only one area placed (allow more clicks)
                    if (mousePositionDictionary.Count <= 1)
                    {
                        Debug.Log("PathNull");
                        selectedPath = null;
                    }
                    
                    // After 3 areas placed, complete the spell
                    if (i >= 2)
                    {
                        if (mousePositionDictionary.Count > i)
                        {
                            Debug.Log("Here");
                            stopMagic = true;
                            CanSelectPaths = false;

                            StartCoroutine(selectedPath.ChangePathMatToOriginalColor());
                            if (vfxVisualVisualGameObject != null)
                            {
                                ObjectPooling.instance.Return(vfxVisualVisualGameObject);
                            }                            

                            StartCoroutine(CoolDownSpells(magicSpellCoolDown));
                            mousePositionDictionary.Clear();
                            break;
                        }
                    }
                }
            }
        }
    }

    #endregion

    #region MechanicSpell

    /// <summary>
    /// Raycast from mouse to find and select an enemy for the mechanic (bomb) spell.
    /// </summary>
    private void EnableMechanicDamage()
    {
        if (MechanicSpellActivated)
        {
            IsSpellActivated = true;
            if (vfxVisualVisualGameObject != null)
            {
                ObjectPooling.instance.Return(vfxVisualVisualGameObject);
            }

            // Raycast from camera through mouse position
            Vector2 mousepos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousepos);
            RaycastHit hit;

            // Note: Using single & instead of && - both conditions evaluated regardless
            if (Physics.Raycast(ray, out hit, 400f) & hit.collider != null)
            {
                // Check if we hit an enemy
                if (hit.collider.gameObject.GetComponent<EnemyBase>() != null)
                {
                    enemyBaseGameObjectRef = hit.collider.gameObject.GetComponent<EnemyBase>();
                    
                    // Can only target visible enemies
                    if (!enemyBaseGameObjectRef.isInvisible)
                    {
                        // Visual feedback: tint enemy green to show selection
                        if (enemyBaseGameObjectRef.gameObject.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                        {
                            enemyBaseGameObjectRef.gameObject.GetComponentInChildren<SkinnedMeshRenderer>().material.color = new(0, 1, 0, 0.5f);
                        }
                        stopMechanic = true;

                        StartCoroutine(CoolDownSpells(mechanicSpellCoolDown));
                        // Lift the enemy (false = not explosive yet, just lifting)
                        StartCoroutine(LiftSelectedEnemy(enemyBaseGameObjectRef, false));
                        print($"<color=red> mouseWorldpos </color>" + hit.point);
                        MechanicSpellActivated = false;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Coroutine that lifts an enemy into the air. If isExplosiveDamage is false, waits for
    /// player to select a drop location. If true, moves enemy to target and triggers explosion.
    /// </summary>
    /// <param name="enemyBase">The enemy to lift</param>
    /// <param name="isExplosiveDamage">True if moving to explosion point, false if initial lift</param>
    private IEnumerator LiftSelectedEnemy(EnemyBase enemyBase, bool isExplosiveDamage)
    {
        if (enemyBase.gameObject != null && enemyBase.gameObject.activeInHierarchy && enemyBaseGameObjectRef != null)
        {
            // Enable lift visual effects on the enemy
            enemyBase.LiftEffectFunction(true, true, enemyBase);

            Vector3 startPos = enemyBase.transform.position;
            Vector3 targetPos = Vector3.zero;
            
            if (!isExplosiveDamage)
            {
                // Initial lift: raise enemy 2 units above current position
                targetPos = new Vector3(startPos.x, enemyBase.transform.position.y + 2, startPos.z);
            }
            else
            {
                // Explosive phase: move enemy to clicked position (drop zone)
                targetPos = currentMousePosition;
            }

            // Smooth interpolation to target position
            float duration = 1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime * 2.5f;  // 2.5x speed multiplier
                float t = elapsed / duration;
                enemyBase.transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
            enemyBase.transform.position = targetPos;

            if (isExplosiveDamage)
            {
                // Enemy has reached drop zone - trigger explosion
                CanSelectPaths = false;
                selectedPath = null;
                enemyBase.myBody.isKinematic = true;  // Freeze physics
                EnableExplosiveDamage(enemyBase);
                yield break;
            }
            else
            {
                // Initial lift complete - wait up to 5 seconds for player to select drop location
                CanSelectPaths = true;
                elapsed = 0f;
                duration = 5f;

                while (elapsed < duration)
                {
                    // If player clicked a path, move to explosive phase
                    if (selectedPath != null)
                    {
                        EnableExplosiveDamage(enemyBase);
                        yield break;
                    }

                    elapsed += Time.deltaTime;
                    yield return null;
                }

                // Timeout: player didn't select a drop location, release the enemy
                if (selectedPath == null)
                {
                    CanSelectPaths = false;

                    // Restore original enemy colors
                    foreach (Color o in enemyBase.originalColors)
                    {
                        enemyBase.gameObject.GetComponentInChildren<SkinnedMeshRenderer>().material.color = o;
                    }
                    enemyBase.isInvisible = false;
                    enemyBase.UpdateVisuals();
                    
                    // Disable lift effects and return enemy to normal state
                    enemyBase.LiftEffectFunction(false, false, enemyBase);
                }
            }
        }
    }

    /// <summary>
    /// Handles the explosion when a lifted enemy is dropped. Either redirects to path
    /// or triggers area damage at drop location.
    /// </summary>
    private void EnableExplosiveDamage(EnemyBase enemyBase)
    {
        if (selectedPath != null)
        {
            // Path selected: move enemy to explosion point
            StartCoroutine(selectedPath.ChangePathMatToOriginalColor());
            StartCoroutine(LiftSelectedEnemy(enemyBase, true));
        }
        else
        {
            // No path: explode at current position
            GameObject explosionBomb = ObjectPooling.instance.Get(bombPrefab);
            if (explosionBomb != null)
            {
                explosionBomb.transform.position = currentMousePosition;
                explosionBomb.transform.rotation = Quaternion.identity;
                explosionBomb.SetActive(true);

                // Find all colliders within blast radius using non-allocating sphere check
                // This avoids creating garbage each frame unlike Physics.OverlapSphere
                int i = Physics.OverlapSphereNonAlloc(currentMousePosition, 5, colliders);
                
                // Process each collider within blast radius
                for (int j = 0; j < i; j++)
                {
                    GameObject affectedEnemy = colliders[j].gameObject;
                    if (affectedEnemy != null)
                    {
                        EnemyBase affectedEnemyBaseRef = affectedEnemy.GetComponent<EnemyBase>();
                        if (affectedEnemyBaseRef != null)
                        {
                            // Kill the bomb enemy and explode nearby enemies
                            enemyBase.Die();
                            StartCoroutine(affectedEnemyBaseRef.ExplodeEnemy(currentMousePosition, affectedEnemyBaseRef));
                            selectedPath = null;
                        }
                    }
                }

                // Return bomb VFX to pool after explosion animation completes
                StartCoroutine(ReturnToPoolAfterDelay(explosionBomb, 2f));
            }
        }
    }

    /// <summary>
    /// Helper coroutine to return pooled objects after a delay.
    /// </summary>
    private IEnumerator ReturnToPoolAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ObjectPooling.instance.Return(obj);
    }

    #endregion

    #region VisualIndicationOfSpells

    /// <summary>
    /// Updates the spell visual indicator to follow the mouse cursor.
    /// Only visible when hovering over valid paths (or always for mechanic spell).
    /// </summary>
    /// <param name="Prefab">The visual indicator prefab to position</param>
    /// <param name="isMechanicSpell">If true, show indicator even without path hover</param>
    public void VisualiseSpellsPrerfab(GameObject Prefab, bool isMechanicSpell = false)
    {
        if (isHoveringOnPotentialPaths || isMechanicSpell)
        {
            // Raycast to find world position under mouse
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            Vector3 mouseWorldPos = Vector3.zero;
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                mouseWorldPos = hit.point;
            }
            
            Prefab.transform.position = mouseWorldPos;
            Prefab.transform.rotation = Quaternion.identity;
            Prefab.SetActive(true);
        }
        else
        {
            // Hide indicator when not hovering over valid targets
            Prefab.SetActive(false);
        }
    }
    #endregion

    #region ActivateSpells
    
    /// <summary>
    /// Called by UI button to activate Fire spell mode.
    /// Resets states and spawns visual indicator.
    /// </summary>
    public void ActivateFireSpell()
    {
        IsSpellActivated = false;
        FireSpellActivated = false;
        if (!FireSpellActivated && !IsSpellActivated)
        {
            Debug.Log("12345");

            CanSelectPaths = true;
            currenSpellType = SpellType.Physical;
            stopFire = false;
            
            // Return existing indicator to pool before getting new one
            if (vfxVisualVisualGameObject != null)
            {
                ObjectPooling.instance.Return(vfxVisualVisualGameObject);
            }
            
            for (int i = 0; i < 1; i++)
            {
                vfxVisualVisualGameObject = ObjectPooling.instance.Get(tinyFlamesVisualPrefab);
            }
        }
    }

    /// <summary>
    /// Called by UI button to activate Magic spell mode.
    /// </summary>
    public void ActivateMagicSpell()
    {
        IsSpellActivated = false;
        MagicSpellActivated = false;
        print("$<color=green> Magic Spell Activated</color>");
        if (!MagicSpellActivated && !IsSpellActivated)
        {
            print("$<color=red> Magic Spell Activated</color>");
            currenSpellType = SpellType.Magic;
            CanSelectPaths = true;
            stopMagic = false;

            if (vfxVisualVisualGameObject != null)
            {
                ObjectPooling.instance.Return(vfxVisualVisualGameObject);
            }
            
            for (int i = 0; i < 1; i++)
            {
                vfxVisualVisualGameObject = ObjectPooling.instance.Get(magicVisualPrefab);
            }
        }
    }

    /// <summary>
    /// Called by UI button to activate Mechanic (bomb) spell mode.
    /// </summary>
    public void ActivateMechanicSpell()
    {
        IsSpellActivated = false;
        MechanicSpellActivated = false;
        stopMechanic = false;
        if (!MechanicSpellActivated && !stopMechanic && !IsSpellActivated)
        {
            currenSpellType = SpellType.Mechanic;

            if (vfxVisualVisualGameObject != null)
            {
                CanSelectPaths = false;
                if (selectedPath != null)
                {
                    // Reset path highlighting since mechanic spell doesn't use paths initially
                    StartCoroutine(selectedPath.ChangePathMatToOriginalColor());
                }
                ObjectPooling.instance.Return(vfxVisualVisualGameObject);
            }
            
            for (int i = 0; i < 1; i++)
            {
                vfxVisualVisualGameObject = ObjectPooling.instance.Get(mechanicVisualPrefab);
            }
        }
    }

    /// <summary>
    /// Called by UI button to activate Imaginary (reveal) spell.
    /// This spell activates immediately without requiring mouse input.
    /// </summary>
    public void ActivateImaginarySpell()
    {
        IsSpellActivated = false;
        ImaginarySpellActivated = false;
        if (!ImaginarySpellActivated && !IsSpellActivated)
        {
            currenSpellType = SpellType.Imaginary;
            ImaginarySpellActivated = true;  // Triggers immediately in Update
        }
    }

    #endregion

    #region DeactivateSpells
    
    /// <summary>
    /// Deactivates all active spells. Called when cancelling spell or switching modes.
    /// </summary>
    public void DeactivateSpells()
    {
        if (IsSpellActivated)
        {
            FireSpellActivated = false;
            MagicSpellActivated = false;
            MechanicSpellActivated = false;
            ImaginarySpellActivated = false;
            IsSpellActivated = false;   
        }
    }
    #endregion

    #region SelectedPath

    /// <summary>
    /// Called by SelectedPath component when player clicks on a valid path.
    /// Stores the path reference and click position for spell targeting.
    /// </summary>
    public void SelectedPathFromPlayer(SelectedPath path, Vector3 mousePosition)
    {
        selectedPath = path;
        currentMousePosition = mousePosition;
        CanSelectPaths = false;
    }

    /// <summary>
    /// Called by SelectedPath component to track hover state for visual indicators.
    /// </summary>
    public void IsHoveringOnPotentialPaths(bool status)
    {
        isHoveringOnPotentialPaths = status;
    }

    #endregion

    #region DisableVFX

    /// <summary>
    /// Master cooldown coroutine that handles cleanup and cooldown for all spell types.
    /// Behavior varies based on which spell is currently active.
    /// </summary>
    /// <param name="WaitTime">Primary cooldown duration</param>
    /// <param name="enemy">Optional enemy reference for Imaginary spell</param>
    /// <param name="vfx">Optional VFX reference for Imaginary spell cleanup</param>
    /// <param name="optionalTime">Secondary cooldown for Imaginary spell</param>
    private IEnumerator CoolDownSpells(float WaitTime, EnemyBase enemy = null, GameObject vfx = null, float optionalTime = 0)
    {
        // ===== FIRE SPELL COOLDOWN =====
        if (FireSpellActivated)
        {
            selectedPath = null;
            if (currenSpellType == SpellType.Physical)
            {
                yield return new WaitForSeconds(WaitTime);
            }
            
            // Return flame VFX to pool one by one with small delay for visual effect
            foreach (GameObject o in flames)
            {
                yield return new WaitForSeconds(0.2f);
                ObjectPooling.instance.Return(o);
            }
            flames.Clear();
        }
        
        // ===== MAGIC SPELL COOLDOWN =====
        if (MagicSpellActivated)
        {
            if (currenSpellType == SpellType.Magic)
            {
                yield return new WaitForSeconds(WaitTime);
            }
            selectedPath = null;
            currentMousePosition = Vector3.zero;
            stopMagic = false;
        }
        
        // ===== MECHANIC SPELL COOLDOWN =====
        if (MechanicSpellActivated)
        {
            if (currenSpellType == SpellType.Mechanic)
            {
                yield return new WaitForSeconds(WaitTime);
            }
            selectedPath = null;
        }

        // ===== IMAGINARY SPELL COOLDOWN =====
        // More complex: fades out reveal VFX before returning to pool
        if (ImaginarySpellActivated)
        {
            if (currenSpellType == SpellType.Imaginary)
            {
                yield return new WaitForSeconds(WaitTime);
            }
            selectedPath = null;
            
            // Make enemy visible again
            enemy.isInvisible = false;
            enemy.UpdateVisuals();

            // Fade out the reveal VFX particles over time
            float elapsed = 0;
            float duration = 5;
            List<ParticleSystem> ps = vfx.GetComponentsInChildren<ParticleSystem>().ToList();
            List<ParticleSystemRenderer> psRenderer = vfx.GetComponentsInChildren<ParticleSystemRenderer>().ToList();

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed * 2 / duration;  // 2x speed for faster fade

                // Lerp particle color intensity from 1 to 0
                for (int i = 0; i < ps.Count; i++)
                {
                    var emission = ps[i].emission;
                    emission.enabled = false;  // Stop emitting new particles
                    
                    originalColorfloat.Add(psRenderer[i].material.GetFloat("_AllColorFactor"));
                    for (int j = 0; j < originalColorfloat.Count; j++)
                    {
                        originalColorfloat[j] = Mathf.Lerp(1, 0, t);
                        psRenderer[i].material.SetFloat("_AllColorFactor", originalColorfloat[j]);
                    }

                    yield return null;
                }
            }
            
            // Cleanup after fade completes
            if (elapsed >= duration)
            {
                originalColorfloat.Clear();
                ps.Clear();
                psRenderer.Clear();

                ObjectPooling.instance.Return(vfx);
                yield return new WaitForSeconds(optionalTime);
                stopImaginary = false;
            }
        }
    }

    #endregion

    #region Getters

    public bool FireSpellActivated { get; set; }
    public bool MagicSpellActivated { get; set; }
    public bool MechanicSpellActivated { get; set; }
    public bool ImaginarySpellActivated { get; set; }
    public bool CanSelectPaths { get; internal set; }
    public SpellType currenSpellType { get; internal set; }

    public float GetFireSpellCoolDown() => fireSpellCoolDown;
    public float GetMagicSpellCoolDown() => magicSpellCoolDown;
    public float GetMechanicSpellCoolDown() => mechanicSpellCoolDown;
    public float GetImaginarySpellCoolDown() => imaginarySpellCoolDown;
    public float GetImaginaryVFXCoolDown() => imaginaryVFXCoolDown;
    
    #endregion
}