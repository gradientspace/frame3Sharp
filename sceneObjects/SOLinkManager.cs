using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    /// <summary>
    /// Manage a set of SOLinks for a Scene. Basically just a List manager,
    /// but handles unlinking, emitting change ops on link removal, etc
    /// </summary>
    public class SOLinkManager
    {
        public FScene Scene;

        // contains all links
        public List<SOLink> Links = new List<SOLink>();

        // contains links by name - subset of Links
        public Dictionary<string, SOLink> NamedLinks = new Dictionary<string, SOLink>();



        public SOLinkManager(FScene scene)
        {
            Scene = scene;
            Scene.ChangedEvent += Scene_ChangedEvent;
        }

        private void Scene_ChangedEvent(object sender, SceneObject so, SceneChangeType type)
        {
            if ( type == SceneChangeType.Removed ) {
                RemoveAllLinks(so);
            }
        }


        public void AddLink(SOLink link)
        {
            link.Manager = this;
            Links.Add(link);
            if ( link.Name != null ) {
                if (NamedLinks.ContainsKey(link.Name))
                    throw new Exception("SOLinkManager.RegisterLink: named link already exists!");
                NamedLinks[link.Name] = link;
            }
        }


        public SOLink FindLinkByUUID(string uuid)
        {
            foreach ( var link in Links ) {
                if (link.UUID == uuid)
                    return link;
            }
            return null;
        }


        public bool RemoveLink(SOLink link)
        {
            bool bFound = Links.Remove(link);
            if ( bFound ) {
                if (link.Name != null && NamedLinks.ContainsKey(link.Name))
                    NamedLinks.Remove(link.Name);

                // We might be running this *in* a ChangeOp, in which case we
                // do not want to push another one. ugly problem =\
                if (Scene.History.InPastState) {
                    link.Unlink();
                } else {
                    IChangeOp change = link.GetRemoveChange();
                    if (change == null) {
                        // we cannot undo this link removal
                        link.Unlink();
                    } else {
                        Scene.History.PushChange(change, false);
                    }
                }

            }
            return bFound;
        }


        public bool RemoveLinkByUUID(string uuid)
        {
            SOLink found = FindLinkByUUID(uuid);
            if (found == null)
                return false;
            return RemoveLink(found);
        }


        void RemoveAllLinks(SceneObject so)
        {
            List<SOLink> toRemove = new List<SOLink>();
            foreach ( SOLink link in Links ) {
                if (link.Source == so || link.Target == so)
                    toRemove.Add(link);
            }
            foreach (SOLink link in toRemove)
                RemoveLink(link);
        }


    }
}
