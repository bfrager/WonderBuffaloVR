using UnityEngine;
using UnityEngine.Rendering;

namespace HVR.Interface
{
    public class HvrRenderInterface
	{
		const int MAXIMUM_VIEWPORTS = 2;
		GraphicsDeviceType m_initializedGraphicsDeviceType = GraphicsDeviceType.Null;
		HvrViewportInterface[] m_viewports = new HvrViewportInterface [MAXIMUM_VIEWPORTS];
		HvrFrameBufferInterface m_frameBuffer;
		int m_viewportIndex = 0;

		public HvrFrameBufferInterface frameBuffer
		{
            get 
            {
                return m_frameBuffer;
            }
		}

		public HvrViewportInterface CurrentViewport() 
		{
			HvrViewportInterface viewport = m_viewports[m_viewportIndex];
			if (viewport == null || !viewport.IsValid() || m_initializedGraphicsDeviceType != SystemInfo.graphicsDeviceType)
			{
				InitializeViewports();
				viewport = m_viewports[m_viewportIndex];
			}
            return viewport;
		}

		public HvrViewportInterface FlipViewport()
		{
			HvrViewportInterface viewport = CurrentViewport();
			m_viewportIndex = (m_viewportIndex + 1) % MAXIMUM_VIEWPORTS;
			return viewport;
		}

		private void InitializeViewports()
		{			
			m_initializedGraphicsDeviceType = SystemInfo.graphicsDeviceType;
			m_frameBuffer = new HvrFrameBufferInterface();
			m_viewports = new HvrViewportInterface [MAXIMUM_VIEWPORTS];
			m_viewportIndex = 0;

			for ( int i = 0; i < MAXIMUM_VIEWPORTS; ++i )
			{
                HvrViewportInterface viewport = new HvrViewportInterface();
				viewport.SetFrameBuffer(m_frameBuffer);
				m_viewports[i] = viewport;
			}
		}
    }
}
