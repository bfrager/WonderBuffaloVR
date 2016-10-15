using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using HVR.Interface;
using HVR.Utils;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HVR
{
    public class HvrShadowCasterCollector
    {
        static HvrShadowCasterCollector m_Instance;
        HashSet<HvrShadowCaster> m_ShadowCasters = new HashSet<HvrShadowCaster>();

        public static HvrShadowCasterCollector instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new HvrShadowCasterCollector();
                }
                return m_Instance;
            }
        }

        public HvrShadowCasterCollector()
        {
        }

        public HashSet<HvrShadowCaster> GetShadowCasters()
        {
            return m_ShadowCasters;
        }

        internal void Add(HvrShadowCaster shadowCaster)
        {
            Remove(shadowCaster);
            m_ShadowCasters.Add(shadowCaster);
        }

        internal HvrShadowCaster Remove(HvrShadowCaster shadowCaster)
        {
            if (m_ShadowCasters.Remove(shadowCaster))
            {
                return shadowCaster;
            }
            return null;
        }
    }

    [ExecuteInEditMode]
    [RequireComponent(typeof(Light))]
    public class HvrShadowCaster : MonoBehaviour
    {
        public enum ShadowQuality
        {
            Low = 0,
            Medium,
            High,
            VeryHigh
        }

        static int[] BASE_RESOLUTION = new int[]
        {
            256, 512, 1024, 2048
        };

        static int[] BASE_RESOLUTION_FOR_POINT = new int[]
        {
            128, 256, 512, 1024
        };

        const int MAX_VIEWPORT_NUM_FOR_DIRECTIONAL = 8;
        const int MAX_VIEWPORT_NUM = 2;

        Light m_DependentLight;

        RenderTexture m_DepthmapCopy; // handled by SyncDepthmapCopy()

        RenderTexture m_DepthmapDirectionalSpecial_GeomOnly;
        RenderTexture m_DepthmapDirectionalSpecial_GeomOnlyDepth;
        RenderTexture m_DepthmapDirectionalSpecial_HVROnly;
        RenderTexture m_DepthmapDirectionalSpecial_HVROnlyDepth;
        RenderTexture m_ColorRT;
        RenderTexture m_DepthRT;

        HvrViewportSwapChain m_SwapChain;

        struct ViewportRecord
        {
            //public RenderInterface renderInterface;
            public RenderTexture color;
            public RenderTexture depth;
            public HvrViewportSwapChain swapchain;
            public Quaternion rotation;
            public HvrViewportInterface viewport; // no ownership
        }

        Dictionary<CubemapFace, ViewportRecord> m_PointLightCubemapBookkeep = new Dictionary<CubemapFace, ViewportRecord>();

        CommandBuffer m_CommandBufferBakeSM;
        CommandBuffer m_CommandBufferBakeSM2;
        CommandBuffer m_CommandBufferCopySM;
        
        bool m_CommandBufferAdded;

        BoundingSphere[] m_CachedBoundingSphere = new BoundingSphere[4];
        Matrix4x4[] m_CachedDirectionalLightViewMatrix = new Matrix4x4[4];
        Matrix4x4[] m_CachedDirectionalLightProjectionMatrix = new Matrix4x4[4];

        LightType m_CachedLightType = LightType.Area;

        [SerializeField, HideInInspector]
        ShadowQuality m_ShadowQuality = ShadowQuality.High;

        [ExposeProperty]
        public ShadowQuality ShadowResolution
        {
            get
            {
                return m_ShadowQuality;
            }

            set
            {
                if (m_ShadowQuality != value)
                {
                    m_ShadowQuality = value;

                    _ReleaseRenderTextures();
                    _InitialiseRenderTextures();
                }
            }
        }

        [SerializeField, HideInInspector]
        bool m_SelfShadowing = false;
        [ExposeProperty]
        public bool SelfShadowing
        {
            get
            {
                return m_SelfShadowing;
            }
            set
            {
                if (m_SelfShadowing != value)
                {
                    EnsureDependentLight();
                    if (GetLightType() == LightType.Directional)
                    {
                        // directional light doesn't support self shadowing
                        value = false;
                        Debug.Log("Directional light doesn't support self shadowing at the moment.");
                    }

                    // need to re-order the command buffer created
                    ShuffleCommandBuffer(value);
                }

                m_SelfShadowing = value;
            }
        }

        public float m_yBias = 0.0f;

        // Resource stuff
        Shader m_CasterShader;
        Material m_CasterMaterial;
        Mesh m_CasterMesh;
        Mesh m_CubeMesh;
        Cubemap m_Cubemap;

        // Directional light only
        Matrix4x4 m_LightSpaceView;
        Matrix4x4 m_LightSpaceProject;
        Matrix4x4 m_LightSpaceProjectGPU;

        void EnsureDependentLight()
        {
            if (m_DependentLight == null)
            {
                m_DependentLight = GetComponent<Light>();
            }
        }

        void ShuffleCommandBuffer(bool selfShadowing)
        {
            EnsureDependentLight();

            CreateCommandBuffer();

            // assume it has to be re-ordered
            m_DependentLight.RemoveCommandBuffer(LightEvent.AfterShadowMap, m_CommandBufferBakeSM);
            m_DependentLight.RemoveCommandBuffer(LightEvent.AfterShadowMap, m_CommandBufferBakeSM2);
            m_DependentLight.RemoveCommandBuffer(LightEvent.AfterShadowMap, m_CommandBufferCopySM);

            if (selfShadowing)
            {
                m_DependentLight.AddCommandBuffer(LightEvent.AfterShadowMap, m_CommandBufferBakeSM);
                m_DependentLight.AddCommandBuffer(LightEvent.AfterShadowMap, m_CommandBufferBakeSM2);
                m_DependentLight.AddCommandBuffer(LightEvent.AfterShadowMap, m_CommandBufferCopySM);
            }
            else
            {
                m_DependentLight.AddCommandBuffer(LightEvent.AfterShadowMap, m_CommandBufferCopySM);
                m_DependentLight.AddCommandBuffer(LightEvent.AfterShadowMap, m_CommandBufferBakeSM);
                m_DependentLight.AddCommandBuffer(LightEvent.AfterShadowMap, m_CommandBufferBakeSM2);
            }
        }

        void CreateCommandBuffer()
        {
            if (!m_CommandBufferAdded)
            {
                m_CommandBufferCopySM = new CommandBuffer();
                m_CommandBufferCopySM.name = "HVR ShadowCaster Copy Depth CB";

                m_CommandBufferBakeSM = new CommandBuffer();
                m_CommandBufferBakeSM.name = "HVR ShadowCaster Bake Depth CB";

                m_CommandBufferBakeSM2 = new CommandBuffer();
                m_CommandBufferBakeSM2.name = "HVR ShadowCaster Bake Depth CB2";

                if (m_SelfShadowing)
                {
                    // bake shadowmap before the copy, so the HVR depth is taken into account
                    m_DependentLight.AddCommandBuffer(LightEvent.AfterShadowMap, m_CommandBufferBakeSM);
                    m_DependentLight.AddCommandBuffer(LightEvent.AfterShadowMap, m_CommandBufferBakeSM2);
                    m_DependentLight.AddCommandBuffer(LightEvent.AfterShadowMap, m_CommandBufferCopySM);
                }
                else
                {
                    // bake shadowmap after the copy, so the HVR depth is excluded
                    m_DependentLight.AddCommandBuffer(LightEvent.AfterShadowMap, m_CommandBufferCopySM);
                    m_DependentLight.AddCommandBuffer(LightEvent.AfterShadowMap, m_CommandBufferBakeSM);
                    m_DependentLight.AddCommandBuffer(LightEvent.AfterShadowMap, m_CommandBufferBakeSM2);
                }

                m_CommandBufferAdded = true;
            }
        }

        RenderTexture _GenerateDepthmapCopy()
        {
            RenderTexture rt;
            if (m_DependentLight.type == LightType.Point)
            {
                int cubeMapSize = _GetBaseMapSizeForPoint();
                rt = new RenderTexture(cubeMapSize, cubeMapSize, 16, RenderTextureFormat.RFloat);
                rt.isCubemap = true;
            }
            else
            {
                int mapSize = _GetBaseMapSize();
                rt = new RenderTexture(mapSize, mapSize, 16, RenderTextureFormat.RFloat);
            }

            if (!rt.IsCreated())
            {
                rt.Create();
            }

            CommandBuffer clearDepthCB = new CommandBuffer();
            if (rt.isCubemap)
            {
                // +X
                clearDepthCB.Clear();
                clearDepthCB.SetRenderTarget(rt, 0, CubemapFace.PositiveX);
                clearDepthCB.ClearRenderTarget(true, true, Color.blue);
                Graphics.ExecuteCommandBuffer(clearDepthCB);
                // -X
                clearDepthCB.Clear();
                clearDepthCB.SetRenderTarget(rt, 0, CubemapFace.NegativeX);
                clearDepthCB.ClearRenderTarget(true, true, Color.green);
                Graphics.ExecuteCommandBuffer(clearDepthCB);
                // +Y
                clearDepthCB.Clear();
                clearDepthCB.SetRenderTarget(rt, 0, CubemapFace.PositiveY);
                clearDepthCB.ClearRenderTarget(true, true, Color.cyan);
                Graphics.ExecuteCommandBuffer(clearDepthCB);
                // -Y
                clearDepthCB.Clear();
                clearDepthCB.SetRenderTarget(rt, 0, CubemapFace.NegativeY);
                clearDepthCB.ClearRenderTarget(true, true, Color.yellow);
                Graphics.ExecuteCommandBuffer(clearDepthCB);
                // +Z
                clearDepthCB.Clear();
                clearDepthCB.SetRenderTarget(rt, 0, CubemapFace.PositiveZ);
                clearDepthCB.ClearRenderTarget(true, true, Color.magenta);
                Graphics.ExecuteCommandBuffer(clearDepthCB);
                // -Z
                clearDepthCB.Clear();
                clearDepthCB.SetRenderTarget(rt, 0, CubemapFace.NegativeZ);
                clearDepthCB.ClearRenderTarget(true, true, Color.black);
                Graphics.ExecuteCommandBuffer(clearDepthCB);
            }

            clearDepthCB.Release();

            return rt;
        }

        void SyncDepthmapCopy()
        {
            if (m_DependentLight)
            {
                if (m_DepthmapCopy != null)
                {
                    if ((m_DepthmapCopy.isCubemap && m_DependentLight.type != LightType.Point) ||
                        (!m_DepthmapCopy.isCubemap && m_DependentLight.type == LightType.Point))
                    {
                        // release depthmap copy
                        m_DepthmapCopy.Release();
                        m_DepthmapCopy = null;

                        // recreate depthmap again
                        m_DepthmapCopy = _GenerateDepthmapCopy();
                    }
                }
                else
                {
                    m_DepthmapCopy = _GenerateDepthmapCopy();
                }
            }
        }

        int _GetBaseMapSize()
        {
            return BASE_RESOLUTION[(int)m_ShadowQuality];
        }

        int _GetBaseMapSizeForPoint()
        {
            return BASE_RESOLUTION_FOR_POINT[(int)m_ShadowQuality];
        }

        void _InitialiseRenderTextures()
        {
            SyncDepthmapCopy();

            int baseMapSize = _GetBaseMapSize();
            int baseMapSizeForPoint = _GetBaseMapSizeForPoint();

            if (m_DepthmapDirectionalSpecial_HVROnly == null)
            {
                m_DepthmapDirectionalSpecial_HVROnly = new RenderTexture(baseMapSize, baseMapSize, 0, RenderTextureFormat.RFloat);
            }
            if (!m_DepthmapDirectionalSpecial_HVROnly.IsCreated())
            {
                m_DepthmapDirectionalSpecial_HVROnly.Create();
            }

            if (m_DepthmapDirectionalSpecial_HVROnlyDepth == null)
            {
                m_DepthmapDirectionalSpecial_HVROnlyDepth = new RenderTexture(baseMapSize, baseMapSize, 16, RenderTextureFormat.Depth);
            }

            if (!m_DepthmapDirectionalSpecial_HVROnlyDepth.IsCreated())
            {
                m_DepthmapDirectionalSpecial_HVROnlyDepth.Create();
            }

            if (m_DepthmapDirectionalSpecial_GeomOnly == null)
            {
                m_DepthmapDirectionalSpecial_GeomOnly = new RenderTexture(baseMapSize, baseMapSize, 0, RenderTextureFormat.RFloat);
            }
            if (!m_DepthmapDirectionalSpecial_GeomOnly.IsCreated())
            {
                m_DepthmapDirectionalSpecial_GeomOnly.Create();
            }

            if (m_DepthmapDirectionalSpecial_GeomOnlyDepth == null)
            {
                m_DepthmapDirectionalSpecial_GeomOnlyDepth = new RenderTexture(baseMapSize, baseMapSize, 16, RenderTextureFormat.Depth);
            }
            if (!m_DepthmapDirectionalSpecial_GeomOnlyDepth.IsCreated())
            {
                m_DepthmapDirectionalSpecial_GeomOnlyDepth.Create();
            }

            // force to clear the depth map, otherwise if scene has no mesh, the HVR actors will rendered in shadow
            RenderTexture[] renderTextureList =
            {
                m_DepthmapCopy, m_DepthmapDirectionalSpecial_HVROnlyDepth, m_DepthmapDirectionalSpecial_GeomOnlyDepth
            };
            CommandBuffer clearDepthCB = new CommandBuffer();
            foreach (RenderTexture rt in renderTextureList)
            {
                clearDepthCB.Clear();
                clearDepthCB.SetRenderTarget(rt);
                clearDepthCB.ClearRenderTarget(true, true, Color.white);
                Graphics.ExecuteCommandBuffer(clearDepthCB);
            }

            clearDepthCB.Release();

            // create cubemap related resources for point light
            if (!m_PointLightCubemapBookkeep.ContainsKey(CubemapFace.PositiveZ))
            {
                ViewportRecord record = new ViewportRecord();
                record.rotation = Quaternion.Euler(0, 0, 0);
                record.swapchain = new HvrViewportSwapChain(MAX_VIEWPORT_NUM);
                record.color = new RenderTexture(baseMapSizeForPoint, baseMapSizeForPoint, 0, RenderTextureFormat.ARGB32);
                record.depth = new RenderTexture(baseMapSizeForPoint, baseMapSizeForPoint, 16, RenderTextureFormat.Depth);
                m_PointLightCubemapBookkeep[CubemapFace.PositiveZ] = record;
            }
            if (!m_PointLightCubemapBookkeep.ContainsKey(CubemapFace.NegativeZ))
            {
                ViewportRecord record = new ViewportRecord();
                record.rotation = Quaternion.Euler(0, 180, 0);
                record.swapchain = new HvrViewportSwapChain(MAX_VIEWPORT_NUM);
                record.color = new RenderTexture(baseMapSizeForPoint, baseMapSizeForPoint, 0, RenderTextureFormat.ARGB32);
                record.depth = new RenderTexture(baseMapSizeForPoint, baseMapSizeForPoint, 16, RenderTextureFormat.Depth);
                m_PointLightCubemapBookkeep[CubemapFace.NegativeZ] = record;
            }
            if (!m_PointLightCubemapBookkeep.ContainsKey(CubemapFace.PositiveX))
            {
                ViewportRecord record = new ViewportRecord();
                record.rotation = Quaternion.Euler(0, 90, 0);
                record.swapchain = new HvrViewportSwapChain(MAX_VIEWPORT_NUM);
                record.color = new RenderTexture(baseMapSizeForPoint, baseMapSizeForPoint, 0, RenderTextureFormat.ARGB32);
                record.depth = new RenderTexture(baseMapSizeForPoint, baseMapSizeForPoint, 16, RenderTextureFormat.Depth);
                m_PointLightCubemapBookkeep[CubemapFace.PositiveX] = record;
            }
            if (!m_PointLightCubemapBookkeep.ContainsKey(CubemapFace.NegativeX))
            {
                ViewportRecord record = new ViewportRecord();
                record.rotation = Quaternion.Euler(0, 270, 0);
                record.swapchain = new HvrViewportSwapChain(MAX_VIEWPORT_NUM);
                record.color = new RenderTexture(baseMapSizeForPoint, baseMapSizeForPoint, 0, RenderTextureFormat.ARGB32);
                record.depth = new RenderTexture(baseMapSizeForPoint, baseMapSizeForPoint, 16, RenderTextureFormat.Depth);
                m_PointLightCubemapBookkeep[CubemapFace.NegativeX] = record;
            }
            if (!m_PointLightCubemapBookkeep.ContainsKey(CubemapFace.PositiveY))
            {
                ViewportRecord record = new ViewportRecord();
                record.rotation = Quaternion.Euler(-90, 0, 0);
                record.swapchain = new HvrViewportSwapChain(MAX_VIEWPORT_NUM);
                record.color = new RenderTexture(baseMapSizeForPoint, baseMapSizeForPoint, 0, RenderTextureFormat.ARGB32);
                record.depth = new RenderTexture(baseMapSizeForPoint, baseMapSizeForPoint, 16, RenderTextureFormat.Depth);
                m_PointLightCubemapBookkeep[CubemapFace.PositiveY] = record;
            }
            if (!m_PointLightCubemapBookkeep.ContainsKey(CubemapFace.NegativeY))
            {
                ViewportRecord record = new ViewportRecord();
                record.rotation = Quaternion.Euler(90, 0, 0);
                record.swapchain = new HvrViewportSwapChain(MAX_VIEWPORT_NUM);
                record.color = new RenderTexture(baseMapSizeForPoint, baseMapSizeForPoint, 0, RenderTextureFormat.ARGB32);
                record.depth = new RenderTexture(baseMapSizeForPoint, baseMapSizeForPoint, 16, RenderTextureFormat.Depth);
                m_PointLightCubemapBookkeep[CubemapFace.NegativeY] = record;
            }

            if (!m_ColorRT)
            {
                m_ColorRT = new RenderTexture(baseMapSize, baseMapSize, 0, RenderTextureFormat.ARGB32);
            }

            if (!m_DepthRT)
            {
                m_DepthRT = new RenderTexture(baseMapSize, baseMapSize, 16, RenderTextureFormat.Depth);
            }
        }

        void _ReleaseRenderTextures()
        {
            if (m_DepthmapCopy != null)
            {
                m_DepthmapCopy.Release();
                m_DepthmapCopy = null;
            }

            if (m_DepthmapDirectionalSpecial_HVROnly != null)
            {
                m_DepthmapDirectionalSpecial_HVROnly.Release();
                m_DepthmapDirectionalSpecial_HVROnly = null;
            }

            if (m_DepthmapDirectionalSpecial_HVROnlyDepth != null)
            {
                m_DepthmapDirectionalSpecial_HVROnlyDepth.Release();
                m_DepthmapDirectionalSpecial_HVROnlyDepth = null;
            }

            if (m_DepthmapDirectionalSpecial_GeomOnly != null)
            {
                m_DepthmapDirectionalSpecial_GeomOnly.Release();
                m_DepthmapDirectionalSpecial_GeomOnly = null;
            }

            if (m_DepthmapDirectionalSpecial_GeomOnlyDepth != null)
            {
                m_DepthmapDirectionalSpecial_GeomOnlyDepth.Release();
                m_DepthmapDirectionalSpecial_GeomOnlyDepth = null;
            }

            if (m_ColorRT != null)
            {
                m_ColorRT.Release();
                m_ColorRT = null;
            }
            if (m_DepthRT != null)
            {
                m_DepthRT.Release();
                m_DepthRT = null;
            }

            // remove render textures inside cubemap book keep
            foreach (KeyValuePair<CubemapFace, ViewportRecord> pair in m_PointLightCubemapBookkeep)
            {
                pair.Value.depth.Release();
                pair.Value.color.Release();
            }

            m_PointLightCubemapBookkeep = new Dictionary<CubemapFace, ViewportRecord>();
        }


        void OnEnable()
        {
            EnsureDependentLight();

            CreateCommandBuffer();

            if (m_CasterShader == null)
            {
                m_CasterShader = Resources.Load("8i/shaders/HVRShadowRenderer") as Shader;
            } 

            if (m_CasterMaterial == null)
            {
                m_CasterMaterial = new Material(m_CasterShader);
                m_CasterMaterial.hideFlags = HideFlags.DontSave;
            }

            if (m_CasterMesh == null)
            {
                m_CasterMesh = CompositeBufferUtils.GenerateQuad();
                m_CasterMesh.hideFlags = HideFlags.HideAndDontSave;
            }

            if (m_CubeMesh == null)
            {
                Vector3[] vertices = new Vector3[8];
                AABB aabb = new AABB(new Vector3(-1,-1,-1), new Vector3(1,1,1));
                aabb.CalcVertex(ref vertices);

                // mesh vertices in Light Space
                m_CubeMesh = new Mesh();
                m_CubeMesh.vertices = vertices;
                m_CubeMesh.triangles = aabb.Indices;
            }

            HvrShadowCasterCollector.instance.Add(this);

            _InitialiseRenderTextures();

        }
        
        void OnDisable()
        {
            EnsureDependentLight();

            _DestroyResources();

            HvrShadowCasterCollector.instance.Remove(this);
        }

        void _DestroyResources()
        {
            if (m_CommandBufferAdded)
            {
                m_DependentLight.RemoveCommandBuffer(LightEvent.AfterShadowMap, m_CommandBufferBakeSM);
                m_CommandBufferBakeSM.Release();

                m_DependentLight.RemoveCommandBuffer(LightEvent.AfterShadowMap, m_CommandBufferBakeSM2);
                m_CommandBufferBakeSM2.Release();

                m_DependentLight.RemoveCommandBuffer(LightEvent.AfterShadowMap, m_CommandBufferCopySM);
                m_CommandBufferCopySM.Release();

                m_CommandBufferAdded = false;
                m_CommandBufferBakeSM = null;
                m_CommandBufferBakeSM2 = null;
                m_CommandBufferCopySM = null;
            }

            _ReleaseRenderTextures();
        }

        void OnDestroy()
        {
            if (m_DependentLight != null)
            {
                _DestroyResources();
            }

            HvrShadowCasterCollector.instance.Remove(this);
        }

        void Awake()
        {
            m_CommandBufferAdded = false;
        }

        // Use this for initialization
        void Start()
        {
            EnsureDependentLight();

            OnEnable();

        }

        void Update()
        {
            DoRender();
        }
#if UNITY_EDITOR
        void OnRenderObject()
        {
            if (UnityEditor.EditorApplication.isPaused)
            {
                // do draw in game view, otherwise changing in inspectors will not affect the paused game
                DoRender();
            }
        }
#endif

        void DoRender()
        {
            if (m_DependentLight == null)
                return;

            LightType lightType = m_DependentLight.type;
            if (m_CachedLightType != lightType || m_SwapChain == null)
            {
                // Pull to detect the change of light type, recreate resources, swap chains, etc
                if (lightType == LightType.Directional)
                {
                    m_SwapChain = new HvrViewportSwapChain(MAX_VIEWPORT_NUM_FOR_DIRECTIONAL);
                }
                else if (lightType == LightType.Spot)
                {
                    m_SwapChain = new HvrViewportSwapChain(MAX_VIEWPORT_NUM);
                }

            }

#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                //Debug.Log("Update shadow casting when not playing.");
            }
#endif

            SyncDepthmapCopy();

            float effectiveRange = Mathf.Min(m_DependentLight.range, QualitySettings.shadowDistance);
            Matrix4x4 inverseViewProjection = Matrix4x4.identity;

            RenderBuffer originalColorBuffer = Graphics.activeColorBuffer;
            RenderBuffer originalDepthBuffer = Graphics.activeDepthBuffer;

            if (m_DependentLight.type == LightType.Point)
            {
                Matrix4x4 invertY = Matrix4x4.Scale(new Vector3(1, -1, 1));

                if (m_CommandBufferAdded)
                {
                    // save the depth cubemap
                    m_CommandBufferCopySM.Clear();
                    m_CommandBufferBakeSM.Clear();

                    // Blit to cubemap doesn't work
                    //BuiltinRenderTextureType shadowmap = BuiltinRenderTextureType.CurrentActive;
                    //m_CommandBufferCopySM.SetShadowSamplingMode(shadowmap, ShadowSamplingMode.RawDepth);
                    //m_CommandBufferCopySM.SetGlobalTexture("_MainTex", shadowmap);
                    //m_CommandBufferCopySM.SetRenderTarget(m_DepthmapCopy, 0, CubemapFace.PositiveY);
                    //m_CommandBufferCopySM.DrawMesh(m_CubeMesh, Matrix4x4.identity, m_CasterMaterial, 0, 4);
                    //m_CommandBufferCopySM.Blit(shadowmap, m_DepthmapCopy, m_CasterMaterial, 4);

                    int cubeMapSize = _GetBaseMapSizeForPoint();
                    foreach (KeyValuePair<CubemapFace, ViewportRecord> pair in m_PointLightCubemapBookkeep)
                    {
                        CubemapFace cubemapFace = pair.Key;

                        Matrix4x4 view = Matrix4x4.TRS(m_DependentLight.transform.position, pair.Value.rotation, Vector3.one);
                        view = view.inverse; // from world to view

                        // NOTE: From Unity script API Camera.worldToCameraMatrix,
                        // "Note that camera space matches OpenGL convention: camera's forward is the negative Z axis. This is different from Unity's convention, where forward is the positive Z axis."
                        Matrix4x4 invertZ = Matrix4x4.Scale(new Vector3(1, 1, -1));
                        view = invertZ * view; // apply Unity convention


                        Matrix4x4 proj = Matrix4x4.Perspective(90.0f, 1.0f, m_DependentLight.shadowNearPlane, m_DependentLight.shadowNearPlane + m_DependentLight.range);
                        m_LightSpaceProject = GL.GetGPUProjectionMatrix(proj, false);

                        inverseViewProjection = (proj * view).inverse;

                        HvrViewportSwapChain swapchain = pair.Value.swapchain;
                        HvrViewportInterface viewport = swapchain.NextViewport(view, m_LightSpaceProject, m_DependentLight.shadowNearPlane, effectiveRange, 0, 0, cubeMapSize, cubeMapSize);

                        // Render viewport
                        Graphics.SetRenderTarget(pair.Value.color.colorBuffer, pair.Value.depth.depthBuffer);
                        GL.Clear(true, true, Color.red, 1.0f);
                        HvrStaticInterface.Self().RenderCamera(null, viewport);
                        Graphics.SetRenderTarget(null);

                        // looks like the depth cubemap is not working for command buffer
                        m_CommandBufferBakeSM.SetRenderTarget(BuiltinRenderTextureType.CurrentActive, BuiltinRenderTextureType.CurrentActive, 0, cubemapFace);
                        m_CommandBufferBakeSM.ClearRenderTarget(true, false, Color.white);
                        m_CommandBufferBakeSM.SetGlobalMatrix("_ViewProjectInverse", inverseViewProjection);
                        m_CommandBufferBakeSM.SetGlobalTexture("_oDEP", pair.Value.depth);
                        m_CommandBufferBakeSM.SetGlobalFloat("_yBias", m_yBias);

                        m_CommandBufferBakeSM.DrawMesh(m_CasterMesh, Matrix4x4.identity, m_CasterMaterial, 0, 2);


                        // [WORKAROUND] Copy Unity's shadow cubemap
                        // It's hard to copy Unity's shadow cubemap for later usage since:
                        // 1. CommandBuffer.Blit won't work on cubemaps
                        // 2. Feed CommandBuffer.SetGlobalTexture() with BuiltinRenderTextureType.CurrentActive have no effects(current render target cannot be acquired for texture maybe?)
                        // Solution: DRAW ALL MESHES TO THE SELF-MANAGED CUBEMAP, VERY SLOW BUT NO CHOICE
                        m_CommandBufferCopySM.SetRenderTarget(m_DepthmapCopy, 0, cubemapFace);
                        m_CommandBufferCopySM.ClearRenderTarget(true, true, Color.white);

                        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore ||
                            SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGL2 ||
                            SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 ||
                            SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
                        {
                            view = invertY * view;
                        }

                        // for loop to render all meshes can be seen from the camera and casting shadows
                        MeshRenderer[] meshRenderers = GameObject.FindObjectsOfType<MeshRenderer>();
                        foreach (MeshRenderer meshRenderer in meshRenderers)
                        {
                            // must be enabled and casts shadow
                            if (meshRenderer.enabled && meshRenderer.shadowCastingMode != ShadowCastingMode.Off)
                            {
                                MeshFilter filter;
                                if (filter = meshRenderer.GetComponent<MeshFilter>())
                                {
                                    // TODO: find a correct but efficient algorithm for culling shadow caster
                                    if (filter.sharedMesh != null)// && GeometryUtility.TestPlanesAABB(frustumPlanes, meshRenderer.bounds))
                                    {
                                        for (int i = 0; i < filter.sharedMesh.subMeshCount; ++i)
                                        {
                                            m_CommandBufferCopySM.SetGlobalMatrix("_matModelViewProj", m_LightSpaceProject * view * filter.transform.localToWorldMatrix);
                                            m_CommandBufferCopySM.SetGlobalMatrix("_matModel", filter.transform.localToWorldMatrix);
                                            m_CommandBufferCopySM.DrawMesh(filter.sharedMesh, Matrix4x4.identity, m_CasterMaterial, 0, 3); // use a special pass 3 (draw world space mesh position - light position)
                                        }
                                    }
                                }
                            }
                        }

                        if (SelfShadowing)
                        {
                            // and draw the HVRs
                            m_CommandBufferCopySM.SetGlobalMatrix("_ViewProjectInverse", inverseViewProjection);
                            m_CommandBufferCopySM.SetGlobalTexture("_oDEP", pair.Value.depth);
                            m_CommandBufferCopySM.DrawMesh(m_CasterMesh, Matrix4x4.identity, m_CasterMaterial, 0, 2);
                        }

                    }

                    m_CommandBufferBakeSM2.Clear(); // do nothing
                }
            }
            else
            {
                HvrViewportInterface viewport;

                int mapSize = _GetBaseMapSize();
                if (m_DependentLight.type == LightType.Directional)
                {
                    int shadowSplit = QualitySettings.shadowCascades;
                    
                    float[] splitPercent;
                    switch(shadowSplit)
                    {
                        case 4:
                            {
                                Vector3 split4 = QualitySettings.shadowCascade4Split;
                                splitPercent = new float[]
                                {
                                    split4.x, split4.y, split4.z, 1.0f
                                };
                            }
                            break;

                        case 2:
                            {
                                splitPercent = new float[]
                                {
                                    QualitySettings.shadowCascade2Split, 1.0f
                                };
                            }
                            break;
                        default:
                            {
                                splitPercent = new float[]
                                {
                                    1.0f
                                };
                            }
                            break;
                    }


                    // making four cascades
                    Rect[] cascadeShadowRect;

                    if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore ||
                        SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGL2 ||
                        SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 ||
                        SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
                    {
                        switch (shadowSplit)
                        {
                            case 4:
                                {
                                    cascadeShadowRect = new Rect[]
                                    {
                                        new Rect(0, 0, mapSize/2, mapSize/2),
                                        new Rect(mapSize/2, 0, mapSize/2, mapSize/2),
                                        new Rect(0, mapSize/2, mapSize/2, mapSize/2),
                                        new Rect(mapSize/2, mapSize/2, mapSize/2, mapSize/2),

                                    };
                                }
                                break;

                            case 2:
                                {
                                    cascadeShadowRect = new Rect[]
                                    {
                                        new Rect(0, 0, mapSize, mapSize/2),
                                        new Rect(0, mapSize/2, mapSize, mapSize/2)
                                    };
                                }
                                break;

                            default:
                                {
                                    cascadeShadowRect = new Rect[]
                                    {
                                        new Rect(0, 0, mapSize, mapSize)
                                    };
                                }
                                break;
                        }

                    }
                    else
                    {
                        switch (shadowSplit)
                        {
                            case 4:
                                {
                                    cascadeShadowRect = new Rect[]
                                    {
                                        new Rect(0, mapSize/2, mapSize/2, mapSize/2),
                                        new Rect(mapSize/2, mapSize/2, mapSize/2, mapSize/2),
                                        new Rect(0, 0, mapSize/2, mapSize/2),
                                        new Rect(mapSize/2, 0, mapSize/2, mapSize/2)
                                    };
                                }
                                break;

                            case 2:
                                {
                                    cascadeShadowRect = new Rect[]
                                    {
                                        new Rect(0, mapSize/2, mapSize, mapSize/2),
                                        new Rect(0, 0, mapSize, mapSize/2),
                                    };
                                }
                                break;

                            default:
                                {
                                    cascadeShadowRect = new Rect[]
                                    {
                                        new Rect(0, 0, mapSize, mapSize)
                                    };
                                }
                                break;
                        }


                    }


                    int i;
                    Camera cam = CameraHelper.GetMainCamera();

                    Graphics.SetRenderTarget(m_ColorRT.colorBuffer, m_DepthRT.depthBuffer);
                    GL.Clear(true, true, Color.red, 1.0f);
                    

                    float Rmax = BoundsBuilder.GetBoundingSphereForCameraCascade(cam, 1.0f).radius;
                    for (i = 0; i < shadowSplit; ++i)
                    {
                        // Match the stablization of directional light shadow in Unity.
                        // The idea has been discussed here: http://the-witness.net/news/2010/03/graphics-tech-shadow-maps-part-1/
                        // and here: http://bryanlawsmithblog.blogspot.co.nz/2014/12/rendering-post-stable-cascaded-shadow.html
                        // So fundamentally our goal is to have a perfect stationary shadow when camera moves(panning, rotating)
                        // in directional lights. As in directional lights, the shadow map doesn't move along with the light source
                        // since the position of directional lights have no meanings. The shadow map is generated by calculating a
                        // bounding sphere of the view frustum. So the shadow map moves when camera moves. This causes shadow
                        // "shimmering" problem because when the sampling shader samples the shadowmap, it will come across with
                        // subpixels that makes the edge of shadow swim. The idea is to "snap" the movement in a exact step of one
                        // single shadowmap texel. The calculation here involves compute the bounding sphere, taking the shadow map size,
                        // and use a reference point and transform it into clip space, then change its unit to texel and get the 
                        // round off offset. Using the offset in the orthogonal projection matrix, and you can get a perfect texel aligned
                        // shadow map and thus eliminate the shimmering effect.
                        Rect rect = cascadeShadowRect[i];
                        float split = splitPercent[i];


                        
                        BoundingSphere worldSpaceFrustumSphere = BoundsBuilder.GetBoundingSphereForCameraCascade(cam, split); // in world space
                        m_CachedBoundingSphere[i] = worldSpaceFrustumSphere;
                        //worldSpaceFrustumSphere.radius = Mathf.Ceil(worldSpaceFrustumSphere.radius * 16.0f) / 16.0f;

                        // make the stabalization compatible with shadow split
                        int textureMapSizeX = GetShadowmapDimension();
                        int textureMapSizeY = GetShadowmapDimension();
                        if (shadowSplit == 4)
                        {
                            textureMapSizeX /= 2;
                            textureMapSizeY /= 2;
                        }
                        else if (shadowSplit == 2)
                        {
                            textureMapSizeY /= 2;
                        }

                        //int textureMapSize = shadowSplit > 1 ? GetShadowmapDimension() / 2 : GetShadowmapDimension();
                        Quaternion orientation = m_DependentLight.transform.rotation;
                        Vector3 euler = orientation.eulerAngles;
                        orientation = Quaternion.Euler(euler.x, euler.y, 0); // make no funny rotation on z axis

                        Vector3 fromCenterToOrigin = new Vector3(0, 0, -worldSpaceFrustumSphere.radius);
                        Vector3 origin = worldSpaceFrustumSphere.position + m_DependentLight.transform.rotation * fromCenterToOrigin;

                        Matrix4x4 view = Matrix4x4.TRS(origin, orientation, Vector3.one).inverse;
                        Matrix4x4 invertZ = Matrix4x4.Scale(new Vector3(1, 1, -1));
                        view = invertZ * view;
                        m_LightSpaceView = view;
                        m_CachedDirectionalLightViewMatrix[i] = view;

                        Matrix4x4 proj = Matrix4x4.Ortho(
                            -worldSpaceFrustumSphere.radius, +worldSpaceFrustumSphere.radius,
                            -worldSpaceFrustumSphere.radius, +worldSpaceFrustumSphere.radius,
                            0 - (Rmax - worldSpaceFrustumSphere.radius), 2 * worldSpaceFrustumSphere.radius // in order to prevent depth clipping when casting shadow, we need to extend near plane to the boundary of the maximum radius
                            //0, 2 * worldSpaceFrustumSphere.radius
                            );

                        Matrix4x4 viewProject = proj * view;
                        Vector3 shadowMapOrigin = new Vector3(0, 0, 0);
                        shadowMapOrigin = viewProject.MultiplyPoint(shadowMapOrigin); // in clip space
                        shadowMapOrigin.x *= textureMapSizeX / 2.0f; // in clip space but texel unit
                        shadowMapOrigin.y *= textureMapSizeY / 2.0f;

                        Vector3 shadowMapOriginRounded = new Vector3(
                                Mathf.Round(shadowMapOrigin.x),
                                Mathf.Round(shadowMapOrigin.y),
                                Mathf.Round(shadowMapOrigin.z)
                            );

                        Vector3 roundOffset = shadowMapOrigin - shadowMapOriginRounded; // fraction of offset
                        roundOffset.x *= 2.0f / textureMapSizeX; // fraction in default unit
                        roundOffset.y *= 2.0f / textureMapSizeY;
                        roundOffset.x *= -1; // have to turn the direction on x, y
                        roundOffset.y *= -1;
                        roundOffset.z = 0;

                        // apply back to projection matrix
                        Vector4 column3 = proj.GetColumn(3);
                        column3 += new Vector4(roundOffset.x, roundOffset.y, 0, 0);
                        proj.SetColumn(3, column3);
                        // Or do the following if in a more meaningful way
                        //Matrix4x4 translate = Matrix4x4.TRS(roundOffset, Quaternion.identity, Vector3.one);
                        //proj = translate * proj;

                        m_LightSpaceProject = GL.GetGPUProjectionMatrix(proj, false);
                        m_LightSpaceProjectGPU = GL.GetGPUProjectionMatrix(proj, true);

                        m_CachedDirectionalLightProjectionMatrix[i] = GL.GetGPUProjectionMatrix(proj, false);

                        viewport = m_SwapChain.NextViewport(m_LightSpaceView, m_LightSpaceProject, 0 - (Rmax - worldSpaceFrustumSphere.radius), 2 * worldSpaceFrustumSphere.radius,
                            (int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);

                        // Note: looks like ViewportInterface.SetDimensions doesn't work. Use low level GL api to override it
                        GL.Viewport(rect);
                        HvrStaticInterface.Self().RenderCamera(null, viewport);
                        
                    }

                    // fill in the zeros
                    for(; i < 4; ++i)
                    {
                        m_CachedBoundingSphere[i] = new BoundingSphere();
                        m_CachedDirectionalLightProjectionMatrix[i] = Matrix4x4.zero;
                        m_CachedDirectionalLightViewMatrix[i] = Matrix4x4.zero;
                    }

                    GL.Viewport(cam.pixelRect);
                    
                }
                else
                {
                    Matrix4x4 view = m_DependentLight.transform.worldToLocalMatrix;
                    // NOTE: From Unity script API Camera.worldToCameraMatrix,
                    // "Note that camera space matches OpenGL convention: camera's forward is the negative Z axis. This is different from Unity's convention, where forward is the positive Z axis."
                    Matrix4x4 invertZ = Matrix4x4.Scale(new Vector3(1, 1, -1));
                    view = invertZ * view;
                    m_LightSpaceView = view;

                    Matrix4x4 proj = Matrix4x4.Perspective(m_DependentLight.spotAngle, 1.0f, m_DependentLight.shadowNearPlane, m_DependentLight.shadowNearPlane + m_DependentLight.range);
                    m_LightSpaceProject = GL.GetGPUProjectionMatrix(proj, false);
                    m_LightSpaceProjectGPU = GL.GetGPUProjectionMatrix(proj, true);

                    viewport = m_SwapChain.NextViewport(m_LightSpaceView, m_LightSpaceProject, m_DependentLight.shadowNearPlane, m_DependentLight.shadowNearPlane + m_DependentLight.range, 
                        0, 0, mapSize, mapSize);

                    // Render viewport
                    Graphics.SetRenderTarget(m_ColorRT.colorBuffer, m_DepthRT.depthBuffer);
                    GL.Clear(true, true, Color.red, 1.0f);
                    HvrStaticInterface.Self().RenderCamera(null, viewport);
                }

                

                if (m_CommandBufferAdded)
                {
                    m_CommandBufferCopySM.Clear();
                    RenderTargetIdentifier shadowmap = BuiltinRenderTextureType.CurrentActive;
                    m_CommandBufferCopySM.SetShadowSamplingMode(shadowmap, ShadowSamplingMode.RawDepth);
                    m_CommandBufferCopySM.Blit(shadowmap, m_DepthmapCopy);

                    if (m_DependentLight.type == LightType.Directional)
                    {
                        // camera space
                        //Camera camera = Camera.main;
                        //Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);

                        m_CommandBufferBakeSM.Clear();
                        m_CommandBufferBakeSM.SetRenderTarget(m_DepthmapDirectionalSpecial_HVROnly, m_DepthmapDirectionalSpecial_HVROnlyDepth);
                        m_CommandBufferBakeSM.ClearRenderTarget(true, true, Color.white); // NOTE: clear depth
                        m_CommandBufferBakeSM.SetGlobalTexture("_oDEP", m_DepthRT);//viewport.frameBuffer.renderDepthBuffer);
                        m_CommandBufferBakeSM.SetGlobalMatrix("_Projection", m_LightSpaceProject);
                        m_CommandBufferBakeSM.SetGlobalMatrix("_ProjectionInverse", m_LightSpaceProject.inverse);
                        m_CommandBufferBakeSM.SetGlobalFloat("_ErrorBias", 0.005f);
                        //m_CommandBufferBakeSM.SetGlobalMatrix("_ViewProject", m_LightSpaceProjectGPU * m_LightSpaceView);
                        //m_CommandBufferBakeSM.SetGlobalMatrix("_ViewProjectInverse", (m_LightSpaceProjectGPU * m_LightSpaceView).inverse);
                        m_CommandBufferBakeSM.DrawMesh(m_CasterMesh, Matrix4x4.identity, m_CasterMaterial, 0, 0);

                        m_CommandBufferBakeSM2.Clear();
                        m_CommandBufferBakeSM2.SetRenderTarget(m_DepthmapDirectionalSpecial_GeomOnly, m_DepthmapDirectionalSpecial_GeomOnlyDepth);
                        m_CommandBufferBakeSM2.ClearRenderTarget(true, true, Color.white);
                        // for loop to render all meshes can be seen from the camera and casting shadows
                        MeshRenderer[] meshRenderers = GameObject.FindObjectsOfType<MeshRenderer>();
                        foreach (MeshRenderer meshRenderer in meshRenderers)
                        {
                            // must be enabled and casts shadow
                            if (meshRenderer.enabled && meshRenderer.shadowCastingMode != ShadowCastingMode.Off)
                            {
                                MeshFilter filter;
                                if (filter = meshRenderer.GetComponent<MeshFilter>())
                                {
                                    // TODO: find a correct but efficient algorithm for shadow caster
                                    if (filter.sharedMesh != null)// && GeometryUtility.TestPlanesAABB(frustumPlanes, meshRenderer.bounds))
                                    {
                                        for (int i = 0; i < filter.sharedMesh.subMeshCount; ++i)
                                        {
                                            m_CommandBufferBakeSM2.SetGlobalMatrix("_matModelViewProject", m_LightSpaceProjectGPU * m_LightSpaceView * filter.transform.localToWorldMatrix);
                                            m_CommandBufferBakeSM2.DrawMesh(filter.sharedMesh, Matrix4x4.identity, m_CasterMaterial, 0, 1); // use a special pass 1 (draw world space mesh depth)
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (m_DependentLight.type == LightType.Spot)
                    {
                        // bake depth of HVR to current Unity's shadowmap target
                        m_CommandBufferBakeSM.Clear();
                        m_CommandBufferBakeSM.SetGlobalTexture("_oDEP", m_DepthRT);//viewport.frameBuffer.renderDepthBuffer);
                        m_CommandBufferBakeSM.SetGlobalMatrix("_Projection", m_LightSpaceProject);
                        m_CommandBufferBakeSM.SetGlobalMatrix("_ProjectionInverse", m_LightSpaceProject.inverse);
                        m_CommandBufferBakeSM.SetGlobalFloat("_ErrorBias", 0.0f);
                        //m_CommandBufferBakeSM.SetGlobalMatrix("_ViewProject", m_LightSpaceProjectGPU * m_LightSpaceView);
                        //m_CommandBufferBakeSM.SetGlobalMatrix("_ViewProjectInverse", (m_LightSpaceProjectGPU * m_LightSpaceView).inverse);
                        m_CommandBufferBakeSM.DrawMesh(m_CasterMesh, Matrix4x4.identity, m_CasterMaterial, 0, 0);

                        m_CommandBufferBakeSM2.Clear(); // do nothing
                    }
                }
            }

            Graphics.SetRenderTarget(originalColorBuffer, originalDepthBuffer);
            m_CachedLightType = lightType;
        }

        public RenderTexture GetLightspaceDepthTexture()
        {
            return m_DepthmapCopy;
        }

        public Matrix4x4 GetLightspaceViewProjectionMatrix()
        {
            return m_LightSpaceProject * m_LightSpaceView;
        }

        public Vector4 GetWorldspaceLightPositionRangeVector()
        {
            Vector3 pos = m_DependentLight.transform.position;
            return new Vector4(pos.x, pos.y, pos.z, 1.0f / m_DependentLight.range);
        }

        public Vector4 GetWorldspaceLightDirectionAngleVector()
        {
            Vector3 dir = m_DependentLight.transform.rotation * Vector3.forward;
            return new Vector4(dir.x, dir.y, dir.z, m_DependentLight.spotAngle * 0.0174533f);
        }

        public RenderTexture GetDirectionalLightspaceHVRDepthTexture()
        {
            return m_DepthmapDirectionalSpecial_HVROnlyDepth;
        }

        public RenderTexture GetDirectionalLightspaceGeomDepthTexture()
        {
            return m_DepthmapDirectionalSpecial_GeomOnlyDepth;
        }

        public LightType GetLightType()
        {
            return m_DependentLight.type;
        }

        public bool IsShadowMultiSampled()
        {
            return m_DependentLight.shadows == LightShadows.Soft;
        }

        public float GetShadowStrength()
        {
            return m_DependentLight.shadowStrength;
        }

        public int GetShadowmapDimension()
        { 
            // TODO: Unity should provide API to fetch shadow map size, or at least the resolution settings
            return _GetBaseMapSize();
        }

        public BoundingSphere[] GetDirectionalLightBoundingSphere()
        {
            return m_CachedBoundingSphere;
        }

        public Matrix4x4[] GetDirectionalLightWorld2ShadowMatrix()
        {
            if (QualitySettings.shadowCascades > 1)
            {
                if (QualitySettings.shadowCascades == 4)
                {
                    return new Matrix4x4[4]
                    {
                        Matrix4x4.TRS(new Vector3(-0.5f, -0.5f, 0), Quaternion.identity, new Vector3(0.5f, 0.5f, 1)) * m_CachedDirectionalLightProjectionMatrix[0] * m_CachedDirectionalLightViewMatrix[0],
                        Matrix4x4.TRS(new Vector3(0.5f, -0.5f, 0), Quaternion.identity, new Vector3(0.5f, 0.5f, 1)) * m_CachedDirectionalLightProjectionMatrix[1] * m_CachedDirectionalLightViewMatrix[1],
                        Matrix4x4.TRS(new Vector3(-0.5f, 0.5f, 0), Quaternion.identity, new Vector3(0.5f, 0.5f, 1)) * m_CachedDirectionalLightProjectionMatrix[2] * m_CachedDirectionalLightViewMatrix[2],
                        Matrix4x4.TRS(new Vector3(0.5f, 0.5f, 0), Quaternion.identity, new Vector3(0.5f, 0.5f, 1)) * m_CachedDirectionalLightProjectionMatrix[3] * m_CachedDirectionalLightViewMatrix[3]
                    };
                }
                else
                {
                    return new Matrix4x4[4]
                    {
                        Matrix4x4.TRS(new Vector3(0, -0.5f, 0), Quaternion.identity, new Vector3(1, 0.5f, 1)) * m_CachedDirectionalLightProjectionMatrix[0] * m_CachedDirectionalLightViewMatrix[0],
                        Matrix4x4.TRS(new Vector3(0, 0.5f, 0), Quaternion.identity, new Vector3(1, 0.5f, 1)) * m_CachedDirectionalLightProjectionMatrix[1] * m_CachedDirectionalLightViewMatrix[1],
                        m_CachedDirectionalLightProjectionMatrix[2] * m_CachedDirectionalLightViewMatrix[2],
                        m_CachedDirectionalLightProjectionMatrix[3] * m_CachedDirectionalLightViewMatrix[3]
                    };
                }
            }
            else
            {
                return new Matrix4x4[4]
                {
                    m_CachedDirectionalLightProjectionMatrix[0] * m_CachedDirectionalLightViewMatrix[0],
                    m_CachedDirectionalLightProjectionMatrix[1] * m_CachedDirectionalLightViewMatrix[1],
                    m_CachedDirectionalLightProjectionMatrix[2] * m_CachedDirectionalLightViewMatrix[2],
                    m_CachedDirectionalLightProjectionMatrix[3] * m_CachedDirectionalLightViewMatrix[3]
                };
            }
        }
    }

}