using UnityEditor;
using UnityEngine;

namespace HVR.Editor
{
    [InitializeOnLoad]
    public class EditorGameViewUpdater : MonoBehaviour
    {
        static EditorGameViewUpdater()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
        }

        EditorGameViewUpdater()
        {
            EditorApplication.update -= Update;
        }

        static void Update()
        {
            CheckActors();
        }

        static void CheckActors()
        {
            bool shouldUpdate = false;

            HvrActor[] actors = GameObject.FindObjectsOfType<HvrActor>();

            foreach (HvrActor actor in actors)
            {
                if (actor.hvrAsset != null && actor.hvrAsset.IsDirty())
                {
                    shouldUpdate = true;
                }
            }

            if (shouldUpdate && !Application.isPlaying)
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }
}
