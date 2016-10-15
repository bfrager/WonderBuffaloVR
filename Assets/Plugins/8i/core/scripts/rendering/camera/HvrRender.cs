using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using System.Collections.Generic;
using HVR.Interface;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace HVR
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("8i/Render/HVR Render")]

    public class HvrRender : ImageEffectHelper.PostEffectsBase
    {
        public class RenderPackage
        {
            public RenderTexture color;
            public RenderTexture depth;

            public Mesh quadMesh;

            public const CameraEvent cmdBufCameraEvent = CameraEvent.AfterDepthTexture;
            public const string cmdBufNamePrefix = "8i_HvrActor_";

            public Statistic stat_preRender = new Statistic();
            public Statistic stat_postRender = new Statistic();

            const int MAXIMUM_VIEWPORTS = 2;
            public HvrViewportSwapChain viewportSwapChain = new HvrViewportSwapChain(MAXIMUM_VIEWPORTS);

            private CommandBuffer cmdbuf_depthComposite;
            private Material cmdBuf_depthCompositeMaerial;

            private string uniqueId;

            public RenderPackage(Camera cam)
            {
                // Create a new unique id here so when we come to check whether the command buffer is attached to the camera we can use the ID to find it.
                // It appears that adding a command buffer to the camera creates an instance, as you cannot compare it to the original - Tom
                uniqueId = UniqueIdRegistry.GetNewID();

                Shader shader = Resources.Load("8i/shaders/HVRRender_CommandBufferDepthComposite") as Shader;
                cmdBuf_depthCompositeMaerial = new Material(shader);

                quadMesh = CompositeBufferUtils.GenerateQuad();

                cmdbuf_depthComposite = new CommandBuffer();
                cmdbuf_depthComposite.name = cmdBufNamePrefix + uniqueId;

                CreateTextures(cam.pixelWidth, cam.pixelHeight);
            }

            // Render Buffers
            public bool UpdateRenderBufferSizes(int pixelWidth, int pixelHeight)
            {
                bool changed = false;

                if ((color.width != pixelWidth || color.height != pixelHeight) ||
                    (depth.width != pixelWidth || depth.height != pixelHeight))
                {
                    CreateTextures(pixelWidth, pixelHeight);

                    changed = true;
                }

                return changed;
            }

            public void CreateTextures(int pixelWidth, int pixelHeight)
            {
                if (color)
                    color.Release();

                if (depth)
                    depth.Release();

                // RenderTextureReadWrite.Linear so that the colors within the textures are never modified as they go through the pipeline
                // http://docs.unity3d.com/ScriptReference/RenderTextureReadWrite.html
                // When using Gamma color space, no conversions are done of any kind, and this setting is not used.
                // When Linear color space is used, then by default non-HDR render textures are considered to contain sRGB data (i.e. "regular colors"), and fragment shaders are considered to output linear color values.
                // So by default the fragment shader color value is converted into sRGB when rendering into a texture; and when sampling the texture in the shader the sRGB colors are converted into linear values.

                color = new RenderTexture(pixelWidth, pixelHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
                {
                    name = "Color",
                    generateMips = false,
                    useMipMap = false,
                    filterMode = FilterMode.Bilinear,
                    anisoLevel = 0,
                    hideFlags = HideFlags.DontSave
                };
                color.Create();

                // http://docs.unity3d.com/ScriptReference/RenderTextureReadWrite.html
                // "Note that some render texture formats are always considered to contain "linear" data and
                // no sRGB conversions are ever performed on them, no matter what is the read-write setting.
                // This is true for all "HDR" (floating point) formats, and other formats like Depth or Shadowmap."

                depth = new RenderTexture(pixelWidth, pixelHeight, 16, RenderTextureFormat.Depth)
                {
                    name = "Depth",
                    generateMips = false,
                    useMipMap = false,
                    filterMode = FilterMode.Bilinear,
                    anisoLevel = 0,
                    hideFlags = HideFlags.DontSave
                };
                depth.Create();
            }

            // Command Buffer
            public void SetCommandBufferAttached(Camera cam, bool attached)
            {
                bool isCommandBufferAttached = IsCommandBufferAttachedToCamera(cam);

                if (attached && !isCommandBufferAttached)
                    cam.AddCommandBuffer(cmdBufCameraEvent, cmdbuf_depthComposite);

                if (!attached && isCommandBufferAttached)
                    cam.RemoveCommandBuffer(cmdBufCameraEvent, cmdbuf_depthComposite);
            }

            bool IsCommandBufferAttachedToCamera(Camera cam)
            {
                CommandBuffer[] cmdbuffs = cam.GetCommandBuffers(cmdBufCameraEvent);

                for (int i = 0; i < cmdbuffs.Length; i++)
                {
                    if (cmdbuffs[i].name == cmdbuf_depthComposite.name)
                        return true;
                }

                return false;
            }

            public void RenderCommandBuffer()
            {
                cmdbuf_depthComposite.Clear();
                cmdbuf_depthComposite.SetGlobalTexture("_oDEP", depth);
                cmdbuf_depthComposite.DrawMesh(quadMesh, Matrix4x4.identity, cmdBuf_depthCompositeMaerial, 0, 0);
            }

            public void Release(Camera cam)
            {
                if (color)
                    color.Release();

                if (depth)
                    depth.Release();

                if (cmdbuf_depthComposite != null)
                {
                    // NOTE: After Unity recompiles the script, it will creates new Camera and move previous command buffers to the new one. 
                    // so we need to remove command buffers here otherwise you will see command buffer stacking up on Camera.
                    // However when game stops, the camera is also destroyed. Accessing the camera here will cause MissingReferenceException.
                    // We need to check the validity of camera in this way:
                    if (cam != null && !cam.Equals(null))
                        cam.RemoveCommandBuffer(cmdBufCameraEvent, cmdbuf_depthComposite);

                    cmdbuf_depthComposite.Release();
                    cmdbuf_depthComposite = null;
                }


            }
        }

        #region Public Properties
        public enum eRenderMode
        {
            standard,
            composite,
            direct
        };

        public eRenderMode renderMode
        {
            get
            {
                return m_renderMode;
            }
            set
            {
                // setter guard
                if (m_renderMode != value)
                {
                    RemoveAllCommandBuffers();
                    m_renderMode = value;
                }
            }
        }

        public Dictionary<HvrActor, RenderPackage> renderPairs = new Dictionary<HvrActor, RenderPackage>();

        public Statistic stat_onPreRender = new Statistic();
        public Statistic stat_onPostRender = new Statistic();

        public Material compositeMaterial;
        #endregion

        #region Private Members
        RenderingPath m_currentRenderPath;

        const int MAXIMUM_VIEWPORTS = 2;
        private HvrViewportSwapChain m_viewportSwapChain = new HvrViewportSwapChain(MAXIMUM_VIEWPORTS);

        [SerializeField]
        private eRenderMode m_renderMode = eRenderMode.standard;

        private ColorSpace m_previousColorSpace;

        Camera cam
        {
            get { return gameObject.GetComponent<Camera>(); }
        }
        #endregion

        #region Monobehaviour Functions
        void Awake()
        {
#if UNITY_EDITOR
            // If this camera is a scene camera, make sure to clean it up between scene loads
            if (EditorHelper.IsSceneViewCamera(cam) && !Application.isPlaying)
            {
                onLoadScene -= ReleaseAndCleanUp;
                onLoadScene += ReleaseAndCleanUp;
            }
#endif

            m_currentRenderPath = cam.actualRenderingPath;
            m_previousColorSpace = QualitySettings.activeColorSpace;

            if (compositeMaterial == null)
            {
                Shader compositeShader = Resources.Load("8i/shaders/HVRRender_SinglePassComposite") as Shader;

                if (compositeShader != null)
                {
                    compositeMaterial = CheckShaderAndCreateMaterial(compositeShader, compositeMaterial);
                    compositeMaterial.hideFlags = HideFlags.DontSave;
                }
            }
        }

        new void Start()
        {
            ReleaseAndCleanUp();
        }

        void OnDisable()
        {
            for (int i = 0; i < renderPairs.Count; i++)
            {
                KeyValuePair<HvrActor, RenderPackage> pair = renderPairs.ElementAt(i);

                RenderPackage renderPackage = pair.Value;

                renderPackage.SetCommandBufferAttached(cam, false);
            }
        }

        void OnPreRender()
        {
            float startTime = Time.realtimeSinceStartup;

            switch (m_renderMode)
            {
                case eRenderMode.standard:
                    DoStandardRender_PreRender();
                    break;
                default:
                    // Nothing
                    break;
            }

            stat_onPreRender.Accumulate(Time.realtimeSinceStartup - startTime);
        }

        void OnPostRender()
        {
            float startTime = Time.realtimeSinceStartup;

            switch (m_renderMode)
            {
                case eRenderMode.standard:
                    DoStandardRender_PostRender();
                    break;
                case eRenderMode.composite:
                    DoCompositeRender();
                    break;
                case eRenderMode.direct:
                    DoDirectRender();
                    break;
                default:
                    // Nothing
                    break;
            }

            stat_onPostRender.Accumulate(Time.realtimeSinceStartup - startTime);
        }

        void OnDestroy()
        {
#if UNITY_EDITOR
            if (EditorHelper.IsSceneViewCamera(cam))
                onLoadScene -= ReleaseAndCleanUp;
#endif
            ReleaseAndCleanUp();
        }
        #endregion

        #region HvrRender Functions
        public void AddActor(HvrActor actor)
        {
            RenderPackage renderPackage = new RenderPackage(cam);

            renderPairs[actor] = renderPackage;

            actor.onDestroy -= RemoveActor;
            actor.onDestroy += RemoveActor;
        }

        public void RemoveActor(HvrActor actor)
        {
            // HACK - In the case that the scene is being destroyed, this function will be called by a HvrActor 'onDestroy' delegate event
            // it's possible that this component will be destroyed before it's able to call this function - Tom
            if (this == null)
                return;

            if (renderPairs.ContainsKey(actor))
            {
                RenderPackage package = renderPairs[actor];

                package.Release(cam);

                renderPairs.Remove(actor);
            }

            // In the case that this is a temporary object that shouldn't persist, remove it once the final renderpair has been removed
            if (hideFlags == HideFlags.DontSave || hideFlags == HideFlags.HideAndDontSave)
            {
                // In the case that the last renderpair was just removed from the camera, destroy this component
                if (renderPairs.Count == 0)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(this);
                    }
                    else
                    {
                        // Destroy Immediate is to be called from the editor when in edit mode
                        DestroyImmediate(this);
                    }
                }
            }
        }

        void ReleaseAndCleanUp()
        {
            foreach (KeyValuePair<HvrActor, RenderPackage> pair in renderPairs)
            {
                RenderPackage renderPackage = pair.Value;
                renderPackage.Release(cam);
            }
            renderPairs.Clear();

            // Remove any unused command buffers may still be attached to this camera
            // Without this check, command buffers would just keep stacking up after events, such as Unity recompiling code 
            RemoveAllCommandBuffers();
        }

        void RemoveAllCommandBuffers()
        {
            CommandBuffer[] cmdBuffers = cam.GetCommandBuffers(RenderPackage.cmdBufCameraEvent);

            foreach (CommandBuffer cmdbuf in cmdBuffers)
            {
                if (cmdbuf.name.StartsWith(RenderPackage.cmdBufNamePrefix))
                    cam.RemoveCommandBuffer(RenderPackage.cmdBufCameraEvent, cmdbuf);
            }
        }

        // Direct render

        void DoDirectRender()
        {
            HvrViewportInterface viewport = m_viewportSwapChain.NextViewport(cam, false);
            HvrStaticInterface.Self().RenderCamera(this, viewport);
        }

        // Composite Render

        void DoCompositeRender()
        {
            // Early exit if compositeMaterial is not set
            if (compositeMaterial == null)
                return;

            int width = cam.pixelWidth;
            int height = cam.pixelHeight;

            // Store the previous render buffers so we can restore them later.
            RenderBuffer originalColorBuffer = Graphics.activeColorBuffer;
            RenderBuffer originalDepthBuffer = Graphics.activeDepthBuffer;

            // Create temporary render textures
            RenderTexture colorTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            RenderTexture depthTexture = RenderTexture.GetTemporary(width, height, 16, RenderTextureFormat.Depth);

            // Set the rendertargets so the HvrEngine can render into them
            Graphics.SetRenderTarget(colorTexture.colorBuffer, depthTexture.depthBuffer);
            GL.Clear(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f), 1.0f);

            // Render the actor
            HvrViewportInterface viewport = m_viewportSwapChain.NextViewport(cam, false);
            HvrStaticInterface.Self().RenderCamera(this, viewport);

            // Do any color grading
            HvrColorGrading colorGrading = gameObject.GetComponent<HvrColorGrading>();
            if (colorGrading != null && colorGrading.isActiveAndEnabled)
            {
                colorGrading.Grade(colorTexture);
            }

            compositeMaterial.SetTexture("_HvrColorTex", colorTexture);
            compositeMaterial.SetTexture("_HvrDepthTex", depthTexture);

            Graphics.SetRenderTarget(originalColorBuffer, originalDepthBuffer);

            GL.PushMatrix();
            GL.LoadOrtho();

            compositeMaterial.SetPass(0);

            GL.Begin(GL.QUADS);
            GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(0, 1, 0);
            GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(1, 1, 0);
            GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(1, 0, 0);
            GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(0, 0, 0);
            GL.End();

            GL.PopMatrix();

            // Release temporary textures
            RenderTexture.ReleaseTemporary(colorTexture);
            RenderTexture.ReleaseTemporary(depthTexture);
        }

        // Standard Render

        void DoStandardRender_PreRender()
        {
            bool forceRecreateTextures = false;

            // If the rendering path has changed, recreate the buffers
            if (m_currentRenderPath != cam.actualRenderingPath)
            {
                m_currentRenderPath = cam.actualRenderingPath;
                forceRecreateTextures = true;
            }

            // If the rendering path has changed, recreate the buffers
            if (m_previousColorSpace != QualitySettings.activeColorSpace)
            {
                m_previousColorSpace = QualitySettings.activeColorSpace;
                forceRecreateTextures = true;
            }

            // Run the PreRender steps over all renderpairs
            for (int i = 0; i < renderPairs.Count; i++)
            {
                float startTime = Time.realtimeSinceStartup;

                KeyValuePair<HvrActor, RenderPackage> pair = renderPairs.ElementAt(i);

                HvrActor actor = pair.Key;
                RenderPackage renderPackage = pair.Value;

                if (forceRecreateTextures)
                    renderPackage.CreateTextures(cam.pixelWidth, cam.pixelHeight);

                bool shouldRenderActor = actor.isActiveAndEnabled;

                if (shouldRenderActor && actor.occlusionCullingEnabled)
                {
                    actor.cullingGroup.targetCamera = cam;
                    shouldRenderActor = actor.cullingGroup.IsVisible(0);
                }

                // Make sure the actor is visible 
                if (shouldRenderActor)
                {
                    switch (actor.actorRenderMethod)
                    {
                        case HvrActor.eRenderMethod.direct:
                            renderPackage.SetCommandBufferAttached(cam, false);

                            break;
                        case HvrActor.eRenderMethod.standard:
                            renderPackage.SetCommandBufferAttached(cam, true);
                            renderPackage.UpdateRenderBufferSizes(cam.pixelWidth, cam.pixelHeight);
                            StandardRender_FillRenderBuffer(actor, renderPackage);

                            break;
                    }
                }
                else
                {
                    renderPackage.SetCommandBufferAttached(cam, false);

                    switch (actor.actorRenderMethod)
                    {
                        case HvrActor.eRenderMethod.standard:
                            renderPackage.color.DiscardContents();
                            renderPackage.depth.DiscardContents();
                            break;
                    }
                }

                renderPackage.stat_preRender.Accumulate(Time.realtimeSinceStartup - startTime);
            }
        }

        void DoStandardRender_PostRender()
        {
            for (int i = 0; i < renderPairs.Count; i++)
            {
                float startTime = Time.realtimeSinceStartup;

                KeyValuePair<HvrActor, RenderPackage> pair = renderPairs.ElementAt(i);

                HvrActor actor = pair.Key;
                RenderPackage renderPackage = pair.Value;

                bool shouldRenderActor = actor.isActiveAndEnabled;

                if (shouldRenderActor && actor.occlusionCullingEnabled)
                {
                    actor.cullingGroup.targetCamera = cam;
                    shouldRenderActor = actor.cullingGroup.IsVisible(0);
                }

                if (shouldRenderActor)
                {
                    switch (actor.actorRenderMethod)
                    {
                        case HvrActor.eRenderMethod.direct:
                            StandardRender_Direct(actor, renderPackage);
                            break;

                        case HvrActor.eRenderMethod.standard:
                            renderPackage.RenderCommandBuffer();
                            StandardRender_Standard(actor, renderPackage);
                            break;
                    }
                }

                renderPackage.stat_postRender.Accumulate(Time.realtimeSinceStartup - startTime);
            }
        }

        void StandardRender_Direct(HvrActor actor, RenderPackage renderPackage)
        {
            HvrViewportInterface viewport = renderPackage.viewportSwapChain.NextViewport(cam, false);
            HvrStaticInterface.Self().RenderActor(this, actor.actorInterface, viewport);
        }

        void StandardRender_Standard(HvrActor actor, RenderPackage renderPackage)
        {
            // Composite the actor into the frame
            actor.material.SetTexture("_HvrColorTex", renderPackage.color);
            actor.material.SetTexture("_HvrDepthTex", renderPackage.depth);

            HvrShadowRender hvrShadowRender = gameObject.GetComponent<HvrShadowRender>();
            if (hvrShadowRender && hvrShadowRender.enabled)
            {
                // Multiply current drawn scene with HVR screen space shadowed texture generated above
                // This gives a multi-light HVR with shadow on
                actor.material.SetTexture("_ScreenSpaceShadowTex", hvrShadowRender.ScreenSpaceShadow);
                // should only main camera have HvrRender attached
                actor.material.EnableKeyword("RECEIVE_SHADOWS");
            }
            else
            {
                actor.material.DisableKeyword("RECEIVE_SHADOWS");
            }

            Matrix4x4 inverseViewProjection = (cam.projectionMatrix * cam.worldToCameraMatrix).inverse;
            actor.material.SetMatrix("_ViewProjectInverse", inverseViewProjection);

            actor.material.SetPass(0);

            Graphics.DrawMeshNow(renderPackage.quadMesh, Matrix4x4.identity, 0);
        }

        void StandardRender_FillRenderBuffer(HvrActor actor, RenderPackage renderPackage)
        {
            // Store the previous render buffers so we can restore them later.
            RenderBuffer originalColorBuffer = Graphics.activeColorBuffer;
            RenderBuffer originalDepthBuffer = Graphics.activeDepthBuffer;

            // Set the rendertargets so the HvrEngine can render into them
            Graphics.SetRenderTarget(renderPackage.color.colorBuffer, renderPackage.depth.depthBuffer);
            GL.Clear(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f), 1.0f);

            // Get the viewport to render
            HvrViewportInterface viewport = renderPackage.viewportSwapChain.NextViewport(cam, true);

            // Render the actor
            HvrStaticInterface.Self().RenderActor(this, actor.actorInterface, viewport);

            // Do any color grading
            HvrColorGrading colorGrading = actor.gameObject.GetComponent<HvrColorGrading>();
            if (colorGrading != null && colorGrading.isActiveAndEnabled)
            {
                colorGrading.Grade(renderPackage.color);
            }

            // Restore the previous renderbuffers
            Graphics.SetRenderTarget(originalColorBuffer, originalDepthBuffer);
        }

        #endregion

        #region Unity Editor Functions
#if UNITY_EDITOR

        public delegate void OnLoadSceneEvent();
        public static OnLoadSceneEvent onLoadScene;

        // Called on asset being double clicked and opened in UnityEditor project window
        [UnityEditor.Callbacks.OnOpenAsset]
        static bool OnOpenAsset(int instanceID, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceID).GetType() == typeof(UnityEditor.SceneAsset))
            {
                // Delay the check until the next EditorApplication tick. This will allow the check to occur
                // if the user opened a scene, but hit cancel and didn't want anything to change.
                EditorApplication.delayCall += CheckIfSceneLoaded;
            }

            return false; // we did not handle the open just listened to it
        }

        static void CheckIfSceneLoaded()
        {
            // Only clean up if the scene is NOT dirty, this will be the case if the user did not choose to load a scene
            // It will be clean if the user loaded a scene, or reloaded the current active one.
            if (EditorSceneManager.GetActiveScene().isDirty == false)
            {
                if (onLoadScene != null)
                    onLoadScene();
            }
        }
#endif
        #endregion
    }
}