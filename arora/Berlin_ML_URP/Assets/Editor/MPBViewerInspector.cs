using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(MPBViewer))]
    public class MPBViewerInspector : Editor
    {
        private MPBViewer m_viewer;

        public void OnEnable()
        {
            m_viewer = (MPBViewer)target;
        }

        private string GetName(string displayName, string name)
        {
            return displayName + " [" + name + "]";
        }

        private void ShowPropertyBlock(int index, Material mat, MaterialPropertyBlock block)
        {
            #region check paramter
            if (mat == null)
            {
                EditorGUILayout.LabelField("mat is null.");
                return;
            }
            if (block == null)
            {
                EditorGUILayout.LabelField("block is null.");
                return;
            }
            #endregion

            MaterialProperty[] props = MaterialEditor.GetMaterialProperties(new Material[] { mat });
            for (var i = 0; i < props.Length; i++)
            {
                MaterialProperty prop = props[i];
                string name = prop.name;
                string displayName = prop.displayName;
                MaterialProperty.PropType type = prop.type;
                switch (type)
                {
                    case MaterialProperty.PropType.Color:
                        {
                            Color value = block.GetColor(name);
                            Color newValue = EditorGUILayout.ColorField(GetName(displayName, name), value);
                            if (newValue != value)
                            {
                                m_viewer.SetValue(index, name, newValue);
                            }
                        }
                        break;
                    case MaterialProperty.PropType.Vector:
                        {
                            Vector4 value = block.GetVector(name);
                            Vector4 newValue = EditorGUILayout.Vector4Field(GetName(displayName, name), value);
                            if (newValue != value)
                            {
                                m_viewer.SetValue(index, name, newValue);
                            }
                        }
                        break;
                    case MaterialProperty.PropType.Float:
                        {
                            float value = block.GetFloat(name);
                            float newValue = EditorGUILayout.FloatField(GetName(displayName, name), value);
                            if (newValue != value)
                            {
                                m_viewer.SetValue(index, name, newValue);
                            }
                        }
                        break;
                    case MaterialProperty.PropType.Range:
                        {
                            float value = block.GetFloat(name);
                            float newValue = EditorGUILayout.FloatField(GetName(displayName, name), value);
                            if (newValue != value)
                            {
                                m_viewer.SetValue(index, name, newValue);
                            }
                        }
                        break;
                    case MaterialProperty.PropType.Texture:
                        {
                            Texture value = block.GetTexture(name);
                            Texture newValue = EditorGUILayout.ObjectField(
                                GetName(displayName, name), value, typeof(Texture), true) as Texture;
                            if (newValue != value)
                            {
                                m_viewer.SetValue(index, name, newValue);
                            }
                        }
                        break;
                    default:
                        {
                        }
                        break;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            #region auto refresh
            m_viewer.m_autoRefresh = EditorGUILayout.Toggle("Auto Refresh", m_viewer.m_autoRefresh);
            if (m_viewer.m_autoRefresh)
            {
                m_viewer.m_autoRefreshFreq = EditorGUILayout.IntField("Auto Refresh Freq", m_viewer.m_autoRefreshFreq);
            }
            else
            {
                if (GUILayout.Button("Refresh"))
                {
                    m_viewer.Refresh();
                }
            }
            #endregion

            #region mpb info
            List<Material> mats = m_viewer.m_sharedMaterialList;
            List<MaterialPropertyBlock> blocks = m_viewer.m_blockList;
            for (int i = 0; i < mats.Count; i++)
            {
                m_viewer.m_foldoutList[i] = EditorGUILayout.Foldout(m_viewer.m_foldoutList[i], mats[i].name);
                if (m_viewer.m_foldoutList[i])
                {
                    ShowPropertyBlock(i, mats[i], blocks[i]);
                }
            }
            #endregion

            serializedObject.ApplyModifiedProperties();
        }
    }

}