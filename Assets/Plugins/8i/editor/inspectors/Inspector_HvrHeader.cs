using HVR;
using HVR.Utils;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace HVR.Editor
{
    public static class Inspector_HvrHeader
    {
        static GUIStyle m_alignLeftMiddleStyle;
        static Texture m_logoTex;

        public static GUIStyle alignLeftMiddleStyle
        {
            get
            {
                if (m_alignLeftMiddleStyle == null)
                {
                    m_alignLeftMiddleStyle = new GUIStyle(GUI.skin.GetStyle("Label"))
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 10
                    };
                }

                return m_alignLeftMiddleStyle;
            }
        }
        static Texture logoTex
        {
            get
            {
                if (m_logoTex == null)
                {
                    m_logoTex = Resources.Load("8i/Editor/icons/8iLogo") as Texture;
                }
                return m_logoTex;
            }
        }

        public static void Draw()
        {
            Color editorColor = GUI.color;

            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);

            GUILayout.BeginHorizontal(GUI.skin.GetStyle("TextArea"));
            {
                GUI.color = editorColor;

                GUILayout.Label(logoTex, alignLeftMiddleStyle, GUILayout.Height(20), GUILayout.Width(20));

                GUILayout.Label("Version: " + HVR.VersionInfo.VERSION, alignLeftMiddleStyle, GUILayout.Height(20));

                if (HVR.VersionInfo.VERSION == "-1")
                {
                    GUI.color = Color.red;
                    EditorGUILayout.LabelField("8i USE ONLY", alignLeftMiddleStyle, GUILayout.Height(20));
                    GUI.color = editorColor;
                }

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }
    }
}
