using System;
using System.Collections.Generic;
using g3;
using UnityEngine;

namespace f3
{
    public partial class HUDUtil
    {

        /// <summary>
        /// This is very hacky.
        /// </summary>
        public static void AddDropShadow(HUDStandardItem item, Cockpit cockpit, Colorf color,
            float falloffWidthPx, Vector2f offset, float fZShift, bool bTrackCockpitScaling = true)
        {
            if (item is IBoxModelElement == false)
                throw new Exception("HUDUtil.AddDropShadow: can only add drop shadow to IBoxModelElement");

            float falloffWidth = falloffWidthPx * cockpit.GetPixelScale();

            // [TODO] need interface that provides a HUDShape?
            var shape = item as IBoxModelElement;
            float w = shape.Size2D.x + falloffWidth;
            float h = shape.Size2D.y + falloffWidth;

            fRectangleGameObject meshGO = GameObjectFactory.CreateRectangleGO("shadow", w, h, color, false);
            meshGO.RotateD(Vector3f.AxisX, -90.0f);
            fMaterial dropMat = MaterialUtil.CreateDropShadowMaterial(color, w, h, falloffWidth);
            meshGO.SetMaterial(dropMat);

            item.AppendNewGO(meshGO, item.RootGameObject, false);
            BoxModel.Translate(meshGO, offset, fZShift);

            if (bTrackCockpitScaling) {
                PreRenderBehavior pb = meshGO.AddComponent<PreRenderBehavior>();
                pb.ParentFGO = meshGO;
                pb.AddAction(() => {
                    Vector3f posW = item.RootGameObject.PointToWorld(meshGO.GetLocalPosition());
                    ((Material)dropMat).SetVector("_Center", new Vector4(posW.x, posW.y, posW.z, 0));
                    //float curWidth = falloffWidthPx * cockpit.GetPixelScale();  // [RMS] what is this for?
                    Vector2f origSize = shape.Size2D + falloffWidth * Vector2f.One;
                    Vector2f size = cockpit.GetScaledDimensions(origSize);
                    float ww = size.x;
                    float hh = size.y;
                    ((Material)dropMat).SetVector("_Extents", new Vector4(ww / 2, hh / 2, 0, 0));
                    float newWidth = falloffWidthPx * cockpit.GetPixelScale();
                    ((Material)dropMat).SetFloat("_FalloffWidth", newWidth);
                });
            }
        }
    }
}
