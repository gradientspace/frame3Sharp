using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    class GOWrapperSO : BaseSO
    {
        GameObject parentGO;

        // Currently we do not handle the case of child GOs having multiple different
        // materials. The material stack in BaseSO only considers a single SOMaterial for
        // the entire object. If you set this to false, then material change requests (eg
        // by the selection system) will be ignored
        public bool AllowMaterialChanges = true;


        public GOWrapperSO()
        {
        }

        public GOWrapperSO Create(GameObject gameObj)
        {
            fMaterial unityMaterial = gameObj.GetMaterial();
            if (unityMaterial == null) {
                AllowMaterialChanges = false;
            } else {
                SOMaterial setMaterial = new UnitySOMaterial(unityMaterial);
                AssignSOMaterial(setMaterial);
            }

            this.parentGO = gameObj;
            AppendExistingGO(gameObj);

            List<GameObject> children = new List<GameObject>();
            UnityUtil.CollectAllChildren(gameObj, children);
            for (int i = 0; i < children.Count; ++i)
                AppendExistingGO(children[i]);

            return this;
        }



        //
        // SceneObject impl
        //
        override public GameObject RootGameObject
        {
            get { return parentGO; }
        }

        // GOWrapperSO is for wrapping GOs that were created elsewhere, which
        //  we probably are not going to serialize
        //  (To change this behavior, subclass and override)
        override public string UUID
        {
            get { return SceneUtil.InvalidUUID; }
        }

        override public string Name
        {
            get { return parentGO.GetName(); }
            set { parentGO.SetName(value); }
        }

        // [RMS] not sure this is the right thing to do...not actually using this type right now??
        override public SOType Type { get { return SOTypes.Unknown; } }


        override public SceneObject Duplicate()
        {
            GOWrapperSO copy = new GOWrapperSO();
            GameObject duplicateGO = UnityEngine.Object.Instantiate(this.parentGO);
            copy.Create(duplicateGO);
            copy.AssignSOMaterial(GetAssignedSOMaterial());
            return copy;
        }


        override public void AssignSOMaterial(SOMaterial m)
        {
            if ( AllowMaterialChanges )
                base.AssignSOMaterial(m);
        }
        override public SOMaterial GetAssignedSOMaterial()
        {
            return base.GetAssignedSOMaterial();
        }

        override public void PushOverrideMaterial(Material m) {
            if (AllowMaterialChanges)
                base.PushOverrideMaterial(m);
        }
        override public void PopOverrideMaterial() {
            if (AllowMaterialChanges)
                base.PopOverrideMaterial();
        }


    }
}
