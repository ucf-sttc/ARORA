using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetSegmentationColor : MonoBehaviour
{
    public Color GetColorForString(string input)
    {
        return ColorEncoding.EncodeTagAsColor(input);
    }
}
