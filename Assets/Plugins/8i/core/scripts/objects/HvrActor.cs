//
// 8i Plugin for Unity
//

using HVR.Interface;
using UnityEngine;
using System;
using HVR.Android;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HVR
{
    [ExecuteInEditMode]
    public class HvrActor : MonoBehaviour
    {
        #region Public Properties

        public enum eRenderMethod
        {
            standard,
            direct
        };

        public HvrActorInterface actorInterface
        {
            get
            {
                if (m_actorInterface == null)
                    m_actorInterface = new HvrActorInterface();

                return m_actorInterface;
            }
        }

        public HvrAsset hvrAsset;

        /// <summary>
        /// In the Editor this refers to the GUID of a folder or file in the project.
        /// In a build this refers to a ID that is used to find a path relative to the built exe.
        /// </summary>
        public string dataGuid
        {
            get
            {
                return m_dataGuid;
            }
            set
            {
                m_dataGuid = value;
            }
        }

        /// <summary>
        /// The material used for standard rendering
        /// </summary>
        public Material material
        {
            get
            {
                if (m_material == null)
                {
                    Shader shader = Resources.Load("8i/shaders/HVRActor_Standard") as Shader;
                    m_material = new Material(shader);
                }

                return m_material;
            }
        }

        /// <summary>
        /// Render method within Unity
        /// </summary>
        public eRenderMethod actorRenderMethod;

        /// <summary>
        /// Render method within the HvrEngine
        /// </summary>
        public HvrAsset.eRenderMethod assetRenderMethod
        {
            get
            {
                return m_assetRenderMethod;
            }
            set
            {
                // add a setter guard here. setting the render method every frame will wipe out the rendered content and display nothing
                if (m_assetRenderMethod != value)
                {
                    m_assetRenderMethod = value;

                    if (hvrAsset != null)
                        hvrAsset.SetRenderMethod(m_assetRenderMethod);
                }
            }
        }

        /// <summary>
        /// Whether actor should use Unity's occlusion culling system when determining if it should render
        /// </summary>
        public bool occlusionCullingEnabled = false;

        /// <summary>
        /// Occlusion culling creates a sphere based on actor bounds dimensions, this allows for a radius offset
        /// </summary>
        public float occlusionCullingOffset = 0.0f;

        public CullingGroup cullingGroup
        {
            get
            {
                if (m_cullingGroup == null)
                {
                    m_cullingGroup = new CullingGroup();
                    m_cullingGroup.SetBoundingSpheres(boundingSpheres);
                    m_cullingGroup.SetBoundingSphereCount(1);
                }

                return m_cullingGroup;
            }
        }

        BoundingSphere[] boundingSpheres
        {
            get
            {
                if (m_boundingSpheres == null)
                {
                    m_boundingSpheres = new BoundingSphere[1];
                }

                return m_boundingSpheres;
            }
        }

        /// <summary>
        /// Scale factor to scale the transforms scale by
        /// </summary>
        public float actorScaleFactor = 0.01f;

        public delegate void OnHvrAssetChanged(HvrAsset asset);
        public OnHvrAssetChanged onHvrAssetChanged;
        public delegate void OnDestroyEvent(HvrActor actor);
        public OnDestroyEvent onDestroy;

#if UNITY_EDITOR
        public Action editorUpdater { get; set; }
#endif

        /// <summary>
        /// InstanceID is used to detect whether an game object was duplicated, it should not be manually modified
        /// </summary>
        public int instanceID
        {
            get
            {
                return m_instanceID;
            }
        }

        #endregion

        #region Private Members

        private HvrActorInterface m_actorInterface;

        private CullingGroup m_cullingGroup;

        private BoundingSphere[] m_boundingSpheres;

        [SerializeField]
        private int m_instanceID = 0;

        [SerializeField]
        private string m_dataGuid;

        [SerializeField]
        private Material m_material;

        [SerializeField]
        private HvrAsset.eRenderMethod m_assetRenderMethod = HvrAsset.eRenderMethod.pointBlend;

        #endregion

        #region Monobehaviour Functions

        void Start()
        {
            // In the case the gameObject was duplicated
            if (WasThisGameObjectDuplicated())
                m_material = new Material(m_material); // Copies the material properties when duplicated

            // Call initialize here so that the hvrAsset gets created immediately
            Initialize();

            // Call LateUpdate in ensure that the HvrActor's transform is being set and
            // that any HvrAssets get initialized - Tom
            LateUpdate();
        }

        void OnEnable()
        {
#if UNITY_EDITOR
            EditorApplication.playmodeStateChanged -= OnEditorPlayModeChanged;
            EditorApplication.playmodeStateChanged += OnEditorPlayModeChanged;

            // If the Editor is not in 'Play Mode' make sure the actor is still updating itself
            if (!Application.isPlaying)
            {
                EditorApplication.update -= EditorUpdate;
                EditorApplication.update += EditorUpdate;
            }
#endif

            // Run initialize here as the assetInterface will be removed upon Unity Editor script recompilation
            // Calling Initialize here will make sure the actor is initialized and has an hvrAsset assigned
            Initialize();

            SetVisible(true);
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
            EditorApplication.playmodeStateChanged -= OnEditorPlayModeChanged;
#endif

            SetVisible(false);

            if (m_cullingGroup != null)
            {
                m_cullingGroup.Dispose();
                m_cullingGroup = null;
            }
        }

        void LateUpdate()
        {
            UpdateTransform();

            // Update the CullingGroup BoundingSpheres here
            if (occlusionCullingEnabled)
            {
                UpdateBoundingSpheres();
            }

            // Call this within LateUpdate so any HvrAssets that have been created get correctly initialized
            HvrStaticInterface.Self().LateUpdate();

#if UNITY_EDITOR
            CallEditorUpdater();
#endif
        }

        void OnRenderObject()
        {
            // Set visibility of actor
            SetVisible(isActiveAndEnabled);

            // Get the current camera that is rendering
            Camera cam = Camera.current;

            //// Check if camera is valid

            // This check needs to be made or else within the editor or else the actors can render into unexpected places in the Unity Editor...
            // such as the Material Preview window or FrameBuffer window. This is possible because these areas of the Editor use an internal camera to render them
            // So this checks if the camera is an object within the scene, or an editor scene view camera.
            // - Tom
            bool isGameViewCamera = false;
            bool isEditorSceneViewCamera = false;

            isGameViewCamera = cam.gameObject.activeInHierarchy;

#if UNITY_EDITOR
            isEditorSceneViewCamera = EditorHelper.IsSceneViewCamera(cam);
#endif

            // Early exit if camera is not valid
            if (!isGameViewCamera && !isEditorSceneViewCamera)
                return;

            HvrRender actorRender = cam.GetComponent<HvrRender>();

            if (actorRender != null)
            {
                // Check whether this camera's culling mask allows this layer
                if (isActiveAndEnabled && CameraHelper.IsLayerVisibleInCullingMask(gameObject.layer, cam.cullingMask))
                {
                    if (!actorRender.renderPairs.ContainsKey(this))
                        actorRender.AddActor(this);
                }
                else
                {
                    // Make sure to remove the actor since it's not needed
                    if (actorRender.renderPairs.ContainsKey(this))
                        actorRender.RemoveActor(this);
                }
            }
        }

        void OnDestroy()
        {
#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
            EditorApplication.playmodeStateChanged -= OnEditorPlayModeChanged;
#endif
            if (onDestroy != null)
                onDestroy(this);
        }
        #endregion

        #region HvrActor Functions
        public virtual void Initialize()
        {
            // In the case that the dataGUID is not an empty string and the actor is null,
            // assume that this actor needs to have an asset assigned. - Tom
            if (!string.IsNullOrEmpty(m_dataGuid))
            {
                if (hvrAsset == null)
                {
                    string dataPath = GetActorDataPath();

                    // Only assign a new asset if the path is valid
                    if (!string.IsNullOrEmpty(dataPath))
                    {
                        HvrAsset asset = new HvrAsset(dataPath);
                        SetAsset(asset);
                    }
                }
            }
        }

        public virtual void SetAsset(HvrAsset _hvrAsset)
        {
            hvrAsset = _hvrAsset;

            if (hvrAsset != null)
            {
                // Make sure to set the render method immediately
                // But only if the asset needs to be changed - Tom
                if (hvrAsset.GetRenderMethod() != assetRenderMethod)
                    hvrAsset.SetRenderMethod(assetRenderMethod);

                SetAssetInterface(hvrAsset.assetInterface);

                // Always update the transform after setting a new interface - Tom
                UpdateTransform();

                if (hvrAsset != null)
                {
                    hvrAsset.onInterfaceChanged -= SetAssetInterface;
                    hvrAsset.onInterfaceChanged += SetAssetInterface;
                }
            }
            else
            {
                SetAssetInterface(null);
            }

            if (onHvrAssetChanged != null)
                onHvrAssetChanged(hvrAsset);
        }

        public virtual string GetActorDataPath()
        {
            string dataPath = "";

            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
#if UNITY_EDITOR
                    if (m_dataGuid != "")
                    {
                        string projectAssetPath = Application.dataPath;
                        projectAssetPath = projectAssetPath.Substring(0, projectAssetPath.Length - "Assets".Length);
                        if (AssetDatabase.IsValidFolder(AssetDatabase.GUIDToAssetPath(m_dataGuid)))
                        {
                            dataPath = projectAssetPath + AssetDatabase.GUIDToAssetPath(m_dataGuid);
                        }
                    }
#endif
                    break;

                case RuntimePlatform.WindowsPlayer:
                    dataPath = Application.dataPath + "/../8i/" + m_dataGuid;
                    break;

                case RuntimePlatform.Android:
                    dataPath = AndroidFileUtils.GetExternalPublicDirectory("8i/" + m_dataGuid);
                    break;

                case RuntimePlatform.IPhonePlayer:
                    dataPath = Application.dataPath + "/8i/" + m_dataGuid + "/";
                    break;
                default:
                    break;
            }

            return dataPath;
        }

        void UpdateBoundingSpheres()
        {
            Bounds b = GetBoundsAABB();
            boundingSpheres[0].position = b.center;
            boundingSpheres[0].radius = Vector3.Distance(b.center, b.max) + occlusionCullingOffset;
        }

        // Checks

        bool WasThisGameObjectDuplicated()
        {
            // http://answers.unity3d.com/questions/483434/how-to-call-a-method-when-a-gameobject-has-been-du.html
            // Catch duplication of this GameObject

            bool wasDuplicated = false;

            if (m_instanceID == 0)
            {
                m_instanceID = GetInstanceID();
            }

            if (m_instanceID != GetInstanceID() && GetInstanceID() < 0)
            {
                m_instanceID = GetInstanceID();
                wasDuplicated = true;
            }

            return wasDuplicated;
        }

        // Actor Interface

        public void SetAssetInterface(HvrAssetInterface assetInterface)
        {
            actorInterface.SetAssetInterface(assetInterface);
        }

        void SetVisible(bool visible)
        {
            actorInterface.SetVisible(visible);
        }

        public void UpdateTransform()
        {
            actorInterface.SetTransform(transform, actorScaleFactor);
        }

        public Bounds GetBoundsAABB()
        {
            return actorInterface.GetAABB();
        }

        public Bounds GetBounds()
        {
            Bounds b = actorInterface.GetBounds();

            const float CENTIMETRES_TO_METRES = 1.0f / 100.0f;

            Vector3 center = b.center;
            Vector3 size = b.size;

            center = center * CENTIMETRES_TO_METRES;
            size = size * CENTIMETRES_TO_METRES;

            center.Scale(transform.lossyScale);
            size.Scale(transform.lossyScale);

            b.center = center;
            b.size = size;

            return b;
        }

        #endregion

        #region Unity Editor Functions
#if UNITY_EDITOR

        void CallEditorUpdater()
        {
            if (this.editorUpdater != null)
            {
                this.editorUpdater();
            }
        }

        void OnEditorPlayModeChanged()
        {
            if (EditorApplication.isPlaying)
            {
                // Editor is now in 'Play' mode
                EditorApplication.update -= EditorUpdate;

                // Looks funny but actually means: in play mode and pause button is pushed
                if (EditorApplication.isPaused)
                {
                    // Attach the HvrActor to the Editor Update Loop
                    EditorApplication.update += EditorUpdate;
                }
            }
            else
            {
                // Editor is now in 'Edit' mode
                EditorApplication.update -= EditorUpdate;
            }
        }

        void EditorUpdate()
        {
            LateUpdate();
        }

        void EditorCheckDataPaths()
        {
            if (hvrAsset != null)
            {
                string dataPath = GetActorDataPath();

                if (string.IsNullOrEmpty(dataPath))
                {
                    // In the case that the data object has been deleted, clear the asset
                    hvrAsset.SetData("");
                }
                else
                {
                    // In the case that the location of the data object has changed in the project, update the actor asset
                    if (hvrAsset.dataPath != dataPath)
                    {
                        hvrAsset.SetData(dataPath);
                    }
                }
            }
        }

        void OnDrawGizmos()
        {
            // HACKY - Call this within OnDrawGizmos because it's a simpler place to run checks in
            // the Editor without attaching them to EditorApplication.update - Tom
            EditorCheckDataPaths();

            // DrawBounds here in order to draw an invisible, selectable cube around the actor
            DrawBounds(false);
        }

        void OnDrawGizmosSelected()
        {
            DrawBounds(true);
            DrawDebugInfo();
        }

        public void DrawDebugInfo()
        {
            if (occlusionCullingEnabled)
            {
                Gizmos.color = new Color(0.0f, 0.8f, 0.0f, 0.8f);
                Gizmos.DrawWireSphere(boundingSpheres[0].position, boundingSpheres[0].radius);
            }
        }

        public void DrawBounds(bool selected)
        {
            Matrix4x4 origMatrix = Gizmos.matrix;
            Color origColor = GUI.color;

            Bounds b = GetBounds();
            var col = new Color(0.0f, 0.7f, 1f, 1.0f);
            col.a = 0.0f;

            // Set Gizmos.matrix here in order for the transform to affect Gizmos.DrawCube below
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

            Gizmos.color = col;
            Gizmos.DrawCube(b.center, b.size);

            if (selected)
            {
                col.a = 0.5f;
                Gizmos.color = col;
                Gizmos.DrawWireCube(b.center, b.size);
            }

            Gizmos.matrix = origMatrix;
        }
#endif
        #endregion
    }
}
