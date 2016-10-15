using UnityEngine;

namespace HVR
{
    // Attach this component to a gameobject that has a box collider set to 'trigger'

    public class TriggerHvrActor : MonoBehaviour
    {
        public HvrActor actor;

        void OnTriggerEnter(Collider other)
        {
            if (actor != null && actor.hvrAsset != null)
                actor.hvrAsset.Play();
        }
    }
}
