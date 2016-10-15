using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class SelectSceneCamera : MonoBehaviour
{
    [MenuItem("8i/Useful/Select SceneCamera", false, 10)]
    static void CreateObject_HVRActor(MenuCommand menuCommand)
    {
        Camera[] sceneCameras = InternalEditorUtility.GetSceneViewCameras();

        foreach (Camera camera in sceneCameras)
        {
            Selection.activeGameObject = camera.gameObject;
        }
    }
}
