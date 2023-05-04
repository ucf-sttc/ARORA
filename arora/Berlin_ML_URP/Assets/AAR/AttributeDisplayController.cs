using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttributeDisplayController : MonoBehaviour
{
    public float range;
    public int maximumSimultaneousViews;
    public List<AttributeClass> objectsInRange;
    public GameObject target, attributeDataDisplayPrefab;
    public List<GameObject> attributeDisplays;
    public bool attributeEnabled, zoomInOnPanels;

    int i;
    Collider[] unsortedColliders;
    AttributeClass attributeClass;
    WaitForSeconds threeSeconds = new WaitForSeconds(3);

    void Start()
    {
        objectsInRange = new List<AttributeClass>();
        attributeDisplays = new List<GameObject>();
        StartCoroutine(updateObjectsInRange());
    }

    public void SetAttributeEnabled(bool val)
    {
        attributeEnabled = val;
        if (val)
            UpdatePanels();
        else
            HidePanels();
    }

    public void SetZoomInOnPanels(bool newValue)
    {
        zoomInOnPanels = newValue;
        UpdatePanels();
    }

    public void SetRange(int value)
    {
        switch (value)
        {
            case 0:
                range = 25;
                break;
            case 1:
                range = 50;
                break;
            case 2:
                range = 75;
                break;
        }
    }

    IEnumerator updateObjectsInRange()
    {
        while(true)
        {
            if (!attributeEnabled || !Camera.main)
            {
                yield return threeSeconds;
                continue;
            }

            i = 0;
            objectsInRange.Clear();

            Quaternion overlapBoxRotation = target.transform.rotation;
            overlapBoxRotation.eulerAngles = target.transform.rotation.eulerAngles + new Vector3(0, 45, 0);
            unsortedColliders = Physics.OverlapBox(target.transform.position + target.transform.forward * Mathf.Sqrt(range*range*2), new Vector3(range, range, range), overlapBoxRotation);

            while (objectsInRange.Count < maximumSimultaneousViews && i < unsortedColliders.Length)
            {
                Vector3 vectorToTarget = unsortedColliders[i].transform.position - Camera.main.transform.position;
                if (Physics.Raycast(Camera.main.transform.position, vectorToTarget, out RaycastHit hit) && hit.transform.gameObject == unsortedColliders[i].gameObject)
                {
                    attributeClass = unsortedColliders[i].GetComponent<AttributeClass>();
                    if (attributeClass != null && !objectsInRange.Contains(attributeClass))
                        objectsInRange.Add(attributeClass);
                }
                i++;
            }

            UpdatePanels();
            yield return threeSeconds;
        }
    }

    void UpdatePanels()
    {
        int i;
        for (i = 0; i < objectsInRange.Count; i++)
        {
            if (i == attributeDisplays.Count)
                attributeDisplays.Add(Instantiate(attributeDataDisplayPrefab, transform));

            AttributeClass a = objectsInRange[i];
            attributeDisplays[i].GetComponent<AttributeDataDisplay>().Initialize(a, zoomInOnPanels, i);
            attributeDisplays[i].SetActive(true);
            if (zoomInOnPanels && i == 2)
                break;
        }
        for (i++; i < attributeDisplays.Count; i++)
            attributeDisplays[i].SetActive(false);
    }

    public void HidePanels()
    {
        foreach (GameObject display in attributeDisplays)
            display.SetActive(false);
    }
}
