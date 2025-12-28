using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellAbility : MonoBehaviour
{
    private SelectedPath selectedPath;
    private List<GameObject> flames = new();
    private GameObject magicAreas;
    public static SpellAbility instance;
    private bool stopFire = false;
    private bool stopMagic = false;
    private Vector3 currentMousePosition;
    private Dictionary<Vector3, Vector3> mousePositionDictionary = new();

    public enum SpellType {Physical,Magic,Mechanic,Imaginary}
   

    private void Awake()
    {
        instance = this;
    }
    private void OnEnable()
    {
        FireSpellActivated = false;
        MiracleSpellActivated = false;
    }

    private void Update()
    {
     
       

        if(Input.GetKeyDown(KeyCode.A))
        {
            FireSpellActivated = false;//for testing only
            MiracleSpellActivated = false;
            CanSelectPaths = false;
        }
        if (Input.GetKeyDown(KeyCode.S))
        {

            FireSpellActivated = true;//Eren has to enable this spell and this variable (bool) in skill tree //for testing only
            currenSpellType = SpellType.Physical;
            stopFire = false;

        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            currenSpellType = SpellType.Magic;
            MiracleSpellActivated = true;
            stopMagic = false;
        }

        if(Input.GetKeyDown(KeyCode.D))
        {
            currenSpellType = SpellType.Mechanic;
            MechanicSpellActivated = true;

        }

        #region FireSpell


        if (FireSpellActivated)
        {
            CanSelectPaths = true;
            FireSpell();

        }
        else
        {
            if (currenSpellType == SpellType.Physical)
            {
                selectedPath = null;
            }
        }
        #endregion

        #region MiracleSpell
        

        if(MiracleSpellActivated)
        {
           
            CanSelectPaths = true;
            MiracleSpell();
        }
        #endregion

        if(MechanicSpellActivated)
        {

            CanSelectPaths= true;

        }
    }

    #region FireSpell

    public void FireSpell()
    {

        if (selectedPath != null)
        {
            
            if (!stopFire)
            {
                for (int i = 0; i <= (int)selectedPath.FlameArea.bounds.size.magnitude * 3; i++)
                {


                    flames.Add(ObjectPooling.instance.GetPoolObject(PoolGameObjectType.Flames));                 
                    if (flames.Count > 0 && flames[i] != null)
                    {

                        flames[i].SetActive(true);
                        flames[i].transform.position = GiveRandomPointOnMesh(selectedPath.FlameArea.bounds);
                        flames[i].transform.rotation = Quaternion.identity;



                    }

                    if (i >= ((int)selectedPath.FlameArea.bounds.size.magnitude) - 1)
                    {
                        Debug.Log("Here");
                        stopFire = true;
                        StartCoroutine(DisableVFX(5f));
                        break;
                    }
                }
            }
        }
    }

    #endregion

    #region MiracleSpell

    public void MiracleSpell()
    {
        
        if (selectedPath != null)
        {
            
            if (!stopMagic)
            {
              
                for (int i = 0; i <= 3; i++)
                {
                                         
                    if (!mousePositionDictionary.ContainsKey(currentMousePosition) && currentMousePosition != Vector3.zero)
                    {
                        magicAreas = ObjectPooling.instance.GetPoolObject(PoolGameObjectType.MagicArea);
                        if (magicAreas != null)
                        {
                            
                            mousePositionDictionary.Add(currentMousePosition, magicAreas.transform.position);
                            magicAreas.transform.localPosition = currentMousePosition;
                            magicAreas.transform.rotation = Quaternion.Euler(-90, 0, 0);
                            magicAreas.SetActive(true);
                            selectedPath = null;
                           
                        
                        }


                    }

                    if (i >= 2)
                    {
                        if (mousePositionDictionary.Count > i)
                        {
                            Debug.Log("Here");
                            stopMagic = true;
                            StartCoroutine(DisableVFX(5));
                            CanSelectPaths = true;
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





    #endregion




    private Vector3 GiveRandomPointOnMesh(Bounds bounds)
    {           
        return new Vector3(bounds.center.x, bounds.center.y, Random.Range(bounds.min.z, bounds.max.z));
    }

    public void SelectedPathFromPlayer(SelectedPath path,Vector3 mousePosition)
    {
        selectedPath = path;       
        currentMousePosition = mousePosition;
        CanSelectPaths = false;
    }
    #region DisableVFX
    private IEnumerator DisableVFX(float WaitTime)
    {
       
        yield return new WaitForSeconds(WaitTime);
        switch(currenSpellType)
        {
            case SpellType.Physical:
            foreach (GameObject o in flames)
            {
                yield return new WaitForSeconds(0.3f);//for disbaling fire in a gradual way
                ObjectPooling.instance.ReturnGameObejctToPool(PoolGameObjectType.Flames, o);
            }
            flames.Clear();
            break;
            case SpellType.Magic:                
               
                currentMousePosition = Vector3.zero;
                stopMagic = false;
                break;
        }
       
    }
    #endregion


    public bool FireSpellActivated {  get; set; }

    public bool MiracleSpellActivated { get; set; }

    public bool MechanicSpellActivated { get; set; }

    public bool CanSelectPaths { get; internal set; }

    public SpellType currenSpellType { get; internal set; }
}
