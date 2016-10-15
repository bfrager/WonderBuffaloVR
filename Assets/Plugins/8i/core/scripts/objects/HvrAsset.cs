using UnityEngine;
using System.IO;

using HVR.Interface;

namespace HVR
{
    public class HvrAsset
    {
        enum eState
        {
            eState_None = 0,
            eState_Initialising = 1 << 0,
            eState_Playing = 1 << 1,
            eState_Seeking = 1 << 2,
            eState_Count
        };

        public enum eRenderMethod
        {
            pointSprite,
            pointBlend
        };

        public HvrAssetInterface assetInterface
        {
            get
            {
                return m_assetInterface;
            }
        }
        public eRenderMethod renderMethod = eRenderMethod.pointBlend;
        public string dataPath;

        public delegate void OnPlayEvent();
        public OnPlayEvent onPlay;
        public delegate void OnSeekEvent(float time);
        public OnSeekEvent onSeek;
        public delegate void OnPauseEvent();
        public OnPauseEvent onPause;
        public delegate void OnStopEvent();
        public OnStopEvent onStop;
        public delegate void OnInterfaceChanged(HvrAssetInterface assetInterface);
        public OnInterfaceChanged onInterfaceChanged;

        float lastCurrentTime = 0;
        bool m_Dirty;

        HvrAssetInterface m_assetInterface;

        public HvrAsset(string path)
        {
            SetData(path);
            SetRenderMethod(GetRenderMethodForPlatform());
        }

        ~HvrAsset()
        {
            m_assetInterface = null;
            HvrPlayerInterfaceAPI.Player_GarbageCollect();
        }

        // ActorAsset functions
        //-------------------------------------------------------------------------
        public void SetData(string path)
        {
            m_assetInterface = new HvrAssetInterface(path);

            dataPath = path;

            if (onInterfaceChanged != null)
                onInterfaceChanged(m_assetInterface);

            SetDirty();
        }

        // Asset Interface Functions
        //-------------------------------------------------------------------------
        public void Play()
        {
            if (m_assetInterface != null)
            {
                m_assetInterface.Play();

                if (onPlay != null)
                    onPlay();
            }
        }
        public void Pause()
        {
            if (m_assetInterface != null)
            {
                m_assetInterface.Pause();

                if (onPause != null)
                    onPause();
            }
        }
        public void Stop()
        {
            if (m_assetInterface != null)
            {
                m_assetInterface.Seek(0);
                m_assetInterface.Pause();

                if (onStop != null)
                    onStop();
            }
        }
        public void Seek(float seconds)
        {
            if (m_assetInterface != null)
            {
                if (seconds <= GetDuration())
                    m_assetInterface.Seek(seconds);

                if (onSeek != null)
                    onSeek(seconds);
            }
        }
        public void Step(int frames)
        {
            if (m_assetInterface != null)
                m_assetInterface.Step(frames);
        }
        public void SetLooping(bool looping)
        {
            if (m_assetInterface != null)
                m_assetInterface.SetLooping(looping);
        }
        public bool IsPlaying()
        {
            eState state = eState.eState_None;
            if (m_assetInterface != null)
                state = (eState)m_assetInterface.GetState();

            return (state & eState.eState_Playing) != 0;
        }
        public int GetState()
        {
            if (m_assetInterface != null)
                return m_assetInterface.GetState();

            return 0;
        }
        public float GetCurrentTime()
        {
            if (m_assetInterface != null)
                return m_assetInterface.GetCurrentTime();

            return 0;
        }
        public float GetDuration()
        {
            if (m_assetInterface != null)
                return m_assetInterface.GetDuration();

            return 0;
        }
        public eRenderMethod GetRenderMethod()
        {
            return renderMethod;
        }
        public void SetRenderMethod(HvrAsset.eRenderMethod _renderMethod)
        {
            renderMethod = _renderMethod;

            if (m_assetInterface != null)
                m_assetInterface.SetRenderMethodType((int)_renderMethod);

            SetDirty();
        }

        void SetDirty()
        {
            m_Dirty = true;
        }

        // Checks
        //-------------------------------------------------------------------------
        public bool IsDirty()
        {
            if (lastCurrentTime != GetCurrentTime())
            {
                lastCurrentTime = GetCurrentTime();
                return true;
            }

            if (m_Dirty)
            {
                m_Dirty = false;
                return true;
            }

            return false;
        }

        // Other
        //-------------------------------------------------------------------------
        eRenderMethod GetRenderMethodForPlatform()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                    return eRenderMethod.pointBlend;
                case RuntimePlatform.WindowsEditor:
                    return eRenderMethod.pointBlend;
                case RuntimePlatform.IPhonePlayer:
                    return eRenderMethod.pointSprite;
                case RuntimePlatform.Android:
                    return eRenderMethod.pointSprite;
                default:
                    return eRenderMethod.pointBlend;
            }
        }
    }
}
