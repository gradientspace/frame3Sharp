using System;
using System.Collections.Generic;
using UnityEngine;
using g3;

namespace f3
{
	public abstract class HUDStandardItem : GameObjectSet, SceneUIElement
	{
        float _alphaFade = 1.0f;
        SceneUIParent parent;

		public HUDStandardItem ()
		{
            Enabled = true;
        }

		// utility functions

		public Frame3f GetObjectFrame() {
			return UnityUtil.GetGameObjectFrame (RootGameObject, CoordSpace.ObjectCoords);
		}
		public void SetObjectFrame(Frame3f value) {
            UnityUtil.SetGameObjectFrame (RootGameObject, value, CoordSpace.ObjectCoords);
		}

        public virtual string Name
        {
            get { return RootGameObject.GetName(); }
            set { RootGameObject.SetName(value); }
        }

        public virtual bool Enabled { get; set; }

        // used for HUD animated transitions
        public virtual float AlphaFade
        {
            get { return _alphaFade; }
            set { _alphaFade = value; SetAllGOAlphaMultiply(_alphaFade); }
        }


        //
        // abstract impl of SceneUIElement
        //

        public abstract GameObject RootGameObject { get; }

		public virtual SceneUIParent Parent
        {
            get { return parent; }
            set { parent = value; }
        }

		public virtual void Disconnect() {
			// standard is to do nothing
		}

        public virtual bool IsVisible {
            get {
                return RootGameObject.IsVisible();
            }
            set {
                RootGameObject.SetVisible(value);
            }
        }

        public virtual void SetLayer(int nLayer)
        {
            RootGameObject.SetLayer(nLayer);
            foreach (var go in GameObjects)
                UnityUtil.SetLayerRecursive(go, nLayer);
        }

        public virtual void PreRender()
        {
        }

        public virtual bool FindRayIntersection (UnityEngine.Ray ray, out UIRayHit hit)
		{
            hit = null;
            if (Enabled == false)
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
        public virtual bool FindHoverRayIntersection(UnityEngine.Ray ray, out UIRayHit hit)
        {
            hit = null;
            if (Enabled == false)
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
        virtual public void UpdateHover(Ray ray, UIRayHit hit)
        {
        }
        virtual public void EndHover(Ray ray)
        {
        }

        virtual public void AddVisualElements( List<GameObject> objects, bool bKeepPosition )
        {
            foreach (GameObject o in objects) {
                this.AppendNewGO(o, RootGameObject, bKeepPosition);
            }
        }


	}
}

