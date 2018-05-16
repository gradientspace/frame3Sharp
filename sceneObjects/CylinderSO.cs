using UnityEngine;
using System;
using g3;

namespace f3
{
	public class CylinderSO : PrimitiveSO
    {
		float radius;
		float height;

		GameObject cylinder;
		GameObject topCap, bottomCap, body;

        public CylinderSO()
        {
            radius = 1.0f;
            height = 5.0f;

            Parameters.Register(
                "radius", () => { return Radius; }, (f) => { Radius = f; }, radius);
            Parameters.Register(
                "diameter", () => { return Diameter; }, (f) => { Diameter = f; }, 2 * radius, true);
            Parameters.Register(
                "height", () => { return Height; }, (f) => { Height = f; }, height);
            Parameters.Register(
                "scaled_radius", () => { return ScaledRadius; }, (f) => { ScaledRadius = f; }, radius);
            Parameters.Register(
                "scaled_diameter", () => { return ScaledDiameter; }, (f) => { ScaledDiameter = f; }, Diameter);
            Parameters.Register(
                "scaled_height", () => { return ScaledHeight; }, (f) => { ScaledHeight = f; }, height);
        }

        public CylinderSO Create( SOMaterial defaultMaterial) {

			cylinder = new GameObject(UniqueNames.GetNext("Cylinder"));
            AssignSOMaterial(defaultMaterial);       // need to do this to setup BaseSO material stack

            Material useMaterial = CurrentMaterial;

            topCap = AppendMeshGO ("topCap", 
				MeshGenerators.CreateDisc (radius, 1, 16),
                useMaterial, cylinder);
			topCap.transform.localPosition += 0.5f * height * Vector3.up;

			bottomCap = AppendMeshGO ("bottomCap", 
				MeshGenerators.CreateDisc (radius, 1, 16),
                useMaterial, cylinder);
			bottomCap.transform.RotateAround (Vector3.zero, Vector3.right, 180.0f);
			bottomCap.transform.localPosition -= 0.5f * height * Vector3.up;

			body = AppendMeshGO ("body", 
				MeshGenerators.CreateCylider (radius, height, 16),
                useMaterial, cylinder);
			body.transform.localPosition -= 0.5f * height * Vector3.up;

            update_shift();
            cylinder.transform.position += centerShift;

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
                centerShift = new Vector3(0, 0.5f * height, 0);
            else if (Center == CenterModes.Corner)
                centerShift = new Vector3(radius, 0.5f * height, radius);
        }


        override public void UpdateGeometry()
        {
            if (cylinder == null)
                return;

            topCap.GetComponent<MeshFilter>().sharedMesh = MeshGenerators.CreateDisc(radius, 1, 16);
            topCap.transform.localPosition = 0.5f * height * Vector3.up;

            bottomCap.GetComponent<MeshFilter>().sharedMesh = MeshGenerators.CreateDisc(radius, 1, 16);
            bottomCap.transform.localPosition = -0.5f * height * Vector3.up;

            body.SetMesh(MeshGenerators.CreateCylider(radius, height, 16));
            body.transform.localPosition = -0.5f * height * Vector3.up;

            // if we want to scale away/towards bottom of cylinder, then we need to 
            //  translate along axis as well...
            Frame3f f = GetLocalFrame(CoordSpace.ObjectCoords);
            float fScale = cylinder.transform.localScale[1];
            f.Origin -= f.FromFrameV(fScale * centerShift);
            update_shift();
            f.Origin += f.FromFrameV(fScale * centerShift);
            SetLocalFrame(f, CoordSpace.ObjectCoords);

            // apparently this is expensive?
            if (DeferRebuild == false) {
                topCap.GetComponent<MeshCollider>().sharedMesh = topCap.GetComponent<MeshFilter>().sharedMesh;
                bottomCap.GetComponent<MeshCollider>().sharedMesh = bottomCap.GetComponent<MeshFilter>().sharedMesh;
                body.GetComponent<MeshCollider>().sharedMesh = body.GetComponent<MeshFilter>().sharedMesh;
            }

            increment_timestamp();
        }



        public float Radius {
            get { return radius; }
            set { radius = value;
                  if (cylinder != null)
                    UpdateGeometry();
            }
        }
        public float ScaledRadius
        {
            get { return radius * cylinder.transform.localScale[0]; }
            set { Radius = value / cylinder.transform.localScale[0]; }
        }


        // convenient at times
        public float Diameter
        {
            get { return 2 * radius; }
            set { Radius = value * 0.5f; }
        }
        public float ScaledDiameter
        {
            get { return 2 * ScaledRadius; }
            set { ScaledRadius = value * 0.5f; }
        }


        public float Height
        {
            get { return height; }
            set {
                height = value;
                if (cylinder != null)
                    UpdateGeometry();
            }
        }
        public float ScaledHeight {
            get { return height * cylinder.transform.localScale[1]; }
            set { Height = value / cylinder.transform.localScale[1]; }
        }



        //
        // SceneObject impl
        //
        override public fGameObject RootGameObject
        {
            get { return cylinder; }
        }

        override public string Name
        {
            get { return cylinder.GetName(); }
            set { cylinder.SetName(value); }
        }

        override public SOType Type { get { return SOTypes.Cylinder; } }


        override public SceneObject Duplicate()
        {
            CylinderSO copy = new CylinderSO();
            copy.radius = this.radius;
            copy.height = this.height;
            copy.Create(this.GetAssignedSOMaterial());
            copy.SetLocalFrame(
                this.GetLocalFrame(CoordSpace.ObjectCoords), CoordSpace.ObjectCoords);
            copy.SetLocalScale(this.GetLocalScale());
            return copy;
        }

        override public AxisAlignedBox3f GetLocalBoundingBox()
        {
            // [RMS] I think this is wrong now...
            return new AxisAlignedBox3f(Vector3f.Zero, ScaledRadius, ScaledHeight / 2, ScaledRadius);
        }


        override public bool FindRayIntersection(Ray3f ray, out SORayHit hit)
        {
            hit = null;
            GameObjectRayHit hitg = null;
            if (FindGORayIntersection(ray, out hitg)) {
                if (hitg.hitGO != null) {
                    hit = new SORayHit(hitg, this);

                    // compute analytic normal on cylinder body
                    if ( hitg.hitGO == body ) {
                        Vector3 up = this.RootGameObject.GetRotation() * Vector3.up;
                        Vector3 l = hitg.hitPos - this.RootGameObject.GetPosition();
                        l -= Vector3.Dot(l, up) * up;
                        l.Normalize();
                        hit.hitNormal = l;
                    }

                    return true;
                }
            }
            return false;
        }

    }
}

