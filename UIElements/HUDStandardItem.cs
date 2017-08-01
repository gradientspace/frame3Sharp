using System;
using System.Collections.Generic;
using UnityEngine;
using g3;

namespace f3
{
	public abstract class HUDStandardItem : GameObjectSet, IElementFrame, SceneUIElement
	{
        float _alphaFade = 1.0f;
        SceneUIParent parent;
        bool enabled = true;
        bool is_interactive = true;

        /// <summary>
        /// Any Action added to this set will be called every frame, during PreRender().
        /// The main purpose here is to allow UI update code to be attached to the object 
        /// (eg "if some state == Y, set color to X"). This kind of code has to go /somewhere/,
        /// if you use PerFrameActions you can have it as a lambda right at the place
        /// the object is constructed.
        /// 
        /// Of course you can also use this to attach any other code, but
        /// be warned that at some point in the future we may decide not to 
        /// run UpdateActions every frame, for performance reasons.
        /// </summary>
        public ActionSet UpdateActions;


		public HUDStandardItem ()
		{
            Enabled = true;
            UpdateActions = new ActionSet();
        }

		// utility functions

		public virtual Frame3f GetObjectFrame() {
			return UnityUtil.GetGameObjectFrame (RootGameObject, CoordSpace.ObjectCoords);
		}
		public virtual void SetObjectFrame(Frame3f value) {
            UnityUtil.SetGameObjectFrame (RootGameObject, value, CoordSpace.ObjectCoords);
		}

        public virtual string Name
        {
            get { return RootGameObject.GetName(); }
            set { RootGameObject.SetName(value); }
        }

        public virtual bool Enabled {
            get { return enabled; }
            set {
                if (enabled != value) {
                    enabled = value;
                    OnEnabledChanged();
                }
            }
        }
        protected virtual void OnEnabledChanged() { }       // override to respond to enabled-changed


        // used for HUD animated transitions
        public virtual float AlphaFade
        {
            get { return _alphaFade; }
            set { _alphaFade = value; SetAllGOAlphaMultiply(_alphaFade); }
        }


        //
        // abstract impl of SceneUIElement
        //

        public abstract fGameObject RootGameObject { get; }

		public virtual SceneUIParent Parent
        {
            get { return parent; }
            set { parent = value; }
        }


		public virtual void Disconnect() {
            FUtil.SafeSendEvent(OnDisconnected, this, EventArgs.Empty);
		}
        public event EventHandler OnDisconnected;


        public virtual bool IsVisible {
            get {
                return RootGameObject.IsVisible();
            }
            set {
                RootGameObject.SetVisible(value);
            }
        }


        public virtual bool IsInteractive {
            get { return is_interactive; }
            set { is_interactive = value; }
        }

        public virtual void SetLayer(int nLayer)
        {
            RootGameObject.SetLayer(nLayer);
            foreach (var go in GameObjects)
                UnityUtil.SetLayerRecursive(go, nLayer);
        }
        public virtual int Layer
        {
            get { return RootGameObject.GetLayer(); }
        }


        public virtual void PreRender()
        {
            if (UpdateActions != null)
                UpdateActions.Run();
        }

        public virtual bool FindRayIntersection(Ray3f ray, out UIRayHit hit)
		{
            hit = null;
            if (Enabled == false || IsInteractive == false)
                return false;

			GameObjectRayHit hitg = null;
			if (FindGORayIntersection (ray, out hitg)) {
				if (hitg.hitGO != null) {
					hit = new UIRayHit (hitg, this);
					return true;
				}
			}
			return false;
		}

        // override and return true to get hover events
        public virtual bool FindHoverRayIntersection(Ray3f ray, out UIRayHit hit)
        {
            hit = null;
            if (Enabled == false || IsInteractive == false)
                return false;
            if (EnableHover == false)
                return false;

            GameObjectRayHit hitg = null;
            if (FindGORayIntersection(ray, out hitg)) {
                if (hitg.hitGO != null) {
                    hit = new UIRayHit(hitg, this);
                    return true;
                }
            }
            return false;
        }


        virtual public bool WantsCapture(InputEvent e)
        {
            return false;
        }

        virtual public bool BeginCapture(InputEvent e)
        {
            return false;
        }

        virtual public bool UpdateCapture(InputEvent e)
		{
			return true;
		}

		virtual public bool EndCapture(InputEvent e)
		{
			return false;
		}

        virtual public bool EnableHover {
            get { return false; }
        }
        virtual public void UpdateHover(Ray3f ray, UIRayHit hit)
        {
        }
        virtual public void EndHover(Ray3f ray)
        {
        }

        virtual public void AddVisualElements( List<fGameObject> objects, bool bKeepPosition )
        {
            foreach (fGameObject o in objects) {
                this.AppendNewGO(o, RootGameObject, bKeepPosition);
            }
        }


	}
}

