using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace f3
{
    public class HUDBuilder
    {

        public static HUDButton CreateIconClickButton(HUDShape shape,
                                     float fHUDRadius, float fAngleHorz, float fAngleVert,
                                     string icon,
                                     IGameObjectGenerator addGeometry = null)
        {
            Material mat = MaterialUtil.CreateTransparentImageMaterial(icon);
            HUDButton button = new HUDButton() { Shape = shape };
            button.Create(mat);
            if (addGeometry != null)
                button.AddVisualElements(addGeometry.Generate(), true);
            HUDUtil.PlaceInSphere(button, fHUDRadius, fAngleHorz, fAngleVert);
            return button;
        }

        public static HUDButton CreateDiscIconClickButton(float fButtonRadius,
                                    float fHUDRadius, float fAngleHorz, float fAngleVert,
                                    string icon,
                                    IGameObjectGenerator addGeometry = null)
        {
            HUDShape shape = new HUDShape(HUDShapeType.Disc, fButtonRadius);
            return CreateIconClickButton(shape, fHUDRadius, fAngleHorz, fAngleVert, icon, addGeometry);
        }

        public static HUDButton CreateRectIconClickButton(float fButtonWidth, float fButtonHeight,
                                    float fHUDRadius, float fAngleHorz, float fAngleVert,
                                    string icon,
                                    IGameObjectGenerator addGeometry = null)
        {
            HUDShape shape = new HUDShape(HUDShapeType.Rectangle, fButtonWidth, fButtonHeight );
            return CreateIconClickButton(shape, fHUDRadius, fAngleHorz, fAngleVert, icon, addGeometry);
        }




        public static HUDButton CreateGeometryIconClickButton(HUDShape shape,
                                     float fHUDRadius, float fAngleHorz, float fAngleVert,
                                     Color bgColor,
                                     IGameObjectGenerator addGeometry = null)
        {
            Material mat = (bgColor.a == 1.0f) ?
                MaterialUtil.CreateStandardMaterial(bgColor) : MaterialUtil.CreateTransparentMaterial(bgColor);
            HUDButton button = new HUDButton() { Shape = shape };
            button.Create(mat);
            if (addGeometry != null)
                button.AddVisualElements(addGeometry.Generate(), true);
            HUDUtil.PlaceInSphere(button, fHUDRadius, fAngleHorz, fAngleVert);
            return button;
        }



        public static HUDButton CreateMeshClickButton(
            Mesh mesh, Color color, float fMeshScale, Quaternion meshRotation,
            float fHUDRadius, float fAngleHorz, float fAngleVert,
            IGameObjectGenerator addGeometry = null)
        {
            Material mat = (color.a < 1.0f) ?
                MaterialUtil.CreateTransparentMaterial(color, color.a) :
                MaterialUtil.CreateStandardMaterial(color);

            HUDButton button = new HUDButton();
            button.Create(mesh, mat, fMeshScale, meshRotation);

            if (addGeometry != null)
                button.AddVisualElements(addGeometry.Generate(), true);
            HUDUtil.PlaceInSphere(button, fHUDRadius, fAngleHorz, fAngleVert);
            return button;
        }



        public static HUDButton CreateMeshClickButton(
            Mesh mesh, Color color, float fMeshScale, Quaternion meshRotation,
            HUDSurface hudSurf, float dx, float dy,
            IGameObjectGenerator addGeometry = null)
        {
            Material mat = (color.a < 1.0f) ?
                MaterialUtil.CreateTransparentMaterial(color, color.a) :
                MaterialUtil.CreateStandardMaterial(color);

            HUDButton button = new HUDButton();
            button.Create(mesh, mat, fMeshScale, meshRotation);

            if (addGeometry != null)
                button.AddVisualElements(addGeometry.Generate(), true);
            hudSurf.Place(button, dx, dy);
            return button;
        }



        public static HUDToggleButton CreateToggleButton(float fButtonRadius, float fHUDRadius,
                                            float fAngleHorz, float fAngleVert,
                                            string enabledIcon, string disabledIcon,
                                            IGameObjectGenerator addGeometry = null)
        {
            return CreateToggleButton(
                new HUDShape(HUDShapeType.Disc, fButtonRadius),
                fHUDRadius, fAngleHorz, fAngleVert, enabledIcon, disabledIcon, addGeometry);
        }
        public static HUDToggleButton CreateToggleButton(HUDShape shape, float fHUDRadius,
                                            float fAngleHorz, float fAngleVert,
                                            string enabledIcon, string disabledIcon,
                                            IGameObjectGenerator addGeometry = null)
        {
            fMaterial enabledMat = MaterialUtil.CreateTransparentImageMaterialF(enabledIcon);
            fMaterial disabledMat = MaterialUtil.CreateTransparentImageMaterialF(disabledIcon);

            HUDToggleButton button = new HUDToggleButton() { Shape = shape };
            button.Create(enabledMat);
            if (addGeometry != null)
                button.AddVisualElements(addGeometry.Generate(), true);

            HUDUtil.PlaceInSphere(button, fHUDRadius, fAngleHorz, fAngleVert);
            button.OnToggled += (s, bEnabled) => {
                button.SetAllGOMaterials(bEnabled ? enabledMat : disabledMat);
            };
            return button;
        }

    }
}
