using System;
using System.Collections.Generic;
using System.Linq;
using g3;

namespace f3
{
    public interface DCurve3Source
    {
        DCurve3 Curve { get; set; }
    }


    public class DCurve3VerticesEditedOp : BaseChangeOp
    {
        public DCurve3Source CurveSource;
        public Vector3d[] Before;
        public Vector3d[] After;

        public override string Identifier() { return "DCurve3ChangeOp"; }
        public override OpStatus Apply() {
            CurveSource.Curve.SetVertices(After);
            return OpStatus.Success;
        }
        public override OpStatus Revert() {
            CurveSource.Curve.SetVertices(Before);
            return OpStatus.Success;
        }
        public override OpStatus Cull() {
            Before = After = null;
            return OpStatus.Success;
        }

        public DCurve3VerticesEditedOp(DCurve3Source source, bool bStoreAsBefore = true) : base(false)
        {
            CurveSource = source;
            if (bStoreAsBefore)
                StoreBefore();
        }
        public void StoreBefore() {
            Before = CurveSource.Curve.Vertices.ToArray();
        }
        public void StoreAfter() {
            After = CurveSource.Curve.Vertices.ToArray();
        }
    }




}
