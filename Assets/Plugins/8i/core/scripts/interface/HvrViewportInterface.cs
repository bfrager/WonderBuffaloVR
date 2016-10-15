using UnityEngine;
using System;

namespace HVR.Interface
{
	public class HvrViewportInterface
	{
		public Int32 handle;
		public HvrFrameBufferInterface frameBuffer = null;
		float[] viewMatrix = new float[16];
		float[] projectionMatrix = new float[16];
		bool m_changed = false;

		public HvrViewportInterface()
		{
			handle = HvrPlayerInterfaceAPI.Viewport_Create();
		}

		~HvrViewportInterface()
		{
			HvrPlayerInterfaceAPI.Viewport_Delete(handle);
		}

		public bool IsChanged()
		{
			return m_changed;
		}

		public bool IsValid()
		{
			return HvrPlayerInterfaceAPI.Viewport_IsValid(handle);
		}

		public void SetViewMatrix(Matrix4x4 view)
		{
			float[] viewF = HvrPlayerInterfaceHelper.GetFloatsFromMatrix(view, viewMatrix);
			HvrPlayerInterfaceAPI.Viewport_SetViewMatrix(handle, viewF);
		}

		public void SetProjMatrix(Matrix4x4 proj)
		{
			float[] projF = HvrPlayerInterfaceHelper.GetFloatsFromMatrix(proj, projectionMatrix);
			HvrPlayerInterfaceAPI.Viewport_SetProjMatrix(handle, projF);
		}

		public void SetNearFarPlane(float near, float far)
		{
			HvrPlayerInterfaceAPI.Viewport_SetNearFarPlane(handle, near, far);
		}

		public void SetDimensions(float x, float y, float width, float height)
		{
			HvrPlayerInterfaceAPI.Viewport_SetDimensions(handle, x, y, width, height);
		}

		public void SetFrameBuffer(HvrFrameBufferInterface target)
		{
			frameBuffer = target;
			HvrPlayerInterfaceAPI.Viewport_SetFrameBuffer(handle, frameBuffer != null ? frameBuffer.handle : 0);
        }

        public void SetChanged(bool changed)
        {
        	m_changed = changed;
        }
	}
}
