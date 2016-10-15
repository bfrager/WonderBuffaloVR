using UnityEngine;

namespace HVR.Android
{
    public class LoaderSpin : MonoBehaviour
    {
        // Update is called once per frame
        void Update()
        {
            Vector3 rotation = transform.eulerAngles;
            rotation.z -= Time.deltaTime * 150;
            transform.eulerAngles = rotation;
        }
    }
}