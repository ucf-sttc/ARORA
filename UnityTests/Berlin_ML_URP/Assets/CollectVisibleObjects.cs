using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CollectVisibleObjects : MonoBehaviour
{
    [System.Serializable]
    public struct AttributeObject
    {
        public CollectVisibleObjects instance;
        public AttributeClass attributeData;
        public Renderer[] renderers;
        public void  SetVisibility(bool value)
        {

            if (value == true && !instance.visibleAttributeObjects.Contains(this))
            {
                instance.visibleAttributeObjects.Add(this);
            }
            else if(instance.visibleAttributeObjects.Contains(this))
            {
                foreach (Renderer r in renderers)
                    if (r.isVisible)
                        return;
                instance.visibleAttributeObjects.Remove(this);

            }
        }
        public bool CheckVisibility()
        {
            if (renderers.Length > 0)
            {
                foreach (Renderer r in renderers)
                    if (r.isVisible)
                        return true;
                return false;
            }
            else //Setup case for tree instance colliders
                return false;
        }
    }
    List<AttributeObject> attributeObjects;
    public List<AttributeObject> visibleAttributeObjects;

    void Awake()
    {
        attributeObjects = new List<AttributeObject>();
        visibleAttributeObjects = new List<AttributeObject>();
        foreach(AttributeClass ac in FindObjectsOfType<AttributeClass>())
        {
            Debug.Log("AttributeClass Find: " + ac.gameObject.name);
            AttributeObject attributeObject = new AttributeObject();
            attributeObject.attributeData = ac;
            attributeObject.renderers = ac.gameObject.GetComponentsInChildren<Renderer>();
            /*
            attributeObject.collector = this;
            foreach(Renderer r in attributeObject.renderers)
            {
                FlagVisibility flagVisibility = (FlagVisibility)r.gameObject.AddComponent(typeof(FlagVisibility));
                flagVisibility.referencedEntry = attributeObject;
            }
            */
            attributeObjects.Add(attributeObject);
        }
    }

    public void FindVisibleAttributeObjects()
    {
        visibleAttributeObjects.Clear();
        foreach(AttributeObject obj in attributeObjects)
        {
            if (obj.CheckVisibility())
                visibleAttributeObjects.Add(obj);
        }    
    }

    void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += RenderPipelineManagerEventListener;
    }

    void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= RenderPipelineManagerEventListener;
    }

    void RenderPipelineManagerEventListener(ScriptableRenderContext context, Camera camera)
    {
        OnPreRender();
    }

    void OnPreRender()
    {
        FindVisibleAttributeObjects();
        //Debug.Log("Collected visible objects: " + visibleAttributeObjects.Count);
    }
}
