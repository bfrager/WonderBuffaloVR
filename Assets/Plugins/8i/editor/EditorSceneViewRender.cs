using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HVR.Editor
{
	[InitializeOnLoad]
	public class EditorSceneViewRender : MonoBehaviour
	{
		static bool shouldRender = true;

		static EditorSceneViewRender()
		{
            EditorApplication.update -= Update;
			EditorApplication.update += Update;
		}

        EditorSceneViewRender()
		{
			EditorApplication.update -= Update;
		}

		static void Update()
		{
			CheckCameras();
		}

		static void CheckCameras()
		{
			Camera[] sceneCameras = InternalEditorUtility.GetSceneViewCameras();

            foreach (Camera camera in sceneCameras)
            {
                if (shouldRender)
                {
                    if (!camera.GetComponent<HvrRender>())
                    {
                        HvrRender hvrRender = camera.gameObject.AddComponent<HvrRender>();
                        hvrRender.hideFlags = HideFlags.HideAndDontSave;
                        SceneView.RepaintAll();
                    }
                }
                else
                {
                    if (camera.GetComponent<HvrRender>())
                    {
                        CleanPostEffects(camera);
                        SceneView.RepaintAll();
                    }
                }
            }
		}

		static void CleanPostEffects(Camera sceneCamera)
		{
			if (sceneCamera == null) return;

			List<Component> wipeList = new List<Component>(sceneCamera.GetComponents<Component>());

			foreach (Component component in wipeList)
			{
				if (IsForbiddenComponent(sceneCamera, component)) continue;

				DestroyImmediate(component);
			}
		}

		static bool IsForbiddenComponent(Camera sceneCamera, Component component)
		{
			if (component == null ||
				component is Transform ||
				component is Camera ||
				component is FlareLayer) return true;
			if (component == sceneCamera.GetComponent("HaloLayer")) return true;

			return false;
		}
	}

}
