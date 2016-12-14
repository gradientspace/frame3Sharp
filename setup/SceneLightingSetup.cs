using UnityEngine;
using System.Collections.Generic;
using g3;

namespace f3 {

	public class SceneLightingSetup : MonoBehaviour {

		public FContext Scene { get; set; }

        List<GameObject> lights = new List<GameObject>();

		// Use this for initialization
		void Start () {
			Vector3 vCornerPos = 20.0f * new Vector3 (1, 3, 1).normalized;

			for (int k = 0; k < 4; k++) {
				GameObject lightObj = new GameObject (string.Format ("spotlight{0}", k));
				Light lightComp = lightObj.AddComponent<Light> ();
				lightComp.type = LightType.Directional;
				lightComp.transform.position = vCornerPos;
				lightComp.transform.LookAt (Vector3.zero);
				lightComp.transform.RotateAround(Vector3.zero, Vector3.up, (float)k * 90.0f);

				lightComp.intensity = 0.1f;
				lightComp.color = Color.white;

				if ( k == 0 || k == 1 )
					lightComp.shadows = LightShadows.Hard;

				lightComp.transform.SetParent ( this.gameObject.transform );

                lights.Add(lightObj);

            }

			this.gameObject.transform.SetParent (Scene.GetScene ().RootGameObject.transform);
		}

        int cur_shadow_dist = -1;

		void Update () {

            if (Scene == null)
                return;

            // use vertical height of light to figure out appropriate shadow distance.
            // distance changes if we scale scene, and if we don't do this, shadow
            // map res gets very blurry.
            Frame3f sceneFrameW = Scene.Scene.SceneFrame;
            Vector3f thisW = lights[0].transform.position;
            float fHeight =
                Vector3f.Dot(( thisW - sceneFrameW.Origin), sceneFrameW.Y);

            int nShadowDist = (int)Mathf.Round( fHeight * 1.5f );
            if (cur_shadow_dist != nShadowDist) {
                QualitySettings.shadowDistance = nShadowDist;
                cur_shadow_dist = nShadowDist;
            }
		}
	}


}
