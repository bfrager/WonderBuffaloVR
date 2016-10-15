using UnityEditor;
using UnityEngine;

namespace HVR.Editor
{
    public class MenuItems : MonoBehaviour
    {
        [MenuItem("GameObject/8i/Create HVR Actor", false, 10)]
        static void CreateObject_HVRActor(MenuCommand menuCommand)
        {
            CreateObject<HvrActor>(menuCommand, "HVR Actor");
        }

        [MenuItem("GameObject/8i/Create HVR Actor Clone", false, 10)]
        static void CreateObject_HVRActorClone(MenuCommand menuCommand)
        {
            CreateObject<HvrActor_Clone>(menuCommand, "HVR Actor Clone");
        }

        static void CreateObject<T>(MenuCommand menuCommand, string name)
        {
            // Create a custom game object
            GameObject go = new GameObject(name);
            go.AddComponent(typeof(T));

            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
    }
}

