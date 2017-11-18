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

        public float LightAltitudeAngle = 45.0f;

        public int LightCount = 4;

        public int ShadowLightCount = 2;


        List<GameObject> lights = new List<GameObject>();

		// Use this for initialization
		void Start () {
            Vector3f xz = new Vector3f(1, 0, 1); xz.Normalize();
            Vector3f angleVec = Quaternionf.AxisAngleD(new Vector3f(xz.x, 0, -xz.z), -LightAltitudeAngle) * xz;
            //Vector3f angleVec = new Vector3f(1, 3, 1);
            angleVec.Normalize();
			Vector3f vCornerPos = LightDistance * angleVec;

            // create a set of overheard spotlights
            float rotAngle = 360.0f / (float)LightCount;
			for (int k = 0; k < LightCount; k++) {
				GameObject lightObj = new GameObject (string.Format ("spotlight{0}", k));
				Light lightComp = lightObj.AddComponent<Light> ();
				lightComp.type = LightType.Directional;
				lightComp.transform.position = vCornerPos;
				lightComp.transform.LookAt (Vector3.zero);
				lightComp.transform.RotateAround(Vector3.zero, Vector3.up, (float)k * rotAngle);

				lightComp.intensity = 0.1f;
				lightComp.color = Color.white;

                // only add shadows on first two lights
				if ( k < ShadowLightCount )
					lightComp.shadows = LightShadows.Hard;

				lightComp.transform.SetParent ( this.gameObject.transform );

                lights.Add(lightObj);

            }

            // parent lights to Scene so they move with it
            Context.GetScene().RootGameObject.AddChild(this.gameObject);
		}

        int cur_shadow_dist = -1;


        Frame3f lastSceneFrameW;


		void Update () {

            if (Context == null)
                return;

            // [TODO] need to consider camera distane here?


            Frame3f sceneFrameW = Context.Scene.SceneFrame;
            if (sceneFrameW.EpsilonEqual(lastSceneFrameW, 0.001f))
                return;
            lastSceneFrameW = sceneFrameW;

            // use vertical height of light to figure out appropriate shadow distance.
            // distance changes if we scale scene, and if we don't do this, shadow
            // map res gets very blurry.
            Vector3f thisW = lights[0].transform.position;
            float fHeight =
                Vector3f.Dot(( thisW - sceneFrameW.Origin), sceneFrameW.Y);
            float fShadowDist = fHeight * 1.5f;

            // lights need to be in-range
            if (fShadowDist < LightDistance)
                fShadowDist = LightDistance * 1.5f;

            // need to be a multiple of eye distance
            float fEyeDist = sceneFrameW.Origin.Distance(Camera.main.transform.position);
            fShadowDist = Mathf.Max(fShadowDist, 1.5f * fEyeDist);

            int nShadowDist = (int)Snapping.SnapToIncrement(fShadowDist, 50);

            if (cur_shadow_dist != nShadowDist) {
                QualitySettings.shadowDistance = nShadowDist;
                cur_shadow_dist = nShadowDist;
            }
		}
	}


}
