using UnityEngine;
using System;

namespace HVR
{
    [ExecuteInEditMode]
    [AddComponentMenu("8i/HVR Color Grading")]
    public class HvrColorGrading : MonoBehaviour
    {
        public static HvrColorGrading instance;

        #region Attributes
        [AttributeUsage(AttributeTargets.Field)]
        public class SettingsGroup : Attribute
        { }

        public class IndentedGroup : PropertyAttribute
        { }

        public class ChannelMixer : PropertyAttribute
        { }

        public class ColorPrimariesGroup : PropertyAttribute
        { }

        public class Curve : PropertyAttribute
        {
            public Color color = Color.white;

            public Curve()
            { }

            public Curve(float r, float g, float b, float a) // Can't pass a struct in an attribute
            {
                color = new Color(r, g, b, a);
            }
        }
        #endregion

        #region Settings

        [Serializable]
        public struct ColorPrimariesSettings
        {
            [ColorUsage(false)]
            public Vector4 lift;

            [ColorUsage(false)]
            public Vector4 gamma;

            [ColorUsage(false)]
            public Vector4 gain;

            public static ColorPrimariesSettings defaultSettings
            {
                get
                {
                    return new ColorPrimariesSettings
                    {
                        lift = Vector4.zero,
                        gamma = Vector4.one,
                        gain = Vector4.one
                    };
                }
            }
        }

        public struct ColorPrimariesRGBMinMax
        {
            public Vector2 lift;
            public Vector2 gamma;
            public Vector2 gain;

            public static ColorPrimariesRGBMinMax defaultSettings
            {
                get
                {
                    return new ColorPrimariesRGBMinMax
                    {
                        lift = new Vector2(-1.0f, 1.0f),
                        gamma = new Vector2(0.0f, 4.5f),
                        gain = new Vector2(0.0f, 8.0f)
                    };
                }
            }
        }

        [Serializable]
        public struct BasicsSettings
        {
            [Range(-0.5f, 0.5f), Tooltip("Shift the hue of all colors.")]
            public float hue;

            [Range(0f, 2f), Tooltip("Pushes the intensity of all colors.")]
            public float saturation;

            [Range(0f, 5f), Tooltip("Brightens or darkens all colors.")]
            public float value;

            [Space, Range(0f, 2f), Tooltip("Expands or shrinks the overall range of tonal values.")]
            public float contrast;

            [Range(-1f, 1f), Tooltip("Adjusts the saturation so that clipping is minimized as colors approach full saturation.")]
            public float vibrance;

            public static BasicsSettings defaultSettings
            {
                get
                {
                    return new BasicsSettings
                    {
                        contrast = 1f,
                        hue = 0f,
                        saturation = 1f,
                        value = 1f,
                        vibrance = 0f
                    };
                }
            }
        }

        [Serializable]
        public struct ChannelMixerSettings
        {
            public int currentChannel;
            public Vector3[] channels;

            public static ChannelMixerSettings defaultSettings
            {
                get
                {
                    return new ChannelMixerSettings
                    {
                        currentChannel = 0,
                        channels = new[]
                        {
                            new Vector3(1f, 0f, 0f),
                            new Vector3(0f, 1f, 0f),
                            new Vector3(0f, 0f, 1f)
                        }
                    };
                }
            }
        }

        [Serializable]
        public struct CurvesSettings
        {
            [Curve]
            public AnimationCurve master;

            [Curve(1f, 0f, 0f, 1f)]
            public AnimationCurve red;

            [Curve(0f, 1f, 0f, 1f)]
            public AnimationCurve green;

            [Curve(0f, 1f, 1f, 1f)]
            public AnimationCurve blue;

            public static CurvesSettings defaultSettings
            {
                get
                {
                    return new CurvesSettings
                    {
                        master = defaultCurve,
                        red = defaultCurve,
                        green = defaultCurve,
                        blue = defaultCurve
                    };
                }
            }

            public static AnimationCurve defaultCurve
            {
                get { return new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)); }
            }
        }

        public enum ColorGradingPrecision
        {
            Normal = 16,
            High = 32
        }

        [Serializable]
        public struct ColorGradingSettings
        {
            [Tooltip("Internal LUT precision. \"Normal\" is 256x16, \"High\" is 1024x32. Prefer \"Normal\" on mobile devices.")]
            public ColorGradingPrecision precision;

            [Space, ColorPrimariesGroup]
            public ColorPrimariesSettings colorPrimaries;

            [Space, IndentedGroup]
            public BasicsSettings basics;

            [Space, ChannelMixer]
            public ChannelMixerSettings channelMixer;

            [Space, IndentedGroup]
            public CurvesSettings curves;

            [Space, Tooltip("Use dithering to try and minimize color banding in dark areas.")]
            public bool useDithering;

            public static ColorGradingSettings defaultSettings
            {
                get
                {
                    return new ColorGradingSettings
                    {
                        useDithering = false,
                        precision = ColorGradingPrecision.Normal,
                        colorPrimaries = ColorPrimariesSettings.defaultSettings,
                        basics = BasicsSettings.defaultSettings,
                        channelMixer = ChannelMixerSettings.defaultSettings,
                        curves = CurvesSettings.defaultSettings
                    };
                }
            }

            internal void Reset()
            {
                colorPrimaries = ColorPrimariesSettings.defaultSettings;
                curves = CurvesSettings.defaultSettings;
            }
        }

        [SerializeField, SettingsGroup]
        private ColorGradingSettings m_ColorGrading = ColorGradingSettings.defaultSettings;
        public ColorGradingSettings colorGrading
        {
            get { return m_ColorGrading; }
            set
            {
                m_ColorGrading = value;
                SetDirty();
            }
        }
        #endregion

        private Texture2D m_IdentityLut;
        private RenderTexture m_InternalLut;
        private Texture2D m_CurveTexture;

        private Texture2D identityLut
        {
            get
            {
                if (m_IdentityLut == null || m_IdentityLut.height != lutSize)
                {
                    DestroyImmediate(m_IdentityLut);
                    m_IdentityLut = GenerateIdentityLut(lutSize);
                }

                return m_IdentityLut;
            }
        }

        private RenderTexture internalLutRt
        {
            get
            {
                if (m_InternalLut == null || !m_InternalLut.IsCreated() || m_InternalLut.height != lutSize)
                {
                    DestroyImmediate(m_InternalLut);
                    m_InternalLut = new RenderTexture(lutSize * lutSize, lutSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
                    {
                        name = "Internal LUT",
                        filterMode = FilterMode.Bilinear,
                        anisoLevel = 0,
                        hideFlags = HideFlags.DontSave
                    };
                }

                return m_InternalLut;
            }
        }

        private Texture2D curveTexture
        {
            get
            {
                if (m_CurveTexture == null)
                {
                    m_CurveTexture = new Texture2D(256, 1, TextureFormat.ARGB32, false, true)
                    {
                        name = "Curve texture",
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear,
                        anisoLevel = 0,
                        hideFlags = HideFlags.DontSave
                    };
                }

                return m_CurveTexture;
            }
        }

        [SerializeField]
        private Shader m_Shader;
        public Shader shader
        {
            get
            {
                if (m_Shader == null)
                {
                    m_Shader = Shader.Find("Hidden/8i/HVRColorGrading");
                    m_Shader.hideFlags = HideFlags.DontSave;
                }

                return m_Shader;
            }
        }

        private Material m_Material;
        public Material material
        {
            get
            {
                if (m_Material == null)
                    m_Material = new Material(shader);// ImageEffectHelper.CheckShaderAndCreateMaterial(shader);

                return m_Material;
            }
        }

        public bool isGammaColorSpace
        {
            get { return QualitySettings.activeColorSpace == ColorSpace.Gamma; }
        }

        public int lutSize
        {
            get { return (int)colorGrading.precision; }
        }

        private enum Pass
        {
            LutGen,
            ColorGrading,
        }

        public bool validUserLutSize { get; private set; }

        private ColorSpace m_previousColorSpace = ColorSpace.Uninitialized;

        private bool m_Dirty = true;

        public void SetDirty()
        {
            m_Dirty = true;
        }

        void OnEnable()
        {
            instance = this;

            SetDirty();
        }

        private void OnDisable()
        {
            if (m_Material != null)
                DestroyImmediate(m_Material);

            if (m_IdentityLut != null)
                DestroyImmediate(m_IdentityLut);

            if (m_InternalLut != null)
                DestroyImmediate(internalLutRt);

            if (m_CurveTexture != null)
                DestroyImmediate(m_CurveTexture);

            m_Material = null;
            m_IdentityLut = null;
            m_InternalLut = null;
            m_CurveTexture = null;
        }

        private void OnValidate()
        {
            SetDirty();
        }

        private static Texture2D GenerateIdentityLut(int dim)
        {
            Color[] newC = new Color[dim * dim * dim];
            float oneOverDim = 1f / ((float)dim - 1f);

            for (int i = 0; i < dim; i++)
                for (int j = 0; j < dim; j++)
                    for (int k = 0; k < dim; k++)
                        newC[i + (j * dim) + (k * dim * dim)] = new Color((float)i * oneOverDim, Mathf.Abs((float)k * oneOverDim), (float)j * oneOverDim, 1f);

            Texture2D tex2D = new Texture2D(dim * dim, dim, TextureFormat.RGB24, false, true)
            {
                name = "Identity LUT",
                filterMode = FilterMode.Bilinear,
                anisoLevel = 0,
                hideFlags = HideFlags.DontSave
            };
            tex2D.SetPixels(newC);
            tex2D.Apply();

            return tex2D;
        }

        private void GenCurveTexture()
        {
            AnimationCurve master = colorGrading.curves.master;
            AnimationCurve red = colorGrading.curves.red;
            AnimationCurve green = colorGrading.curves.green;
            AnimationCurve blue = colorGrading.curves.blue;

            Color[] pixels = new Color[256];

            for (float i = 0f; i <= 1f; i += 1f / 255f)
            {
                float m = Mathf.Clamp(master.Evaluate(i), 0f, 1f);
                float r = Mathf.Clamp(red.Evaluate(i), 0f, 1f);
                float g = Mathf.Clamp(green.Evaluate(i), 0f, 1f);
                float b = Mathf.Clamp(blue.Evaluate(i), 0f, 1f);
                pixels[(int)Mathf.Floor(i * 255f)] = new Color(r, g, b, m);
            }

            curveTexture.SetPixels(pixels);
            curveTexture.Apply();
        }

        [ImageEffectTransformsToLDR]
        public void Grade(RenderTexture source)
        {
            RenderTexture tempRT = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

            // Check whether the color space has been changed
            if (m_previousColorSpace != QualitySettings.activeColorSpace)
            {
                m_previousColorSpace = QualitySettings.activeColorSpace;

                // Force recreation of lut texture
                if (m_InternalLut != null)
                    DestroyImmediate(internalLutRt);

                SetDirty();
            }

            material.shaderKeywords = null;

            material.EnableKeyword("ENABLE_COLOR_GRADING");

            if (m_Dirty || !m_InternalLut.IsCreated())
            {
                GenCurveTexture();

                material.SetVector("_Lift", colorGrading.colorPrimaries.lift);
                material.SetVector("_Gamma", colorGrading.colorPrimaries.gamma);
                material.SetVector("_Gain", colorGrading.colorPrimaries.gain);

                material.SetVector("_ContrastGainGamma", new Vector3(colorGrading.basics.contrast, colorGrading.colorPrimaries.gain.x, 1f / colorGrading.colorPrimaries.gamma.x));
                material.SetFloat("_Contrast", colorGrading.basics.contrast);
                material.SetFloat("_Vibrance", colorGrading.basics.vibrance);
                material.SetVector("_ContrastGainGamma", new Vector3(colorGrading.basics.contrast, 1.0f, 1.0f));
                material.SetVector("_HSV", new Vector4(colorGrading.basics.hue, colorGrading.basics.saturation, colorGrading.basics.value));
                material.SetVector("_ChannelMixerRed", colorGrading.channelMixer.channels[0]);
                material.SetVector("_ChannelMixerGreen", colorGrading.channelMixer.channels[1]);
                material.SetVector("_ChannelMixerBlue", colorGrading.channelMixer.channels[2]);
                material.SetTexture("_CurveTex", curveTexture);
                internalLutRt.MarkRestoreExpected();
                Graphics.Blit(identityLut, internalLutRt, material, (int)Pass.LutGen);
                m_Dirty = false;
            }

            material.EnableKeyword("ENABLE_COLOR_GRADING");

            if (colorGrading.useDithering)
                material.EnableKeyword("ENABLE_DITHERING");

            material.SetTexture("_InternalLutTex", internalLutRt);
            material.SetVector("_InternalLutParams", new Vector3(1f / internalLutRt.width, 1f / internalLutRt.height, internalLutRt.height - 1f));

            Graphics.Blit(source, tempRT, material, (int)Pass.ColorGrading);

            Graphics.Blit(tempRT, source);

            RenderTexture.ReleaseTemporary(tempRT);
        }
    }
}
