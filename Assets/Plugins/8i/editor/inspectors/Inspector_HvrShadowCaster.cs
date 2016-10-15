using UnityEditor;
using UnityEngine;
using System.Collections;

using HVR;
using HVR.Utils;

namespace HVR.Editor
{
    [CustomEditor(typeof(HvrShadowCaster))]
    public class Inspector_HvrShadowCaster : UnityEditor.Editor
    {
        HvrShadowCaster m_Instance;
        PropertyField[] m_fields;

        Color editorColor;

        public void OnEnable()
        {
            m_Instance = target as HvrShadowCaster;
            m_fields = ExposeProperties.GetProperties(m_Instance);
            editorColor = GUI.color;
        }

        public override void OnInspectorGUI()
        {
            if (m_Instance == null)
                return;

            Undo.RecordObject(target, "HVR Shadow Caster");

            GUI.color = editorColor;

            Inspector_HvrHeader.Draw();
            GUILayout.Space(4);

            EditorGUILayout.LabelField("PREVIEW COMPONENT ONLY", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField("NOT FINAL", EditorStyles.centeredGreyMiniLabel);

            GUILayout.Space(4);

            this.DrawDefaultInspector();
            ExposeProperties.Expose(m_fields);

            if (GUI.changed)
            {
                Repaint();
            }
        }
    }
}