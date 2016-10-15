using UnityEngine;

namespace HVR
{
    [AddComponentMenu("8i/Components/HvrActor Audiosource Sync")]
    public class HvrActorAudioSourceSync : MonoBehaviour
    {
        public HvrActor actor;
        public AudioSource audioSource;
        public float offset = 0;

        void Awake()
        {
            audioSource.Stop();
        }

        void Update()
        {
            if (actor == null || actor.hvrAsset == null || audioSource == null || audioSource.clip == null)
                return;

            if (!audioSource.isActiveAndEnabled)
                return;

            if (actor.hvrAsset.IsPlaying())
            {
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
            else
            {
                if (audioSource.isPlaying)
                    audioSource.Stop();
            }

            float targetTime = actor.hvrAsset.GetCurrentTime() - offset;
            float delta = targetTime - audioSource.time;

            if (targetTime >= 0 && targetTime < audioSource.clip.length)
            {
                if (Mathf.Abs(delta) > 0.2f)
                {
                    audioSource.time = targetTime;
                }
                else
                {
                    audioSource.pitch = (delta + 1.0f) * Time.timeScale;
                }
            }
            else
            {
                audioSource.Stop();
            }
        }
    }
}