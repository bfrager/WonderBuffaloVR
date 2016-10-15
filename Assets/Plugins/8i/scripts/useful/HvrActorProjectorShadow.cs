using UnityEngine;

namespace HVR
{
    [ExecuteInEditMode]
    public class HvrActorProjectorShadow : MonoBehaviour
    {
        public HvrActor actor;
        public Projector projector;
        public float sizeMultiplier = 1.0f;

        void Update()
        {
            if (actor == null || projector == null)
                return;

            if (actor.transform == projector.transform)
            {
                Debug.LogWarning("[" + this.GetType().ToString() + "] Actor and Projector transforms cannot be the same", this);
                projector = null;
                return;
            }

            Bounds actorBounds = actor.GetBounds();

            Vector3 pos = actor.transform.position;

            // Modify pos so that it takes into account the actorBounds center
            pos += actor.transform.right * actorBounds.center.x;
            pos += actor.transform.forward * actorBounds.center.z;
            pos += actor.transform.up * actorBounds.size.y;

            Quaternion rot = actor.transform.rotation;
            // Always face down the actor
            rot *= Quaternion.Euler(Vector3.right * 90);

            float size = Mathf.Max(actorBounds.size.x, actorBounds.size.z) / 2.0f;
            size = size * sizeMultiplier;

            // Set the new transform position and rotation
            projector.transform.position = pos;
            projector.transform.rotation = rot;

            // Apply Projector Settings
            projector.orthographicSize = size;
        }
    }
}
