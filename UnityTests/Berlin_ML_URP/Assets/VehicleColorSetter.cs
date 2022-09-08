using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleColorSetter : MonoBehaviour
{
    [System.Serializable]
    public struct ReplacedMaterialInfo
    {
        public Renderer renderer;
        public int materialIndex;

        public ReplacedMaterialInfo(Renderer r, int i)
        {
            renderer = r;
            materialIndex = i;
        }
    }
    public Material[] materialsToReplace;
    public List<ReplacedMaterialInfo> replacedMaterials;

    public Color color;
    public int textureIndex;
    public Material sharedVehiclePaintMaterial;

    MaterialPropertyBlock mpb;

    private void OnEnable()
    {
        setupMaterialWithInstancedTexture();
    }

    private void OnValidate()
    {
        setupMaterialWithInstancedTexture();
    }

    //Outdated
    #region Outdated
    Texture2D texture;
    public void Initialize(Color newColor, Texture2D newTexture)
    {
        //Undo.RecordObject(this, "Set color");
        color = newColor;
        texture = newTexture;
        setupMaterial();
    }

    void setupMaterial()
    {
        mpb = new MaterialPropertyBlock();
        
        for (int i = 0; i < replacedMaterials.Count; i++)
        {
            Material sharedMaterial = replacedMaterials[i].renderer.sharedMaterials[replacedMaterials[i].materialIndex];
            sharedMaterial.shader = Shader.Find("Custom/Metalic_Instanced");
            replacedMaterials[i].renderer.GetPropertyBlock(mpb,0);
            if (color != null)
                mpb.SetColor("_Color", color);
            if (texture != null)
            {
                //mpb.SetTexture("_MainTex", texture);
                if (sharedMaterial.GetTexture("_MainTex") != texture)
                   sharedMaterial.SetTexture("_MainTex", texture);
            }

            replacedMaterials[i].renderer.SetPropertyBlock(mpb,0);
        }
    }
    #endregion

    [ContextMenu("Locate materials for replacement")]
    void LocateMaterialsForReplacement()
    {
        replacedMaterials = new List<ReplacedMaterialInfo>();
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            for (int i = 0; i < r.sharedMaterials.Length; i++)
                for(int j =0;j<materialsToReplace.Length;j++)
                    if (r.sharedMaterials[i].name == materialsToReplace[j].name)
                        replacedMaterials.Add(new ReplacedMaterialInfo(r, i));
    }


    
    public void Initialize(Color newColor, int index)
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Initialize vehicle color setter script");
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        //Undo.RecordObject(this, "Set color");
        color = newColor;
        textureIndex = index;
        setupMaterialWithInstancedTexture();
    }

    void setupMaterialWithInstancedTexture()
    {
        mpb = new MaterialPropertyBlock();
        if(sharedVehiclePaintMaterial != null)
            for (int i = 0; i < replacedMaterials.Count; i++)
            {
                Material[] sharedMaterials = replacedMaterials[i].renderer.sharedMaterials;
                sharedMaterials[replacedMaterials[i].materialIndex] = sharedVehiclePaintMaterial;
                replacedMaterials[i].renderer.sharedMaterials = sharedMaterials;

                replacedMaterials[i].renderer.GetPropertyBlock(mpb, replacedMaterials[i].materialIndex);
                
                mpb.SetColor("_ReplacementColor", color);
                mpb.SetInt("_TextureIndex", textureIndex);

                replacedMaterials[i].renderer.SetPropertyBlock(mpb, replacedMaterials[i].materialIndex);
            }
    }

    public void ClearPropertyBlocks()
    {
        mpb = new MaterialPropertyBlock();
        for (int i = 0; i < replacedMaterials.Count; i++)
        {
            replacedMaterials[i].renderer.SetPropertyBlock(mpb);
            replacedMaterials[i].renderer.sharedMaterials[replacedMaterials[i].materialIndex] = materialsToReplace[0];
        }
    }
}
