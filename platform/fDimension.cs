using System;
using g3;

namespace f3
{
    public enum DimensionType
    {
        //Pixels,       // not implemented yet
        WorldUnits, SceneUnits,
        //SOUnits,      // not implemented yet
        VisualAngle
    }


    /// <summary>
    /// fDimension represents a dimension in one of multiple sets of units.
    /// The purpose of this is to handle UI sizing where we want to size the same
    /// objects by different units in different situations.
    /// 
    /// For example in VR we might want to specify a label height by visual angle,
    /// while on desktop we might want to specify pixels.
    /// </summary>
    public abstract class fDimension
    {
        DimensionType type;
        public DimensionType Type {
            get { return type; }
        }

        public abstract double WorldValue { get; }
        public abstract double SceneValue { get; }
        public abstract fDimension Clone();

        public float WorldValuef {
            get { return (float)WorldValue; }
        }
        public float SceneValuef {
            get { return (float)SceneValue; }
        }

        protected fDimension(DimensionType eType)
        {
            type = eType;
        }


        protected FScene ActiveScene {
            get { return FScene.Active; }       // [TODO] this is bad!!
        }



        public virtual void Add(fDimension dim) {
            throw new NotImplementedException("fDimension.Add: must be implemented in subclass");
        }
        public virtual void Scale(double scale) {
            throw new NotImplementedException("fDimension.Scale: must be implemented in subclass");
        }


        // simplify constructors
        public static fDimension World(double fWorld) { return new fWorldDimension(fWorld); }
        public static fDimension World(Func<double> worldF) { return new fDynamicWorldDimension(worldF); }
        public static fDimension Scene(double fScene) { return new fSceneDimension(fScene); }
        public static fDimension Scene(Func<double> sceneF) { return new fDynamicSceneDimension(sceneF); }
        public static fDimension VisualAngle(Vector3f position, double fDegrees) { return new fVisualAngleDimension(position, fDegrees); }
        public static fDimension VisualAngle(Func<Vector3f> positionF, double fDegrees) { return new fDynamicVisualAngleDimension(positionF, fDegrees); }
        public static fDimension VisualAngle(fPosition position, double fDegrees) { return new fPositionVisualAngleDimension(position, ()=> { return fDegrees; } ); }
    }




    public abstract class fConstantDimension : fDimension
    {
        protected double value;
        protected fConstantDimension(DimensionType eType, double fValue) : base(eType) {
            value = fValue;
        }

        public override void Scale(double scale) {
            value *= scale;
        }

        protected void add(double d) { value += d; }

    }


    public class fWorldDimension : fConstantDimension
    {
        public fWorldDimension(double fWorld) : base(DimensionType.WorldUnits, fWorld)
        {
        }

        public override fDimension Clone() {
            return new fWorldDimension(this.value);
        }

        public override double WorldValue {
            get { return base.value; }
        }
        public override double SceneValue {
            get { return ActiveScene.ToSceneDimension((float)base.value); }
        }

        public override void Add(fDimension dim) {
            base.add(dim.WorldValue);
        }
    }



    public class fDynamicWorldDimension : fDimension
    {
        Func<double> worldValueF;   

        public fDynamicWorldDimension(Func<double> worldF) : base(DimensionType.WorldUnits)
        {
            worldValueF = worldF;
        }

        public override fDimension Clone()
        {
            return new fDynamicWorldDimension(this.worldValueF);
        }


        public override double WorldValue {
            get { return worldValueF();}
        }
        public override double SceneValue {
            get { return ActiveScene.ToSceneDimension((float)worldValueF()); }
        }
    }





    public class fSceneDimension : fConstantDimension
    {
        public fSceneDimension(double fWorld) : base(DimensionType.SceneUnits, fWorld)
        {
        }

        public override fDimension Clone()
        {
            return new fSceneDimension(this.value);
        }

        public override double WorldValue {
            get { return ActiveScene.ToWorldDimension((float)base.value); }
        }
        public override double SceneValue {
            get { return base.value; }
        }

        public override void Add(fDimension dim) {
            base.add(dim.SceneValue);
        }

    }



    public class fDynamicSceneDimension : fDimension
    {
        Func<double> sceneValueF;   

        public fDynamicSceneDimension(Func<double> sceneF) : base(DimensionType.SceneUnits)
        {
            sceneValueF = sceneF;
        }

        public override fDimension Clone()
        {
            return new fDynamicWorldDimension(this.sceneValueF);
        }


        public override double WorldValue {
            get { return ActiveScene.ToWorldDimension((float)sceneValueF()); }
        }
        public override double SceneValue {
            get { return sceneValueF(); }
        }
    }



    public class fVisualAngleDimension : fConstantDimension
    {
        Vector3f position;
        public virtual Vector3f Position {
            get { return position; }
        }

        public fVisualAngleDimension(Vector3f position, double fValue) : base(DimensionType.VisualAngle, fValue)
        {
            this.position = position;
        }

        public override fDimension Clone()
        {
            return new fVisualAngleDimension(this.position, this.value);
        }

        public override double WorldValue {
            get {
                return VRUtil.GetVRRadiusForVisualAngle(Position,
                    ActiveScene.ActiveCamera.GetPosition(), (float)base.value);
            }
        }
        public override double SceneValue {
            get { return ActiveScene.ToSceneDimension((float)WorldValue); }
        }
    } 





    public class fDynamicVisualAngleDimension : fConstantDimension
    {
        Func<Vector3f> positionF;   
        public virtual Vector3f Position {
            get { return positionF(); }
        }

        public fDynamicVisualAngleDimension(Func<Vector3f> positionF, double fValue) : base(DimensionType.VisualAngle, fValue)
        {
            this.positionF = positionF;
        }

        public override fDimension Clone()
        {
            return new fDynamicVisualAngleDimension(this.positionF, this.value);
        }

        public override double WorldValue {
            get {
                return VRUtil.GetVRRadiusForVisualAngle(Position,
                    ActiveScene.ActiveCamera.GetPosition(), (float)base.value);
            }
        }
        public override double SceneValue {
            get { return ActiveScene.ToSceneDimension((float)WorldValue); }
        }
    } 




    public class fPositionVisualAngleDimension : fDimension
    {
        public fPosition Position;
        Func<double> AngleF;

        public fPositionVisualAngleDimension(fPosition pos, Func<double> angleF) : base(DimensionType.VisualAngle)
        {
            this.Position = pos;
            this.AngleF = angleF;
        }

        public override fDimension Clone()
        {
            return new fPositionVisualAngleDimension(this.Position, this.AngleF);
        }

        public override double WorldValue {
            get {
                return VRUtil.GetVRRadiusForVisualAngle(Position.WorldPoint,
                    ActiveScene.ActiveCamera.GetPosition(), (float)AngleF());
            }
        }
        public override double SceneValue {
            get { return ActiveScene.ToSceneDimension((float)WorldValue); }
        }
    } 



}
