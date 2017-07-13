﻿using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityLabs
{
    [CustomEditor(typeof(MultiMaterial), true)]
    public class MultiMaterialEditor : Editor
    {
        MaterialEditor[] m_MaterialEditors;
        SerializedProperty m_Materials;

        Renderer m_Renderer;
        MaterialArray m_RendererMaterialArray;

        MultiMaterial m_MultiMaterial;
        SerializedObject m_MultiMaterialData;
        SerializedProperty[] m_MaterialProperties;

        bool useRenderer
        {
            get
            {
                return m_MultiMaterial == null || m_MultiMaterial.multiMaterialData == null;
            }
        }

        MaterialArray materialArray
        {
            get { return m_RendererMaterialArray ?? m_MultiMaterial.multiMaterialData.materialArrayData; }
        }

        public void OnEnable()
        {
            m_MaterialEditors = new MaterialEditor[] {};
            m_MultiMaterial = target as MultiMaterial;
            if (m_MultiMaterial.multiMaterialData != null)
                m_MultiMaterialData = new SerializedObject(m_MultiMaterial.multiMaterialData);
            m_Renderer = m_MultiMaterial.gameObject.GetComponent<Renderer>();

            ValidateEditorData();
            MaterialArrayDrawers.UpdateShaderNames();
        }

        void ValidateEditorData()
        {
            if (m_MultiMaterial.multiMaterialData != null && m_MultiMaterialData == null)
            {
                m_MultiMaterialData = new SerializedObject(m_MultiMaterial.multiMaterialData);
            }

            if (useRenderer)
            {
                if (m_Renderer == null)
                    m_Renderer = m_MultiMaterial.GetComponent<Renderer>();

                m_RendererMaterialArray = m_Renderer != null ? 
                    new MaterialArray { materials = m_Renderer.sharedMaterials } : null;
                m_MaterialProperties = null;
            }
            else
            {
                m_Renderer = null;
                m_RendererMaterialArray = null;
            }

            if (m_Renderer == null)
            {
                m_Materials = m_MultiMaterialData.FindProperty(string.Format("{0}.{1}",
                    MultiMaterialData.materialArrayPub, MaterialArray.materialsPub));

                var materialPropList = new List<SerializedProperty>();
                for (var i = 0; i < m_Materials.arraySize; ++i)
                {
                    materialPropList.Add(m_Materials.GetArrayElementAtIndex(i));
                }
                m_MaterialProperties = materialPropList.ToArray();
            }
        }

        public override void OnInspectorGUI()
        {
            var changed = false;
            EditorGUI.BeginChangeCheck();
            serializedObject.Update();
            base.OnInspectorGUI();
            serializedObject.ApplyModifiedProperties();
            changed = EditorGUI.EndChangeCheck();

            ValidateEditorData();

            if (!useRenderer)
            {
                m_MultiMaterialData.Update();
                EditorGUI.BeginChangeCheck();
                m_Materials.arraySize = EditorGUILayout.DelayedIntField("Size",
                    m_Materials.arraySize);
                m_MultiMaterialData.ApplyModifiedProperties();
                if (EditorGUI.EndChangeCheck())
                {
                    if (m_MultiMaterial.multiMaterialData != null)
                        m_MultiMaterialData = new SerializedObject(m_MultiMaterial.multiMaterialData);
                    ValidateEditorData();
                    changed = true;
                }
            }
            else if (m_Renderer != null)
            {
                if (CreateMultiMaterialDataButton(false))
                {
                    if (m_MultiMaterial.multiMaterialData != null)
                        m_MultiMaterialData = new SerializedObject(m_MultiMaterial.multiMaterialData);
                    ValidateEditorData();
                }
            }

            if (!useRenderer || m_Renderer != null)
            {
                MaterialArrayDrawers.OnInspectorGUI(m_MultiMaterialData, materialArray,
                    ref m_MaterialEditors, changed, m_MaterialProperties);

                if (m_Renderer == null)
                {
//                    if (GUILayout.Button("Add Selected"))
//                    {
//                        m_MultiMaterialData.Update();
//                        var matHash = new HashSet<Material>();
//                        if (materialArray.materials.Length > 0)
//                        {
//                            foreach (var mat in materialArray.materials)
//                            {
//                                matHash.Add(mat);
//                            }
//                        }
//                        foreach (var obj in Selection.objects)
//                        {
//                            var mat = obj as Material;
//                            if (mat != null)
//                            {
//                                matHash.Add(mat);
//                            }
//                            var go = obj as GameObject;
//                            if (go != null)
//                            {
//                                var meshRenderers = go.GetComponentsInChildren<MeshRenderer>(true);
//                                foreach (var meshRenderer in meshRenderers)
//                                {
//                                    foreach (var sharedMaterial in meshRenderer.sharedMaterials)
//                                    {
//                                        matHash.Add(sharedMaterial);
//                                    }
//                                }
//                                var skinnedMeshRenderers = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
//                                foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
//                                {
//                                    foreach (var sharedMaterial in skinnedMeshRenderer.sharedMaterials)
//                                    {
//                                        matHash.Add(sharedMaterial);
//                                    }
//                                }
//                            }
//                        }
//                        materialArray.materials = matHash.ToArray();
//
//                        m_MultiMaterialData.ApplyModifiedProperties();
//                        MaterialArrayDrawers.RebuildMaterialEditors(ref m_MaterialEditors, materialArray);
//                        return;
//                    }

                    if (MaterialArrayDrawers.AddSelectedButtons(m_MultiMaterialData, materialArray))
                    {
                        MaterialArrayDrawers.RebuildMaterialEditors(ref m_MaterialEditors, materialArray);
                        return;
                    }
                }

                if (GUILayout.Button("Select Materials"))
                {
                    if (materialArray != null && materialArray.materials != null &&
                        materialArray.materials.Length > 0)
                    {
                        Selection.objects = materialArray.materials;
                    }
                }
            }
        }

        bool CreateMultiMaterialDataButton(bool changed)
        {
            EditorGUI.BeginDisabledGroup(m_Renderer == null);
            if (GUILayout.Button("Create From Renderer"))
            {
                var saveMultiMaterialData = CreateInstance<MultiMaterialData>();
                var rendererMaterialArray = new MaterialArray {materials = m_Renderer.sharedMaterials};
                saveMultiMaterialData.materialArrayData = rendererMaterialArray;

                var path = EditorUtility.SaveFilePanelInProject("Multi Material Data Save Window",
                    m_Renderer.gameObject.name + " Multi Material Data", "asset",
                    "Enter a file name to save the multi material data to");

                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.CreateAsset(saveMultiMaterialData, path);
                    AssetDatabase.Refresh();
                }

                var loadMultiMaterialData = AssetDatabase.LoadAssetAtPath<MultiMaterialData>(path);
                if (loadMultiMaterialData != null)
                {
                    serializedObject.Update();
                    var dataProp = serializedObject.FindProperty(MultiMaterial.multiMaterialDataPub);
                    dataProp.objectReferenceValue = loadMultiMaterialData;
                    serializedObject.ApplyModifiedProperties();
                }
                ValidateEditorData();
                return true;
            }
            EditorGUI.EndDisabledGroup();
            return changed;
        }
    }
}
