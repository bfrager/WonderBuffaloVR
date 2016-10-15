using UnityEngine;
using UnityEngine.Rendering;
using HVR.Interface;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HVR
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]

    public class HvrShadowRender : ImageEffectHelper.PostEffectsBase
    {
        HvrRenderInterface m_renderInterface;
        HvrViewportInterface m_viewPort;

        Mesh commandBuffer_mesh;

        CommandBuffer activeCommandBuffer;
        CommandBuffer collectHVRScreenShadowCB;

        RenderTexture screenShadowRT;
        Shader shadowCollectShader;
        Material shadowCollectMaterial;

        Material directionalShadow_material;
        Shader directionalShadow_shader;

        public bool visualiseShadowCascading;

        public HvrRenderInterface renderInterface
        {
            get
            {
                if (m_renderInterface == null)
                {
                    m_renderInterface = new HvrRenderInterface();
                }

                return m_renderInterface;
            }
        }
        public HvrViewportInterface ViewPort
        {
            get
            {
                return m_viewPort;
            }
        }
        public RenderTexture ScreenSpaceShadow
        {
            get
            {
                return screenShadowRT;
            }
        }
        public Camera cam
        {
            get
            {
                return gameObject.GetComponent<Camera>();
            }
        }

        void Awake()
        {
#if UNITY_EDITOR
            EditorApplication.playmodeStateChanged += OnPlaymodeStateChanged;
#endif
        }

#if UNITY_EDITOR
        void OnPlaymodeStateChanged()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // exit from playing
                //Debug.Log("Switching editor from PLAYING to STOP");
                OnDisable();
                OnEnable();
            }
        }
#endif

        void OnDestroy()
        {
#if UNITY_EDITOR
            EditorApplication.playmodeStateChanged -= OnPlaymodeStateChanged;
#endif
        }

        void OnEnable()
        {
            if (commandBuffer_mesh == null)
            {
                commandBuffer_mesh = CompositeBufferUtils.GenerateQuad();
                commandBuffer_mesh.hideFlags = HideFlags.HideAndDontSave;
            }

            if (directionalShadow_shader == null)
                directionalShadow_shader = Resources.Load("8i/shaders/HVRShadowDirectional") as Shader;

            if (directionalShadow_material == null)
                directionalShadow_material = new Material(directionalShadow_shader);

            if (activeCommandBuffer == null)
            {
                activeCommandBuffer = new CommandBuffer();
                activeCommandBuffer.name = "CmdBuf_HvrShadowRender";
            }

            if (collectHVRScreenShadowCB == null)
            {
                collectHVRScreenShadowCB = new CommandBuffer();
                collectHVRScreenShadowCB.name = "Collect HVR Screen Space Shadow";
            }

            if (screenShadowRT == null)
            {
                screenShadowRT = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.R8); // use less fancy format
                screenShadowRT.name = "HVR screen space shadow";
            }

            if (shadowCollectShader == null)
                shadowCollectShader = Resources.Load("8i/shaders/HVRShadowCollect") as Shader;

            if (shadowCollectMaterial == null)
                shadowCollectMaterial = new Material(shadowCollectShader);

            cam.AddCommandBuffer(CameraEvent.AfterForwardOpaque, collectHVRScreenShadowCB);
            cam.AddCommandBuffer(CameraEvent.AfterForwardOpaque, activeCommandBuffer);
        }

        void OnDisable()
        {
            Camera cam = GetComponent<Camera>();

            if (cam != null && activeCommandBuffer != null)
            {
                cam.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, activeCommandBuffer);
                activeCommandBuffer.Release();
                activeCommandBuffer = null;
            }

            if (cam != null && collectHVRScreenShadowCB != null)
            {
                cam.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, collectHVRScreenShadowCB);
                collectHVRScreenShadowCB.Release();
                collectHVRScreenShadowCB = null;
            }

            if (screenShadowRT != null)
            {
                screenShadowRT.Release();
                screenShadowRT = null;
            }
        }

        public override bool CheckResources()
        {
            isSupported = CheckSupport(true, true);
            return isSupported;
        }

        void OnPreRender()
        {
            if (CheckResources() == false)
                return;

            int width = cam.pixelWidth;
            int height = cam.pixelHeight;

            m_viewPort = renderInterface.FlipViewport();
            m_viewPort.SetViewMatrix(cam.worldToCameraMatrix);

            m_viewPort.SetProjMatrix(GL.GetGPUProjectionMatrix(cam.projectionMatrix, false));
            m_viewPort.SetNearFarPlane(cam.nearClipPlane, cam.farClipPlane);
            m_viewPort.SetDimensions(0, 0, width, height);
            m_viewPort.frameBuffer.Resize(width, height);

            // Render viewport
            HvrStaticInterface.Self().RenderCamera(this, m_viewPort);

            CommandBuffer_Refill(cam, m_viewPort);
        }

        void CommandBuffer_Refill(Camera cam, HvrViewportInterface viewport)
        {
            // STEP 0: Bake depth into the camera depth texture, in order to serve post effects.
            // NOTE: this step has been moved to Actor class, to reduce dependence on HvrRender

            // STEP 1: Set target as a screen space shadow texture, for reach kind of light(spot, directional, point),
            // draw HVR screen space shadow additively
            collectHVRScreenShadowCB.Clear();
            collectHVRScreenShadowCB.SetRenderTarget(screenShadowRT);
            collectHVRScreenShadowCB.ClearRenderTarget(true, true, Color.white);

            collectHVRScreenShadowCB.SetGlobalTexture("_oDEP", viewport.frameBuffer.renderDepthBuffer);
            Matrix4x4 inverseViewProjection = (cam.projectionMatrix * cam.worldToCameraMatrix).inverse;
            collectHVRScreenShadowCB.SetGlobalMatrix("_ViewProjectInverse", inverseViewProjection);

            // foreach light
            foreach (HvrShadowCaster shadowCaster in HvrShadowCasterCollector.instance.GetShadowCasters())
            {
                // TODO: feed more data into _HVRLightShadowData
                float fake_strength = 0.5f + (1.0f - shadowCaster.GetShadowStrength()) * 0.5f;
                collectHVRScreenShadowCB.SetGlobalVector("_HVRLightShadowData", new Vector4(fake_strength, 0, 0, 0));

                float oneOverDimension = 1.0f / (float)shadowCaster.GetShadowmapDimension();
                Vector4 texelInfo = new Vector4();
                texelInfo.x = (float)shadowCaster.GetShadowmapDimension();
                texelInfo.y = (float)shadowCaster.GetShadowmapDimension();
                texelInfo.w = oneOverDimension;
                texelInfo.z = oneOverDimension;

                if (shadowCollectMaterial != null)
                {
                    if (shadowCaster.IsShadowMultiSampled())
                    {
                        shadowCollectMaterial.EnableKeyword("SHADOWS_SOFT");
                    }
                    else
                    {
                        shadowCollectMaterial.DisableKeyword("SHADOWS_SOFT");
                    }
                }

                if (shadowCaster.GetLightType() == LightType.Directional)
                {
                    collectHVRScreenShadowCB.SetGlobalTexture("_oLSDEP", shadowCaster.GetDirectionalLightspaceGeomDepthTexture());
                    collectHVRScreenShadowCB.SetGlobalMatrix("_LSViewProject", shadowCaster.GetLightspaceViewProjectionMatrix());
                    collectHVRScreenShadowCB.SetGlobalVector("_HVRMapTexelSize", texelInfo);

                    collectHVRScreenShadowCB.DrawMesh(commandBuffer_mesh, Matrix4x4.identity, shadowCollectMaterial, 0, 1);

                }
                else if (shadowCaster.GetLightType() == LightType.Spot)
                {
                    collectHVRScreenShadowCB.SetGlobalTexture("_oLSDEP", shadowCaster.GetLightspaceDepthTexture());
                    collectHVRScreenShadowCB.SetGlobalMatrix("_LSViewProject", shadowCaster.GetLightspaceViewProjectionMatrix());

                    collectHVRScreenShadowCB.SetGlobalVector("_LightPosRange", shadowCaster.GetWorldspaceLightPositionRangeVector());
                    collectHVRScreenShadowCB.SetGlobalVector("_LightDirectionAngle", shadowCaster.GetWorldspaceLightDirectionAngleVector());
                    collectHVRScreenShadowCB.SetGlobalVector("_HVRMapTexelSize", texelInfo);

                    collectHVRScreenShadowCB.DrawMesh(commandBuffer_mesh, Matrix4x4.identity, shadowCollectMaterial, 0, 0);
                }
                else if (shadowCaster.GetLightType() == LightType.Point)
                {
                    collectHVRScreenShadowCB.SetGlobalTexture("_oLSDEPCUBE", shadowCaster.GetLightspaceDepthTexture()); // this should be a copy of Unity's shadow cubemap
                    collectHVRScreenShadowCB.SetGlobalVector("_LightPosRange", shadowCaster.GetWorldspaceLightPositionRangeVector()); // requires point light position and 1/range

                    collectHVRScreenShadowCB.DrawMesh(commandBuffer_mesh, Matrix4x4.identity, shadowCollectMaterial, 0, 2);
                }
            }

            // STEP 2: Multiply current drawn scene with HVR screen space shadowed texture generated above
            // This gives a multi-light HVR with shadow on
            // NOTE: this steps has been moved to Actor class, to reduce dependence on HvrRender
            activeCommandBuffer.Clear();

            // STEP 3: Shadow resolve for directional light. 
            // Directional light in Unity uses a variation of cascaded shadow map, which parameters are unknown or hard to guess.
            // In order to generate shadows in directional light for HVR, we use a separate shadow map(actuall two shadow maps, more on this later)
            // to write HVR depth and scene depth onto. 
            // In this step, we need to resolve shadow based on our version of shadow map. So we need to render every mesh that receives shadow and for
            // every directional light.
            Matrix4x4 proj = Matrix4x4.Perspective(cam.fieldOfView, cam.aspect, cam.nearClipPlane, cam.farClipPlane);

            // for every directional lights, we render the mesh 
            // 1. inside camera frustum,
            // 2. and enabled,
            // 3. and as shadow receiver,
            // 4. and can NOT be a shadow only mesh
            foreach (HvrShadowCaster shadowCaster in HvrShadowCasterCollector.instance.GetShadowCasters())
            {
                if (shadowCaster.GetLightType() == LightType.Directional)
                {
                    if (directionalShadow_material != null)
                    {
                        if (shadowCaster.IsShadowMultiSampled())
                        {
                            directionalShadow_material.EnableKeyword("SHADOWS_SOFT");
                        }
                        else
                        {
                            directionalShadow_material.DisableKeyword("SHADOWS_SOFT");
                        }

                        if (visualiseShadowCascading)
                        {
                            directionalShadow_material.EnableKeyword("DEBUG_CASCADE_ON");
                        }
                        else
                        {
                            directionalShadow_material.DisableKeyword("DEBUG_CASCADE_ON");
                        }
                    }

                    float oneOverDimension = 1.0f / (float)shadowCaster.GetShadowmapDimension();
                    Vector4 texelInfo = new Vector4();
                    texelInfo.x = (float)shadowCaster.GetShadowmapDimension();
                    texelInfo.y = (float)shadowCaster.GetShadowmapDimension();
                    texelInfo.w = oneOverDimension;
                    texelInfo.z = oneOverDimension;

                    activeCommandBuffer.SetGlobalVector("_HVRMapTexelSize", texelInfo);

                    activeCommandBuffer.SetGlobalTexture("_texLSDepth", shadowCaster.GetDirectionalLightspaceHVRDepthTexture());
                    // light space
                    activeCommandBuffer.SetGlobalMatrix("_matLSViewProject", shadowCaster.GetLightspaceViewProjectionMatrix());

                    // feed bounding sphere data
                    Matrix4x4 boundingSphereData = new Matrix4x4();
                    BoundingSphere[] boundingSphereList = shadowCaster.GetDirectionalLightBoundingSphere();
                    for (int i = 0; i < 4; ++i)
                    {
                        boundingSphereData.SetRow(i,
                            new Vector4(boundingSphereList[i].position.x, boundingSphereList[i].position.y, boundingSphereList[i].position.z, boundingSphereList[i].radius));
                    }
                    activeCommandBuffer.SetGlobalMatrix("_matShadowSplitSphere", boundingSphereData);

                    Vector4 shadowSplitSquaredRadii = new Vector4();
                    for (int i = 0; i < 4; ++i)
                    {
                        shadowSplitSquaredRadii[i] = boundingSphereList[i].radius * boundingSphereList[i].radius;
                    }
                    activeCommandBuffer.SetGlobalVector("_vecShadowSplitSqRadii", shadowSplitSquaredRadii);

                    // no support for setting matrix array into command buffer in 5.3.5, so have to do this one by one
                    Matrix4x4[] world2Shadow = shadowCaster.GetDirectionalLightWorld2ShadowMatrix();
                    for (int i = 0; i < 4; ++i)
                    {
                        activeCommandBuffer.SetGlobalMatrix("_matWorld2Shadow" + i.ToString(), world2Shadow[i]);
                    }

                    float fake_strength = 0.85f + ((1.0f - 0.85f) * (1.0f - shadowCaster.GetShadowStrength()));
                    activeCommandBuffer.SetGlobalVector("_HVRLightShadowData", new Vector4(fake_strength, 0, 0, 0));

                    Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(cam);
                    MeshRenderer[] meshRenderers = GameObject.FindObjectsOfType<MeshRenderer>();

                    foreach (MeshRenderer meshRenderer in meshRenderers)
                    {
                        // if shadow only, do not draw the mesh at all
                        if (meshRenderer.enabled && meshRenderer.receiveShadows && meshRenderer.shadowCastingMode != ShadowCastingMode.ShadowsOnly)
                        {
                            MeshFilter filter;
                            if (filter = meshRenderer.GetComponent<MeshFilter>())
                            {
                                if (filter.sharedMesh != null && GeometryUtility.TestPlanesAABB(frustumPlanes, meshRenderer.bounds))
                                {
                                    for (int i = 0; i < filter.sharedMesh.subMeshCount; ++i)
                                    {
                                        activeCommandBuffer.SetGlobalMatrix("_matModel", meshRenderer.transform.localToWorldMatrix);
                                        // NOTE: In Unity 5.4.0b24 with OpenVR as the VR runtime, it will strangely render the mesh even without correct clip space position
                                        // The guess is the command buffer happens on camera's post render event, and Unity is hijacking the matrix that feed into the vertex stream,
                                        // which causes problem when it fails to guess the "right" matrix. It turns out(from testing blindly) if you can utilise the "built-in" matrix
                                        // and it turns out to be okay, except the model matrix is ignored(weird, but reasonable, since it only make sense to hijack the view & projection
                                        // matrix and leave the model matrix untouched). So we need to set "UNITY_MATRIX_VP" here and multiply the vertex with it and the "_matModel" above
                                        // to make all these rendering correct, under OpenVR and Unity 5.4.0b24.
                                        activeCommandBuffer.SetGlobalMatrix("UNITY_MATRIX_VP", GL.GetGPUProjectionMatrix(proj, true) * cam.worldToCameraMatrix);
                                        activeCommandBuffer.DrawMesh(filter.sharedMesh, Matrix4x4.identity, directionalShadow_material, i, 0);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
