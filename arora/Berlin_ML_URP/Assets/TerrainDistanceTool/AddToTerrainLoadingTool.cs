using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddToTerrainLoadingTool : MonoBehaviour
{
    private void Awake()
    {
        FindObjectOfType<TerrainLoadingTool>().SplitStaticObjects(gameObject, true);
    }
}
