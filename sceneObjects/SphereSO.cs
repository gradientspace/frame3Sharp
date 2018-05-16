using UnityEngine;
using System;
using g3;

namespace f3
{
	public class SphereSO : PrimitiveSO
    {
		float radius;

		GameObject sphere;
		GameObject sphereMesh;

        public SphereSO()
		{
			radius = 2.5f;

            Parameters.Register(
                "radius", () => { return Radius; }, (f) => { Radius = f; }, radius);
            Parameters.Register(
                "diameter", () => { return Diameter; }, (f) => { Diameter = f; }, 2*radius, true);
            Parameters.Register(
                "scaled_radius", () => { return ScaledRadius; }, (f) => { ScaledRadius = f; }, Radius);
            Parameters.Register(
                "scaled_diameter", () => { return ScaledDiameter; }, (f) => { ScaledDiameter = f; }, Diameter);

        }

        public SphereSO Create( SOMaterial defaultMaterial) {
            AssignSOMaterial(defaultMaterial);       // need to do this to setup BaseSO material stack

            sphere = new GameObject(UniqueNames.GetNext("Sphere"));
            sphereMesh = AppendUnityPrimitiveGO("sphereMesh", PrimitiveType.Sphere, CurrentMaterial, sphere);
            sphereMesh.transform.localScale = new Vector3(2*radius, 2*radius, 2*radius);

            update_shift();
            sphere.transform.position += centerShift;

            increment_timestamp();
            return this;
		}


        // to reposition relative to desired center point we need to shift
        // object, but need to be able to undo this shift after changing params
        Vector3 centerShift;
        void update_shift()
        {
            centerShift = Vector3.zero;
            if (Center == CenterModes.Base)
                centerShift = new Vector3(0, radius, 0);
            else if (Center == CenterModes.Corner)
                centerShift = new Vector3(radius, radius, radius);
        }


        override public void UpdateGeometry()
        {
            if (sphere == null)
                return;

            sphereMesh.GetComponent<MeshFilter>().sharedMesh = UnityUtil.GetPrimitiveMesh(PrimitiveType.Sphere);
            sphereMesh.transform.localScale = new Vector3(2 * radius, 2 * radius, 2 * radius);

            // if we want to scale away/towards bottom of sphere, then we need to 
            //  translate along axis as well...
            Frame3f f = GetLocalFrame(CoordSpace.ObjectCoords);
            float fScale = sphere.transform.localScale[1];
            f.Origin -= f.FromFrameV(fScale * centerShift);
            update_shift();
            f.Origin += f.FromFrameV(fScale * centerShift);
            SetLocalFrame(f, CoordSpace.ObjectCoords);

            // apparently this is expensive?
            if (DeferRebuild == false) {
                sphereMesh.GetComponent<MeshCollider>().sharedMesh = sphereMesh.GetComponent<MeshFilter>().sharedMesh;
            }

            increment_timestamp();
        }



        public float Radius
        {
            get { return radius; }
            set {
                radius = value;
                if (sphere != null)
                    UpdateGeometry();
            }
        }
        public float ScaledRadius {
            get { return radius * sphere.transform.localScale[0]; }
            set { Radius = value / sphere.transform.localScale[0]; }
        }

        // convenient at times
        public float Diameter {
            get { return 2 * radius; }
            set { Radius = value * 0.5f; }
        }
        public float ScaledDiameter {
            get { return 2 * ScaledRadius; }
            set { ScaledRadius = value * 0.5f; }
        }



        //
        // SceneObject impl
        //
        override public fGameObject RootGameObject
        {
            get { return sphere; }
        }

        override public string Name {
            get { return sphere.GetName(); }
            set { sphere.SetName(value); }
        }

        override public SOType Type { get { return SOTypes.Sphere; } }

        override public SceneObject Duplicate()
        {
            SphereSO copy = new SphereSO();
            copy.radius = this.radius;
            copy.Create(this.GetAssignedSOMaterial());
            copy.SetLocalFrame(
                this.GetLocalFrame(CoordSpace.ObjectCoords), CoordSpace.ObjectCoords);
            copy.SetLocalScale(this.GetLocalScale());
            return copy;
        }

        override public AxisAlignedBox3f GetLocalBoundingBox() {
            // [RMS] this may be wrong now?
            return new AxisAlignedBox3f(Vector3f.Zero, ScaledRadius);
        }


        override public bool FindRayIntersection(Ray3f ray, out SORayHit hit)
        {
            hit = null;
            GameObjectRayHit hitg = null;
            if (FindGORayIntersection(ray, out hitg)) {
                if (hitg.hitGO != null) {
                    hit = new SORayHit(hitg, this);

                    // compute analytic normal
                    hit.hitNormal = (hit.hitPos - this.RootGameObject.GetPosition()).Normalized;

                    return true;
                }
            }
            return false;
        }

    }
}

