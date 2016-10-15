using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace HVR.Editor
{
    class BuildPostProcessor
    {
        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget target, string buildExecutablePath)
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                // Don't run the post process if we're targeting Android, because the user should be using the custom build pipeline, 
                // found in the 8i/Android menu.
            }
            else
            {
                PostBuildDataCopier.instance.ExportAssetData(buildExecutablePath);

                if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows | EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64)
                {
                    CopyWindowsSevenDll(buildExecutablePath);
                }
            }
        }

        public static void CopyWindowsSevenDll(string buildExecutablePath)
        {
            string dllName = "d3dcompiler_47.dll";

            string[] buildPathSplit = buildExecutablePath.Split('/');
            string buildExeName = buildPathSplit[buildPathSplit.Length - 1];
            string buildExeDirectory = buildExecutablePath.Remove(buildExecutablePath.Length - buildExeName.Length, buildExeName.Length);

            string buildDataDirectory = buildExecutablePath.Replace(".exe", "") + "_Data";
            string buildPluginsDirectory = buildDataDirectory + "/Plugins";

            string originalDllLocation = buildPluginsDirectory + "/" + dllName;
            string targetDllLocation = buildExeDirectory + "/" + dllName;

            File.Copy(originalDllLocation, targetDllLocation, true);
        }
    }
    public class PostBuildDataCopier
    {
        static PostBuildDataCopier m_instance;
        public static PostBuildDataCopier instance
        {
            get
            {
                if (m_instance == null)
                    m_instance = new PostBuildDataCopier();

                return m_instance;
            }
        }

        FileCopier fileCopier;

        public PostBuildDataCopier()
        {
            fileCopier = null;

            EditorApplication.update -= Update; // Trick to make sure it has not already been added - Tom
            EditorApplication.update += Update;
        }

        public void CleanHRVAssetClipData(string buildExecutablePath)
        {
            string buildDirectoryPath = GetBuildDirectoryPath(buildExecutablePath);
            if (Directory.Exists(buildDirectoryPath + "8i/"))
            {
                Directory.Delete(buildDirectoryPath + "8i/", true);
            }
        }

        public void ExportAssetData(string exePath)
        {
            CleanHRVAssetClipData(exePath);

            string buildDirectoryPath = GetBuildDirectoryPath(exePath);
            List<HvrActor> actorsInProject = GetAllActorsInScene();

            Debug.Log("[8i] Exporting " + actorsInProject.Count + " Actor Data");

            List<string[]> copyMappings = new List<string[]>();

            string[] enabledScenePaths = EditorHelper.GetEnabledScenesInBuild();

            for (int i = 0; i < enabledScenePaths.Length; i++)
            {
                Scene scene = EditorSceneManager.OpenScene(enabledScenePaths[i]);
                GameObject[] sceneRootObjects = scene.GetRootGameObjects();
                List<GameObject> sceneGameObjects = new List<GameObject>();
                List<HvrActor> actors = new List<HvrActor>();

                foreach (GameObject go in sceneRootObjects)
                {
                    sceneGameObjects.Add(go);
                    sceneGameObjects.AddRange(GetAllChildren(go));
                }

                foreach (GameObject go in sceneGameObjects)
                {
                    if (go.GetComponent<HvrActor>())
                        actors.Add(go.GetComponent<HvrActor>());
                }

                foreach (HvrActor actor in actors)
                {
                    string dataPath = actor.GetActorDataPath();

                    // If the actor does not have any data assigned
                    if (dataPath == "")
                        continue;

                    if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
                    {
                        // Just in case of different file systems returning different results
                        dataPath = dataPath.Replace("/", "\\");
                        dataPath = dataPath.Replace(@"\", "\\");
                    }

                    bool isDir = Directory.Exists(dataPath);

                    if (isDir)
                    {
                        DirectoryInfo dataFolder = new DirectoryInfo(dataPath);
                        List<string> files = GetAllDataInDirectory(dataFolder);
                        foreach (string file in files)
                        {
                            string sourceFile = file.Replace(dataFolder.FullName + "\\", "");
                            string destinationFile = buildDirectoryPath + "8i/" + actor.dataGuid + "/" + sourceFile;
                            copyMappings.Add(new string[] { file, destinationFile });
                        }
                    }
                    else
                    {
                        FileInfo dataFileInfo = new FileInfo(dataPath);
                        string destinationFile = buildDirectoryPath + "8i/" + actor.dataGuid;
                        copyMappings.Add(new string[] { dataFileInfo.FullName, destinationFile });
                    }
                }

                EditorSceneManager.CloseScene(scene, true);
            }

            fileCopier = new FileCopier();
            fileCopier.Start(copyMappings.ToArray(), true);
        }

        private string GetBuildDirectoryPath(string buildExecutablePath)
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
            {
                // Anything in the Data dir is automatically bundled with the app
                return buildExecutablePath + "/Data/";
            }

            string[] buildPathSplit = buildExecutablePath.Split('/');
            string buildExeName = buildPathSplit[buildPathSplit.Length - 1];
            string buildDirectoryPath = buildExecutablePath.Remove(buildExecutablePath.Length - buildExeName.Length, buildExeName.Length);

            return buildDirectoryPath;
        }

        private List<HvrActor> GetAllActorsInScene()
        {
            string[] enabledScenePaths = EditorHelper.GetEnabledScenesInBuild();

            List<HvrActor> actors = new List<HvrActor>();

            for (int i = 0; i < enabledScenePaths.Length; i++)
            {
                Scene scene = EditorSceneManager.OpenScene(enabledScenePaths[i]);
                GameObject[] sceneRootObjects = scene.GetRootGameObjects();

                List<GameObject> everyObjectInAllScenes = new List<GameObject>();

                foreach (GameObject go in sceneRootObjects)
                {
                    everyObjectInAllScenes.Add(go);
                    everyObjectInAllScenes.AddRange(GetAllChildren(go));
                }

                for (int o = 0; o < everyObjectInAllScenes.Count; o++)
                {
                    if (everyObjectInAllScenes[o].GetComponent<HvrActor>())
                    {
                        actors.Add(everyObjectInAllScenes[o].GetComponent<HvrActor>());
                    }
                }

                EditorSceneManager.CloseScene(scene, true);
            }

            return actors;
        }

        public List<GameObject> GetAllChildren(GameObject parent)
        {
            List<GameObject> go = new List<GameObject>();

            foreach (Transform child in parent.transform)
            {
                go.Add(child.gameObject);
                go.AddRange(GetAllChildren(child.gameObject));
            }

            return go;
        }

        private List<string> GetAllDataInDirectory(DirectoryInfo assetDataDirInfo)
        {
            List<string> assetFiles = new List<string>();

            if (assetDataDirInfo.Exists == false)
            {
                return assetFiles;
            }

            FileInfo[] allAssetFiles = assetDataDirInfo.GetFiles("*.*", SearchOption.AllDirectories);

            for (int j = 0; j < allAssetFiles.Length; j++)
            {
                string file = allAssetFiles[j].FullName;

                // Skip meta files as they should not be used
                if (file.EndsWith(".meta"))
                    continue;

                file = file.Replace("/", "\\");
                file = file.Replace(@"\", "\\");

                assetFiles.Add(allAssetFiles[j].FullName);
            }

            return assetFiles;
        }

        public void Update()
        {
            if (fileCopier != null)
            {
                if (fileCopier.copyComplete == false)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(
                    "Export Progress",
                    fileCopier.GetCopyOutput(),
                    fileCopier.GetProgress()))
                    {
                        EditorUtility.ClearProgressBar();
                    }
                }
                else
                {
                    fileCopier = null;
                    EditorUtility.ClearProgressBar();
                }
            }
        }
    }
}
