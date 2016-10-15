using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace HVR
{
#if UNITY_EDITOR
    public static class EditorHelper
    {
        public static bool IsSceneViewCamera(Camera cam)
        {
            Camera[] internalCameras = InternalEditorUtility.GetSceneViewCameras();
            for (int i = 0; i < internalCameras.Length; i++)
            {
                if (internalCameras[i] == cam)
                    return true;
            }
            return false;
        }

        public static System.Object GetMainGameView()
        {
            System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            System.Reflection.MethodInfo GetMainGameView = T.GetMethod("GetMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            System.Object obj = GetMainGameView.Invoke(null, null);

            return obj;
        }

        /// <summary>
        /// Used to get assets of a certain type and file extension from entire project
        /// </summary>
        /// <param name="type">The type to retrieve. eg typeof(GameObject).</param>
        /// <param name="fileExtension">The file extention the type uses eg ".prefab".</param>
        /// <returns>An Object array of assets.</returns>
        public static T[] GetProjectAssetsOfType<T>(string fileExtension) where T : UnityEngine.Object
        {
            List<T> tempObjects = new List<T>();
            DirectoryInfo directory = new DirectoryInfo(Application.dataPath);
            FileInfo[] goFileInfo = directory.GetFiles("*" + fileExtension, SearchOption.AllDirectories);

            int i = 0;
            int goFileInfoLength = goFileInfo.Length;
            FileInfo tempGoFileInfo; string tempFilePath;
            T tempGO;
            for (; i < goFileInfoLength; i++)
            {
                tempGoFileInfo = goFileInfo[i];
                if (tempGoFileInfo == null)
                    continue;

                tempFilePath = tempGoFileInfo.FullName;
                tempFilePath = tempFilePath.Replace(@"\", "/").Replace(Application.dataPath, "Assets");
                tempGO = AssetDatabase.LoadAssetAtPath(tempFilePath, typeof(T)) as T;
                if (tempGO == null)
                {
                    continue;
                }
                else if (!(tempGO is T))
                {
                    continue;
                }

                tempObjects.Add(tempGO);
            }

            return tempObjects.ToArray();
        }

        public static string GetFullPathToAsset(UnityEngine.Object asset)
        {
            string datapath = Application.dataPath;
            datapath = datapath.Substring(0, datapath.Length - 6);

            string assetPath = AssetDatabase.GetAssetPath(asset.GetInstanceID());

            return datapath + assetPath;
        }

        public static string[] GetEnabledScenesInBuild()
        {
            return (from scene in EditorBuildSettings.scenes where scene.enabled select scene.path).ToArray();
        }

        public static string[] GetAllScenesInBuild()
        {
            return (from scene in EditorBuildSettings.scenes select scene.path).ToArray();
        }

        public static string[] GetAllScenes()
        {
            return (from scene in AssetDatabase.GetAllAssetPaths() where scene.EndsWith(".unity") select scene).ToArray();
        }

        public static void Draw(Bounds bounds, Transform transform)
        {
            Vector3 v3Center = bounds.center;
            Vector3 v3Extents = bounds.extents;

            Vector3 v3FrontTopLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z);  // Front top left corner
            Vector3 v3FrontTopRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z);  // Front top right corner
            Vector3 v3FrontBottomLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z);  // Front bottom left corner
            Vector3 v3FrontBottomRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z);  // Front bottom right corner
            Vector3 v3BackTopLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z);  // Back top left corner
            Vector3 v3BackTopRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z);  // Back top right corner
            Vector3 v3BackBottomLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z);  // Back bottom left corner
            Vector3 v3BackBottomRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z);  // Back bottom right corner

            v3FrontTopLeft = transform.TransformPoint(v3FrontTopLeft);
            v3FrontTopRight = transform.TransformPoint(v3FrontTopRight);
            v3FrontBottomLeft = transform.TransformPoint(v3FrontBottomLeft);
            v3FrontBottomRight = transform.TransformPoint(v3FrontBottomRight);
            v3BackTopLeft = transform.TransformPoint(v3BackTopLeft);
            v3BackTopRight = transform.TransformPoint(v3BackTopRight);
            v3BackBottomLeft = transform.TransformPoint(v3BackBottomLeft);
            v3BackBottomRight = transform.TransformPoint(v3BackBottomRight);

            Gizmos.DrawLine(v3FrontTopLeft, v3FrontTopRight);
            Gizmos.DrawLine(v3FrontTopRight, v3FrontBottomRight);
            Gizmos.DrawLine(v3FrontBottomRight, v3FrontBottomLeft);
            Gizmos.DrawLine(v3FrontBottomLeft, v3FrontTopLeft);

            Gizmos.DrawLine(v3BackTopLeft, v3BackTopRight);
            Gizmos.DrawLine(v3BackTopRight, v3BackBottomRight);
            Gizmos.DrawLine(v3BackBottomRight, v3BackBottomLeft);
            Gizmos.DrawLine(v3BackBottomLeft, v3BackTopLeft);

            Gizmos.DrawLine(v3FrontTopLeft, v3BackTopLeft);
            Gizmos.DrawLine(v3FrontTopRight, v3BackTopRight);
            Gizmos.DrawLine(v3FrontBottomRight, v3BackBottomRight);
            Gizmos.DrawLine(v3FrontBottomLeft, v3BackBottomLeft);
        }
    }
#endif

    public static class ImageEffectHelper
    {
        public static bool IsSupported(Shader s, bool needDepth, bool needHdr, MonoBehaviour effect)
        {
            if (s == null || !s.isSupported)
            {
                Debug.LogWarningFormat("Missing shader for image effect {0}", effect);
                return false;
            }

            if (!SystemInfo.supportsImageEffects || !SystemInfo.supportsRenderTextures)
            {
                Debug.LogWarningFormat("Image effects aren't supported on this device ({0})", effect);
                return false;
            }

            if (needDepth && !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth))
            {
                Debug.LogWarningFormat("Depth textures aren't supported on this device ({0})", effect);
                return false;
            }

            if (needHdr && !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
            {
                Debug.LogWarningFormat("Floating point textures aren't supported on this device ({0})", effect);
                return false;
            }

            return true;
        }

        public static Material CheckShaderAndCreateMaterial(Shader s)
        {
            if (s == null || !s.isSupported)
                return null;

            var material = new Material(s);
            material.hideFlags = HideFlags.DontSave;
            return material;
        }

        public static bool supportsDX11
        {
            get { return SystemInfo.graphicsShaderLevel >= 50 && SystemInfo.supportsComputeShaders; }
        }

        public class PostEffectsBase : MonoBehaviour
        {
            protected bool supportHDRTextures = true;
            protected bool supportDX11 = false;
            protected bool isSupported = true;

            protected Material CheckShaderAndCreateMaterial(Shader s, Material m2Create)
            {
                if (!s)
                {
                    Debug.Log("Missing shader in " + ToString());
                    enabled = false;
                    return null;
                }

                if (s.isSupported && m2Create && m2Create.shader == s)
                    return m2Create;

                if (!s.isSupported)
                {
                    NotSupported();
                    Debug.Log("The shader " + s.ToString() + " on effect " + ToString() + " is not supported on this platform!");
                    return null;
                }
                else
                {
                    m2Create = new Material(s);
                    m2Create.hideFlags = HideFlags.DontSave;
                    if (m2Create)
                        return m2Create;
                    else return null;
                }
            }


            protected Material CreateMaterial(Shader s, Material m2Create)
            {
                if (!s)
                {
                    Debug.Log("Missing shader in " + ToString());
                    return null;
                }

                if (m2Create && (m2Create.shader == s) && (s.isSupported))
                    return m2Create;

                if (!s.isSupported)
                {
                    return null;
                }
                else
                {
                    m2Create = new Material(s);
                    m2Create.hideFlags = HideFlags.DontSave;
                    if (m2Create)
                        return m2Create;
                    else return null;
                }
            }

            void OnEnable()
            {
                isSupported = true;
            }

            protected bool CheckSupport()
            {
                return CheckSupport(false);
            }


            public virtual bool CheckResources()
            {
                Debug.LogWarning("CheckResources () for " + ToString() + " should be overwritten.");
                return isSupported;
            }


            protected void Start()
            {
                CheckResources();
            }

            protected bool CheckSupport(bool needDepth)
            {
                isSupported = true;
                supportHDRTextures = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf);
                supportDX11 = SystemInfo.graphicsShaderLevel >= 50 && SystemInfo.supportsComputeShaders;

                if (!SystemInfo.supportsImageEffects || !SystemInfo.supportsRenderTextures)
                {
                    NotSupported();
                    return false;
                }

                if (needDepth && !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth))
                {
                    NotSupported();
                    return false;
                }

                if (needDepth)
                    GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;

                return true;
            }

            protected bool CheckSupport(bool needDepth, bool needHdr)
            {
                if (!CheckSupport(needDepth))
                    return false;

                if (needHdr && !supportHDRTextures)
                {
                    NotSupported();
                    return false;
                }

                return true;
            }


            public bool Dx11Support()
            {
                return supportDX11;
            }


            protected void ReportAutoDisable()
            {
                Debug.LogWarning("The image effect " + ToString() + " has been disabled as it's not supported on the current platform.");
            }

            // deprecated but needed for old effects to survive upgrading
            bool CheckShader(Shader s)
            {
                Debug.Log("The shader " + s.ToString() + " on effect " + ToString() + " is not part of the Unity 3.2+ effects suite anymore. For best performance and quality, please ensure you are using the latest Standard Assets Image Effects (Pro only) package.");
                if (!s.isSupported)
                {
                    NotSupported();
                    return false;
                }
                else
                {
                    return false;
                }
            }


            protected void NotSupported()
            {
                enabled = false;
                isSupported = false;
                return;
            }


            protected void DrawBorder(RenderTexture dest, Material material)
            {
                float x1;
                float x2;
                float y1;
                float y2;

                RenderTexture.active = dest;
                bool invertY = true; // source.texelSize.y < 0.0ff;
                                     // Set up the simple Matrix
                GL.PushMatrix();
                GL.LoadOrtho();

                for (int i = 0; i < material.passCount; i++)
                {
                    material.SetPass(i);

                    float y1_; float y2_;
                    if (invertY)
                    {
                        y1_ = 1.0f; y2_ = 0.0f;
                    }
                    else
                    {
                        y1_ = 0.0f; y2_ = 1.0f;
                    }

                    // left
                    x1 = 0.0f;
                    x2 = 0.0f + 1.0f / (dest.width * 1.0f);
                    y1 = 0.0f;
                    y2 = 1.0f;
                    GL.Begin(GL.QUADS);

                    GL.TexCoord2(0.0f, y1_); GL.Vertex3(x1, y1, 0.1f);
                    GL.TexCoord2(1.0f, y1_); GL.Vertex3(x2, y1, 0.1f);
                    GL.TexCoord2(1.0f, y2_); GL.Vertex3(x2, y2, 0.1f);
                    GL.TexCoord2(0.0f, y2_); GL.Vertex3(x1, y2, 0.1f);

                    // right
                    x1 = 1.0f - 1.0f / (dest.width * 1.0f);
                    x2 = 1.0f;
                    y1 = 0.0f;
                    y2 = 1.0f;

                    GL.TexCoord2(0.0f, y1_); GL.Vertex3(x1, y1, 0.1f);
                    GL.TexCoord2(1.0f, y1_); GL.Vertex3(x2, y1, 0.1f);
                    GL.TexCoord2(1.0f, y2_); GL.Vertex3(x2, y2, 0.1f);
                    GL.TexCoord2(0.0f, y2_); GL.Vertex3(x1, y2, 0.1f);

                    // top
                    x1 = 0.0f;
                    x2 = 1.0f;
                    y1 = 0.0f;
                    y2 = 0.0f + 1.0f / (dest.height * 1.0f);

                    GL.TexCoord2(0.0f, y1_); GL.Vertex3(x1, y1, 0.1f);
                    GL.TexCoord2(1.0f, y1_); GL.Vertex3(x2, y1, 0.1f);
                    GL.TexCoord2(1.0f, y2_); GL.Vertex3(x2, y2, 0.1f);
                    GL.TexCoord2(0.0f, y2_); GL.Vertex3(x1, y2, 0.1f);

                    // bottom
                    x1 = 0.0f;
                    x2 = 1.0f;
                    y1 = 1.0f - 1.0f / (dest.height * 1.0f);
                    y2 = 1.0f;

                    GL.TexCoord2(0.0f, y1_); GL.Vertex3(x1, y1, 0.1f);
                    GL.TexCoord2(1.0f, y1_); GL.Vertex3(x2, y1, 0.1f);
                    GL.TexCoord2(1.0f, y2_); GL.Vertex3(x2, y2, 0.1f);
                    GL.TexCoord2(0.0f, y2_); GL.Vertex3(x1, y2, 0.1f);

                    GL.End();
                }

                GL.PopMatrix();
            }
        }
    }

    public class RenderTextureUtility
    {
        //Temporary render texture handling
        private List<RenderTexture> m_TemporaryRTs = new List<RenderTexture>();

        public RenderTexture GetTemporaryRenderTexture(int width, int height, int depthBuffer = 0, RenderTextureFormat format = RenderTextureFormat.ARGBHalf, FilterMode filterMode = FilterMode.Bilinear)
        {
            var rt = RenderTexture.GetTemporary(width, height, depthBuffer, format);
            rt.filterMode = filterMode;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.name = "RenderTextureUtilityTempTexture";
            m_TemporaryRTs.Add(rt);
            return rt;
        }

        public void ReleaseTemporaryRenderTexture(RenderTexture rt)
        {
            if (rt == null)
                return;

            if (!m_TemporaryRTs.Contains(rt))
            {
                Debug.LogErrorFormat("Attempting to remove texture that was not allocated: {0}", rt);
                return;
            }

            m_TemporaryRTs.Remove(rt);
            RenderTexture.ReleaseTemporary(rt);
        }

        public void ReleaseAllTemporaryRenderTextures()
        {
            for (int i = 0; i < m_TemporaryRTs.Count; ++i)
                RenderTexture.ReleaseTemporary(m_TemporaryRTs[i]);

            m_TemporaryRTs.Clear();
        }
    }

    public static class UniqueIdRegistry
    {
        public static Dictionary<String, int> Mapping = new Dictionary<String, int>();

        public static string GetNewID()
        {
            string id = Guid.NewGuid().ToString();

            // Make sure the id is unique
            while (Contains(id))
                id = Guid.NewGuid().ToString();

            return id;
        }

        public static bool Contains(string id)
        {
            return Mapping.ContainsKey(id);
        }

        public static void Deregister(String id)
        {
            Mapping.Remove(id);
        }

        public static void Register(String id, int instanceID)
        {
            if (!Contains(id))
                Mapping.Add(id, instanceID);
        }

        public static int GetInstanceId(string id)
        {
            return Mapping[id];
        }
    }

    public class AABB
    {
        Vector3 m_Min;
        Vector3 m_Max;
        bool m_VertexSynced;
        Vector3[] m_CachedVert;
        int[] m_idx = new int[]
        {
            0, 1, 2, 2, 1, 3, // front 
            4, 5, 6, 6, 5, 7, // back
            5, 0, 7, 7, 0, 2, // left
            1, 4, 3, 3, 4, 6, // right
            5, 4, 0, 0, 4, 1, // top
            2, 3, 7, 7, 3, 6 // bottom
        };

        public Vector3 min
        {
            get
            {
                return m_Min;
            }

            set
            {
                m_Min = value;
                m_VertexSynced = false;
            }
        }
        public Vector3 max
        {
            get
            {
                return m_Max;
            }

            set
            {
                m_Max = value;
                m_VertexSynced = false;
            }
        }

        public AABB(Vector3 min, Vector3 max)
        {
            m_Min = min;
            m_Max = max;
        }

        public static AABB Create(Vector3[] vertices)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (Vector3 v in vertices)
            {
                if (v.x < min.x)
                {
                    min.x = v.x;
                }
                if (v.y < min.y)
                {
                    min.y = v.y;
                }

                if (v.z < min.z)
                {
                    min.z = v.z;
                }

                if (v.x > max.x)
                {
                    max.x = v.x;
                }
                if (v.y > max.y)
                {
                    max.y = v.y;
                }

                if (v.z > max.z)
                {
                    max.z = v.z;
                }
            }

            return new AABB(min, max);
        }

        public int[] Indices
        {
            get
            {
                return m_idx;
            }
        }

        public void CalcVertex(ref Vector3[] vertices)
        {
            if (m_CachedVert == null || !m_VertexSynced)
            {
                m_CachedVert = new Vector3[8];

                // front
                m_CachedVert[0] = new Vector3(m_Min.x, m_Max.y, m_Max.z);
                m_CachedVert[1] = new Vector3(m_Max.x, m_Max.y, m_Max.z);
                m_CachedVert[2] = new Vector3(m_Min.x, m_Min.y, m_Max.z);
                m_CachedVert[3] = new Vector3(m_Max.x, m_Min.y, m_Max.z);

                // back
                m_CachedVert[4] = new Vector3(m_Max.x, m_Max.y, m_Min.z);
                m_CachedVert[5] = new Vector3(m_Min.x, m_Max.y, m_Min.z);
                m_CachedVert[6] = new Vector3(m_Max.x, m_Min.y, m_Min.z);
                m_CachedVert[7] = new Vector3(m_Min.x, m_Min.y, m_Min.z);

                m_VertexSynced = true;
            }

            // copy out
            for (int i = 0; i < m_CachedVert.Length; ++i)
            {
                vertices[i] = m_CachedVert[i];
            }
        }

        public Bounds ToBounds()
        {
            Bounds bounds = new Bounds();
            bounds.SetMinMax(m_Min, m_Max);
            return bounds;
        }

    }

    public static class FrustumBuilder
    {
        public static void GenerateVertex(float near, float far, float fovDeg, float aspect, out Vector3[] nearPlaneVertices, out Vector3[] farPlaneVertices)
        {
            float halfFOV = fovDeg * Mathf.Deg2Rad * 0.5f;
            float Hnear = Mathf.Tan(halfFOV) * near * 2;
            float Wnear = Hnear * aspect;
            float Hfar = Mathf.Tan(halfFOV) * far * 2;
            float Wfar = Hfar * aspect;

            nearPlaneVertices = new Vector3[4];
            farPlaneVertices = new Vector3[4];

            nearPlaneVertices[0] = new Vector3(-Wnear * 0.5f, Hnear * 0.5f, near);
            nearPlaneVertices[1] = new Vector3(Wnear * 0.5f, Hnear * 0.5f, near);
            nearPlaneVertices[2] = new Vector3(-Wnear * 0.5f, -Hnear * 0.5f, near);
            nearPlaneVertices[3] = new Vector3(Wnear * 0.5f, -Hnear * 0.5f, near);

            farPlaneVertices[0] = new Vector3(-Wfar * 0.5f, Hfar * 0.5f, far);
            farPlaneVertices[1] = new Vector3(Wfar * 0.5f, Hfar * 0.5f, far);
            farPlaneVertices[2] = new Vector3(-Wfar * 0.5f, -Hfar * 0.5f, far);
            farPlaneVertices[3] = new Vector3(Wfar * 0.5f, -Hfar * 0.5f, far);
        }

        public static Mesh Create(Vector3[] nearPlane, Vector3[] farPlane)
        {
            Vector3[] vertices = new Vector3[8];

            System.Array.Copy(nearPlane, vertices, nearPlane.Length);
            System.Array.Copy(farPlane, 0, vertices, 4, farPlane.Length);

            int[] indices = new int[]
            {
                0, 1, 2, 2, 1, 3, // near plane
                5, 4, 7, 7, 4, 6, // far plane

                4, 5, 0, 0, 5, 1, // ceiling
                2, 3, 6, 6, 3, 7, // floor

                6, 4, 2, 2, 4, 0, // left wall
                5, 7, 1, 1, 7, 3// right wall
            };

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = indices;
            return mesh;
        }
    }

    public static class BoundsBuilder
    {
        public static Bounds GetBoundsForCamera(Camera cam)
        {
            Vector3[] nearPlaneVertices;
            Vector3[] farPlaneVertices;
            Vector3[] totalVertices = new Vector3[8];

            FrustumBuilder.GenerateVertex(cam.nearClipPlane, cam.farClipPlane,
                            cam.fieldOfView, cam.aspect, out nearPlaneVertices, out farPlaneVertices);

            System.Array.Copy(nearPlaneVertices, totalVertices, nearPlaneVertices.Length);
            System.Array.Copy(farPlaneVertices, 0, totalVertices, 4, farPlaneVertices.Length);

            // transform frustum vertices into world space
            for (int i = 0; i < totalVertices.Length; ++i)
            {
                totalVertices[i] = cam.transform.localToWorldMatrix.MultiplyPoint(totalVertices[i]);
            }

            AABB aabb = AABB.Create(totalVertices);
            return aabb.ToBounds();
        }

        public static BoundingSphere GetBoundingSphereForCamera(Camera cam)
        {
            return GetBoundingSphereForCameraCascade(cam, 1.0f);
        }

        public static BoundingSphere GetBoundingSphereForCameraCascade(Camera cam, float percent)
        {
            // in camera space
            BoundingSphere sphere = new BoundingSphere();

            float theta_v = cam.fieldOfView * Mathf.Deg2Rad; // vertical theta
            float diagonal = Mathf.Sqrt(cam.pixelWidth * cam.pixelWidth + cam.pixelHeight * cam.pixelHeight);
            float theta = 2.0f * Mathf.Atan((diagonal / (float)cam.pixelHeight) * Mathf.Tan(theta_v * 0.5f)); // calculate the diagonal theta, which is used in the bounding sphere calculation

            float far = cam.farClipPlane * percent;
            float near = cam.nearClipPlane;

            float R = far / (2.0f * Mathf.Cos(theta * 0.5f) * Mathf.Cos(theta * 0.5f));
            sphere.radius = R;
            Vector3 center = new Vector3(0, 0, R - near);
            sphere.position = cam.transform.localToWorldMatrix.MultiplyPoint(center); // world center position

            return sphere;
        }
    }

    public static class CameraHelper
    {
        public static Camera GetMainCamera()
        {
            Camera cam = Camera.main; // or Camera.current?
                                      // basically check if "SteamVR.active" is true without requiring the SteamVR imported
            Type typeSteamVR = Type.GetType("SteamVR");
            if (typeSteamVR != null)
            {
                System.Reflection.PropertyInfo property = typeSteamVR.GetProperty("active");
                if (property != null)
                {
                    bool isActive = (bool)property.GetValue(null, null);
                    if (isActive)
                    {
                        Type typeSteamVRCamera = Type.GetType("SteamVR_Camera");
                        if (typeSteamVRCamera != null)
                        {
                            // SteamVR conceals the real Camera so find it out, note GetComponentInChild() won't do the work
                            for (int i = 0; i < cam.transform.childCount; ++i)
                            {
                                Transform childTransform = cam.transform.GetChild(i);
                                if (childTransform.GetComponent(typeSteamVRCamera) && childTransform.GetComponent<Camera>())
                                {
                                    cam = childTransform.GetComponent<Camera>();
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return cam;
        }

        public static bool IsLayerVisibleInCullingMask(int layer, int cullingMask)
        {
            return (((1 << layer) & cullingMask) != 0);
        }
    }

    public static class CompositeBufferUtils
    {
        public static Mesh GenerateQuad()
        {
            Vector3[] vertices = new Vector3[4] {
                new Vector3( 1.0f, 1.0f, 0.0f),
                new Vector3(-1.0f, 1.0f, 0.0f),
                new Vector3(-1.0f,-1.0f, 0.0f),
                new Vector3( 1.0f,-1.0f, 0.0f),
            };
            int[] indices = new int[6] { 0, 1, 2, 2, 3, 0 };

            Mesh r = new Mesh();
            r.vertices = vertices;
            r.triangles = indices;
            return r;
        }

        public static Mesh GenerateDetailedQuad()
        {
            const int div_x = 325;
            const int div_y = 200;

            var cell = new Vector2(2.0f / div_x, 2.0f / div_y);
            var vertices = new Vector3[65000];
            var indices = new int[(div_x - 1) * (div_y - 1) * 6];
            for (int iy = 0; iy < div_y; ++iy)
            {
                for (int ix = 0; ix < div_x; ++ix)
                {
                    int i = div_x * iy + ix;
                    vertices[i] = new Vector3(cell.x * ix - 1.0f, cell.y * iy - 1.0f, 0.0f);
                }
            }
            for (int iy = 0; iy < div_y - 1; ++iy)
            {
                for (int ix = 0; ix < div_x - 1; ++ix)
                {
                    int i = ((div_x - 1) * iy + ix) * 6;
                    indices[i + 0] = (div_x * (iy + 1)) + (ix + 1);
                    indices[i + 1] = (div_x * (iy + 0)) + (ix + 1);
                    indices[i + 2] = (div_x * (iy + 0)) + (ix + 0);

                    indices[i + 3] = (div_x * (iy + 0)) + (ix + 0);
                    indices[i + 4] = (div_x * (iy + 1)) + (ix + 0);
                    indices[i + 5] = (div_x * (iy + 1)) + (ix + 1);
                }
            }

            Mesh r = new Mesh();
            r.vertices = vertices;
            r.triangles = indices;
            return r;
        }
    }

    public class Statistic
    {
        float m_avg = 0.0f;
        float m_min = float.MaxValue;
        float m_max = 0.0f;
        int m_samples = 0;
        const int SAMPLES_PER_BLOCK = 30;

        public void Accumulate(float metric)
        {
            if (m_samples >= SAMPLES_PER_BLOCK)
            {
                m_samples = 0;
                m_avg = 0.0f;
                m_min = float.MaxValue;
                m_max = 0.0f;
            }
            m_samples += 1;
            m_min = Math.Min(metric, m_min);
            m_max = Math.Max(metric, m_max);
            m_avg += (metric - m_avg) / m_samples;
        }

        public float Avg()
        {
            return m_avg;
        }

        public float Min()
        {
            if (m_min == float.MaxValue)
                return 0.0f;

            return m_min;
        }

        public float Max()
        {
            return m_max;
        }
    }
}
