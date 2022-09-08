/*
 * Editor script to save camera renders to files
 * 
 * 1. Attach to VisualCarAgent or VectorVisualCarAgent object
 * 2. Position agent in scene as desired
 * 3. Use context action "Save Render Textures" (right-click on component in Inspector)
 */
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.MLAgents.Sensors;

public class SaveRenderTextures : MonoBehaviour
{
    public TextureFormat depthFormat = TextureFormat.R8;
    public TextureFormat rgbFormat = TextureFormat.ARGB32;
    public TextureFormat segFormat = TextureFormat.ARGB32;
    NavAgent agent;
    Camera[] cameras;

    void Awake()
    {
        LoadAgent();
    }

    void LoadAgent()
    {
        agent = GetComponent<VectorVisualCarAgent>();
        if(agent)
        {
            cameras = ((VectorVisualCarAgent)agent).renderCameras;
        }
        else if (agent == null)
        {
            agent = GetComponent<VisualCarAgent>();
            cameras = ((VisualCarAgent)agent).renderCameras;
        }
        else
        {
            Debug.LogError("Object is not an agent with cameras");
        }
    }

    [ContextMenu("Save Render Textures")]
    void SaveToFiles()
    {
        if (agent == null)
            LoadAgent();
        string imagepath = "";
        int index = 0;
        var oldRT = RenderTexture.active;
        foreach(Camera c in cameras)
        {
            if (c != null && c.gameObject.activeSelf)
            {
                c.Render();
                Texture2D tex;
                foreach (RenderTextureSensorComponent rts in c.GetComponents<RenderTextureSensorComponent>())
                {
                    int width = rts.RenderTexture.width;
                    int height = rts.RenderTexture.height;

                    Debug.Log(rts.RenderTexture.name + " " + rts.RenderTexture.format);
                    RenderTexture.active = rts.RenderTexture;

                    if (rts.RenderTexture.name == "DepthMapRenderTexture")
                    {
                        tex = new Texture2D(width, height, depthFormat, false, true);
                        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    }
                    else if (rts.RenderTexture.name == "VisualObservationRenderTexture")
                    {
                        tex = new Texture2D(width, height, rgbFormat, false, true);
                        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                        ChangeColorSpace(tex);
                    }
                    else if (rts.RenderTexture.name == "SegmentationRenderTexture")
                    {
                        tex = new Texture2D(width, height, segFormat, false, true);
                        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                        ChangeColorSpace(tex);
                    }
                    else
                    {
                        Debug.LogWarning("Unknown render texture");
                        tex = new Texture2D(width, height);
                        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    }

                    tex.Apply();
                    File.WriteAllBytes(imagepath + "img" + index++ + ".png", tex.EncodeToPNG());
                }
            }
        }
        RenderTexture.active = oldRT;
    }

    // this is used to convert gamma-corrected images back to linear space as seen in Unity editor
    void ChangeColorSpace(Texture2D tex)
    {
        for(int x = 0; x < tex.width; x++)
            for(int y = 0; y < tex.height; y++)
            {
                tex.SetPixel(x, y, new Color(Mathf.Pow(tex.GetPixel(x, y).r, 1f / 2.2f),
                                             Mathf.Pow(tex.GetPixel(x, y).g, 1f / 2.2f),
                                             Mathf.Pow(tex.GetPixel(x, y).b, 1f / 2.2f),
                                             Mathf.Pow(tex.GetPixel(x, y).a, 1f / 2.2f)));
            }
    }
}
