using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManipulateShaders : MonoBehaviour
{
    public bool useAlternateColorForTreeCollidersDuringTagSegmentation;
    public string keyForAlternateColoringOfTreesCollidersDuringTagSegmentation;

    public bool onlySegmentParkedVehicles;

    public List<Shader> shaders;
    public Shader[] replacementShaders;
    
    [ContextMenu("Find All Shaders In Use")]
    public void FindShaders()
    {
        shaders = new List<Shader>();
        foreach (Renderer r in FindObjectsOfType<Renderer>())
            foreach (Material m in r.sharedMaterials)
            {
                if (m!= null && !shaders.Contains(m.shader))
                    shaders.Add(m.shader);
            }
        foreach (Terrain t in FindObjectsOfType<Terrain>())
            if (t.materialTemplate != null && !shaders.Contains(t.materialTemplate.shader))
                shaders.Add(t.materialTemplate.shader);
    }

    [ContextMenu("Replace Shaders")]
    public void ReplaceShaders()
    {
        int i = 0;
        if (replacementShaders.Length != shaders.Count)
        {
            Debug.LogError("Replacement array doesn't match shader array size");
            return;
        }

        foreach (Renderer r in FindObjectsOfType<Renderer>())
            foreach (Material m in r.sharedMaterials)
                if (m != null && shaders.Contains(m.shader))
                {
                    int index = shaders.IndexOf(m.shader);
                    if (replacementShaders[index] != null)
                    {
                        m.shader = replacementShaders[index];
                        i++;
                    }
                        
                }
        foreach (Terrain t in FindObjectsOfType<Terrain>())
            if (t.materialTemplate != null && shaders.Contains(t.materialTemplate.shader))
            {
                int index = shaders.IndexOf(t.materialTemplate.shader);
                if (replacementShaders[index] != null)
                {
                    t.materialTemplate.shader = replacementShaders[index];
                    i++;
                }
                    
            }
        Debug.Log("Replaced the shader of " + i + " materials");
    }

    [ContextMenu("Setup segmentation values")]
    public void UpdateSegmentationInfo()
    {
        foreach (Renderer r in FindObjectsOfType<Renderer>())
            UpdateMaterials(r);
        foreach (Terrain t in FindObjectsOfType<Terrain>())
            UpdateMaterials(t);
    }

    public void UpdateMaterials(Component c)
    {
        var id = c.gameObject.GetInstanceID();
        var layer = c.gameObject.layer;
        var tag = c.gameObject.tag;

        List<Material> materials = new List<Material>();
        if (c is Terrain)
            materials.Add((c as Terrain).materialTemplate);
        else
            materials.AddRange((c as Renderer).sharedMaterials);

        foreach(Material m in materials)
        {
            if(m != null)
            {
                m.SetColor("_ObjectSegmentationColor", ColorEncoding.EncodeIDAsColor(id));
                m.SetColor("_TagSegmentationColor", ColorEncoding.EncodeTagAsColor(tag));
                m.SetColor("_LayerSegmentationColor", ColorEncoding.EncodeLayerAsColor(layer));

                if (useAlternateColorForTreeCollidersDuringTagSegmentation && layer == LayerMask.NameToLayer("TreeColliders"))
                {
                    m.SetColor("_TagSegmentationColor", ColorEncoding.EncodeTagAsColor(c.GetComponentInParent<AttributeClass>().GetValueForKey(keyForAlternateColoringOfTreesCollidersDuringTagSegmentation)));
                }

                //When we want parked vehicle segemenation on the layer segmentation and everything else to be black
                if (onlySegmentParkedVehicles)
                {
                    if (layer == LayerMask.NameToLayer("ParkedVehicles"))
                        m.SetColor("_LayerSegmentationColor", Color.red);
                    else
                        m.SetColor("_LayerSegmentationColor", Color.black);
                }

            }
        }
    }
}
