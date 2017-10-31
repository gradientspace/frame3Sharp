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

        public SOFrameLink(SceneObject From, SceneObject To) : base()
        {
            Target = To;
            Source = From;
            update_source();

            To.OnTransformModified += OnTargetModified;
        }
        public SOFrameLink(SceneObject From, SceneObject To, Frame3f setRelativeF)
        {
            Target = To;
            Source = From;
            relativeF = setRelativeF;
            To.OnTransformModified += OnTargetModified;
        }

        void update_source()
        {
            Frame3f FrameS = Source.GetLocalFrame(CoordSpace.SceneCoords);
            relativeF = SceneTransforms.SceneToObject(Target, FrameS);
        }

        private void OnTargetModified(SceneObject so)
        {
            if (IsValid) {
                Frame3f FrameS = SceneTransforms.ObjectToScene(Target, relativeF);
                Source.SetLocalFrame(FrameS, CoordSpace.SceneCoords);
            }
        }

        public override SOLinkType LinkType { get { return SOLinkType.OneWay; } }

        public override IChangeOp GetRemoveChange() {
            return new SORemoveFrameLinkChangeOp(this);
        }

        public override void Unlink()
        {
            Target.OnTransformModified -= OnTargetModified;
        }
    }






    public class SOAddFrameLinkChangeOp : BaseChangeOp
    {
        public FScene Scene;
        public string TargetUUID, SourceUUID, LinkUUID;

        public override string Identifier() { return "SOFrameLinkRemovedChangeOp"; }
        public override OpStatus Apply() {
            SceneObject target = Scene.FindByUUID(TargetUUID);
            SceneObject source = Scene.FindByUUID(SourceUUID);
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

        public SOAddFrameLinkChangeOp(SceneObject source, SceneObject target) : base(false)
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
            SceneObject target = Scene.FindByUUID(TargetUUID);
            SceneObject source = Scene.FindByUUID(SourceUUID);
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
