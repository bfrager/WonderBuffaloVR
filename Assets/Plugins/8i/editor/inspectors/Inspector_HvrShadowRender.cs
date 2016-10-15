
using HVR.Interface;
using UnityEditor;
using UnityEngine;

namespace HVR.Editor
{
    [CustomEditor(typeof(HvrShadowRender))]
    [CanEditMultipleObjects]
    public class Inspector_HvrShadowRender : UnityEditor.Editor
    {
        HvrShadowRender hvrShadowRender;

        public float texScrollArea = 0;
        public Vector2 scrollViewPos = Vector2.zero;

        Shader hvrRenderEditorPreviewShader;
        Material hvrRenderEditorPreviewMat;

        bool showDebugTextures = false;

        bool CheckResources()
        {
            if (hvrRenderEditorPreviewShader == null)
                hvrRenderEditorPreviewShader = Resources.Load("8i/Editor/shaders/HVRRender_EditorBufferPreview") as Shader;

            if (hvrRenderEditorPreviewShader != null)
            {
                hvrRenderEditorPreviewMat = new Material(hvrRenderEditorPreviewShader);
            }
            else
            {
                return false;
            }

            return true;
        }

        public override void OnInspectorGUI()
        {
            hvrShadowRender = (HvrShadowRender)target;

            Undo.RecordObject(target, "HVR Render Object");

            Inspector_HvrHeader.Draw();
            GUILayout.Space(4);

            EditorGUILayout.LabelField("PREVIEW COMPONENT ONLY", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField("NOT FINAL", EditorStyles.centeredGreyMiniLabel);

            GUILayout.Space(4);

            if (hvrShadowRender && CheckResources())
                DrawCustomInspector(target);

            if (GUI.changed)
            {
                Repaint();
            }
        }

        public void DrawCustomInspector(UnityEngine.Object target)
        {
            showDebugTextures = EditorGUILayout.Toggle("Debug", showDebugTextures);

            if (showDebugTextures)
            {
                DrawDebugTextures();
            }

            hvrShadowRender.visualiseShadowCascading = EditorGUILayout.Toggle("Visualise Shadow Cascading", hvrShadowRender.visualiseShadowCascading);
        }

        void DrawDebugTextures()
        {
            scrollViewPos = EditorGUILayout.BeginScrollView(scrollViewPos, false, false, GUILayout.MinHeight(240));
            {
                EditorGUILayout.BeginHorizontal();
                {
                    {
                        Rect rect = EditorGUILayout.BeginHorizontal("box", GUILayout.Height(220), GUILayout.Width(210));
                        {
                            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, 15), "Color:");

                            HvrViewportInterface viewport = hvrShadowRender.renderInterface.CurrentViewport();
                            Texture tex = viewport.frameBuffer.renderColourBuffer;
                            if (tex)
                                EditorGUI.DrawPreviewTexture(new Rect(rect.x + 5, rect.y + 15, 200, 200), tex, hvrRenderEditorPreviewMat);

                            GUILayout.Space(200);
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    {
                        Rect rect = EditorGUILayout.BeginHorizontal("box", GUILayout.Height(220), GUILayout.Width(210));
                        {
                            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, 15), "Depth:");

                            HvrViewportInterface viewport = hvrShadowRender.renderInterface.CurrentViewport();
                            Texture tex = viewport.frameBuffer.renderDepthBuffer;
                            if (tex)
                                EditorGUI.DrawPreviewTexture(new Rect(rect.x + 5, rect.y + 15, 200, 200), tex, hvrRenderEditorPreviewMat);

                            GUILayout.Space(200);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
