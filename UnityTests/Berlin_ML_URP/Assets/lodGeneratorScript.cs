using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityMeshSimplifier;

public class lodGeneratorScript : MonoBehaviour
{
    public GameObject objectParent;
    LodGenerationSettings[] settings;
   

    [ContextMenu("Generate LODs")]
    public void generateLODs()
    {
        settings = GetComponentsInChildren<LodGenerationSettings>();
        for (int i=0;i<objectParent.transform.childCount;i++)
        {
            Transform t = objectParent.transform.GetChild(i);
            LODGeneratorHelper localHelp = SelectGeneratorHelper(t.gameObject);
            if (t.gameObject.GetComponent<LODGroup>() == null)
            {
                if(t.gameObject.GetComponent<LODGeneratorHelper>()==null)
                    t.gameObject.AddComponent<LODGeneratorHelper>();
                LODGeneratorHelper lodhelp = t.gameObject.GetComponent<LODGeneratorHelper>();
                lodhelp.FadeMode = localHelp.FadeMode;
                lodhelp.Levels = localHelp.Levels;
                lodhelp.SimplificationOptions = localHelp.SimplificationOptions;
                lodhelp.AutoCollectRenderers = localHelp.AutoCollectRenderers;
                lodhelp.AnimateCrossFading = localHelp.AnimateCrossFading;
                LODGenerator.GenerateLODs(lodhelp);
            }            
        }
        Debug.Log("Done adding LODs");


    }

    [ContextMenu("Remove LODs")]
    public void removeLODs()
    {
        if (objectParent == null)
        {
            Debug.Log("Please specify object parent");
            return;
        }
        foreach (Transform t in objectParent.GetComponentsInChildren<Transform>())
        {
            if (t!=null && t.gameObject.GetComponent<LODGeneratorHelper>() != null)
            {
                LODGenerator.DestroyLODs(t.gameObject.GetComponent<LODGeneratorHelper>());
            }
        }
        Debug.Log("Done removing LODs");

    }

    LODGeneratorHelper SelectGeneratorHelper(GameObject target)
    {
        int selectedSettingIndex = 0;
        int targetTriangles = 0;
        foreach (MeshFilter filter in target.GetComponentsInChildren<MeshFilter>())
            targetTriangles = filter.mesh.triangles.Length;

        while (targetTriangles > settings[selectedSettingIndex].numberOfTriangles && selectedSettingIndex != settings.Length - 1)
            selectedSettingIndex++;
        return settings[selectedSettingIndex].LodGeneratorHelper;
    }
}
