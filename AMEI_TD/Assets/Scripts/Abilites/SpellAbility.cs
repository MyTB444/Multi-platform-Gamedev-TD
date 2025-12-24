using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellAbility : MonoBehaviour
{
    private SelectedPath selectedPath;
    private List<GameObject> flames = new();
    public static SpellAbility instance;
    private bool stopFire = false;
    [SerializeField] private float coolDownTimer;
   
  


    private void Awake()
    {
        instance = this;
    }
    private void OnEnable()
    {
        FireSpellActivated = false;
    }

    private void Update()
    {
     
        if(Input.GetKeyDown(KeyCode.S))
        {
          
           FireSpellActivated = true;//Eren has to enable this spell and this variable (bool) in skill tree //for testing only
            stopFire = false;
            
        }

        if(Input.GetKeyDown(KeyCode.A))
        {
            FireSpellActivated = false;//for testing only
        }
       

        if (FireSpellActivated)
        {
            CanSelectPaths = true;

            if (selectedPath != null)
            {
                Debug.Log((int)selectedPath.FlameArea.bounds.extents.magnitude);
                if (!stopFire)
                {
                    for (int i = 0; i <= (int)selectedPath.FlameArea.bounds.size.magnitude*3; i++)
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
                            StartCoroutine(DisableFire());
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            selectedPath = null;
        }
    }
    private Vector3 GiveRandomPointOnMesh(Bounds bounds)
    {           
        return new Vector3(bounds.center.x, bounds.center.y, Random.Range(bounds.min.z, bounds.max.z));
    }

    public void SelectedPathFromPlayer(SelectedPath path)
    {
        selectedPath = path;
        CanSelectPaths = false;
    }

    private IEnumerator DisableFire()
    {
        yield return new WaitForSeconds(10f);
        foreach (GameObject o in flames)
        {
            yield return new WaitForSeconds(0.3f);
            ObjectPooling.instance.ReturnGameObejctToPool(PoolGameObjectType.Flames ,o);
        }
    }



    public bool FireSpellActivated {  get; set; }
    public bool CanSelectPaths { get; internal set; }
}
