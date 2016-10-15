//
// 8i Plugin for Unity
//

using HVR.Interface;
using UnityEngine;
using HVR.Android;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HVR
{
    [ExecuteInEditMode]
    public class HvrActor_Clone : HvrActor
    {
        #region Public Members
        public HvrActor sourceActor
        {
            get
            {
                return m_sourceActor;
            }
            set
            {
                // Do not allow a clone to reference a clone, there be dragons - Tom
                if (value != null && value.GetType() != typeof(HvrActor))
                    return;

                SetActor(value);

                m_sourceActor = value;
            }
        }
        #endregion

        #region Private Members
        [SerializeField]
        private HvrActor m_sourceActor;
        #endregion

        #region HvrActor_Clone
        void SetActor(HvrActor _sourceActor)
        {
            if (m_sourceActor != null)
                m_sourceActor.onHvrAssetChanged -= SetAsset;

            m_sourceActor = _sourceActor;

            if (_sourceActor == null)
            {
                SetAsset(null);
            }
            else
            {
                _sourceActor.onHvrAssetChanged -= SetAsset;
                _sourceActor.onHvrAssetChanged += SetAsset;

                if (_sourceActor.hvrAsset != null)
                {
                    SetAsset(_sourceActor.hvrAsset);
                }
            }
        }

        #endregion

        #region HvrActor Overrides

        public override void Initialize()
        {
            base.Initialize();

            SetActor(sourceActor);
        }

        public override void SetAsset(HvrAsset _hvrAsset)
        {
            if (_hvrAsset != null)
            {
                SetAssetInterface(_hvrAsset.assetInterface);

                // Always update the transform after setting a new interface - Tom
                UpdateTransform();

                if (_hvrAsset != null)
                {
                    _hvrAsset.onInterfaceChanged -= SetAssetInterface;
                    _hvrAsset.onInterfaceChanged += SetAssetInterface;
                }
            }
            else
            {
                SetAssetInterface(null);
            }

            if (onHvrAssetChanged != null)
                onHvrAssetChanged(_hvrAsset);
        }

        public override string GetActorDataPath()
        {
            Debug.LogWarning(this.GetType().ToString() + " does not reference a data path", this);
            return "";
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            // DrawBounds here in order to draw an invisible, selectable cube around the actor
            DrawBounds(false);
        }
        void OnDrawGizmosSelected()
        {
            DrawBounds(true);
            DrawDebugInfo();
            DrawActorParentLink();
        }
        void DrawActorParentLink()
        {
            if (sourceActor != null)
            {
                Vector3 sourceTop = sourceActor.GetBoundsAABB().center + (sourceActor.transform.up * sourceActor.GetBoundsAABB().size.y / 2f);
                Vector3 thisTop = GetBoundsAABB().center + (transform.up * GetBoundsAABB().size.y / 2f);

                Handles.DrawDottedLine(sourceTop, thisTop, 2);

                Vector3 labelPosition = thisTop + (transform.up * 0.1f);
                Handles.Label(labelPosition, "Clone", EditorStyles.label);
            }
        }
#endif

        #endregion
    }
}
