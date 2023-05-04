using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class terrainDeformScript : MonoBehaviour
{

    public Terrain t;
    public TerrainData td;
    private bool useProjectile = false, craterModeEnabled = false;
    public int craterSize;
    public float craterVariance, craterDepth, blastRange, blastForce, explosionOffset, projectileSpeed;
    public GameObject explosion, deadReplace, projectile;
    public List<GameObject> deadTrees = new List<GameObject>(), ringStore = new List<GameObject>();
    public uiManagerScript uims;
    public Slider sizeSlider, depthSlider;
    public GameObject circlePreview;

    // Start is called before the first frame update
    void Start()
    {
        uims = GameObject.FindObjectOfType<uiManagerScript>();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !uims.menuOpen)//PUT CHECKS FOR MENUS!
        {
            raycastClick();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            craterModeEnabled = !craterModeEnabled;
        }
        //raycastHover();
    }

    public void updateCraterSize()
    {
        craterSize = (int)sizeSlider.value;
    }
    public void updateCraterDepth()
    {
        craterDepth = depthSlider.value;
    }




    public void cycleProjectileUse()
    {
        useProjectile = !useProjectile;
    }

    void createProjectile(Vector2 center)
    {
        GameObject tempP = Instantiate(projectile, new Vector3(center.x, 700, center.y), Quaternion.identity);
        tempP.GetComponent<projectileScript>().center = center;
        tempP.transform.localScale = Vector3.one * 4;
        tempP.GetComponent<Rigidbody>().velocity = Vector3.down * projectileSpeed;
    }

    public void generateCrater(Vector2 center)
    {
        
        TerrainCollider collider = t.GetComponent<TerrainCollider>();
        Bounds bounds = collider.bounds;
        int SStoUU = (int)(td.heightmapResolution / collider.bounds.size.x);
        Debug.Log(SStoUU);
        center = center * SStoUU;
        craterSize *= SStoUU;
        float[,] heights = new float[td.heightmapResolution, td.heightmapResolution];
        heights = td.GetHeights(0, 0, td.heightmapResolution, td.heightmapResolution);
        float initialHeight = heights[(int)(center.x), (int)(center.y)];
        ringStore.Add(Instantiate(explosion, new Vector3(center.y, heights[(int)center.x, (int)center.y]*200 + explosionOffset, center.x), Quaternion.identity));//this is for particle effect cleanup
        Invoke("ringUpkeep", 5.0f);//calls the ringUpkeep function in five seconds to clean up particles that are left in the scene

        for (int i=-craterSize; i<=craterSize; i++)
        {
            for(int j = -craterSize; j <= craterSize; j++)
            {
                float dist = Mathf.Abs(Vector2.Distance(Vector2.zero, new Vector2(i, j)));
                if ( dist <= craterSize)//if the distance between the center of the circle and a surrounding sample point is less than the max crater width, let's depress that point
                {
                    if((int)center.x + i<td.heightmapResolution && (int)center.y + j<td.heightmapResolution)
                        heights[(int)center.x + i, (int)center.y + j] -= (craterDepth/15f) * (Mathf.Cos(Mathf.PI + ((1-(dist / craterSize))*Mathf.PI)) + 1);//craterDepth /(Mathf.Abs(dist)+2);
                    //we access the point that we would like to modify, and reduce it's value by the crater depth multiplied by some trigonometric math in order to have the depth 
                    //correlate with the distance from the center. Using Cos, we're able to create a smooth, natural looking depression in the terrain
                }
            }
        }
        td.SetHeights(0, 0, heights);//after we've modified the heights array, we now set all the heights on the terrain equal to the heights array

        craterSize /= SStoUU;
        

        ArrayList instances = new ArrayList();//contains tree instances

        foreach (TreeInstance tree in td.treeInstances)//for each tree in the terrain
        {
            float distance = Vector3.Distance(Vector3.Scale(tree.position, td.size*SStoUU) + t.transform.position, new Vector3(center.y, heights[(int)center.x, (int)center.y], center.x));
            if (distance < blastRange)//if the tree is within the blast range
            {
                // the tree is in range - destroy it
                GameObject dead = Instantiate(deadReplace, Vector3.Scale(tree.position, td.size*SStoUU) + t.transform.position, Quaternion.identity) as GameObject;//instantiate a new physics-enabled tree analog
                deadTrees.Add(dead);//add the tree to a list that contains all the dead trees
                Invoke("treeUpkeep", 5.0f);//call the upkeep function that will cleanup the trees after a set amount of time
                dead.GetComponent<Rigidbody>().maxAngularVelocity = 1;
                dead.GetComponent<Rigidbody>().AddExplosionForce(blastForce, new Vector3(center.y, heights[(int)center.x, (int)center.y], center.x), blastRange, 0.0f);
            }
            else
            {
                // tree is out of range - keep it
                instances.Add(tree);

            }
        }
        td.treeInstances = (TreeInstance[])instances.ToArray(typeof(TreeInstance));//we now set all the trees back up in the terrain, minus the ones that were destroyed

        

    }



    void raycastClick()
    {
        return;//DISABLED
        /*
        TerrainCollider collider = t.GetComponent<TerrainCollider>();       
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit = new RaycastHit();


        if (collider.Raycast(ray, out hit, Mathf.Infinity))//if we hit the terrain
        {
            if (useProjectile)//if the UI option to use the projectile is enabled
            {
                createProjectile(new Vector2(hit.point.x, hit.point.z));
            }
            else
                generateCrater(new Vector2(hit.point.z, hit.point.x));
           }
        */
    }

    void raycastHover()
    {
        //TerrainCollider collider = t.GetComponent<TerrainCollider>();
        //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //RaycastHit hit = new RaycastHit();
        Camera cam = Camera.main;
        Ray r = cam.ScreenPointToRay(Input.mousePosition);

        // Get the mouse position from Event.
        // Note that the y position from Event is inverted.
        RaycastHit hit;
        if (Physics.Raycast(r, out hit, Mathf.Infinity)){
            circlePreview.transform.position = hit.point;

        }

        // Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //if (collider.Raycast(ray, out hit, Mathf.Infinity))//if we hit the terrain
        {
            //circlePreview.transform.position = hit.point + Vector3.up;
        }
    }

    void treeUpkeep()
    {
        GameObject temp = deadTrees[0];
        deadTrees.RemoveAt(0);
        Destroy(temp);

    }

    void ringUpkeep()
    {
        GameObject temp = ringStore[0];
        ringStore.RemoveAt(0);
        Destroy(temp);

    }
}
