using UnityEngine;
using System.Collections.Generic;
using g3;

namespace f3 {

    // Standard Scene lighting, will be used if SceneOptions.EnableDefaultLighting is true
    // on Context creation
    //
    // Notes:
    //    - The shadow distance is set in the Update() function. If your shadows are
    //      disappearing, this may be why. 
	public class SceneLightingSetup : MonoBehaviour {

		public FContext Context { get; set; }

        // This distance determines how far away the lights are positioned from the objects.
        // May be too far (or too close) for your scene!
        public float LightDistance = 20.0f;

        List<GameObject> lights = new List<GameObject>();

		// Use this for initialization
		void Start () {
			Vector3 vCornerPos = LightDistance * new Vector3 (1, 3, 1).normalized;

            // create a set of overheard spotlights
			for (int k = 0; k < 4; k++) {
				GameObject lightObj = new GameObject (string.Format ("spotlight{0}", k));
				Light lightComp = lightObj.AddComponent<Light> ();
				lightComp.type = LightType.Directional;
				lightComp.transform.position = vCornerPos;
				lightComp.transform.LookAt (Vector3.zero);
				lightComp.transform.RotateAround(Vector3.zero, Vector3.up, (float)k * 90.0f);

				lightComp.intensity = 0.1f;
				lightComp.color = Color.white;

                // only add shadows on first two lights
				if ( k == 0 || k == 1 )
					lightComp.shadows = LightShadows.Hard;

				lightComp.transform.SetParent ( this.gameObject.transform );

                lights.Add(lightObj);

            }

            // parent lights to Scene so they move with it
            Context.GetScene().RootGameObject.AddChild(this.gameObject);
		}

        int cur_shadow_dist = -1;



		void Update () {

            if (Context == null)
                return;

            // [TODO] need to consider camera distane here?

            // use vertical height of light to figure out appropriate shadow distance.
            // distance changes if we scale scene, and if we don't do this, shadow
            // map res gets very blurry.
            Frame3f sceneFrameW = Context.Scene.SceneFrame;
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
