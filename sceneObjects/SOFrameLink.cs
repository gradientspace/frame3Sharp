using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{

    /// <summary>
    /// SOFrameLink is a connection that keeps the Source object in the
    /// same position relative to the Target object, as when the link was added.
    /// So basically this rigidly couples two objects.
    /// Currently does not support modifying the coupling...
    /// </summary>
    public class SOFrameLink : StandardSOLink
    {
        Frame3f relativeF;

        public SOFrameLink(TransformableSO From, TransformableSO To) : base()
        {
            Target = To;
            Source = From;
            update_source();

            To.OnTransformModified += OnTargetModified;
        }
        public SOFrameLink(TransformableSO From, TransformableSO To, Frame3f setRelativeF)
        {
            Target = To;
            Source = From;
            relativeF = setRelativeF;
            To.OnTransformModified += OnTargetModified;
        }

        void update_source()
        {
            TransformableSO from = Source as TransformableSO;
            Frame3f FrameS = from.GetLocalFrame(CoordSpace.SceneCoords);
            TransformableSO to = Target as TransformableSO;
            relativeF = SceneTransforms.SceneToObject(to, FrameS);
        }

        private void OnTargetModified(TransformableSO so)
        {
            if (IsValid) {
                TransformableSO from = Source as TransformableSO;
                TransformableSO to = Target as TransformableSO;
                Frame3f FrameS = SceneTransforms.ObjectToScene(to, relativeF);
                from.SetLocalFrame(FrameS, CoordSpace.SceneCoords);
            }
        }

        public override SOLinkType LinkType { get { return SOLinkType.OneWay; } }

        public override IChangeOp GetRemoveChange() {
            return new SORemoveFrameLinkChangeOp(this);
        }

        public override void Unlink()
        {
            TransformableSO to = Target as TransformableSO;
            to.OnTransformModified -= OnTargetModified;
        }
    }






    public class SOAddFrameLinkChangeOp : BaseChangeOp
    {
        public FScene Scene;
        public string TargetUUID, SourceUUID, LinkUUID;

        public override string Identifier() { return "SOFrameLinkRemovedChangeOp"; }
        public override OpStatus Apply() {
            TransformableSO target = Scene.FindByUUID(TargetUUID) as TransformableSO;
            TransformableSO source = Scene.FindByUUID(SourceUUID) as TransformableSO;
            SOLink newLink = new SOFrameLink(source, target);
            Scene.LinkManager.AddLink(newLink);
            LinkUUID = newLink.UUID;
            return OpStatus.Success;
        }
        public override OpStatus Revert() {
            Scene.LinkManager.RemoveLinkByUUID(LinkUUID);
            return OpStatus.Success;
        }
        public override OpStatus Cull() {
            return OpStatus.Success;
        }

        public SOAddFrameLinkChangeOp(TransformableSO source, TransformableSO target) : base(false)
        {
            Scene = source.GetScene();
            TargetUUID = target.UUID;
            SourceUUID = source.UUID;
        }
    }



    public class SORemoveFrameLinkChangeOp : BaseChangeOp
    {
        public FScene Scene;
        public string TargetUUID, SourceUUID, LinkUUID;

        public override string Identifier() { return "SOFrameLinkRemovedChangeOp"; }
        public override OpStatus Apply() {
            Scene.LinkManager.RemoveLinkByUUID(LinkUUID);
            return OpStatus.Success;
        }
        public override OpStatus Revert() {
            TransformableSO target = Scene.FindByUUID(TargetUUID) as TransformableSO;
            TransformableSO source = Scene.FindByUUID(SourceUUID) as TransformableSO;
            SOLink link = new SOFrameLink(source, target);
            Scene.LinkManager.AddLink(link);
            LinkUUID = link.UUID;
            return OpStatus.Success;
        }
        public override OpStatus Cull() {
            return OpStatus.Success;
        }

        public SORemoveFrameLinkChangeOp(SOFrameLink link) : base(false)
        {
            Scene = link.Manager.Scene;
            TargetUUID = link.Target.UUID;
            SourceUUID = link.Source.UUID;
            LinkUUID = link.UUID;
        }
    }






}
