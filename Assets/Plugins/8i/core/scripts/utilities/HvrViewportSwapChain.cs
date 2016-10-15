using HVR.Interface;
using UnityEngine;
using UnityEngine.Rendering;

namespace HVR
{
    public class HvrViewportSwapChain
    {
        int m_maximumViewports = 0;
        HvrViewportInterface[] m_viewports = null;
        int m_viewportIndex = 0;
        float m_currentWidth = 0;
        float m_currentHeight = 0;
        GraphicsDeviceType m_currentGraphicsDeviceType = GraphicsDeviceType.Null;

        public HvrViewportSwapChain(int maximumViewports)
        {
            m_maximumViewports = maximumViewports;
            m_viewports = new HvrViewportInterface[maximumViewports];
            m_viewportIndex = 0;
            m_currentWidth = 0;
            m_currentHeight = 0;
            m_currentGraphicsDeviceType = GraphicsDeviceType.Null;
        }

        private HvrViewportInterface _GetViewportAndFlip()
        {
            HvrViewportInterface viewport = m_viewports[m_viewportIndex];
            if (viewport == null || !viewport.IsValid())
            {
                m_viewports = new HvrViewportInterface[m_maximumViewports];
                m_viewportIndex = 0;
                for (int i = 0; i < m_maximumViewports; ++i)
                {
                    m_viewports[i] = new HvrViewportInterface();
                }
                viewport = m_viewports[m_viewportIndex];
            }

            m_viewportIndex = (m_viewportIndex + 1) % m_maximumViewports; // flip the index

            return viewport;
        }

        public HvrViewportInterface NextViewport(Matrix4x4 view, Matrix4x4 proj, float near, float far, int viewportLeft, int viewportTop, int viewportWidth, int viewportHeight)
        {
            HvrViewportInterface viewport = _GetViewportAndFlip();

            GraphicsDeviceType graphicsDeviceType = SystemInfo.graphicsDeviceType;
            viewport.SetChanged(m_currentWidth != viewportWidth || m_currentHeight != viewportHeight || m_currentGraphicsDeviceType != graphicsDeviceType);
            m_currentWidth = viewportWidth;
            m_currentHeight = viewportHeight;
            m_currentGraphicsDeviceType = graphicsDeviceType;

            viewport.SetViewMatrix(view);
            viewport.SetProjMatrix(proj);
            viewport.SetNearFarPlane(near, far);
            viewport.SetDimensions(viewportLeft, viewportTop, viewportWidth, viewportHeight);
            viewport.SetFrameBuffer(null);

            return viewport;
        }

        public HvrViewportInterface NextViewport(Camera camera, bool forceRenderToTexture)
        {
            HvrViewportInterface viewport = _GetViewportAndFlip();

            // left and top should always be 0 as the viewport we want to render in shouldn't need to be offset in x,y - Tom
            float left = 0;
            float top = 0;

            float width = camera.pixelRect.width;
            float height = camera.pixelRect.height;

            GraphicsDeviceType graphicsDeviceType = SystemInfo.graphicsDeviceType;
            viewport.SetChanged(m_currentWidth != width || m_currentHeight != height || m_currentGraphicsDeviceType != graphicsDeviceType);
            m_currentWidth = width;
            m_currentHeight = height;
            m_currentGraphicsDeviceType = graphicsDeviceType;

            bool renderToTexture = RenderTexture.active != null;

            if (forceRenderToTexture)
                renderToTexture = forceRenderToTexture;

            viewport.SetViewMatrix(camera.worldToCameraMatrix);
            viewport.SetProjMatrix(GL.GetGPUProjectionMatrix(camera.projectionMatrix, renderToTexture));
            viewport.SetNearFarPlane(camera.nearClipPlane, camera.farClipPlane);
            viewport.SetDimensions(left, top, width, height);
            viewport.SetFrameBuffer(null);

            return viewport;
        }
    }
}
