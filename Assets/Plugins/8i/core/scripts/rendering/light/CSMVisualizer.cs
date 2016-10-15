using UnityEngine;

namespace HVR
{
    [RequireComponent(typeof(Light))]
    public class CSMVisualizer : MonoBehaviour
    {
        Light m_DirectionalLight;
        public Camera m_ShootingCamera;

        // Use this for initialization
        void Start()
        {
            EnsureLight();
            m_ShootingCamera = m_ShootingCamera == null ? Camera.main : m_ShootingCamera;
        }

        void EnsureLight()
        {
            if (m_DirectionalLight == null)
            {
                m_DirectionalLight = GetComponent<Light>();
            }
        }

        Mesh GenerateLightSpaceAABBMesh(Vector3[] frustumVertices)
        {
            EnsureLight();

            Matrix4x4 view = m_DirectionalLight.transform.worldToLocalMatrix;
            
            for(int i = 0; i < frustumVertices.Length; ++i)
            {
                frustumVertices[i] = view.MultiplyPoint(frustumVertices[i]);
            }

            Vector3[] vertices = new Vector3[8];

            AABB aabb = AABB.Create(frustumVertices);
            aabb.CalcVertex(ref vertices);

            // mesh vertices in Light Space
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = aabb.Indices;
            return mesh;
        }

        Bounds GenerateLightSpaceBounds(Vector3[] frustumVertices)
        {
            EnsureLight();

            Matrix4x4 view = m_DirectionalLight.transform.worldToLocalMatrix;

            for (int i = 0; i < frustumVertices.Length; ++i)
            {
                frustumVertices[i] = view.MultiplyPoint(frustumVertices[i]);
            }

            AABB aabb = AABB.Create(frustumVertices);
            return aabb.ToBounds();
        }

        // Update is called once per frame
        void Update()
        {
            Vector4[] frustumColors = new Vector4[]
            {
                new Vector4(121.0f/255.0f, 107.0f/255.0f, 130.0f/255.0f, 0.2f),
                new Vector4(116.0f/255.0f, 130.0f/255.0f, 107.0f/255.0f, 0.2f),
                new Vector4(128.0f/255.0f, 130.0f/255.0f, 107.0f/255.0f, 0.2f),
                new Vector4(130.0f/255.0f, 110.0f/255.0f, 107.0f/255.0f, 0.2f),
            };

            Vector4[] aabbColors = new Vector4[]
            {
                new Vector4(121.0f/255.0f, 107.0f/255.0f, 130.0f/255.0f, 0.5f),
                new Vector4(116.0f/255.0f, 130.0f/255.0f, 107.0f/255.0f, 0.5f),
                new Vector4(128.0f/255.0f, 130.0f/255.0f, 107.0f/255.0f, 0.5f),
                new Vector4(130.0f/255.0f, 110.0f/255.0f, 107.0f/255.0f, 0.5f),
            };

            Vector3[] nearPlaneVertices;
            Vector3[] farPlaneVertices;
            Vector3[] totalVertices = new Vector3[8];

            // fetch quality settings, 
            // NOTE: assume camera is perspective
            switch (QualitySettings.shadowCascades)
            {
                case 2:
                    {

                        float frustumSplit = QualitySettings.shadowCascade2Split;
                        float near = m_ShootingCamera.nearClipPlane;
                        float far = m_ShootingCamera.farClipPlane * frustumSplit;

                        FrustumBuilder.GenerateVertex(near, far,
                            m_ShootingCamera.fieldOfView, m_ShootingCamera.aspect, out nearPlaneVertices, out farPlaneVertices);
                        Mesh mesh1 = FrustumBuilder.Create(nearPlaneVertices, farPlaneVertices);
                        Shader shader = Shader.Find("Hidden/8i/TransparentColor");
                        Material material = new Material(shader);
                        MaterialPropertyBlock block1 = new MaterialPropertyBlock();
                        block1.SetVector("_Color", frustumColors[0]);
                        
                        Graphics.DrawMesh(mesh1, m_ShootingCamera.transform.position, m_ShootingCamera.transform.rotation, material, 0, null, 0, block1);


                        near = m_ShootingCamera.farClipPlane * frustumSplit;
                        far = m_ShootingCamera.farClipPlane;
                        FrustumBuilder.GenerateVertex(near, far,
                            m_ShootingCamera.fieldOfView, m_ShootingCamera.aspect, out nearPlaneVertices, out farPlaneVertices);
                        Mesh mesh2 = FrustumBuilder.Create(nearPlaneVertices, farPlaneVertices);
                        MaterialPropertyBlock block2 = new MaterialPropertyBlock();
                        block2.SetVector("_Color", frustumColors[1]);

                        Graphics.DrawMesh(mesh2, m_ShootingCamera.transform.position, m_ShootingCamera.transform.rotation, material, 0, null, 0, block2);

                    }
                    break;

                case 4:
                    {
                        Vector3 frustumSplit = QualitySettings.shadowCascade4Split;

                        // segment 1
                        float near = m_ShootingCamera.nearClipPlane;
                        float far = m_ShootingCamera.farClipPlane * frustumSplit.x;

                        FrustumBuilder.GenerateVertex(near, far,
                            m_ShootingCamera.fieldOfView, m_ShootingCamera.aspect, out nearPlaneVertices, out farPlaneVertices);
                        Mesh mesh1 = FrustumBuilder.Create(nearPlaneVertices, farPlaneVertices);
                        Shader shader = Shader.Find("Hidden/8i/TransparentColor");
                        Material material = new Material(shader);
                        MaterialPropertyBlock block1 = new MaterialPropertyBlock();
                        block1.SetVector("_Color", frustumColors[0]);
                        
                        Graphics.DrawMesh(mesh1, m_ShootingCamera.transform.position, m_ShootingCamera.transform.rotation, material, 0, null, 0, block1);

                        System.Array.Copy(nearPlaneVertices, totalVertices, nearPlaneVertices.Length);
                        System.Array.Copy(farPlaneVertices, 0, totalVertices, 4, farPlaneVertices.Length);

                        // transform frustum vertices into world space
                        for(int i = 0; i < totalVertices.Length; ++i)
                        {
                            totalVertices[i] = m_ShootingCamera.transform.localToWorldMatrix.MultiplyPoint(totalVertices[i]);
                        }
                        Mesh aabbMesh1 = GenerateLightSpaceAABBMesh(totalVertices);
                        MaterialPropertyBlock block1AABB = new MaterialPropertyBlock();
                        block1AABB.SetVector("_Color", aabbColors[0]);
                        Graphics.DrawMesh(aabbMesh1, m_DirectionalLight.transform.position, m_DirectionalLight.transform.rotation, material, 0, null, 0, block1AABB);

                        // segment 2
                        near = m_ShootingCamera.farClipPlane * frustumSplit.x;
                        far = m_ShootingCamera.farClipPlane * frustumSplit.y;
                        FrustumBuilder.GenerateVertex(near, far,
                            m_ShootingCamera.fieldOfView, m_ShootingCamera.aspect, out nearPlaneVertices, out farPlaneVertices);
                        Mesh mesh2 = FrustumBuilder.Create(nearPlaneVertices, farPlaneVertices);
                        MaterialPropertyBlock block2 = new MaterialPropertyBlock();
                        block2.SetVector("_Color", frustumColors[1]);

                        Graphics.DrawMesh(mesh2, m_ShootingCamera.transform.position, m_ShootingCamera.transform.rotation, material, 0, null, 0, block2);

                        System.Array.Copy(nearPlaneVertices, totalVertices, nearPlaneVertices.Length);
                        System.Array.Copy(farPlaneVertices, 0, totalVertices, 4, farPlaneVertices.Length);

                        // transform frustum vertices into world space
                        for (int i = 0; i < totalVertices.Length; ++i)
                        {
                            totalVertices[i] = m_ShootingCamera.transform.localToWorldMatrix.MultiplyPoint(totalVertices[i]);
                        }
                        Mesh aabbMesh2 = GenerateLightSpaceAABBMesh(totalVertices);
                        MaterialPropertyBlock block2AABB = new MaterialPropertyBlock();
                        block2AABB.SetVector("_Color", aabbColors[1]);
                        Graphics.DrawMesh(aabbMesh2, m_DirectionalLight.transform.position, m_DirectionalLight.transform.rotation, material, 0, null, 0, block2AABB);

                        // segment 3
                        near = m_ShootingCamera.farClipPlane * frustumSplit.y;
                        far = m_ShootingCamera.farClipPlane * frustumSplit.z;
                        FrustumBuilder.GenerateVertex(near, far,
                            m_ShootingCamera.fieldOfView, m_ShootingCamera.aspect, out nearPlaneVertices, out farPlaneVertices);
                        Mesh mesh3 = FrustumBuilder.Create(nearPlaneVertices, farPlaneVertices);
                        MaterialPropertyBlock block3 = new MaterialPropertyBlock();
                        block3.SetVector("_Color", frustumColors[2]);

                        Graphics.DrawMesh(mesh3, m_ShootingCamera.transform.position, m_ShootingCamera.transform.rotation, material, 0, null, 0, block3);

                        System.Array.Copy(nearPlaneVertices, totalVertices, nearPlaneVertices.Length);
                        System.Array.Copy(farPlaneVertices, 0, totalVertices, 4, farPlaneVertices.Length);

                        // transform frustum vertices into world space
                        for (int i = 0; i < totalVertices.Length; ++i)
                        {
                            totalVertices[i] = m_ShootingCamera.transform.localToWorldMatrix.MultiplyPoint(totalVertices[i]);
                        }
                        Mesh aabbMesh3 = GenerateLightSpaceAABBMesh(totalVertices);
                        MaterialPropertyBlock block3AABB = new MaterialPropertyBlock();
                        block3AABB.SetVector("_Color", aabbColors[2]);
                        Graphics.DrawMesh(aabbMesh3, m_DirectionalLight.transform.position, m_DirectionalLight.transform.rotation, material, 0, null, 0, block3AABB);


                        // segment 4
                        near = m_ShootingCamera.farClipPlane * frustumSplit.z;
                        far = m_ShootingCamera.farClipPlane;
                        FrustumBuilder.GenerateVertex(near, far,
                            m_ShootingCamera.fieldOfView, m_ShootingCamera.aspect, out nearPlaneVertices, out farPlaneVertices);
                        Mesh mesh4 = FrustumBuilder.Create(nearPlaneVertices, farPlaneVertices);
                        MaterialPropertyBlock block4 = new MaterialPropertyBlock();
                        block4.SetVector("_Color", frustumColors[3]);

                        Graphics.DrawMesh(mesh4, m_ShootingCamera.transform.position, m_ShootingCamera.transform.rotation, material, 0, null, 0, block4);

                        System.Array.Copy(nearPlaneVertices, totalVertices, nearPlaneVertices.Length);
                        System.Array.Copy(farPlaneVertices, 0, totalVertices, 4, farPlaneVertices.Length);

                        // transform frustum vertices into world space
                        for (int i = 0; i < totalVertices.Length; ++i)
                        {
                            totalVertices[i] = m_ShootingCamera.transform.localToWorldMatrix.MultiplyPoint(totalVertices[i]);
                        }
                        Mesh aabbMesh4 = GenerateLightSpaceAABBMesh(totalVertices);
                        MaterialPropertyBlock block4AABB = new MaterialPropertyBlock();
                        block4AABB.SetVector("_Color", aabbColors[3]);
                        Graphics.DrawMesh(aabbMesh4, m_DirectionalLight.transform.position, m_DirectionalLight.transform.rotation, material, 0, null, 0, block4AABB);
                    }
                    break;
                default:
                    {

                        FrustumBuilder.GenerateVertex(m_ShootingCamera.nearClipPlane, m_ShootingCamera.farClipPlane,
                            m_ShootingCamera.fieldOfView, m_ShootingCamera.aspect, out nearPlaneVertices, out farPlaneVertices);
                        Mesh mesh = FrustumBuilder.Create(nearPlaneVertices, farPlaneVertices);
                        Shader shader = Shader.Find("Hidden/8i/TransparentColor");
                        Material material = new Material(shader);
                        MaterialPropertyBlock block1 = new MaterialPropertyBlock();
                        block1.SetVector("_Color", frustumColors[0]);

                        Graphics.DrawMesh(mesh, m_ShootingCamera.transform.position, m_ShootingCamera.transform.rotation, material, 0, null, 0, block1);

                        System.Array.Copy(nearPlaneVertices, totalVertices, nearPlaneVertices.Length);
                        System.Array.Copy(farPlaneVertices, 0, totalVertices, 4, farPlaneVertices.Length);

                        // transform frustum vertices into world space
                        for (int i = 0; i < totalVertices.Length; ++i)
                        {
                            totalVertices[i] = m_ShootingCamera.transform.localToWorldMatrix.MultiplyPoint(totalVertices[i]);
                        }
                        Mesh aabbMesh = GenerateLightSpaceAABBMesh(totalVertices);
                        MaterialPropertyBlock blockAABB = new MaterialPropertyBlock();
                        blockAABB.SetVector("_Color", aabbColors[0]);
                        Graphics.DrawMesh(aabbMesh, m_DirectionalLight.transform.position, m_DirectionalLight.transform.rotation, material, 0, null, 0, blockAABB);

                    }
                    break;
            }
        }

        public Bounds GetLightSpaceBounds()
        {
            Vector3[] nearPlaneVertices;
            Vector3[] farPlaneVertices;
            Vector3[] totalVertices = new Vector3[8];

            FrustumBuilder.GenerateVertex(m_ShootingCamera.nearClipPlane, m_ShootingCamera.farClipPlane,
                            m_ShootingCamera.fieldOfView, m_ShootingCamera.aspect, out nearPlaneVertices, out farPlaneVertices);

            System.Array.Copy(nearPlaneVertices, totalVertices, nearPlaneVertices.Length);
            System.Array.Copy(farPlaneVertices, 0, totalVertices, 4, farPlaneVertices.Length);

            // transform frustum vertices into world space
            for (int i = 0; i < totalVertices.Length; ++i)
            {
                totalVertices[i] = m_ShootingCamera.transform.localToWorldMatrix.MultiplyPoint(totalVertices[i]);
            }
            return GenerateLightSpaceBounds(totalVertices);
        }

        public BoundingSphere GetLightSpaceBoundingSphere()
        {
            EnsureLight();
            // in world space
            BoundingSphere sphere = BoundsBuilder.GetBoundingSphereForCamera(m_ShootingCamera);
            Matrix4x4 viewLS = m_DirectionalLight.transform.worldToLocalMatrix;
            // in camera space
            sphere.position = viewLS.MultiplyPoint(sphere.position);

            return sphere;
        }

    }

}