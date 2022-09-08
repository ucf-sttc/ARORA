using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Renderer))]
public class MPBViewer : MonoBehaviour
{
    #region Data

    public Renderer m_rd;
    public readonly List<Material> m_sharedMaterialList = new List<Material>();
    public List<MaterialPropertyBlock> m_blockList = new List<MaterialPropertyBlock>();
    public List<bool> m_foldoutList = new List<bool>();

    public bool m_autoRefresh = false;
    private int m_autoRefreshCnt = 0;
    public int m_autoRefreshFreq = 10;

    #endregion

    #region Function

    #region Read MaterialPropertyBlock
    private void InnerRefresh()
    {
        m_blockList.Clear();
        m_sharedMaterialList.Clear();

        m_rd = GetComponent<Renderer>();
        if (m_rd == null)
        {
            Log("No Renderer Component Found");
            return;
        }

        Material[] mats = m_rd.sharedMaterials;
        if (mats == null || mats.Length == 0)
        {
            Log("No Shared Material Found");
            return;
        }

        for (int i = 0; i < mats.Length; i++)
        {
            Material mat = mats[i];
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            m_rd.GetPropertyBlock(block, i);
            m_sharedMaterialList.Add(mat);
            m_blockList.Add(block);
        }

        if (m_sharedMaterialList.Count != m_foldoutList.Count)
        {
            m_foldoutList.Clear();
            for (int i = 0; i < m_sharedMaterialList.Count; i++)
            {
                m_foldoutList.Add(false);
            }
        }
    }

    public void Refresh()
    {
        try
        {
            InnerRefresh();
        }
        catch (Exception e)
        {
            Log("Refresh Unknown Error");
            Log(e.Message);
        }
    }
    #endregion

    #region Modify MaterialPropertyBlock
    public void SetValue(int index, string name, Color value)
    {
        m_blockList[index].SetColor(name, value);
        m_rd.SetPropertyBlock(m_blockList[index], index);
    }

    public void SetValue(int index, string name, Vector4 value)
    {
        m_blockList[index].SetVector(name, value);
        m_rd.SetPropertyBlock(m_blockList[index], index);
    }

    public void SetValue(int index, string name, float value)
    {
        m_blockList[index].SetFloat(name, value);
        m_rd.SetPropertyBlock(m_blockList[index], index);
    }

    public void SetValue(int index, string name, Texture value)
    {
        m_blockList[index].SetTexture(name, value);
        m_rd.SetPropertyBlock(m_blockList[index], index);
    }
    #endregion

    #region LiftCycle
    void Update()
    {
        if (m_autoRefresh)
        {
            m_autoRefreshCnt++;
            if (m_autoRefreshCnt > m_autoRefreshFreq)
            {
                Refresh();
                m_autoRefreshCnt = 0;
            }
        }
    }
    #endregion

    #region Utility
    private void Log(string msg)
    {
        Debug.Log("GameObject:" + gameObject.name + ". " + "MPBViewer: " + msg + ".");
    }
    #endregion

    #endregion
}
