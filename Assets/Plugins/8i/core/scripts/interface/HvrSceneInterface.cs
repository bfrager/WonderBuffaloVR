using UnityEngine;
using System;

namespace HVR.Interface
{
    public class HvrSceneInterface
    {
        public Int32 handle;
        public float lastPreRenderedTime = 0;

        private static HvrSceneInterface m_instance;
        public static HvrSceneInterface instance
        {
            get
            {
                if (m_instance == null)
                    m_instance = new HvrSceneInterface();

                return m_instance;
            }
        }

        public HvrSceneInterface()
        {
            handle = HvrPlayerInterfaceAPI.Scene_Create();
        }

        ~HvrSceneInterface()
        {
            HvrPlayerInterfaceAPI.Scene_Delete(handle);
        }

        public bool IsValid()
        {
            return HvrPlayerInterfaceAPI.Scene_IsValid(handle);
        }

        public void AttachActor(HvrActorInterface actor)
        {
            HvrPlayerInterfaceAPI.Scene_AttachActor(handle, actor.handle);
        }

        public void DetachActor(HvrActorInterface actor)
        {
            if (actor != null)
                HvrPlayerInterfaceAPI.Scene_DetachActor(handle, actor.handle);
        }

        public bool ContainsActor(HvrActorInterface actor)
        {
            return HvrPlayerInterfaceAPI.Scene_ContainsActor(handle, actor.handle);
        }
    }
}
