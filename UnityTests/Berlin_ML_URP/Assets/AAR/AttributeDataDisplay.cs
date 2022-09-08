using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AttributeDataDisplay : MonoBehaviour
{
    public Text title;
    public GameObject contentPanel, entryPrefab;
    public LineRenderer lineRenderer;
    public AttributeClass data;
    public int numberOfEntriesToDisplay;
    bool placePanelCloseToCamera;
    int index;

    List<GameObject> entryList;
    GameObject newEntryPanel;
    

    private void Awake()
    {
        entryList = new List<GameObject>();
    }
    private void Update()
    {
        if(data != null && Camera.main)
        {
            if(placePanelCloseToCamera)
            {
                transform.position = Camera.main.transform.position + Camera.main.transform.forward*1.5f - Camera.main.transform.up/2;
                switch (index)
                {
                    case 0:
                        transform.position += - Camera.main.transform.right*.75f;
                        break;
                    case 1:
                        break;
                    case 2:
                        transform.position += Camera.main.transform.right*.75f;
                        break;
                }


                /*
                 * transform.position = Camera.main.transform.position - (Camera.main.transform.up * 0.2f) + ((data.transform.position - Camera.main.transform.position).normalized);
                if (Physics.Raycast(Camera.main.transform.position, transform.position, out RaycastHit hit))
                {
                    if (hit.transform.gameObject != gameObject)
                    {
                        if (Camera.main.WorldToScreenPoint(transform.position).x < Screen.width / 2)
                            transform.position += Camera.main.transform.right;
                        else
                            transform.position -= Camera.main.transform.right;
                    }
                }
                */
            }
            else
            {
                Vector3 vectorToTarget = data.transform.position - Camera.main.transform.position;
                if (Physics.Raycast(Camera.main.transform.position, vectorToTarget, out RaycastHit hit))
                    transform.position = hit.point+Vector3.up-vectorToTarget.normalized/3;
            }
            
            transform.LookAt(Camera.main.transform);
            lineRenderer.SetPositions(new Vector3[] { data.transform.position, contentPanel.transform.position + contentPanel.transform.forward * .1f });
        }
    }
    public void Initialize(AttributeClass data, bool placePanelCloseToCamera, int index)
    {
        this.data = data;
        this.placePanelCloseToCamera = placePanelCloseToCamera;
        this.index = index;

        foreach (GameObject e in entryList)
            Destroy(e);
        entryList.Clear();

        foreach (AttributeClass.Attribute entry in data.attributes)
        {
            if(entry.val != "")
            {
                newEntryPanel = Instantiate(entryPrefab, contentPanel.transform);
                newEntryPanel.transform.GetChild(0).GetComponent<Text>().text = entry.key;
                newEntryPanel.transform.GetChild(1).GetComponent<Text>().text = entry.val;
                entryList.Add(newEntryPanel);
                if (entryList.Count >= numberOfEntriesToDisplay) break;
            }
        }
    }
}
