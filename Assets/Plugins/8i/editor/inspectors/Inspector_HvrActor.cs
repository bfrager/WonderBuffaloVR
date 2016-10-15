using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HVR.Editor
{
    [CustomEditor(typeof(HvrActor))]
    public class Inspector_HvrActor : UnityEditor.Editor
    {
        HvrActor targetActor
        {
            get
            {
                return target as HvrActor;
            }
        }

        public static string[] unityRenderMethodStrings = new string[] { "Standard", "Direct" };
        public static string[] actorRenderMethodStrings = new string[] { "Point Sprite", "Point Blend" };
        public static string[] transparencyMethods = new string[] { "Alpha Blend", "Dither" };
        public static Texture2D icon_pause, icon_play, icon_stop;

        private MaterialEditor materialEditor;

        void OnEnable()
        {
            targetActor.editorUpdater = () => this.Repaint();

            if (targetActor.material != null)
            {
                // Create a new instance of the default MaterialEditor
                materialEditor = (MaterialEditor)CreateEditor(targetActor.material);
            }
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
            serializedObject.Update();

            Undo.RecordObject(targetActor, "HvrActor");

            Inspector_HvrHeader.Draw();

            GUILayout.Space(4);

            DrawHvrActorInspector(targetActor);

            if (targetActor.actorRenderMethod == HvrActor.eRenderMethod.standard)
                Inspector_HvrActor.DrawInspector_ActorMaterialEditor(materialEditor);
        }
        public static void DrawHvrActorInspector(HvrActor actor)
        {
            DrawInspector_Data(actor);

            GUILayout.Space(4);

            if (actor.hvrAsset != null)
            {
                EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                {
                    DrawInspector_UnityRenderMethod(actor);
                    DrawInspector_ActorRenderMethod(actor);
                }
                EditorGUI.indentLevel--;
                GUILayout.Space(4);

                DrawInspector_OcclusionCulling(actor);
                GUILayout.Space(4);

                DrawInspector_PlaybackBar(actor.hvrAsset);
            }

            if (GUI.changed)
            {
                if (!Application.isPlaying)
                {
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    SceneView.RepaintAll();
                }
            }
        }

        public static void DrawInspector_OcclusionCulling(HvrActor actor)
        {
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);

            bool useOcclusionCulling = actor.occlusionCullingEnabled;

            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            {
                useOcclusionCulling = EditorGUILayout.Toggle("Occlusion Culling", actor.occlusionCullingEnabled);
            }
            if (EditorGUI.EndChangeCheck())
            {
                actor.occlusionCullingEnabled = useOcclusionCulling;
            }

            if (actor.occlusionCullingEnabled)
            {
                float occlusionRadiusOffset = actor.occlusionCullingOffset;

                EditorGUI.BeginChangeCheck();
                {
                    EditorGUI.indentLevel++;
                    occlusionRadiusOffset = EditorGUILayout.Slider("Occlusion offset", actor.occlusionCullingOffset, 0, 2);
                    EditorGUI.indentLevel--;
                }
                if (EditorGUI.EndChangeCheck())
                {
                    actor.occlusionCullingOffset = occlusionRadiusOffset;
                }
            }

            EditorGUI.indentLevel--;
        }

        public static void DrawInspector_PlaybackBar(HvrAsset asset)
        {
            GUILayout.BeginVertical(GUI.skin.GetStyle("HelpBox"));
            {
                GUILayout.Space(2);
                DrawInspector_PlaybackBar_AssetTime(asset);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    DrawInspector_PlaybackBar_ControlButtons(asset);
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

        }
        static void DrawInspector_PlaybackBar_AssetTime(HvrAsset asset)
        {
            if (!icon_pause) icon_pause = (Texture2D)Resources.Load("8i/Editor/icons/hvrActor_pause");
            if (!icon_play) icon_play = (Texture2D)Resources.Load("8i/Editor/icons/hvrActor_play");
            if (!icon_stop) icon_stop = (Texture2D)Resources.Load("8i/Editor/icons/hvrActor_stop");

            Rect progressRect = EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(18), GUILayout.MaxHeight(18));
            {
                GUILayout.Space(18);

                Color backgroundcolor = new Color(0.1f, 0.1f, 0.1f, 1.0f);
                Color progressColor = new Color(0.0f, 0.37f, 0.62f, 1.0f);

                Handles.BeginGUI();
                {
                    Vector3[] points = new Vector3[]
                    {
                    new Vector3(progressRect.xMin,  progressRect.yMin, 0),
                    new Vector3(progressRect.xMax,  progressRect.yMin, 0),
                    new Vector3(progressRect.xMax, progressRect.yMax, 0),
                    new Vector3(progressRect.xMin, progressRect.yMax, 0)
                    };

                    Handles.color = backgroundcolor;
                    Handles.DrawAAConvexPolygon(points);
                }
                Handles.EndGUI();

                float progressX = Mathf.Lerp(progressRect.xMin, progressRect.xMax, (asset.GetCurrentTime() / asset.GetDuration()));

                // Progress
                Handles.BeginGUI();
                {
                    Vector3[] points = new Vector3[]
                    {
                    new Vector3(progressRect.xMin,  progressRect.yMin, 0),
                    new Vector3(progressX,  progressRect.yMin, 0),
                    new Vector3(progressX,  progressRect.yMax, 0),
                    new Vector3(progressRect.xMin,  progressRect.yMax, 0)
                    };

                    Handles.color = progressColor;
                    Handles.DrawAAConvexPolygon(points);
                }
                Handles.EndGUI();

                //ProgressLine
                Handles.BeginGUI();
                {
                    Vector3[] points = new Vector3[]
                    {
                    new Vector3(progressX, progressRect.yMin, 0),
                    new Vector3(progressX, progressRect.yMax, 0)
                    };
                    Handles.color = Color.white;

                    Handles.DrawLine(points[0], points[1]);
                }
                Handles.EndGUI();

                GUIStyle timeStyle = new GUIStyle("label");
                timeStyle.alignment = TextAnchor.MiddleCenter;
                timeStyle.normal.textColor = Color.white;
                EditorGUI.LabelField(progressRect, asset.GetCurrentTime().ToString("f2") + " / " + asset.GetDuration().ToString("f2"), timeStyle);
            }
            EditorGUILayout.EndHorizontal();

            float mouseXPos = Event.current.mousePosition.x;

            if (progressRect.Contains(Event.current.mousePosition))
            {
                Handles.BeginGUI();
                {
                    Vector2 startPoint = new Vector2(mouseXPos, progressRect.yMin);
                    Vector2 endPoint = new Vector2(mouseXPos, progressRect.yMax);

                    Vector2 startTangent = new Vector2(mouseXPos, progressRect.yMax);
                    Vector2 endTangent = new Vector2(mouseXPos, progressRect.yMin);
                    Handles.DrawBezier(startPoint, endPoint, startTangent, endTangent, Color.white, null, 3);
                }
                Handles.EndGUI();

                if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) && Event.current.button == 0)
                {
                    float mouseTimeProgress = Mathf.InverseLerp(progressRect.xMin, progressRect.xMax, mouseXPos);
                    float time = Mathf.Lerp(0, asset.GetDuration(), mouseTimeProgress);
                    asset.Seek(time);
                }
            }
        }
        static void DrawInspector_PlaybackBar_ControlButtons(HvrAsset asset)
        {
            GUILayoutOption[] buttonGLO = new GUILayoutOption[]{
                GUILayout.MinWidth(30),
                GUILayout.MinHeight(20),
                GUILayout.MaxHeight(20),
                GUILayout.MaxWidth(30),
            };

            Color origColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 1.0f);

            if (asset.IsPlaying())
            {
                if (GUILayout.Button(icon_pause, buttonGLO))
                    asset.Pause();
            }
            else
            {
                if (GUILayout.Button(icon_play, buttonGLO))
                    asset.Play();
            }

            if (GUILayout.Button(icon_stop, buttonGLO))
                asset.Stop();

            GUI.backgroundColor = origColor;
        }

        static void DrawInspector_Data(HvrActor _actor)
        {
            string pathToDataObject = AssetDatabase.GUIDToAssetPath(_actor.dataGuid);
            Object dataObject = AssetDatabase.LoadAssetAtPath(pathToDataObject, typeof(Object));

            EditorGUI.BeginChangeCheck();
            {
                EditorGUI.indentLevel++;
                dataObject = EditorGUILayout.ObjectField("Data", dataObject, typeof(Object), false);
                EditorGUI.indentLevel--;
            }
            if (EditorGUI.EndChangeCheck())
            {
                string path = "";
                string guid = "";

                if (dataObject != null)
                {
                    path = AssetDatabase.GetAssetPath(dataObject);
                    guid = AssetDatabase.AssetPathToGUID(path);
                }

                _actor.dataGuid = guid;

                if (string.IsNullOrEmpty(guid) || string.IsNullOrEmpty(path))
                {
                    // In the case the user has set an empty string for the dataGuid clear the current asset
                    _actor.SetAsset(null);
                }
                else
                {
                    HvrAsset asset = new HvrAsset(path);
                    _actor.SetAsset(asset);
                }
            }
        }

        public static void DrawInspector_UnityRenderMethod(HvrActor actor)
        {
            actor.actorRenderMethod = (HvrActor.eRenderMethod)EditorGUILayout.Popup("Actor Render Method", (int)actor.actorRenderMethod, unityRenderMethodStrings);
        }
        public static void DrawInspector_ActorRenderMethod(HvrActor actor)
        {
            actor.assetRenderMethod = (HvrAsset.eRenderMethod)EditorGUILayout.Popup("Asset Render Method", (int)actor.assetRenderMethod, actorRenderMethodStrings);
        }

        public static void DrawInspector_ActorMaterialEditor(MaterialEditor _materialEditor)
        {
            if (_materialEditor != null)
            {
                // Draw the material's foldout and the material shader field
                // Required to call _materialEditor.OnInspectorGUI ();
                _materialEditor.DrawHeader();

                // Draw the material properties
                // Works only if the foldout of _materialEditor.DrawHeader () is open
                _materialEditor.OnInspectorGUI();
            }
        }
    }
}
