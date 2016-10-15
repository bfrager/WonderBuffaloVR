using UnityEngine;
using System;

namespace HVR.Interface
{
    public class HvrActorInterface
    {
        public Int32 handle;
        public HvrActorInterface()
        {
            handle = HvrPlayerInterfaceAPI.Actor_Create();

            if (!HvrSceneInterface.instance.ContainsActor(this))
                HvrSceneInterface.instance.AttachActor(this);
        }

        ~HvrActorInterface()
        {
            if (HvrSceneInterface.instance.ContainsActor(this))
                HvrSceneInterface.instance.DetachActor(this);

            HvrPlayerInterfaceAPI.Actor_Delete(handle);
        }

        public bool IsValid()
        {
            return HvrPlayerInterfaceAPI.Actor_IsValid(handle);
        }

        public void SetAssetInterface(HvrAssetInterface assetInterface)
        {
            if (assetInterface == null)
            {
                HvrPlayerInterfaceAPI.Actor_SetAsset(handle, 0);
            }
            else
            {
                HvrPlayerInterfaceAPI.Actor_SetAsset(handle, assetInterface.handle);
            }
        }

        public void SetTransform(Transform trans, float scaleFactor)
        {
            // Set scale factor
            Matrix4x4 matrix = trans.localToWorldMatrix * Matrix4x4.Scale(new Vector3(scaleFactor, scaleFactor, scaleFactor));

            // Account for Unity's -Z orientation, and fix the actors being mirrored
            matrix = matrix * Matrix4x4.Scale(new Vector3(-1, 1, 1));

            float[] transFloat = HvrPlayerInterfaceHelper.GetFloatsFromMatrix(matrix);
            HvrPlayerInterfaceAPI.Actor_SetTransform(handle, transFloat);
        }

        public void SetVisible(bool visible)
        {
            HvrPlayerInterfaceAPI.Actor_SetVisible(handle, visible);
        }

        public bool IsVisible()
        {
            return HvrPlayerInterfaceAPI.Actor_IsVisible(handle);
        }

        public Bounds GetAABB()
        {
            float[] centerF = new float[3];
            float[] sizeF = new float[3];
            HvrPlayerInterfaceAPI.Actor_GetAABB(handle, ref centerF, ref sizeF);

            Vector3 center = new Vector3(centerF[0], centerF[1], centerF[2]);
            Vector3 size = new Vector3(sizeF[0], sizeF[1], sizeF[2]);

            return new Bounds(center, size);
        }

        public Bounds GetBounds()
        {
            float[] centerF = new float[3];
            float[] sizeF = new float[3];
            HvrPlayerInterfaceAPI.Actor_GetBounds(handle, ref centerF, ref sizeF);

            Vector3 center = new Vector3(-centerF[0], centerF[1], centerF[2]);
            Vector3 size = new Vector3(sizeF[0], sizeF[1], sizeF[2]);

            return new Bounds(center, size);
        }
    }
}