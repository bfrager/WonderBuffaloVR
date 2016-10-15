using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace HVR.Editor
{
    [Serializable]
    public class EditorAboutWindow : UnityEditor.EditorWindow
    {
        public static EditorAboutWindow instance;

        private const string TITLE = "8i | About";
        private const string MENU_ITEM = "8i/About";

        private AnimBool showChangeLog;

        private Texture2D _8iLogo;
        private TextAsset changeLog;

        private Vector2 scrollPosition = new Vector2();
        private Vector2 changelogPosition = new Vector2();

        List<String[]> pluginInfo = new List<String[]>();
        List<String[]> hvrEngineInfo = new List<String[]>();

        EditorAboutWindow()
        {
            instance = this;
        }

        /// <summary>
        /// Show the 8i About Window
        /// </summary>
        [MenuItem(MENU_ITEM, false, 100)]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(EditorAboutWindow), false, TITLE);
        }

        public void Awake()
        {
            this.minSize = new Vector2(400, 30);
        }

        public void OnEnable()
        {
            showChangeLog = new AnimBool(true, Repaint);

            _8iLogo = Resources.Load<Texture2D>("8i/editor/icons/8iLogo");
            changeLog = Resources.Load<TextAsset>("8i/editor/plugin_changelog");

            pluginInfo.Add(new string[] { "Version: ", HVR.VersionInfo.VERSION });
            pluginInfo.Add(new string[] { "Build Date: ", HVR.VersionInfo.BUILD_DATE });
            pluginInfo.Add(new string[] { "Hash: ", HVR.VersionInfo.GIT_HASH });

            StringBuilder stringBuilder = new StringBuilder(256);
            if (HVR.Interface.HvrPlayerInterfaceAPI.Player_GetInfo("VERSION", stringBuilder, stringBuilder.Capacity + 1))
                hvrEngineInfo.Add(new string[] { "Version: ", stringBuilder.ToString() });

            stringBuilder = new StringBuilder(256);
            if (HVR.Interface.HvrPlayerInterfaceAPI.Player_GetInfo("BUILD_DATE", stringBuilder, stringBuilder.Capacity + 1))
                hvrEngineInfo.Add(new string[] { "Build Date: ", stringBuilder.ToString() });

            stringBuilder = new StringBuilder(256);
            if (HVR.Interface.HvrPlayerInterfaceAPI.Player_GetInfo("GIT_HASH", stringBuilder, stringBuilder.Capacity + 1))
                hvrEngineInfo.Add(new string[] { "Hash: ", stringBuilder.ToString() });
        }

        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, false, false);
            {
                DrawAboutSection();

                DrawChanges();

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawAboutSection()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(_8iLogo, GUILayout.Height(32), GUILayout.Width(60));

                GUILayout.FlexibleSpace();

                // Website
                if (GUILayout.Button("8i.com", GUILayout.Height(32), GUILayout.Width(60)))
                    Application.OpenURL("http://8i.com");

                // Support
                if (GUILayout.Button("support", GUILayout.Height(32), GUILayout.Width(60)))
                    Application.OpenURL("mailto:support@8i.com");
            }
            GUILayout.EndHorizontal();

            if (HVR.VersionInfo.VERSION == "-1")
            {
                GUI.color = Color.red;

                GUILayout.BeginHorizontal("box");
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("8i USE ONLY. DO NOT DISTRIBUTE", EditorStyles.whiteLargeLabel, GUILayout.Width(80));
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
            }

            string info = "Plugin:";

            for (int i = 0; i < pluginInfo.Count; i++)
            {
                info += "\n";
                info += pluginInfo[i][0] + pluginInfo[i][1];
            }
            info += "\n";

            info += "\n";
            info += "HVR Engine:";

            for (int i = 0; i < hvrEngineInfo.Count; i++)
            {
                info += "\n";
                info += hvrEngineInfo[i][0] + hvrEngineInfo[i][1];
            }

            GUILayout.TextArea(info);
        }

        private void DrawChanges()
        {
            Rect baseRect = EditorGUILayout.GetControlRect();
            GUI.Box(new Rect(baseRect.x - 4, baseRect.y, baseRect.width + 8, baseRect.height), string.Empty, EditorStyles.toolbar);

            showChangeLog.target = EditorGUI.Foldout(baseRect, showChangeLog.target, "Changelog");

            //Extra block that can be toggled on and off.
            using (var group = new EditorGUILayout.FadeGroupScope(showChangeLog.faded))
            {
                if (group.visible)
                {
                    changelogPosition = GUILayout.BeginScrollView(changelogPosition, false, true);
                    {
                        GUILayout.TextArea(changeLog.text);
                    }
                    GUILayout.EndScrollView();
                }
            }
        }

        public List<GameObject> GetAllChildren(GameObject parent)
        {
            List<GameObject> go = new List<GameObject>();

            foreach (Transform child in parent.transform)
            {
                go.Add(child.gameObject);
                go.AddRange(GetAllChildren(child.gameObject));
            }

            return go;
        }
    }
}


