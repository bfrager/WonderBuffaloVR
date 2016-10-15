using UnityEngine;
using System;

namespace HVR.Interface
{
	public class HvrFrameBufferInterface
	{
		public Int32 handle;
		public RenderTexture renderColourBuffer;
		public RenderTexture renderDepthBuffer;
	    private int currentWidth;
	    private int currentHeight;

		public HvrFrameBufferInterface()
		{
			handle = HvrPlayerInterfaceAPI.FrameBuffer_Create();
		}

		~HvrFrameBufferInterface()
		{
			HvrPlayerInterfaceAPI.FrameBuffer_Delete(handle);
		}

		public bool IsValid()
		{
			return HvrPlayerInterfaceAPI.FrameBuffer_IsValid(handle);
		}

		public bool Resize(float width, float height)
		{
	        if ( currentWidth != width || currentHeight != height )
	        {
	            currentWidth = (int) width;
	            currentHeight = (int) height;

	            //Color
	            if (renderColourBuffer != null)
	            {
	            	renderColourBuffer.Release();
	            	renderColourBuffer = null;
	            }

	            renderColourBuffer = new RenderTexture(currentWidth, currentHeight, 0, RenderTextureFormat.ARGB32);
	            renderColourBuffer.filterMode = FilterMode.Point;
	            renderColourBuffer.useMipMap = false;
	            renderColourBuffer.generateMips = false;
	            renderColourBuffer.isPowerOfTwo = false;
	            renderColourBuffer.Create();
	            renderColourBuffer.hideFlags = HideFlags.HideAndDontSave;

	            //Depth
	            if (renderDepthBuffer != null)
	            {
                    renderDepthBuffer.Release();
	            	renderDepthBuffer = null;
	            }
	            
				renderDepthBuffer = new RenderTexture(currentWidth, currentHeight, 16, RenderTextureFormat.Depth);
				renderDepthBuffer.filterMode = FilterMode.Point;
				renderDepthBuffer.useMipMap = false;
				renderDepthBuffer.generateMips = false;
				renderDepthBuffer.isPowerOfTwo = false;
				renderDepthBuffer.Create();
				renderDepthBuffer.hideFlags = HideFlags.HideAndDontSave;

	            HvrPlayerInterfaceAPI.FrameBuffer_SetTextures(handle, currentWidth, currentHeight, renderColourBuffer.GetNativeTexturePtr(), renderDepthBuffer.GetNativeTexturePtr());

	            float[] sizeFloat = new float[2];
	            sizeFloat[0] = width;
	            sizeFloat[1] = height;
	            HvrPlayerInterfaceAPI.FrameBuffer_SetSize(handle, sizeFloat);

	            return true;
	        }
	        return false;
		}
	}
}