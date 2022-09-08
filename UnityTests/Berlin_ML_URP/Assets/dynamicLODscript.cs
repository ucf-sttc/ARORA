using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class dynamicLODscript : MonoBehaviour
{
    public List<Terrain> terrains = new List<Terrain>();
   // public float minDist, maxDist, lowestTerrainRes;
    private Vector3 lastPos=Vector3.zero;
    public float updateDistThold, updateTimeThold;
    public int minimumPixelError;
    public int buildingSpawnPE;
    public GameObject buildingParent;
    private Camera mc;
    // Start is called before the first frame update
    void Start()
    {
        mc = Camera.main;
        
        terrains.AddRange(GameObject.FindObjectsOfType<Terrain>());
        StartCoroutine("dynamicLOD");
    }



    IEnumerator dynamicLOD()
    {
        while (true)
        {
            if(Mathf.Abs(Vector3.Distance(mc.transform.position, lastPos)) > updateDistThold)//if the camera has moved enough since we last checked
            {
                lastPos = mc.transform.position;
                for(int i = 0; i < terrains.Count; i++)
                {
                    float calcVal = (Mathf.Pow(((int)(Mathf.Abs(Vector3.Distance(lastPos, terrains[i].transform.position)))) / 100,2));
                    calcVal = calcVal < 10 ? 1 : calcVal;

                    calcVal = calcVal * calcVal;
                    //calcVal = calcVal < 10 ? 1 : calcVal;
                    if(buildingParent != null)
                    {
                        if (calcVal <= buildingSpawnPE)//turn on the buildings on this terrain
                        {
                            if (buildingParent.transform.Find("building" + terrains[i].gameObject.name.Substring(7))!=null)
                            {
                                buildingParent.transform.Find("building" + terrains[i].gameObject.name.Substring(7)).gameObject.SetActive(true);
                            }

                        }
                        else//shut off the buildings on this terrain
                        {
                            if (buildingParent.transform.Find("building" + terrains[i].gameObject.name.Substring(7))!=null)
                            {
                                buildingParent.transform.Find("building" + terrains[i].gameObject.name.Substring(7)).gameObject.SetActive(false);
                            }
                        }

                    }
                    

                    terrains[i].heightmapPixelError = calcVal<minimumPixelError? minimumPixelError:calcVal;
                    if (i == 0)
                    {
                        //Debug.Log("Terrain #" + i + " HPE = " + terrains[i].heightmapPixelError+ " " +calcVal);
                        //Debug.Log("Terrain #" + i + " Distance = " + (Mathf.Abs(Vector3.Distance(lastPos, terrains[i].transform.position))));

                    }
                    if (i % 20  == 0)
                        yield return null;

                }

            }

            yield return new WaitForSeconds(updateTimeThold);

        }


    }
}
