using System;
using System.Collections.Generic;
using g3;

namespace f3
{

    //
    // panel is like a 2D window, kind of...
    //
    public class HUDPanel : HUDStandardItem, SceneUIParent, IBoxModelElement
    {
        public HUDChildren Children;

        public float Width {
            get { return width; }
            set { if (width != value) { width = value; FUtil.SafeSendEvent(BoundsModifiedEvent, this); } }
        }
        float width;

        public float Height {
            get { return height; }
            set { if (height != value) { height = value; FUtil.SafeSendEvent(BoundsModifiedEvent, this); } }
        }
        float height;

        public float Padding {
            get { return padding; }
            set { if (padding != value) { padding = value; FUtil.SafeSendEvent(BoundsModifiedEvent, this); } }
        }
        float padding;

        public float PaddedWidth { get { return Width - 2*Padding; } }
        public float PaddedHeight { get { return Height - 2*Padding; } }

        fGameObject parent;
        List<IElementLayout> Layouts;

        public BoundsModifiedEventHandler BoundsModifiedEvent;

        public HUDPanel()
        {
            Children = new HUDChildren(this) {
                OnChildAdded = (elem, bKeepWorldPosition) => {
                    elem.SetLayer(this.Layer);
                    parent.AddChild(elem.RootGameObject, bKeepWorldPosition);
                },
                OnChildRemoved = (elem) => {
                    ;
                }
            };
            width = 1;
            height = 1;
            padding = 0;
        }


        // sets total Width and Height such that content area has specified dimensions)
        public void SetContentSize(float width, float height)
        {
            Width = width + 2 * Padding;
            Height = height + 2 * Padding;
            FUtil.SafeSendEvent(BoundsModifiedEvent, this);
        }
        public Vector2f ContentSize {
            get { return BoxModel.PaddedSize(this, Padding); }
        }
        public AxisAlignedBox2f ContentBounds {
            get { if (parent == null)
                    return new AxisAlignedBox2f(ContentSize.x, ContentSize.y);
                else
                    return BoxModel.PaddedBounds(this, Padding).Box;
            }
        }


        public virtual void Create()
        {
            parent = GameObjectFactory.CreateParentGO(UniqueNames.GetNext("HUDPanel"));
        }



        /*
         * SceneUIParent impl 
         */
        public virtual FContext Context {
            get { return Parent.Context; }
        }


        /*
         *  SceneUIElement impl
         */

        public override fGameObject RootGameObject
        {
            get {
                return (parent == null) ? null : parent;
            }
        }

        public override void Disconnect()
        {
            foreach (var ui in Children)
                ui.Disconnect();
            base.Disconnect();
        }

        override public bool IsVisible {
            get {
                // this is a bit meaningless...
                return RootGameObject.IsVisible();
            }
            set {
                RootGameObject.SetVisible(value);
                foreach (var ui in Children)
                    ui.IsVisible = value;
            }
        }


        override public void SetLayer(int nLayer)
        {
            base.SetLayer(nLayer);
            foreach (var ui in Children)
                ui.SetLayer(nLayer);
        }

        // called on per-frame Update()
        override public void PreRender()
        {
            base.PreRender();
            foreach (var ui in Children)
                ui.PreRender();
        }



        public virtual void AttachLayout(IElementLayout layout)
        {
            if (this.Layouts == null)
                this.Layouts = new List<IElementLayout>();
            this.Layouts.Add(layout);
        }

        public virtual void ValidateLayouts()
        {
            // [TODO]
            //if ( this.Layouts != null ) {
            //    foreach ( var layout in this.Layouts )
            //        layout.RecomputeLayout()
            //}
        }



        override public bool FindRayIntersection(Ray3f ray, out UIRayHit hit)
        {
            return HUDUtil.FindNearestRayIntersection(Children, ray, out hit);
        }

        override public bool WantsCapture(InputEvent e)
        {
            throw new InvalidOperationException("HUDPanel.WantsCapture : how is this being called?");
        }
        override public bool BeginCapture(InputEvent e)
        {
            throw new InvalidOperationException("HUDPanel.BeginCapture: how is this being called?");
        }
        override public bool UpdateCapture(InputEvent e)
        {
            throw new InvalidOperationException("HUDPanel.UpdateCapture: how is this being called?");
        }
        override public bool EndCapture(InputEvent e)
        {
            throw new InvalidOperationException("HUDPanel.EndCapture: how is this being called?");
        }



        public override bool FindHoverRayIntersection(Ray3f ray, out UIRayHit hit)
        {
            return HUDUtil.FindNearestHoverRayIntersection(Children, ray, out hit);
        }


        // [RMS] I don't think we ever actually call these functions, do we?? 
        //   hover is sent directly to child that is ray-hit via above, right?

        public override bool EnableHover
        {
            get {
                foreach (var ui in Children)
                    if (ui.EnableHover)
                        return true;
                return false;
            }
        }

        public override void UpdateHover(Ray3f ray, UIRayHit hit)
        {
            throw new InvalidOperationException("HUDPanel.UpdateHover: how is this being called?");
        }

        public override void EndHover(Ray3f ray)
        {
            throw new InvalidOperationException("HUDPanel.EndHover: how is this being called?");
        }




       #region IBoxModelElement implementation


        public Vector2f Size2D {
            get { return new Vector2f(Width, Height); }
        }

        public AxisAlignedBox2f Bounds2D { 
            get {
                Vector2f origin2 = RootGameObject.GetLocalPosition().xy;
                return new AxisAlignedBox2f(origin2, Width/2, Height/2);
            }
        }

        #endregion

    }




    /// <summary>
    /// This is just a BoxModel wrapper for the Content area of a HUDPanel (ie w/ padding)
    /// </summary>
    public class HUDPanelContentBox : IBoxModelElement
    {
        public HUDPanel Panel;

        public HUDPanelContentBox(HUDPanel panel) {
            this.Panel = panel;
        }

        public Vector2f Size2D
        {
            get { return Panel.ContentSize; }
        }

        public AxisAlignedBox2f Bounds2D
        {
            get { return Panel.ContentBounds; }
        }
    }



    /// <summary>
    /// container provider for content area of HUDPanel
    /// </summary>
    public class HUDPanelContainerProvider : IContainerBoundsProvider, IDisposable
    {
        HUDPanel panel;

        public HUDPanelContainerProvider(HUDPanel panel)
        {
            this.panel = panel;
            panel.BoundsModifiedEvent += on_element_modified;
        }
        public virtual void Dispose()
        {
            panel.BoundsModifiedEvent -= on_element_modified;
        }

        public virtual AxisAlignedBox2f ContainerBounds {
            get { return panel.ContentBounds; }
        }

        virtual protected void on_element_modified(object source)
        {
            PostOnModified();
        }

        public virtual void PostOnModified()
        {
            FUtil.SafeSendAnyEvent(OnContainerBoundsModified, this);
        }

        // how to fire??
        public event BoundsModifiedEventHandler OnContainerBoundsModified;
    }

}
