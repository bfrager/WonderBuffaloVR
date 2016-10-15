using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace HVR
{
    [ExecuteInEditMode]
    public class HvrRenderPerfUI : MonoBehaviour
    {
        public Text textPerf;
        public Text textResolution;
        public HvrRender hvrRender;

        public float updateTick = 0.2f;
        private float lastUpdate;

        void Update()
        {
            if (textResolution == null ||
                textPerf == null ||
                hvrRender == null)
                return;

            if (Time.realtimeSinceStartup > lastUpdate + updateTick)
            {
                lastUpdate = Time.realtimeSinceStartup;

                float stat_onPreRender = hvrRender.stat_onPreRender.Avg();
                float stat_onPostRender = hvrRender.stat_onPostRender.Avg();

                float cameraWidth = hvrRender.GetComponent<Camera>().pixelWidth;
                float cameraHeight = hvrRender.GetComponent<Camera>().pixelHeight;

                textResolution.text = cameraWidth + "x" + cameraHeight;

                textPerf.text = ((stat_onPreRender + stat_onPostRender) * 1000).ToString("f4") + "ms";
            }
        }
    }
}
