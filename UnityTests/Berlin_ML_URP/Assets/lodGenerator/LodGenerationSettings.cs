using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityMeshSimplifier;

public class LodGenerationSettings : MonoBehaviour
{
    public int numberOfTriangles;
    
    private LODGeneratorHelper lodGeneratorHelper;
    [HideInInspector]
    public LODGeneratorHelper LodGeneratorHelper { get => GetComponent<LODGeneratorHelper>(); set => lodGeneratorHelper = value; }
}
