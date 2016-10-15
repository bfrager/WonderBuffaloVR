using UnityEngine;
using System;
using System.Text;
using System.Runtime.InteropServices;

namespace HVR.Interface
{
    public static class HvrPlayerInterfaceAPI
    {
        public const string DLLName = "HVRPlayerInterface";

        //-----------------------------------------------------------------------------
        // Player Functions
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Player_Initialise();
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Player_Update(float absoluteTime);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Player_GarbageCollect();

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Player_PrepareRender(Int32 scene);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Player_Render(Int32 scene, Int32 Viewport);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Player_RenderActor(Int32 actor, Int32 Viewport);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Player_SetDefaultRenderMethod(int defaultRenderMethod);

        // System values that can be queried
        //"VERSION_MAJOR", "VERSION_MINOR", "VERSION_REVISION"
        //"VERSION_CHANGES", "VERSION_EDIT", "VERSION"
        //"BUILD_PLATFORM", "BUILD_CONFIG", "BUILD_NUMBER", 
        //"BUILD_HOST", "BUILD_DATE", "BUILD_INFO"
        //"GIT_BRANCH", "GIT_HASH", "GIT_MODIFIED", "GIT_INFO"
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Player_GetInfo(string key, StringBuilder value, int valueSize);

        //-----------------------------------------------------------------------------
        // Actor Functions
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 Actor_Create();
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Actor_Delete(Int32 actor);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Actor_IsValid(Int32 actor);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Actor_SetAsset(Int32 actor, Int32 asset);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Actor_SetTransform(Int32 actor, float[] trans_mat44);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Actor_SetVisible(Int32 actor, bool visible);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Actor_GetBounds(Int32 actor, ref float[] center_vec3, ref float[] size_vec3);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Actor_GetAABB(Int32 actor, ref float[] center_vec3, ref float[] size_vec3);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Actor_IsVisible(Int32 actor);

        //-----------------------------------------------------------------------------
        // Asset Functions
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 Asset_Create(string fileFolder);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Asset_Delete(Int32 asset);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Asset_IsValid(Int32 asset);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Asset_SetRenderMethodType(Int32 asset, int renderMethodType);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Asset_GetMetaCount(Int32 asset);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Asset_GetMetaEntry(Int32 asset, int idx, StringBuilder key, StringBuilder value);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Asset_GetMetaValue(Int32 asset, string key, StringBuilder value);

        //void EXPORT_API  Asset_SetTargetBufferDuration(HVRID asset, float time);
        //float EXPORT_API  Asset_GetTargetBufferDuration(HVRID asset);
        //float EXPORT_API  Asset_GetActualBufferDuration(HVRID asset);
        //float EXPORT_API  Asset_GetPartialBufferDuration(HVRID asset);
        //float EXPORT_API  Asset_GetBufferEndPoint(HVRID asset);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Asset_SetEnableCache(Int32 asset, bool enable);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Asset_ClearCache(Int32 asset);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Asset_Play(Int32 asset);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Asset_Pause(Int32 asset);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Asset_Seek(Int32 asset, float time);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Asset_Step(Int32 asset, int frames);
        
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Asset_SetLooping(Int32 asset, bool looping);
        
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Asset_GetState(Int32 asset);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Asset_GetCurrentTime(Int32 asset);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Asset_GetDuration(Int32 asset);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Asset_IsLooping(Int32 asset);

        //bool EXPORT_API  Asset_HasAudio(HVRID asset);

        //-----------------------------------------------------------------------------
        // FrameBuffer Functions
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 FrameBuffer_Create();
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FrameBuffer_SetTextures(Int32 frameBuffer, int width, int height, IntPtr colourTextureID, IntPtr depthTextureID);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FrameBuffer_Delete(Int32 frameBuffer);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool FrameBuffer_IsValid(Int32 frameBuffer);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FrameBuffer_SetSize(Int32 frameBuffer, float[] size_vec2);

        //-----------------------------------------------------------------------------
        // Scene Functions
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 Scene_Create();
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Scene_Delete(Int32 scene);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Scene_IsValid(Int32 scene);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Scene_AttachActor(Int32 scene, Int32 actor);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Scene_DetachActor(Int32 scene, Int32 actor);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Scene_ContainsActor(Int32 scene, Int32 actor);

        //-----------------------------------------------------------------------------
        // Viewport Functions
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 Viewport_Create();
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Viewport_Delete(Int32 viewport);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Viewport_IsValid(Int32 viewport);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Viewport_SetViewMatrix(Int32 viewport, float[] view_mat44);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Viewport_SetProjMatrix(Int32 viewport, float[] proj_mat44);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Viewport_SetNearFarPlane(Int32 viewport, float near, float far);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Viewport_SetDimensions(Int32 viewport, float x, float y, float width, float height);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Viewport_SetFrameBuffer(Int32 viewport, Int32 frameBuffer);

        //-------------------------------------------------------------------------
        // Unity Functions
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr UnityRenderEventFunc();
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void UnityRenderEvent(int eventID);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Unity_Player_PrepareRender(Int32 scene);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Unity_Player_Render(Int32 scene, Int32 viewport);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Unity_Player_RenderActor(Int32 actor, Int32 viewport);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Unity_FrameBuffer_Clear(Int32 frameBufferId, float red, float green, float blue, float alpha, float depth);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Unity_Player_BeginFrame();
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Unity_Player_EndFrame();

        //-------------------------------------------------------------------------
        // Statistics Functions
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Statistics_GetTrackedValueCount();
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Statistics_GetTrackedValueName(int idx, StringBuilder name);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Statistics_GetMaxTrackedFrames();
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Statistics_SetMaxTrackedFrames(int max);
        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Statistics_GetTrackedFramesCount();

        public const int STAT_MAX = 0;
        public const int STAT_MIN = 1;
        public const int STAT_AVG = 2;
        public const int STAT_CALLS = 3;

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Statistics_GetPerCall(string valueName, int stat);
    }

}
