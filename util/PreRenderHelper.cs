using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace f3
{
    //
    // Context calls PreRender() on SO/UIElem/Tools during Update() passes, but
    //   sometimes we need to do a similar thing in other places. This class
    //   is just a pass-through SceneUIElement you can add to the Scene or Cockpit 
    //   that will let you call an arbitrary function on PreRender().
    //
    //   You *must* call Scene/Cockpit RemoveUIElement to disconnect it, otherwise
    //   it will hang around in the scene
    //
    public class PreRenderHelper : SceneUIElement
    {
        public Action PreRenderF = () => { };

        GameObject dummyGO;

        // set this to true if you want this class to destroy the dummy GO on Disconnect().
        // Usually not necessary as RemoveUIElement() call will destroy it
        bool bDestroyOnDisconnect;

        public PreRenderHelper(string sName = "pre_render_helper", bool bDestroyOnDisconnect = false)
        {
            this.bDestroyOnDisconnect = bDestroyOnDisconnect;
            dummyGO = new GameObject(sName);
        }

        public GameObject RootGameObject { get { return dummyGO; } }

        public void Disconnect() {
            if (bDestroyOnDisconnect)
                GameObject.Destroy(dummyGO);
        }

        public bool IsVisible { get { return true; } set { } }
        public void SetLayer(int nLayer) { }

        // called on per-frame Update()
        public void PreRender()
        {
            PreRenderF();
        }

        public bool FindRayIntersection(Ray ray, out UIRayHit hit) { hit = null; return false; }
        public bool FindHoverRayIntersection(Ray ray, out UIRayHit hit) { hit = null; return false; }
        public bool WantsCapture(InputEvent e) { return false; }
        public bool BeginCapture(InputEvent e) { return false; }
        public bool UpdateCapture(InputEvent e) { return false; }
        public bool EndCapture(InputEvent e) { return false; }
        public bool EnableHover { get { return false; } }
        public void UpdateHover(Ray ray, UIRayHit hit) { }
        public void EndHover(Ray ray) { }
    }
}
