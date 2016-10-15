using UnityEngine;
using System.Text;
using System.Collections.Generic;

namespace HVR.Interface
{
	public class HvrPlayerInterface
	{
		static Dictionary<string, StatValues> m_stats = new Dictionary<string, StatValues>();

		public static bool Initialise()
		{
			if (!SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL") && !SystemInfo.graphicsDeviceVersion.StartsWith("Direct3D 11"))
			{
				return false;
			}

            return HvrPlayerInterfaceAPI.Player_Initialise();
		}

		public static void UpdateTime(float time)
		{
			HvrPlayerInterfaceAPI.Player_Update(time);
		}

		public static void PrepareRender(HvrSceneInterface scene)
		{
			if (scene != null)
			{
				int eventID = HvrPlayerInterfaceAPI.Unity_Player_PrepareRender(scene.handle);
				GL.IssuePluginEvent(HvrPlayerInterfaceAPI.UnityRenderEventFunc(), eventID);
			}
		}

        public static void Render(HvrSceneInterface scene, HvrViewportInterface viewport)
		{
			if (scene != null && viewport != null)
			{
                HvrFrameBufferInterface frameBuffer = viewport.frameBuffer;
                if (frameBuffer != null)
                {
                    int clearEventID = HvrPlayerInterfaceAPI.Unity_FrameBuffer_Clear(frameBuffer.handle, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f);
                    GL.IssuePluginEvent(HvrPlayerInterfaceAPI.UnityRenderEventFunc(), clearEventID);
                }

				int eventID = HvrPlayerInterfaceAPI.Unity_Player_Render(scene.handle, viewport.handle);
				GL.IssuePluginEvent(HvrPlayerInterfaceAPI.UnityRenderEventFunc(), eventID);
			}
		}

        public static void RenderActor(HvrActorInterface actor, HvrViewportInterface viewport)
        {
            if (actor != null && viewport != null)
            {
                HvrFrameBufferInterface frameBuffer = viewport.frameBuffer;
                if (frameBuffer != null)
                {
                    int clearEventID = HvrPlayerInterfaceAPI.Unity_FrameBuffer_Clear(frameBuffer.handle, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f);
                    GL.IssuePluginEvent(HvrPlayerInterfaceAPI.UnityRenderEventFunc(), clearEventID);
                }

                int eventID = HvrPlayerInterfaceAPI.Unity_Player_RenderActor(actor.handle, viewport.handle);
                GL.IssuePluginEvent(HvrPlayerInterfaceAPI.UnityRenderEventFunc(), eventID);
            }
        }

        public struct StatValues
		{
			public float max, min, avg;
		}

		public static void BeginFrame()
		{
			int eventID = HvrPlayerInterfaceAPI.Unity_Player_BeginFrame();
			GL.IssuePluginEvent(HvrPlayerInterfaceAPI.UnityRenderEventFunc(), eventID);
		}

		public static void EndFrame()
		{
			int eventID = HvrPlayerInterfaceAPI.Unity_Player_EndFrame();
			GL.IssuePluginEvent(HvrPlayerInterfaceAPI.UnityRenderEventFunc(), eventID);
		}

		public static void FetchStats(string key, out StatValues values)
		{
			values.max = HvrPlayerInterfaceAPI.Statistics_GetPerCall(key, HvrPlayerInterfaceAPI.STAT_MAX);
			values.min = HvrPlayerInterfaceAPI.Statistics_GetPerCall(key, HvrPlayerInterfaceAPI.STAT_MIN);
			values.avg = HvrPlayerInterfaceAPI.Statistics_GetPerCall(key, HvrPlayerInterfaceAPI.STAT_AVG);
		}

        public static Dictionary<string, StatValues> GetStats()
        {
            return m_stats;
        }

		public static Dictionary<string, StatValues> UpdateStats()
		{
			int count = HvrPlayerInterfaceAPI.Statistics_GetTrackedValueCount();
			for (int i = 0; i < count; ++i)
			{
				StringBuilder key = new StringBuilder(256);
				StatValues val = new StatValues();

				if (HvrPlayerInterfaceAPI.Statistics_GetTrackedValueName(i, key))
				{
					val.max = HvrPlayerInterfaceAPI.Statistics_GetPerCall(key.ToString(), HvrPlayerInterfaceAPI.STAT_MAX);
					val.min = HvrPlayerInterfaceAPI.Statistics_GetPerCall(key.ToString(), HvrPlayerInterfaceAPI.STAT_MIN);
					val.avg = HvrPlayerInterfaceAPI.Statistics_GetPerCall(key.ToString(), HvrPlayerInterfaceAPI.STAT_AVG);
				}

                m_stats[key.ToString()] = val;
			}
			return m_stats;
		}

		public static void LogStats()
		{
			Dictionary<string, StatValues> stats = m_stats;

			Debug.Log("Statistics: " + stats.Keys.Count + "\n");

			int count = 0;

			foreach (KeyValuePair<string, StatValues> pair in stats)
			{
				string log = count + " - ";

				float max = pair.Value.max;
				float min = pair.Value.min;
				float avg = pair.Value.avg;

				log += pair.Key.ToString() + " = " + avg + ", " + min + ", " + max;

				Debug.Log(log + "\n");

				count++;
			}
		}
	}
}
