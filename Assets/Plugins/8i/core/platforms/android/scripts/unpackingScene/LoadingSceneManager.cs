using UnityEngine;
using UnityEngine.SceneManagement;

namespace HVR.Android
{
    public class LoadingSceneManager : MonoBehaviour
    {
        void Start()
        {
#if UNITY_ANDROID
		AndroidFileUtils.Unpack8iAssets();
		LoadNextScene ();

#else
            LoadNextScene();
#endif
        }

        void LoadNextScene()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            int buildIndex = currentScene.buildIndex;

            if (SceneManager.sceneCount >= buildIndex + 1)
            {
                SceneManager.LoadScene(buildIndex + 1);
            }
        }
    }
}