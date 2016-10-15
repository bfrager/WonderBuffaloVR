using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HVR.Editor
{
    [CustomEditor(typeof(HvrRender))]
    [CanEditMultipleObjects]
    public class Inspector_HvrRender : UnityEditor.Editor
    {
        HvrRender hvrRender { get { return (HvrRender)target; } }

        public bool showActors;
        public bool[] showDetails = new bool[0];
        public bool[] showTextures = new bool[0];

        Color editorColor;

        float updateTickLastTime;
        float updateTickTime = 0.25f;

        static string[] renderModes = new string[] { "Standard", "Composite", "Direct" };

        void OnEnable()
        {
            editorColor = GUI.color;
            EditorApplication.update -= UpdateTick;
            EditorApplication.update += UpdateTick;
        }

        void OnDisable()
        {
            EditorApplication.update -= UpdateTick;
        }

        void UpdateTick()
        {
            if (Time.realtimeSinceStartup > updateTickLastTime + updateTickTime)
            {
                updateTickLastTime = Time.realtimeSinceStartup;

                Repaint();
            }
        }

        public override void OnInspectorGUI()
        {
            Undo.RecordObject(hvrRender, "HvrRender");

            Inspector_HvrHeader.Draw();

            GUILayout.Space(4);

            DrawCustomInspector();
        }

        void DrawCustomInspector()
        {
            if (showTextures.Length != hvrRender.renderPairs.Count ||
                showDetails.Length != hvrRender.renderPairs.Count)
            {
                showDetails = new bool[hvrRender.renderPairs.Count];
                showTextures = new bool[hvrRender.renderPairs.Count];
            }

            hvrRender.renderMode = (HvrRender.eRenderMode)EditorGUILayout.Popup("Render Mode", (int)hvrRender.renderMode, renderModes);

            GUI.color = new Color(0.3f, 0.3f, 0.4f, 0.7f);

            GUILayout.BeginVertical(GUI.skin.GetStyle("button"));
            {
                GUI.color = editorColor;

                float total = hvrRender.stat_onPreRender.Avg();
                total += hvrRender.stat_onPostRender.Avg();

                GUILayout.Label("Performance");

                DrawInfo("Total", (total * 1000).ToString("f3") + "ms");

                GUI.color = new Color(1f, 1f, 1f, 0.5f);

                DrawInfo("OnPreRender", (hvrRender.stat_onPreRender.Avg() * 1000).ToString("f3") + "ms");
                DrawInfo("OnPostRender", (hvrRender.stat_onPostRender.Avg() * 1000).ToString("f3") + "ms");

                GUI.color = editorColor;
            }
            GUILayout.EndVertical();

            if (hvrRender.renderMode == HvrRender.eRenderMode.standard)
            {
                GUILayout.Space(4);

                GUI.color = new Color(0.9f, 0.9f, 0.9f, 1f);

                GUILayout.BeginVertical(GUI.skin.GetStyle("button"));
                {
                    GUI.color = editorColor;

                    EditorGUI.indentLevel++;
                    showActors = EditorGUILayout.Foldout(showActors, "Standard Rendering HvrActor Info (" + hvrRender.renderPairs.Count + ")");
                    EditorGUI.indentLevel--;

                    if (showActors)
                    {
                        for (int i = 0; i < hvrRender.renderPairs.Count; i++)
                        {
                            GUI.color = new Color(.3f, .3f, .3f, 1f);

                            GUILayout.BeginVertical(GUI.skin.GetStyle("helpbox"));
                            {
                                GUI.color = editorColor;

                                DrawActorPanel(hvrRender.renderPairs.ElementAt(i), i);
                            }
                            GUILayout.EndVertical();

                            GUILayout.Space(4);
                        }
                    }
                }
                GUILayout.EndVertical();
            }
        }

        void DrawActorPanel(KeyValuePair<HvrActor, HvrRender.RenderPackage> pair, int id)
        {
            float preRender = pair.Value.stat_preRender.Avg();
            float postRender = pair.Value.stat_postRender.Avg();

            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("HvrActor:", GUILayout.Width(40));

                    EditorGUILayout.ObjectField(pair.Key.gameObject, typeof(GameObject), true);

                    GUILayout.FlexibleSpace();

                    string perf = ((preRender + postRender) * 1000).ToString("f3") + "ms";

                    TextAnchor origAlignment = GUI.skin.label.alignment;
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(perf, GUILayout.Width(80));
                    GUI.skin.label.alignment = origAlignment;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUI.color = new Color(0f, 0f, 0f, 1f);

            GUILayout.BeginVertical(GUI.skin.GetStyle("helpbox"));
            {
                GUI.color = editorColor;

                EditorGUI.indentLevel++;
                showDetails[id] = EditorGUILayout.Foldout(showDetails[id], "Details");
                EditorGUI.indentLevel--;

                if (showDetails[id])
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.BeginVertical();
                        {
                            EditorGUILayout.LabelField("Performance", GUILayout.Width(120));

                            DrawInfo("Total", ((preRender + postRender) * 1000).ToString("f3") + "ms");

                            GUI.color = new Color(1f, 1f, 1f, 0.5f);

                            DrawInfo("OnPreRender", (preRender * 1000).ToString("f3") + "ms");
                            DrawInfo("OnPostRender", (postRender * 1000).ToString("f3") + "ms");

                            GUI.color = editorColor;
                        }
                        GUILayout.EndVertical();

                        GUILayout.FlexibleSpace();

                        GUILayout.BeginVertical();
                        {
                            EditorGUILayout.LabelField("Actor Settings", GUILayout.Width(120));

                            DrawInfo("Actor Render Method", pair.Key.actorRenderMethod.ToString());
                            DrawInfo("Asset Render Method", pair.Key.assetRenderMethod.ToString());
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();
                }

            }
            GUILayout.EndVertical();

            if (pair.Key.actorRenderMethod == HvrActor.eRenderMethod.standard)
            {
                GUI.color = new Color(0f, 0f, 0f, 1f);

                GUILayout.BeginVertical(GUI.skin.GetStyle("helpbox"));
                {
                    GUI.color = editorColor;

                    EditorGUI.indentLevel++;
                    showTextures[id] = EditorGUILayout.Foldout(showTextures[id], "Render Buffers");
                    EditorGUI.indentLevel--;

                    if (showTextures[id])
                    {
                        GUILayout.BeginHorizontal();
                        {
                            DrawBuffer("Color", pair.Value.color, 120, 100);
                            DrawBuffer("Depth", pair.Value.depth, 120, 100);
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndVertical();
            }
        }

        void DrawInfo(string header, string value)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(header, EditorStyles.miniLabel, GUILayout.Width(120));
                GUILayout.Label(value, EditorStyles.whiteMiniLabel);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }

        void DrawBuffer(string header, Texture tex, float texPreviewWidth = 140, float texPreviewHeight = 100)
        {
            GUILayout.BeginVertical(GUI.skin.GetStyle("helpbox"));
            {
                GUILayout.BeginVertical();
                {
                    Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(texPreviewWidth), GUILayout.Height(texPreviewHeight));
                    EditorGUI.DrawTextureTransparent(new Rect(rect.x, rect.y, rect.width, rect.height), tex, ScaleMode.ScaleToFit);

                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label(header);
                        GUILayout.Label("Width:" + tex.width, EditorStyles.miniLabel);
                        GUILayout.Label("Height:" + tex.height, EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
        }
    }
}
