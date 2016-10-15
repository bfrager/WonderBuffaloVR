using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace HVR.Interface
{
	public class HvrAssetInterface
	{
		public Int32 handle;
		public HvrAssetInterface(string fileFolder)
		{
			// Make sure that `Player_Initialise()` is called before 
			// `Asset_Create()` so that file formats and codecs are 
			// registered before they are used.
            HvrPlayerInterfaceAPI.Player_Initialise();
            handle = HvrPlayerInterfaceAPI.Asset_Create(fileFolder);
			LogMeta();
        }

        ~HvrAssetInterface()
		{
			HvrPlayerInterfaceAPI.Asset_Delete(handle);
		}

        public void Delete()
        {
            HvrPlayerInterfaceAPI.Asset_Delete(handle);
        }

		public bool IsValid()
		{
			return HvrPlayerInterfaceAPI.Asset_IsValid(handle);
		}

		public void LogMeta()
		{
			int count = HvrPlayerInterfaceAPI.Asset_GetMetaCount(handle);

			for (int i = 0; i < count; ++i)
			{
				StringBuilder key = new StringBuilder(256);
				StringBuilder val = new StringBuilder(256);
				string log = i + " - ";
				if (HvrPlayerInterfaceAPI.Asset_GetMetaEntry(handle, i, key, val))
				{
					log += key.ToString() + " = " + val.ToString();
				}
				Debug.Log(log + "\n");
			}
		}

		public void Play()
		{
			HvrPlayerInterfaceAPI.Asset_Play(handle);
		}

		public void SetEnableCache(bool enable)
		{
			HvrPlayerInterfaceAPI.Asset_SetEnableCache(handle, enable);
		}

		public void ClearCache()
		{
			HvrPlayerInterfaceAPI.Asset_ClearCache(handle);
		}

		public void Pause()
		{
			HvrPlayerInterfaceAPI.Asset_Pause(handle);
		}

		public void Seek(float time)
		{
			HvrPlayerInterfaceAPI.Asset_Seek(handle, time);
		}

		public void Step(int frames)
		{
			HvrPlayerInterfaceAPI.Asset_Step(handle, frames);
		}

	    public void SetLooping(bool looping)
	    {
	        HvrPlayerInterfaceAPI.Asset_SetLooping(handle, looping);
	    }

        public int GetState()
        {
            return HvrPlayerInterfaceAPI.Asset_GetState(handle);
        }

		public float GetCurrentTime()
		{
			return HvrPlayerInterfaceAPI.Asset_GetCurrentTime(handle);
		}

		public float GetDuration()
		{
			return HvrPlayerInterfaceAPI.Asset_GetDuration(handle);
		}

		public void SetRenderMethodType(int renderMethod)
		{
			HvrPlayerInterfaceAPI.Asset_SetRenderMethodType(handle, renderMethod);
		}
	}
}
