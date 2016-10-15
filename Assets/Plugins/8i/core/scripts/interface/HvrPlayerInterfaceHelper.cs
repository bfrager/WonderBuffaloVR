using UnityEngine;
using System;

namespace HVR.Interface
{
	static class HvrPlayerInterfaceHelper
	{
		public static float[] GetFloatsFromMatrix(Matrix4x4 m)
		{
			return GetFloatsFromMatrix(m, new float[16]);
		}

		public static float[] GetFloatsFromMatrix(Matrix4x4 m, float[] f)
		{
			for (int a = 0; a < 16; ++a)
			{
				f[a] = m[a];
			}
			return f;
		}

		public static float[] GetFloatsFromVector2(Vector2 v2)
		{
			float[] f = new float[2];
			for (int a = 0; a < 2; ++a)
			{
				f[a] = v2[a];
			}
			return f;
		}

		public static float[] GetFloatsFromVector3(Vector3 v3)
		{
			float[] f = new float[3];
			for (int a = 0; a < 3; ++a)
			{
				f[a] = v3[a];
			}
			return f;
		}
	}
}