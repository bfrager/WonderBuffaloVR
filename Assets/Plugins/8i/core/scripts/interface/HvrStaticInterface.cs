using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HVR.Interface
{
    public class HvrStaticInterface
    {
        private MonoBehaviour m_previousRenderingBehavior;
        static private HvrStaticInterface instance;
        static public HvrStaticInterface Self()
        {
            if (instance == null)
                instance = new HvrStaticInterface();

            return instance;
        }

        static public DeferredJobQueue deferredJobQueue = new DeferredJobQueue();

        public HvrStaticInterface()
        {
            HvrPlayerInterface.Initialise();

#if UNITY_EDITOR
            EditorApplication.update -= UnityEditorUpdate;
            EditorApplication.update += UnityEditorUpdate;
#endif
        }

#if UNITY_EDITOR
        public void UnityEditorUpdate()
        {
            // Only run while the editor is not in 'Play' mode
            if (Application.isEditor && !Application.isPlaying)
            {
                LateUpdate();

                SceneView.RepaintAll();
            }
        }
#endif

        public void LateUpdate()
        {
            deferredJobQueue.Update();

            float time = Time.time;

#if UNITY_EDITOR
            // Only run while the editor is not in 'Play' mode
            if (!Application.isPlaying)
                time = (float)EditorApplication.timeSinceStartup;
#endif
            
            HvrPlayerInterface.UpdateTime(time);

            if (HvrSceneInterface.instance.lastPreRenderedTime != time)
            {
                HvrPlayerInterface.PrepareRender(HvrSceneInterface.instance);
                HvrSceneInterface.instance.lastPreRenderedTime = time;
            }
        }

        public void RenderCamera(MonoBehaviour renderingBehavior, HvrViewportInterface viewport)
        {
            LateUpdate();

            HvrPlayerInterface.Render(HvrSceneInterface.instance, viewport);
        }

        public void RenderActor(MonoBehaviour renderingBehavior, HvrActorInterface actorInterface, HvrViewportInterface viewport)
        {
            LateUpdate();

            HvrPlayerInterface.RenderActor(actorInterface, viewport);
        }
    }
}
