using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HVR.Editor
{
    [CustomEditor(typeof(HvrActor_Clone))]
    public class Inspector_HvrActor_Clone : UnityEditor.Editor
    {
        HvrActor_Clone actorClone { get { return target as HvrActor_Clone; } }

        private MaterialEditor materialEditor;

        GUIStyle s_CenteredStyle;

        Color editorColor;

        void OnEnable()
        {
            if (actorClone != null && actorClone.material != null)
            {
                // Create a new instance of the default MaterialEditor
                materialEditor = (MaterialEditor)CreateEditor(actorClone.material);
            }

            editorColor = GUI.color;
        }

        void OnDisable()
        {
            if (materialEditor != null)
            {
                // Free the memory used by default MaterialEditor
                DestroyImmediate(materialEditor);
            }
        }

        public override void OnInspectorGUI()
        {
            if (s_CenteredStyle == null)
            {
                s_CenteredStyle = new GUIStyle(GUI.skin.GetStyle("Label"))
                {
                    fontSize = 30,
                    fixedHeight = 34,
                    alignment = TextAnchor.UpperCenter
                };
            }

            serializedObject.Update();

            Undo.RecordObject(actorClone, "HvrActor");

            Inspector_HvrHeader.Draw();

            GUILayout.Space(4);
            GUI.color = new Color(0.0f, 0.6f, 1.00f, 1.0f);
            GUILayout.BeginVertical(GUI.skin.GetStyle("textarea"));
            {
                EditorGUILayout.LabelField("CLONE", s_CenteredStyle);
                GUILayout.Space(30);
                GUI.color = editorColor;

                actorClone.sourceActor = EditorGUILayout.ObjectField("Source Actor", actorClone.sourceActor, typeof(HvrActor), true) as HvrActor;

                GUILayout.Space(4);

                if (actorClone.sourceActor)
                {
                    EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    {
                        Inspector_HvrActor.DrawInspector_UnityRenderMethod(actorClone);

                        EditorGUI.BeginDisabledGroup(true);
                        {
                            Inspector_HvrActor.DrawInspector_ActorRenderMethod(actorClone);
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUI.indentLevel--;
                    GUILayout.Space(4);

                    Inspector_HvrActor.DrawInspector_OcclusionCulling(actorClone);
                    GUILayout.Space(4);

                    EditorGUI.BeginDisabledGroup(true);
                    {
                        if (actorClone.hvrAsset != null)
                        {
                            Inspector_HvrActor.DrawInspector_PlaybackBar(actorClone.hvrAsset);
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }
            }
            GUILayout.EndVertical();

            if (actorClone.actorRenderMethod == HvrActor.eRenderMethod.standard)
            {
                Inspector_HvrActor.DrawInspector_ActorMaterialEditor(materialEditor);
            }

            Repaint();
        }
    }
}
