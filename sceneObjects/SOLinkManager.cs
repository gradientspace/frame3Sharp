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
        protected List<SOLink> Links = new List<SOLink>();

        // contains links by name - subset of Links
        protected Dictionary<string, SOLink> NamedLinks = new Dictionary<string, SOLink>();


        public SOLinkManager(FScene scene)
        {
            Scene = scene;
            Scene.ChangedEvent += Scene_ChangedEvent;
        }

        private void Scene_ChangedEvent(object sender, SceneObject so, SceneChangeType type)
        {
            if ( type == SceneChangeType.Removed ) {
                RemoveAllLinksToSO(so);
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


        /// <summary>
        /// Remove link
        /// </summary>
        public bool RemoveLink(SOLink link)
        {
            bool bFound = Links.Remove(link);
            if ( bFound ) {
                if (link.Name != null && NamedLinks.ContainsKey(link.Name))
                    NamedLinks.Remove(link.Name);
                link.Unlink();
            }
            return bFound;
        }


        /// <summary>
        /// Remove link that has matching uuid
        /// </summary>
        public bool RemoveLinkByUUID(string uuid)
        {
            SOLink found = FindLinkByUUID(uuid);
            if (found == null)
                return false;
            return RemoveLink(found);
        }


        /// <summary>
        /// Remove all named links where the name contains match string
        /// </summary>
        public void RemoveLinksByNameSubstring(string match)
        {
            List<SOLink> toRemove = new List<SOLink>();
            foreach (SOLink link in NamedLinks.Values) {
                if ( link.Name.Contains(match) )
                    toRemove.Add(link);
            }
            foreach (SOLink link in toRemove)
                RemoveLink(link);
        }


        /// <summary>
        /// Remove all links where the Source or Target is SO
        /// </summary>
        public void RemoveAllLinksToSO(SceneObject so, bool bSources = true, bool bTargets = true)
        {
            List<SOLink> toRemove = new List<SOLink>();
            foreach ( SOLink link in Links ) {
                if ( (bSources && link.Source == so) || (bTargets && link.Target == so) )
                    toRemove.Add(link);
            }
            foreach (SOLink link in toRemove)
                RemoveLink(link);
        }


        /// <summary>
        /// Remove all links
        /// </summary>
        public void RemoveAllLinks()
        {
            foreach (var link in Links)
                link.Unlink();
            foreach (var link in NamedLinks.Values)
                link.Unlink();
            Links.Clear();
            NamedLinks.Clear();
        }


    }
}
