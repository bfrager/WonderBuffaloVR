using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HVR.Editor
{
    [CanEditMultipleObjects, CustomEditor(typeof(HvrColorGrading))]
    public class Inspector_HvrColorGrading : UnityEditor.Editor
    {
        #region Property drawers
        [CustomPropertyDrawer(typeof(HvrColorGrading.ColorPrimariesGroup))]
        private class ColorWheelGroupDrawer : PropertyDrawer
        {
            private static GUIStyle s_guistyleButton;

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return 0;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                if (s_guistyleButton == null)
                {
                    s_guistyleButton = new GUIStyle(GUI.skin.GetStyle("Button"))
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = new GUIStyleState()
                        {
                            background = icon_reset_normal
                        },
                        hover = new GUIStyleState()
                        {
                            background = icon_reset_hover
                        },
                        active = new GUIStyleState()
                        {
                            background = icon_reset_active
                        },
                        padding = new RectOffset(2, 2, 2, 2)
                    };
                }

                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

                Color oGuiColor = GUI.color;


                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();

                    foreach (SerializedProperty prop in property)
                    {
                        if (prop.propertyType == SerializedPropertyType.Vector4)
                        {
                            GUI.color = new Color(0.2f, 0.2f, 0.2f, 1.0f);

                            Rect area = EditorGUILayout.BeginVertical(GUI.skin.GetStyle("HelpBox"), GUILayout.MaxHeight(200), GUILayout.MaxWidth(120), GUILayout.MinWidth(120));
                            {
                                GUI.color = oGuiColor;

                                float rgbMin = 0;
                                float rgbMax = 1;

                                System.Type type = HvrColorGrading.ColorPrimariesRGBMinMax.defaultSettings.GetType();
                                FieldInfo[] fields = type.GetFields();
                                foreach (FieldInfo fi in fields)
                                {
                                    if (prop.name == fi.Name)
                                    {
                                        object val = fi.GetValue(HvrColorGrading.ColorPrimariesRGBMinMax.defaultSettings);

                                        if (val.GetType() == typeof(Vector2))
                                        {
                                            rgbMin = ((Vector2)val).x;
                                            rgbMax = ((Vector2)val).y;
                                        }
                                    }
                                }

                                prop.vector4Value = Primaries.DoGUI(area, prop.displayName, prop.vector4Value, rgbMin, rgbMax);
                            }
                            EditorGUILayout.EndVertical();

                            // Reset
                            if (GUI.Button(new Rect(area.xMax - 22, area.y + 2, 20, 20), "", s_guistyleButton))
                            {
                                System.Type type = HvrColorGrading.ColorPrimariesSettings.defaultSettings.GetType();
                                FieldInfo[] fields = type.GetFields();

                                foreach (FieldInfo fi in fields)
                                {
                                    if (prop.name == fi.Name)
                                    {
                                        object val = fi.GetValue(HvrColorGrading.ColorPrimariesSettings.defaultSettings);

                                        if (val.GetType() == typeof(Vector4))
                                            prop.vector4Value = (Vector4)val;
                                    }
                                }
                            }
                        }
                    }

                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        [CustomPropertyDrawer(typeof(HvrColorGrading.IndentedGroup))]
        private class IndentedGroupDrawer : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return 0f;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

                EditorGUI.indentLevel++;

                foreach (SerializedProperty prop in property)
                    EditorGUILayout.PropertyField(prop);

                EditorGUI.indentLevel--;
            }
        }

        [CustomPropertyDrawer(typeof(HvrColorGrading.ChannelMixer))]
        private class ChannelMixerDrawer : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return 0f;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                // TODO: Hardcoded variable names, rewrite this function
                if (property.type != "ChannelMixerSettings")
                    return;

                SerializedProperty currentChannel = property.FindPropertyRelative("currentChannel");
                int intCurrentChannel = currentChannel.intValue;

                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PrefixLabel("Channel");
                    if (GUILayout.Toggle(intCurrentChannel == 0, "Red", EditorStyles.miniButtonLeft)) intCurrentChannel = 0;
                    if (GUILayout.Toggle(intCurrentChannel == 1, "Green", EditorStyles.miniButtonMid)) intCurrentChannel = 1;
                    if (GUILayout.Toggle(intCurrentChannel == 2, "Blue", EditorStyles.miniButtonRight)) intCurrentChannel = 2;
                }
                EditorGUILayout.EndHorizontal();

                SerializedProperty serializedChannel = property.FindPropertyRelative("channels").GetArrayElementAtIndex(intCurrentChannel);
                currentChannel.intValue = intCurrentChannel;

                Vector3 v = serializedChannel.vector3Value;
                v.x = EditorGUILayout.Slider("Red", v.x, -2f, 2f);
                v.y = EditorGUILayout.Slider("Green", v.y, -2f, 2f);
                v.z = EditorGUILayout.Slider("Blue", v.z, -2f, 2f);
                serializedChannel.vector3Value = v;

                EditorGUI.indentLevel--;
            }
        }

        [CustomPropertyDrawer(typeof(HvrColorGrading.Curve))]
        private class CurveDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                HvrColorGrading.Curve attribute = (HvrColorGrading.Curve)base.attribute;

                if (property.propertyType != SerializedPropertyType.AnimationCurve)
                {
                    EditorGUI.LabelField(position, label.text, "Use ClampCurve with an AnimationCurve.");
                    return;
                }

                property.animationCurveValue = EditorGUI.CurveField(position, label, property.animationCurveValue, attribute.color, new Rect(0f, 0f, 1f, 1f));
            }
        }
        #endregion

        #region Styling
        private static Styles s_Styles;
        private class Styles
        {
            public GUIStyle thumb2D = "ColorPicker2DThumb";
            public GUIStyle header = "ShurikenModuleTitle";
            public GUIStyle headerCheckbox = "ShurikenCheckMark";
            public Vector2 thumb2DSize;

            internal Styles()
            {
                thumb2DSize = new Vector2(
                        !Mathf.Approximately(thumb2D.fixedWidth, 0f) ? thumb2D.fixedWidth : thumb2D.padding.horizontal,
                        !Mathf.Approximately(thumb2D.fixedHeight, 0f) ? thumb2D.fixedHeight : thumb2D.padding.vertical
                        );

                header.font = (new GUIStyle("Label")).font;
                header.border = new RectOffset(15, 7, 4, 4);
                header.fixedHeight = 22;
                header.contentOffset = new Vector2(20f, -2f);
            }
        }

        public static readonly Color masterCurveColor = new Color(1f, 1f, 1f, 2f);
        public static readonly Color redCurveColor = new Color(1f, 0f, 0f, 2f);
        public static readonly Color greenCurveColor = new Color(0f, 1f, 0f, 2f);
        public static readonly Color blueCurveColor = new Color(0f, 1f, 1f, 2f);

        public static Texture2D icon_reset_normal;
        public static Texture2D icon_reset_hover;
        public static Texture2D icon_reset_active;

        #endregion

        private HvrColorGrading concreteTarget
        {
            get { return target as HvrColorGrading; }
        }

        // settings group <setting, property reference>
        private Dictionary<FieldInfo, List<SerializedProperty>> m_GroupFields = new Dictionary<FieldInfo, List<SerializedProperty>>();

        private void PopulateMap(FieldInfo group)
        {
            var searchPath = group.Name + ".";
            foreach (var setting in group.FieldType.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                List<SerializedProperty> settingsGroup;
                if (!m_GroupFields.TryGetValue(group, out settingsGroup))
                {
                    settingsGroup = new List<SerializedProperty>();
                    m_GroupFields[group] = settingsGroup;
                }

                var property = serializedObject.FindProperty(searchPath + setting.Name);
                if (property != null)
                    settingsGroup.Add(property);
            }
        }

        private void OnEnable()
        {
            var settingsGroups = typeof(HvrColorGrading).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.GetCustomAttributes(typeof(HvrColorGrading.SettingsGroup), false).Any());

            foreach (var settingGroup in settingsGroups)
                PopulateMap(settingGroup);

            if (icon_reset_normal == null)
                icon_reset_normal = Resources.Load("8i/Editor/icons/hvrgrade_reset_normal") as Texture2D;

            if (icon_reset_hover == null)
                icon_reset_hover = Resources.Load("8i/Editor/icons/hvrgrade_reset_hover") as Texture2D;

            if (icon_reset_active == null)
                icon_reset_active = Resources.Load("8i/Editor/icons/hvrgrade_reset_active") as Texture2D;
        }

        private bool Header(SerializedProperty group, SerializedProperty enabledField)
        {
            var display = group == null || group.isExpanded;
            var enabled = enabledField != null && enabledField.boolValue;
            var title = group == null ? "Unknown Group" : ObjectNames.NicifyVariableName(group.displayName);

            Rect rect = GUILayoutUtility.GetRect(16f, 22f, s_Styles.header);
            GUI.Box(rect, title, s_Styles.header);

            Rect toggleRect = new Rect(rect.x + 4f, rect.y + 4f, 13f, 13f);
            if (Event.current.type == EventType.Repaint)
                s_Styles.headerCheckbox.Draw(toggleRect, false, false, enabled, false);

            Event e = Event.current;
            if (e.type == EventType.MouseDown)
            {
                if (toggleRect.Contains(e.mousePosition) && enabledField != null)
                {
                    enabledField.boolValue = !enabledField.boolValue;
                    e.Use();
                }
                else if (rect.Contains(e.mousePosition) && group != null)
                {
                    display = !display;
                    group.isExpanded = !group.isExpanded;
                    e.Use();
                }
            }
            return display;
        }

        private void DrawFields()
        {
            HvrActor actor = concreteTarget.gameObject.GetComponent<HvrActor>();

            if (actor)
            {
                if (actor.actorRenderMethod == HvrActor.eRenderMethod.direct)
                    EditorGUILayout.HelpBox("The Actor render method is set to 'Direct'\nThis component will have no effect", MessageType.Warning);
            }

            foreach (var group in m_GroupFields)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.Space(3);
                        foreach (var field in group.Value)
                            EditorGUILayout.PropertyField(field);
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
        }

        public override void OnInspectorGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            serializedObject.Update();

            Inspector_HvrHeader.Draw();

            DrawFields();

            serializedObject.ApplyModifiedProperties();
        }

        public static class Primaries
        {
            // hue Wheel
            private static GUIStyle s_centeredLabelStyle;
            private static GUIStyle s_centeredSliderStyle;
            private static GUIStyle s_sliderThumbStyle;

            public static Vector4 DoGUI(Rect area, string title, Vector4 vec, float rgbMin, float rgbMax)
            {
                if (s_centeredLabelStyle == null)
                {
                    s_centeredLabelStyle = new GUIStyle(GUI.skin.GetStyle("Label"))
                    {
                        alignment = TextAnchor.MiddleCenter
                    };
                }

                if (s_centeredSliderStyle == null)
                {
                    s_centeredSliderStyle = new GUIStyle(GUI.skin.GetStyle("VerticalSlider"))
                    {
                        alignment = TextAnchor.UpperCenter
                    };
                }

                if (s_sliderThumbStyle == null)
                    s_sliderThumbStyle = new GUIStyle(GUI.skin.GetStyle("VerticalSliderThumb"));

                GUILayout.Label(title, s_centeredLabelStyle);

                ColorPickerHDRConfig hdrConfig = new ColorPickerHDRConfig(rgbMin, rgbMax, 0, 1);

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    GUIContent content = new GUIContent();
                    vec = EditorGUILayout.ColorField(content, vec, false, false, true, hdrConfig, GUILayout.MaxWidth(80), GUILayout.Height(20));
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginVertical("box");
                {
                    // HSV Value Slider
                    {
                        Color color = vec;

                        float h = 0;
                        float s = 0;
                        float v = 0;

                        Color.RGBToHSV(color, out h, out s, out v);

                        if (v == 0 && color.grayscale != 0)
                            v = color.grayscale;

                        v = GUILayout.HorizontalSlider(v, rgbMin, rgbMax);
                        vec = Color.HSVToRGB(h, s, v, true);
                    }

                    // RGB Sliders
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        vec.x = DrawColorSlider("R", vec.x, rgbMin, rgbMax, Color.red);
                        vec.y = DrawColorSlider("G", vec.y, rgbMin, rgbMax, Color.green);
                        vec.z = DrawColorSlider("B", vec.z, rgbMin, rgbMax, Color.blue);
                        GUILayout.FlexibleSpace();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();

                vec.x = Mathf.Clamp(vec.x, rgbMin, rgbMax);
                vec.y = Mathf.Clamp(vec.y, rgbMin, rgbMax);
                vec.z = Mathf.Clamp(vec.z, rgbMin, rgbMax);

                return vec;
            }

            static float DrawColorSlider(string title, float val, float min, float max, Color colour)
            {
                Color oGuiColor = GUI.color;

                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(40));
                {
                    GUILayout.FlexibleSpace();

                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();
                            GUI.color = colour;
                            val = GUILayout.VerticalSlider(val, max, min, s_centeredSliderStyle, s_sliderThumbStyle, GUILayout.MinWidth(20));
                            GUI.color = oGuiColor;
                            GUILayout.FlexibleSpace();
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();
                            val = EditorGUILayout.FloatField(val, GUILayout.MaxWidth(30));
                            GUILayout.FlexibleSpace();
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.LabelField(title, s_centeredLabelStyle, GUILayout.Width(20));
                            GUILayout.FlexibleSpace();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();

                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                return val;
            }
        }
    }
}
