using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{

    public enum PositionType
    {
        World,
        Scene
        //, SOPosition
    }


    /// <summary>
    /// fPosition represents a position in one of multiple coordinate systems.
    /// The purpose of this is to handle cases where we want to use a point in
    /// multiple coordinate systems. So, a point can be specified in World but
    /// evaluated in Scene, etc.
    /// </summary>
    public abstract class fPosition
    {
        PositionType type;
        public PositionType Type {
            get { return type; }
        }
        
        public abstract Vector3f WorldPoint { get; }
        public abstract Vector3f ScenePoint { get; }

        protected fPosition(PositionType eType)
        {
            type = eType;
        }


        protected FScene ActiveScene {
            get { return FScene.Active; }       // [TODO] this is bad!!
        }


        public static fPosition World(Vector3f fWorld) { return new fWorldPosition(fWorld); }
        public static fPosition World(Func<Vector3f> worldF) { return new fDynamicWorldPosition(worldF); }
        public static fPosition Scene(Vector3f fScene) { return new fWorldPosition(fScene); }
        public static fPosition Scene(Func<Vector3f> sceneF) { return new fDynamicScenePosition(sceneF); }

    }


    public abstract class fConstantPosition : fPosition
    {
        protected Vector3f point;
        protected fConstantPosition(PositionType eType, Vector3f pos) : base(eType) {
            point = pos;
        }
    }


    public class fWorldPosition : fConstantPosition
    {
        public fWorldPosition(Vector3f pt) : base(PositionType.World, pt)
        {
        }

        public override Vector3f WorldPoint {
            get { return base.point; }
        }
        public override Vector3f ScenePoint {
            get { return ActiveScene.ToSceneP(base.point); }
        }
    }



    public class fDynamicWorldPosition : fPosition
    {
        Func<Vector3f> worldPointF;   

        public fDynamicWorldPosition(Func<Vector3f> worldF) : base(PositionType.World) {
            worldPointF = worldF;
        }

        public override Vector3f WorldPoint {
            get { return worldPointF(); }
        }
        public override Vector3f ScenePoint {
            get { return ActiveScene.ToSceneP(worldPointF()); }
        }
    }





    public class fScenePosition : fConstantPosition
    {
        public fScenePosition(Vector3f pt) : base(PositionType.Scene, pt)
        {
        }

        public override Vector3f WorldPoint {
            get { return ActiveScene.ToWorldP(base.point); }
        }
        public override Vector3f ScenePoint {
            get { return base.point; }
        }
    }



    public class fDynamicScenePosition : fPosition
    {
        Func<Vector3f> scenePointF;   

        public fDynamicScenePosition(Func<Vector3f> sceneF) : base(PositionType.Scene) {
            scenePointF = sceneF;
        }

        public override Vector3f WorldPoint {
            get { return ActiveScene.ToWorldP(scenePointF()); }
        }
        public override Vector3f ScenePoint {
            get { return scenePointF(); }
        }
    }



}
