using System;
using UnityEngine;
using g3;

namespace f3
{
    // this class lets you create a primitive and position/size it interactively.
    // supports center modes, handles negative width/height/depth by shifting origin, etc
    // use BuildSO() to convert to a PrimitiveSO at same position

    public class MeshPrimitivePreview
    {
        public enum PrimType { Cylinder = 0, Box = 1, Sphere = 2 }

        PrimType type = PrimType.Cylinder;
        public PrimType Type
        {
            get { return type; }
            set { if (type != value) { type = value; bUpdatePending = true; } }
        }

        Frame3f frame;
        public Frame3f Frame
        {
            get { return frame; }
            set { frame = value; bUpdatePending = true; }
        }

        Frame3f shiftedFrame;

        CenterModes centerMode = CenterModes.Base;
        public CenterModes Center
        {
            get { return centerMode; }
            set { centerMode = value; bUpdatePending = true; }
        }

        float height, width, depth;
        public float Height
        {
            get { return height; }
            set { height = value; bUpdatePending = true; }
        }
        public float Width
        {
            get { return width; }
            set { width = value; bUpdatePending = true; }
        }
        public float Depth
        {
            get { return depth; }
            set { depth = value; bUpdatePending = true; }
        }


        GameObject meshObject;
        int nCurMesh = -1;
        bool bUpdatePending;

        public void Create(SOMaterial useMaterial, GameObject parent, float fMinDimension)
        {
            meshObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fMaterial mat = MaterialUtil.ToMaterialf(useMaterial);
            meshObject.SetMaterial(mat, true);
            bUpdatePending = true;

            meshObject.transform.SetParent(parent.transform, false);

            Height = Width = Depth = fMinDimension;
        }

        public void PreRender()
        {
            update_geometry();
        }


        public PrimitiveSO BuildSO(FScene scene, SOMaterial material)
        {
            Frame3f sceneFrame = scene.ToSceneFrame(shiftedFrame);
            float scale = 1.0f / scene.GetSceneScale();

            float w = (float)Math.Abs(Width);
            float h = (float)Math.Abs(Height);
            float d = (float)Math.Abs(Depth);

            if (Type == PrimType.Cylinder) {
                CylinderSO cyl = new CylinderSO() {
                    Center = this.Center,
                    Radius = w * 0.5f * scale, Height = h * scale
                };
                cyl.Create(material);
                cyl.SetLocalFrame(sceneFrame, CoordSpace.WorldCoords);
                return cyl;

            } else if (Type == PrimType.Box) {
                BoxSO box = new BoxSO() {
                    Center = this.Center,
                    Width = w * scale, Height = h * scale, Depth = d * scale
                };
                box.Create(material);
                box.SetLocalFrame(sceneFrame, CoordSpace.WorldCoords);
                return box;

            } else if (Type == PrimType.Sphere) {
                SphereSO sphere = new SphereSO() { Center = this.Center, Radius = w * 0.5f * scale };
                sphere.Create(material);
                sphere.SetLocalFrame(sceneFrame, CoordSpace.WorldCoords);
                return sphere;
            }
            return null;
        }


        public void Destroy()
        {
            meshObject.transform.parent = null;
            meshObject.Destroy();
        }


        void update_geometry()
        {
            if (bUpdatePending == false)
                return;

            switch (Type) {
                case PrimType.Cylinder:
                    update_cylinder(); break;
                case PrimType.Box:
                    update_box(); break;
                case PrimType.Sphere:
                    update_sphere(); break;
            }

            bUpdatePending = false;
        }


        void update_cylinder()
        {
            if (nCurMesh != (int)PrimType.Cylinder) {
                meshObject.SetMesh(UnityUtil.GetPrimitiveMesh(PrimitiveType.Cylinder));
                nCurMesh = (int)PrimType.Cylinder;
            }

            Transform parent = meshObject.transform.parent;
            meshObject.transform.parent = null;

            float fHeightSignShift = (Height < 0) ? Height : 0;
            float fAbsHeight = Math.Abs(Height);
            float fWidthShift = (Width < 0) ? Width : 0;
            float fAbsWidth = Math.Abs(Width);
            float fDepthShift = (Depth < 0) ? Depth : 0;
            float fAbsDepth = Math.Abs(Depth);

            Vector3f vShift = Vector3f.Zero;
            if (Center == CenterModes.Base)
                vShift = new Vector3f(0, fHeightSignShift + fAbsHeight * 0.5f, 0);
            else if (Center == CenterModes.Corner)
                vShift = new Vector3f(fWidthShift + fAbsWidth * 0.5f,
                    fHeightSignShift + fAbsHeight * 0.5f, fDepthShift + fAbsDepth * 0.5f);

            meshObject.transform.localPosition = Vector3.zero;
            meshObject.transform.localRotation = Quaternion.identity;

            // default unity cylinder is diam=1, height=2
            meshObject.transform.localScale = new Vector3f(fAbsWidth, fAbsHeight / 2.0f, fAbsDepth);

            shiftedFrame = Frame.Translated(Frame.FromFrameV(vShift));
            UnityUtil.SetGameObjectFrame(meshObject, shiftedFrame, CoordSpace.WorldCoords);

            meshObject.transform.SetParent(parent, true);
        }



        void update_box()
        {
            if (nCurMesh != (int)PrimType.Box) {
                meshObject.SetMesh(UnityUtil.GetPrimitiveMesh(PrimitiveType.Cube));
                nCurMesh = (int)PrimType.Box;
            }

            Transform parent = meshObject.transform.parent;
            meshObject.transform.parent = null;

            float fHeightSignShift = (Height < 0) ? Height : 0;
            float fAbsHeight = Math.Abs(Height);
            float fWidthShift = (Width < 0) ? Width : 0;
            float fAbsWidth = Math.Abs(Width);
            float fDepthShift = (Depth < 0) ? Depth : 0;
            float fAbsDepth = Math.Abs(Depth);

            Vector3f vShift = Vector3f.Zero;
            if (Center == CenterModes.Base)
                vShift = new Vector3f(0, fHeightSignShift + fAbsHeight * 0.5f, 0);
            else if (Center == CenterModes.Corner)
                vShift = new Vector3f(fWidthShift + fAbsWidth * 0.5f,
                    fHeightSignShift + fAbsHeight * 0.5f, fDepthShift + fAbsDepth * 0.5f);

            meshObject.transform.localPosition = Vector3.zero;
            meshObject.transform.localRotation = Quaternion.identity;

            // default unity box is 1x1x1 cube
            meshObject.transform.localScale = new Vector3f(fAbsWidth, fAbsHeight, fAbsDepth);

            shiftedFrame = Frame.Translated(Frame.FromFrameV(vShift));
            UnityUtil.SetGameObjectFrame(meshObject, shiftedFrame, CoordSpace.WorldCoords);

            meshObject.transform.SetParent(parent, true);
        }

        void update_sphere()
        {
            if (nCurMesh != (int)PrimType.Sphere) {
                meshObject.SetMesh(UnityUtil.GetPrimitiveMesh(PrimitiveType.Sphere));
                nCurMesh = (int)PrimType.Sphere;
            }

            Transform parent = meshObject.transform.parent;
            meshObject.transform.parent = null;

            float fHeightSignShift = (Height < 0) ? Height : 0;
            float fAbsHeight = Math.Abs(Height);
            float fWidthShift = (Width < 0) ? Width : 0;
            float fAbsWidth = Math.Abs(Width);

            Vector3f vShift = Vector3f.Zero;
            if (Center == CenterModes.Base)
                vShift = new Vector3f(0, fHeightSignShift + fAbsHeight * 0.5f, 0);
            else if (Center == CenterModes.Corner)
                vShift = new Vector3f(fWidthShift + fAbsWidth * 0.5f,
                    fHeightSignShift + fAbsHeight * 0.5f, fWidthShift + fAbsWidth * 0.5f);

            meshObject.transform.localPosition = Vector3.zero;
            meshObject.transform.localRotation = Quaternion.identity;

            // default unity sphere is diam=1
            meshObject.transform.localScale = new Vector3f(fAbsWidth, fAbsWidth, fAbsWidth);

            shiftedFrame = Frame.Translated(Frame.FromFrameV(vShift));
            UnityUtil.SetGameObjectFrame(meshObject, shiftedFrame, CoordSpace.WorldCoords);

            meshObject.transform.SetParent(parent, true);
        }
    }
}
