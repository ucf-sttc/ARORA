using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddObjectToSegmentation : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var segmentationSetup = FindObjectOfType<SegmentationSetup>();
        if(segmentationSetup != null)
            segmentationSetup.SetupGameObjectSegmentation(gameObject);
    }
}
