using UnityEngine;

namespace HVR
{
    public class PlayHvrActor : MonoBehaviour
    {
        public HvrActor actor;
        public bool loop;
        void Start()
        {
            if (actor != null && actor.hvrAsset != null)
            {
                actor.hvrAsset.Play();
                actor.hvrAsset.SetLooping(loop);
            }
        }
    }
}
