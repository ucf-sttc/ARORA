using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

//Clive: Sets up the MaterialPropertyBlocks for each game object in the heirarchy. There is also a commented out section designed to add MaterialPropertyBlocks to the prefabs of terrain trees but it doesn't work
//The material property blocks will contain the colors for the different segmentation modes and the index for the active segmentation mode
public class SegmentationSetup : MonoBehaviour
{
    public int segmentationOutputMode;
    public bool useAlternateColorForTreeCollidersDuringTagSegmentation;
    public string keyForAlternateColoringOfTreesCollidersDuringTagSegmentation;

    public bool useAlternateColorForParkedVehiclesDuringTagSegmentation;
    public string keyForAlternateColoringOfParkedVehiclesDuringTagSegmentation = "MODEL_NAME";
    public bool onlySegmentParkedVehicles;
    int previousSegmentationOutputMode = -1;
    EnvironmentParameters m_EnvParams;
    CommandLineArgs.Args m_EnvArgs;
    ManipulateShaders m;

    // Start is called before the first frame update
    private void Awake()
    {
        m_EnvParams = Academy.Instance.EnvironmentParameters;
        m_EnvArgs = CommandLineArgs.Instance.Parameters;
    }
    void Start()
    {
        Shader.SetGlobalInt("_SegmentationOutputMode", segmentationOutputMode);
        float segmentationMode = m_EnvArgs.GetWithDefault("segmentationMode", -1f);
        if (segmentationMode != -1)
            segmentationOutputMode = (int)segmentationMode;

        CheckSegmentationMode();
        
        float useAlternateColorsTreeCollider = m_EnvParams.GetWithDefault("useAlternateTreeColliderSegmentationColors", -1);
        float useAlternateColorsParkedVehicles = m_EnvParams.GetWithDefault("useAlternateParkedVehiclesSegmentationColors", -1);
        if (useAlternateColorsTreeCollider != -1)
            useAlternateColorForTreeCollidersDuringTagSegmentation = useAlternateColorsTreeCollider == 1;
        if (useAlternateColorsParkedVehicles != -1)
            useAlternateColorForParkedVehiclesDuringTagSegmentation = useAlternateColorsParkedVehicles == 1;
    }

    void CheckSegmentationMode()
    {
        if (segmentationOutputMode != previousSegmentationOutputMode)
        {
            Shader.SetGlobalInt("_SegmentationOutputMode", segmentationOutputMode);
            previousSegmentationOutputMode = segmentationOutputMode;
        }
    }

    public void SetupGameObjectSegmentation(GameObject target)
    {
        foreach (Renderer r in target.GetComponentsInChildren<Renderer>())
            UpdateMaterials(r);

        foreach (Terrain t in target.GetComponentsInChildren<Terrain>())
            UpdateMaterials(t);
    }

    void UpdateMaterials(Component c)
    {
        if(m == null)
            m = gameObject.AddComponent<ManipulateShaders>();

        m.useAlternateColorForTreeCollidersDuringTagSegmentation = useAlternateColorForTreeCollidersDuringTagSegmentation;
        m.keyForAlternateColoringOfTreesCollidersDuringTagSegmentation = keyForAlternateColoringOfTreesCollidersDuringTagSegmentation;
        m.onlySegmentParkedVehicles = onlySegmentParkedVehicles;

        m.UpdateMaterials(c);
    }
}